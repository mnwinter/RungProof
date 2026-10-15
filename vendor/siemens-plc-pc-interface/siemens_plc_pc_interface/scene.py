"""Deterministic first-scene engine and real-time scheduler."""

from __future__ import annotations

import math
import time
from collections import deque
from collections.abc import Callable
from dataclasses import dataclass, replace

from .components import (
    ComponentError,
    ConveyorInputs,
    ConveyorPhotoeye,
    ConveyorPusher,
    ConveyorPusherInputs,
    ConveyorPusherSnapshot,
    ConveyorSnapshot,
)
from .scene_config import (
    ConveyorPusherSceneConfig,
    SceneConfig,
    SceneEventAction,
)
from .update_loop import SimulationUpdateLoop, UpdateResult


class SceneRuntimeError(RuntimeError):
    """A configured scene could not complete a deterministic update."""


@dataclass(frozen=True)
class SceneSnapshot:
    """Observable state after one PLC exchange."""

    scene_time_ms: int
    physics_steps: int
    components: dict[str, ConveyorSnapshot | ConveyorPusherSnapshot]
    update: UpdateResult


@dataclass(frozen=True)
class TimingSnapshot:
    """Bounded timing statistics for the current scene run."""

    cycles: int
    deadline_overruns: int
    schedule_resyncs: int
    average_ms: float
    maximum_ms: float
    p99_ms: float


@dataclass(frozen=True)
class SceneCycleReport:
    """One scene result paired with current runtime timing metrics."""

    snapshot: SceneSnapshot
    cycle_duration_ms: float
    deadline_overrun: bool
    timing: TimingSnapshot


class _TimingTracker:
    """Track recent durations without unbounded memory growth."""

    def __init__(self, window: int = 1_000) -> None:
        self._recent_ms: deque[float] = deque(maxlen=window)
        self._cycles = 0
        self._overruns = 0
        self._resyncs = 0
        self._total_ms = 0.0
        self._maximum_ms = 0.0

    def record(self, duration_ms: float, overrun: bool) -> None:
        self._cycles += 1
        self._overruns += int(overrun)
        self._total_ms += duration_ms
        self._maximum_ms = max(self._maximum_ms, duration_ms)
        self._recent_ms.append(duration_ms)

    def record_resync(self) -> None:
        self._resyncs += 1

    def snapshot(self) -> TimingSnapshot:
        ordered = sorted(self._recent_ms)
        if ordered:
            rank = max(0, math.ceil(0.99 * len(ordered)) - 1)
            p99_ms = ordered[rank]
        else:
            p99_ms = 0.0
        return TimingSnapshot(
            cycles=self._cycles,
            deadline_overruns=self._overruns,
            schedule_resyncs=self._resyncs,
            average_ms=(
                self._total_ms / self._cycles if self._cycles else 0.0
            ),
            maximum_ms=self._maximum_ms,
            p99_ms=p99_ms,
        )


class SceneEngine:
    """
    Run fixed physics steps and one typed PLC update per exchange.

    PLC commands accepted at the end of an exchange are intentionally applied
    to the next exchange. This matches the existing read-first/write-last S7
    cycle and avoids introducing a same-cycle feedback assumption.
    """

    def __init__(
        self,
        config: SceneConfig,
        update_loop: SimulationUpdateLoop,
    ) -> None:
        self.config = config
        self.update_loop = update_loop
        self._models = {}
        self._inputs = {}
        for component in config.components:
            if isinstance(component, ConveyorPusherSceneConfig):
                self._models[component.component_id] = ConveyorPusher(
                    component.model
                )
                self._inputs[component.component_id] = (
                    ConveyorPusherInputs(
                        run_command=False,
                        extend_command=False,
                    )
                )
            else:
                self._models[component.component_id] = ConveyorPhotoeye(
                    component.model
                )
                self._inputs[component.component_id] = ConveyorInputs(
                    run_command=False
                )
        self._scene_time_ms = 0
        self._next_event = 0

    @property
    def scene_time_ms(self) -> int:
        return self._scene_time_ms

    def _events_for_current_step(self) -> set[str]:
        reset_components: set[str] = set()
        while self._next_event < len(self.config.events):
            event = self.config.events[self._next_event]
            if event.at_ms > self._scene_time_ms:
                break
            self._next_event += 1
            model = self._models[event.component_id]
            try:
                if event.action is SceneEventAction.LOAD_OBJECT:
                    model.load_object()
                else:
                    reset_components.add(event.component_id)
            except ComponentError as exc:
                raise SceneRuntimeError(
                    f"event {event.action.value!r} for "
                    f"{event.component_id!r} at {event.at_ms} ms failed: "
                    f"{exc}"
                ) from exc
        return reset_components

    def step(self, now: float) -> SceneSnapshot:
        """Advance one configured PLC exchange using fixed physics steps."""
        snapshots: dict[
            str,
            ConveyorSnapshot | ConveyorPusherSnapshot,
        ] = {}
        dt_s = self.config.physics_step_ms / 1_000

        for _ in range(self.config.physics_steps_per_exchange):
            reset_components = self._events_for_current_step()
            for component in self.config.components:
                component_id = component.component_id
                previous = self._inputs[component_id]
                inputs = replace(
                    previous,
                    reset_scene=component_id in reset_components,
                )
                snapshots[component_id] = self._models[component_id].step(
                    dt_s,
                    inputs,
                )
            self._scene_time_ms += self.config.physics_step_ms

        pc_values: dict[str, bool | int | float] = {}
        for component in self.config.components:
            pc_values.update(
                component.binding.pc_point_values(
                    snapshots[component.component_id]
                )
            )

        update = self.update_loop.step(now, pc_values)
        for component in self.config.components:
            self._inputs[component.component_id] = (
                component.binding.inputs_from_update(update)
            )

        return SceneSnapshot(
            scene_time_ms=self._scene_time_ms,
            physics_steps=self.config.physics_steps_per_exchange,
            components=snapshots,
            update=update,
        )


class SceneRunner:
    """Schedule scene exchanges against a monotonic clock."""

    def __init__(
        self,
        engine: SceneEngine,
        *,
        clock: Callable[[], float] = time.monotonic,
        sleeper: Callable[[float], None] = time.sleep,
    ) -> None:
        self.engine = engine
        self._clock = clock
        self._sleeper = sleeper
        self._timing = _TimingTracker()

    @property
    def timing(self) -> TimingSnapshot:
        return self._timing.snapshot()

    def run(
        self,
        *,
        cycles: int | None,
        on_cycle: Callable[[SceneCycleReport], None] | None = None,
        should_stop: Callable[[], bool] | None = None,
    ) -> TimingSnapshot:
        """
        Run until the cycle limit or interruption.

        Sustained lateness never creates large physics jumps or bursts of PLC
        traffic. The deadline is resynchronized only after lag exceeds the
        configured bounded catch-up window.
        """
        period_s = self.engine.config.plc_exchange_ms / 1_000
        lag_limit_s = (
            self.engine.config.max_catchup_steps
            * self.engine.config.physics_step_ms
            / 1_000
        )
        deadline = self._clock()
        completed = 0
        while cycles is None or completed < cycles:
            if should_stop is not None and should_stop():
                break

            started = self._clock()
            snapshot = self.engine.step(started)
            finished = self._clock()
            duration_ms = (finished - started) * 1_000

            deadline += period_s
            overrun = finished > deadline
            self._timing.record(duration_ms, overrun)
            report = SceneCycleReport(
                snapshot=snapshot,
                cycle_duration_ms=duration_ms,
                deadline_overrun=overrun,
                timing=self._timing.snapshot(),
            )
            if on_cycle is not None:
                on_cycle(report)
            completed += 1
            if cycles is not None and completed >= cycles:
                break
            if should_stop is not None and should_stop():
                break

            current = self._clock()
            delay = deadline - current
            if delay > 0:
                self._sleeper(delay)
            elif -delay > lag_limit_s:
                deadline = current
                self._timing.record_resync()

        return self._timing.snapshot()
