"""Typed simulation update loop with point diagnostics and JSON-lines logs."""

from __future__ import annotations

import json
from collections.abc import Mapping
from dataclasses import dataclass
from enum import Enum
from typing import Protocol, TextIO

from .config import ConfigError, Direction
from .points import PointQuality, PointSample
from .runtime import CycleResult, InterfaceRuntime


SceneValue = bool | int | float


class LoopHealth(str, Enum):
    """Overall health of one scene-to-PLC update."""

    STARTING = "starting"
    HEALTHY = "healthy"
    DEGRADED = "degraded"
    FAULT = "fault"


class DiagnosticLevel(str, Enum):
    """Severity assigned to a point conversion diagnostic."""

    WARNING = "warning"
    ERROR = "error"


@dataclass(frozen=True)
class PointDiagnostic:
    """One actionable point-quality message."""

    point_name: str
    direction: Direction
    quality: PointQuality
    level: DiagnosticLevel
    message: str


@dataclass(frozen=True)
class UpdateResult:
    """Complete scene-facing result of one deterministic PLC cycle."""

    cycle: CycleResult
    health: LoopHealth
    pc_point_samples: dict[str, PointSample]
    plc_point_samples: dict[str, PointSample]
    diagnostics: tuple[PointDiagnostic, ...]


class UpdateLogger(Protocol):
    """Sink for completed simulation update results."""

    def emit(self, result: UpdateResult) -> None:
        """Record one completed update."""


def _sample_record(sample: PointSample) -> dict[str, object]:
    return {
        "value": sample.value,
        "raw_value": sample.raw_value,
        "quality": sample.quality.value,
        "unit": sample.unit,
        "tag": sample.tag_name,
    }


def update_log_record(result: UpdateResult) -> dict[str, object]:
    """Convert an update result to a stable JSON-compatible record."""
    heartbeat = result.cycle.heartbeat
    return {
        "cycle": result.cycle.cycle_number,
        "health": result.health.value,
        "heartbeat": {
            "healthy": heartbeat.healthy,
            "reason": heartbeat.reason,
            "last_echo": heartbeat.last_echo,
            "age_ms": heartbeat.age_ms,
        },
        "pc_points": {
            name: _sample_record(sample)
            for name, sample in result.pc_point_samples.items()
        },
        "plc_points": {
            name: _sample_record(sample)
            for name, sample in result.plc_point_samples.items()
        },
        "diagnostics": [
            {
                "point": diagnostic.point_name,
                "direction": diagnostic.direction.value,
                "quality": diagnostic.quality.value,
                "level": diagnostic.level.value,
                "message": diagnostic.message,
            }
            for diagnostic in result.diagnostics
        ],
    }


class JsonLinesUpdateLogger:
    """Write one compact JSON object per completed simulation update."""

    def __init__(self, stream: TextIO, *, flush: bool = True) -> None:
        self._stream = stream
        self._flush = flush

    def emit(self, result: UpdateResult) -> None:
        json.dump(
            update_log_record(result),
            self._stream,
            allow_nan=False,
            separators=(",", ":"),
            sort_keys=True,
        )
        self._stream.write("\n")
        if self._flush:
            self._stream.flush()


def _diagnostic_for(
    sample: PointSample,
    direction: Direction,
) -> PointDiagnostic | None:
    quality = sample.quality
    if quality is PointQuality.GOOD:
        return None

    if quality is PointQuality.CLAMPED_LOW:
        message = "value clamped to the configured low limit"
        level = DiagnosticLevel.WARNING
    elif quality is PointQuality.CLAMPED_HIGH:
        message = "value clamped to the configured high limit"
        level = DiagnosticLevel.WARNING
    elif quality is PointQuality.FAULT_LOW:
        message = "value is below the configured low limit"
        level = DiagnosticLevel.ERROR
    else:
        message = "value is above the configured high limit"
        level = DiagnosticLevel.ERROR

    if (
        level is DiagnosticLevel.ERROR
        and direction is Direction.PC_TO_PLC
    ):
        message += "; PLC write skipped"

    return PointDiagnostic(
        point_name=sample.point_name,
        direction=direction,
        quality=quality,
        level=level,
        message=message,
    )


def _loop_health(
    cycle: CycleResult,
    diagnostics: tuple[PointDiagnostic, ...],
) -> LoopHealth:
    if (
        not cycle.heartbeat.healthy
        and cycle.heartbeat.reason != "waiting_for_matching_echo"
    ):
        return LoopHealth.FAULT
    if any(
        diagnostic.level is DiagnosticLevel.ERROR
        for diagnostic in diagnostics
    ):
        return LoopHealth.FAULT
    if any(
        diagnostic.level is DiagnosticLevel.WARNING
        for diagnostic in diagnostics
    ):
        return LoopHealth.DEGRADED
    if cycle.heartbeat.reason == "waiting_for_matching_echo":
        return LoopHealth.STARTING
    return LoopHealth.HEALTHY


class SimulationUpdateLoop:
    """
    Apply scene inputs, run one guarded PLC cycle, and publish typed outputs.

    PC point values are stateful. Omitting a point from a later call preserves
    its last accepted value. A point using the ``fault`` range policy does not
    replace the last accepted value and does not stage a PLC write.
    """

    def __init__(
        self,
        runtime: InterfaceRuntime,
        *,
        logger: UpdateLogger | None = None,
    ) -> None:
        if not runtime.config.points:
            raise ConfigError(
                "simulation update loop requires schema version 2 "
                "with configured points"
            )

        self.runtime = runtime
        self.logger = logger
        self._point_model = runtime.point_model
        self._pc_point_names = tuple(
            point.name
            for point in runtime.config.points
            if self._point_model.direction(point.name)
            is Direction.PC_TO_PLC
        )
        self._plc_point_names = tuple(
            point.name
            for point in runtime.config.points
            if self._point_model.direction(point.name)
            is Direction.PLC_TO_PC
        )
        self._accepted_pc_samples = {
            name: sample
            for name in self._pc_point_names
            if (sample := self._point_model.safe_sample(name)) is not None
        }

    def step(
        self,
        now: float,
        pc_point_values: Mapping[str, SceneValue],
    ) -> UpdateResult:
        """
        Run one scene-to-PLC update.

        Unknown, PLC-owned, or type-invalid scene points raise before the PLC
        cycle. Configured range faults are reported without staging that point
        write, allowing the communication heartbeat to continue.
        """
        current_pc_samples = dict(self._accepted_pc_samples)
        attempted_samples = {
            name: self._point_model.encode_pc_value(name, value)
            for name, value in pc_point_values.items()
        }
        current_pc_samples.update(attempted_samples)
        for name, sample in attempted_samples.items():
            if sample.conversion_succeeded:
                assert sample.raw_value is not None
                self.runtime.set_pc_value(sample.tag_name, sample.raw_value)
                self._accepted_pc_samples[name] = sample

        cycle = self.runtime.cycle(now)
        plc_samples = {
            name: self._point_model.decode(
                name,
                cycle.plc_values_read[
                    self.runtime.config.point(name).tag
                ],
            )
            for name in self._plc_point_names
        }

        diagnostics = tuple(
            diagnostic
            for name, sample in (
                *current_pc_samples.items(),
                *plc_samples.items(),
            )
            if (
                diagnostic := _diagnostic_for(
                    sample,
                    self._point_model.direction(name),
                )
            )
            is not None
        )
        result = UpdateResult(
            cycle=cycle,
            health=_loop_health(cycle, diagnostics),
            pc_point_samples=current_pc_samples,
            plc_point_samples=plc_samples,
            diagnostics=diagnostics,
        )
        if self.logger is not None:
            self.logger.emit(result)
        return result
