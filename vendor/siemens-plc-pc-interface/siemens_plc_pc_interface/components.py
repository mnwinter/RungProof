"""Reusable offline equipment models for a future simulation scene."""

from __future__ import annotations

import math
from dataclasses import dataclass
from enum import Enum

from .points import PointQuality
from .update_loop import LoopHealth, SceneValue, UpdateResult


class ComponentError(ValueError):
    """A component definition or update is invalid."""


class ConveyorState(str, Enum):
    """Explicit operating state of the conveyor/photoeye component."""

    RESET = "reset"
    STOPPED_EMPTY = "stopped_empty"
    RUNNING_EMPTY = "running_empty"
    STOPPED_LOADED = "stopped_loaded"
    RUNNING_LOADED = "running_loaded"


class ConveyorPusherState(str, Enum):
    """Explicit operating state of the conveyor/pusher component."""

    RESET = "reset"
    STOPPED_EMPTY = "stopped_empty"
    RUNNING_EMPTY = "running_empty"
    STOPPED_LOADED = "stopped_loaded"
    RUNNING_LOADED = "running_loaded"
    PUSHING = "pushing"
    EXTENDED = "extended"
    RETRACTING = "retracting"


def _finite_number(name: str, value: float) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ComponentError(f"{name} must be a finite number")
    converted = float(value)
    if not math.isfinite(converted):
        raise ComponentError(f"{name} must be a finite number")
    return converted


@dataclass(frozen=True)
class ConveyorPhotoeyeConfig:
    """Physical parameters for one single-object conveyor model."""

    length_m: float
    speed_m_per_s: float
    object_length_m: float
    photoeye_position_m: float
    minimum_photoeye_on_s: float = 0.1

    def __post_init__(self) -> None:
        length = _finite_number("length_m", self.length_m)
        speed = _finite_number("speed_m_per_s", self.speed_m_per_s)
        object_length = _finite_number(
            "object_length_m",
            self.object_length_m,
        )
        photoeye = _finite_number(
            "photoeye_position_m",
            self.photoeye_position_m,
        )
        minimum_on = _finite_number(
            "minimum_photoeye_on_s",
            self.minimum_photoeye_on_s,
        )

        if length <= 0:
            raise ComponentError("length_m must be greater than zero")
        if speed <= 0:
            raise ComponentError("speed_m_per_s must be greater than zero")
        if object_length <= 0:
            raise ComponentError(
                "object_length_m must be greater than zero"
            )
        if object_length > length:
            raise ComponentError(
                "object_length_m must not exceed conveyor length_m"
            )
        if not 0 <= photoeye <= length:
            raise ComponentError(
                "photoeye_position_m must be within the conveyor"
            )
        if minimum_on < 0:
            raise ComponentError(
                "minimum_photoeye_on_s must be non-negative"
            )


@dataclass(frozen=True)
class ConveyorInputs:
    """Commands applied during one component update."""

    run_command: bool
    reset_scene: bool = False

    def __post_init__(self) -> None:
        if not isinstance(self.run_command, bool):
            raise ComponentError("run_command must be true or false")
        if not isinstance(self.reset_scene, bool):
            raise ComponentError("reset_scene must be true or false")


@dataclass(frozen=True)
class ConveyorSnapshot:
    """Observable component state after one update."""

    state: ConveyorState
    motor_running: bool
    object_present: bool
    object_leading_edge_m: float | None
    photoeye_blocked: bool
    object_discharged: bool
    completed_count: int


class ConveyorPhotoeye:
    """
    Deterministic one-dimensional conveyor with one simulated photoeye.

    One object can be on the conveyor at a time. Position is the object's
    leading edge measured from the infeed. The object occupies the interval
    from ``leading_edge - object_length`` through ``leading_edge``.
    """

    def __init__(self, config: ConveyorPhotoeyeConfig) -> None:
        self.config = config
        self._object_leading_edge_m: float | None = None
        self._photoeye_hold_remaining_s = 0.0
        self._photoeye_blocked = False
        self._completed_count = 0
        self._last_inputs = ConveyorInputs(run_command=False)
        self._object_discharged = False

    @property
    def snapshot(self) -> ConveyorSnapshot:
        """Return the current state without advancing simulation time."""
        reset = self._last_inputs.reset_scene
        motor_running = self._last_inputs.run_command and not reset
        object_present = self._object_leading_edge_m is not None

        if reset:
            state = ConveyorState.RESET
        elif motor_running and object_present:
            state = ConveyorState.RUNNING_LOADED
        elif motor_running:
            state = ConveyorState.RUNNING_EMPTY
        elif object_present:
            state = ConveyorState.STOPPED_LOADED
        else:
            state = ConveyorState.STOPPED_EMPTY

        return ConveyorSnapshot(
            state=state,
            motor_running=motor_running,
            object_present=object_present,
            object_leading_edge_m=self._object_leading_edge_m,
            photoeye_blocked=self._photoeye_blocked,
            object_discharged=self._object_discharged,
            completed_count=self._completed_count,
        )

    def load_object(self) -> ConveyorSnapshot:
        """Place one object at the infeed, rejecting product overlap."""
        if self._object_leading_edge_m is not None:
            raise ComponentError(
                "cannot load an object while another object is present"
            )
        self._object_leading_edge_m = 0.0
        self._object_discharged = False
        self._refresh_photoeye(dt_s=0.0, crossed=False)
        return self.snapshot

    def transfer_object(self) -> ConveyorSnapshot:
        """Remove the current object through a simulated transfer action."""
        if self._object_leading_edge_m is None:
            raise ComponentError("cannot transfer an object when none is present")
        self._object_leading_edge_m = None
        self._photoeye_hold_remaining_s = 0.0
        self._photoeye_blocked = False
        self._object_discharged = True
        self._completed_count += 1
        return self.snapshot

    def _is_photoeye_physically_blocked(self) -> bool:
        leading_edge = self._object_leading_edge_m
        if leading_edge is None:
            return False
        trailing_edge = leading_edge - self.config.object_length_m
        return (
            trailing_edge
            <= self.config.photoeye_position_m
            <= leading_edge
        )

    def _object_swept_across_photoeye(
        self,
        previous_leading_edge: float,
        current_leading_edge: float,
    ) -> bool:
        previous_trailing_edge = (
            previous_leading_edge - self.config.object_length_m
        )
        return (
            current_leading_edge >= self.config.photoeye_position_m
            and previous_trailing_edge
            <= self.config.photoeye_position_m
        )

    def _refresh_photoeye(self, dt_s: float, crossed: bool) -> None:
        physically_blocked = self._is_photoeye_physically_blocked()
        if physically_blocked or crossed:
            self._photoeye_hold_remaining_s = max(
                self._photoeye_hold_remaining_s,
                self.config.minimum_photoeye_on_s,
            )
        else:
            self._photoeye_hold_remaining_s = max(
                0.0,
                self._photoeye_hold_remaining_s - dt_s,
            )
        self._photoeye_blocked = (
            physically_blocked
            or self._photoeye_hold_remaining_s > 0
        )

    def step(
        self,
        dt_s: float,
        inputs: ConveyorInputs,
    ) -> ConveyorSnapshot:
        """Advance the component by one non-negative time interval."""
        dt = _finite_number("dt_s", dt_s)
        if dt < 0:
            raise ComponentError("dt_s must be non-negative")
        if not isinstance(inputs, ConveyorInputs):
            raise ComponentError("inputs must be ConveyorInputs")

        self._last_inputs = inputs
        self._object_discharged = False
        if inputs.reset_scene:
            self._object_leading_edge_m = None
            self._photoeye_hold_remaining_s = 0.0
            self._photoeye_blocked = False
            self._completed_count = 0
            return self.snapshot

        crossed = False
        if (
            inputs.run_command
            and self._object_leading_edge_m is not None
            and dt > 0
        ):
            previous = self._object_leading_edge_m
            current = previous + self.config.speed_m_per_s * dt
            crossed = self._object_swept_across_photoeye(
                previous,
                current,
            )
            trailing_edge = current - self.config.object_length_m
            if trailing_edge >= self.config.length_m:
                self._object_leading_edge_m = None
                self._object_discharged = True
                self._completed_count += 1
            else:
                self._object_leading_edge_m = current

        self._refresh_photoeye(dt_s=dt, crossed=crossed)
        return self.snapshot


@dataclass(frozen=True)
class ConveyorPointBinding:
    """Map one conveyor command and photoeye to typed update-loop points."""

    run_command_point: str
    photoeye_point: str

    def __post_init__(self) -> None:
        for name, value in (
            ("run_command_point", self.run_command_point),
            ("photoeye_point", self.photoeye_point),
        ):
            if not isinstance(value, str) or not value.strip():
                raise ComponentError(f"{name} must be a non-empty point name")
        if self.run_command_point == self.photoeye_point:
            raise ComponentError(
                "run command and photoeye must use different points"
            )

    def inputs_from_update(
        self,
        result: UpdateResult,
        *,
        reset_scene: bool = False,
    ) -> ConveyorInputs:
        """
        Convert PLC point feedback into fail-safe component inputs.

        Starting/fault loop health or non-good command quality forces the run
        command false. Degraded health remains usable because it represents a
        clamped point while communication is still progressing.
        """
        try:
            sample = result.plc_point_samples[self.run_command_point]
        except KeyError as exc:
            raise ComponentError(
                f"PLC result is missing run command point "
                f"{self.run_command_point!r}"
            ) from exc

        if sample.quality is not PointQuality.GOOD:
            run_command = False
        elif not isinstance(sample.value, bool):
            raise ComponentError(
                f"run command point {self.run_command_point!r} "
                "must produce a Boolean value"
            )
        else:
            run_command = (
                sample.value
                and result.health
                in (LoopHealth.HEALTHY, LoopHealth.DEGRADED)
            )
        return ConveyorInputs(
            run_command=run_command,
            reset_scene=reset_scene,
        )

    def pc_point_values(
        self,
        snapshot: ConveyorSnapshot,
    ) -> dict[str, SceneValue]:
        """Return the simulated photoeye value for the next PLC update."""
        if not isinstance(snapshot, ConveyorSnapshot):
            raise ComponentError("snapshot must be ConveyorSnapshot")
        return {self.photoeye_point: snapshot.photoeye_blocked}

    @property
    def pc_point_names(self) -> tuple[str, ...]:
        """Return the PC-owned points written by this binding."""
        return (self.photoeye_point,)


@dataclass(frozen=True)
class ConveyorPusherConfig:
    """Physical parameters for one conveyor and single-solenoid pusher."""

    conveyor: ConveyorPhotoeyeConfig
    pusher_stroke_time_s: float
    transfer_position_fraction: float = 0.8

    def __post_init__(self) -> None:
        if not isinstance(self.conveyor, ConveyorPhotoeyeConfig):
            raise ComponentError(
                "conveyor must be ConveyorPhotoeyeConfig"
            )
        stroke_time = _finite_number(
            "pusher_stroke_time_s",
            self.pusher_stroke_time_s,
        )
        transfer_position = _finite_number(
            "transfer_position_fraction",
            self.transfer_position_fraction,
        )
        if stroke_time <= 0:
            raise ComponentError(
                "pusher_stroke_time_s must be greater than zero"
            )
        if not 0 < transfer_position <= 1:
            raise ComponentError(
                "transfer_position_fraction must be greater than zero "
                "and no greater than one"
            )


@dataclass(frozen=True)
class ConveyorPusherInputs:
    """PLC commands applied during one conveyor/pusher update."""

    run_command: bool
    extend_command: bool
    reset_scene: bool = False

    def __post_init__(self) -> None:
        for name, value in (
            ("run_command", self.run_command),
            ("extend_command", self.extend_command),
            ("reset_scene", self.reset_scene),
        ):
            if not isinstance(value, bool):
                raise ComponentError(f"{name} must be true or false")


@dataclass(frozen=True)
class ConveyorPusherSnapshot:
    """Observable conveyor, pusher, and sensor state after one update."""

    state: ConveyorPusherState
    motor_running: bool
    object_present: bool
    object_leading_edge_m: float | None
    photoeye_blocked: bool
    object_transferred: bool
    completed_count: int
    pusher_position: float
    pusher_extended: bool
    pusher_retracted: bool
    pusher_extending: bool


class ConveyorPusher:
    """Deterministic conveyor plus one spring-return simulated pusher."""

    _POSITION_EPSILON = 1e-9

    def __init__(self, config: ConveyorPusherConfig) -> None:
        self.config = config
        self._conveyor = ConveyorPhotoeye(config.conveyor)
        self._pusher_position = 0.0
        self._object_transferred = False
        self._last_inputs = ConveyorPusherInputs(
            run_command=False,
            extend_command=False,
        )

    @property
    def snapshot(self) -> ConveyorPusherSnapshot:
        """Return current state without advancing simulation time."""
        conveyor = self._conveyor.snapshot
        reset = self._last_inputs.reset_scene
        extended = (
            self._pusher_position >= 1.0 - self._POSITION_EPSILON
        )
        retracted = self._pusher_position <= self._POSITION_EPSILON

        if reset:
            state = ConveyorPusherState.RESET
        elif self._last_inputs.extend_command and not extended:
            state = ConveyorPusherState.PUSHING
        elif self._last_inputs.extend_command:
            state = ConveyorPusherState.EXTENDED
        elif not retracted:
            state = ConveyorPusherState.RETRACTING
        else:
            state = ConveyorPusherState(conveyor.state.value)

        return ConveyorPusherSnapshot(
            state=state,
            motor_running=conveyor.motor_running,
            object_present=conveyor.object_present,
            object_leading_edge_m=conveyor.object_leading_edge_m,
            photoeye_blocked=conveyor.photoeye_blocked,
            object_transferred=self._object_transferred,
            completed_count=conveyor.completed_count,
            pusher_position=self._pusher_position,
            pusher_extended=extended,
            pusher_retracted=retracted,
            pusher_extending=(
                self._last_inputs.extend_command and not extended
            ),
        )

    def load_object(self) -> ConveyorPusherSnapshot:
        """Place one object at the conveyor infeed."""
        self._conveyor.load_object()
        self._object_transferred = False
        return self.snapshot

    def step(
        self,
        dt_s: float,
        inputs: ConveyorPusherInputs,
    ) -> ConveyorPusherSnapshot:
        """Advance conveyor motion, pusher motion, and simulated sensors."""
        dt = _finite_number("dt_s", dt_s)
        if dt < 0:
            raise ComponentError("dt_s must be non-negative")
        if not isinstance(inputs, ConveyorPusherInputs):
            raise ComponentError(
                "inputs must be ConveyorPusherInputs"
            )

        self._last_inputs = inputs
        self._object_transferred = False
        if inputs.reset_scene:
            self._pusher_position = 0.0
            self._conveyor.step(
                dt,
                ConveyorInputs(run_command=False, reset_scene=True),
            )
            return self.snapshot

        previous_position = self._pusher_position
        position_delta = dt / self.config.pusher_stroke_time_s
        if inputs.extend_command:
            self._pusher_position = min(
                1.0,
                self._pusher_position + position_delta,
            )
        else:
            self._pusher_position = max(
                0.0,
                self._pusher_position - position_delta,
            )

        conveyor = self._conveyor.step(
            dt,
            ConveyorInputs(run_command=inputs.run_command),
        )
        crossed_transfer_position = (
            inputs.extend_command
            and previous_position
            < self.config.transfer_position_fraction
            <= self._pusher_position
        )
        if (
            crossed_transfer_position
            and conveyor.object_present
            and conveyor.photoeye_blocked
        ):
            self._conveyor.transfer_object()
            self._object_transferred = True

        return self.snapshot


def _boolean_command(
    result: UpdateResult,
    point_name: str,
    role: str,
) -> bool:
    """Return one PLC Boolean command, forcing false on unhealthy data."""
    try:
        sample = result.plc_point_samples[point_name]
    except KeyError as exc:
        raise ComponentError(
            f"PLC result is missing {role} point {point_name!r}"
        ) from exc
    if sample.quality is not PointQuality.GOOD:
        return False
    if not isinstance(sample.value, bool):
        raise ComponentError(
            f"{role} point {point_name!r} must produce a Boolean value"
        )
    return (
        sample.value
        and result.health in (LoopHealth.HEALTHY, LoopHealth.DEGRADED)
    )


@dataclass(frozen=True)
class ConveyorPusherPointBinding:
    """Map pusher commands and three simulated sensors to typed points."""

    run_command_point: str
    extend_command_point: str
    photoeye_point: str
    extended_sensor_point: str
    retracted_sensor_point: str

    def __post_init__(self) -> None:
        values = (
            self.run_command_point,
            self.extend_command_point,
            self.photoeye_point,
            self.extended_sensor_point,
            self.retracted_sensor_point,
        )
        if any(
            not isinstance(value, str) or not value.strip()
            for value in values
        ):
            raise ComponentError(
                "all conveyor pusher point names must be non-empty"
            )
        if len(values) != len(set(values)):
            raise ComponentError(
                "conveyor pusher bindings must use distinct points"
            )

    def inputs_from_update(
        self,
        result: UpdateResult,
        *,
        reset_scene: bool = False,
    ) -> ConveyorPusherInputs:
        """Convert PLC samples into fail-safe conveyor/pusher commands."""
        return ConveyorPusherInputs(
            run_command=_boolean_command(
                result,
                self.run_command_point,
                "run command",
            ),
            extend_command=_boolean_command(
                result,
                self.extend_command_point,
                "extend command",
            ),
            reset_scene=reset_scene,
        )

    def pc_point_values(
        self,
        snapshot: ConveyorPusherSnapshot,
    ) -> dict[str, SceneValue]:
        """Return the three PC-owned feedback points."""
        if not isinstance(snapshot, ConveyorPusherSnapshot):
            raise ComponentError(
                "snapshot must be ConveyorPusherSnapshot"
            )
        return {
            self.photoeye_point: snapshot.photoeye_blocked,
            self.extended_sensor_point: snapshot.pusher_extended,
            self.retracted_sensor_point: snapshot.pusher_retracted,
        }

    @property
    def pc_point_names(self) -> tuple[str, ...]:
        """Return the PC-owned points written by this binding."""
        return (
            self.photoeye_point,
            self.extended_sensor_point,
            self.retracted_sensor_point,
        )
