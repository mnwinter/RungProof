"""Load and validate deterministic simulation-scene JSON files."""

from __future__ import annotations

import json
import math
import re
from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import Any

from .components import (
    ComponentError,
    ConveyorPhotoeyeConfig,
    ConveyorPointBinding,
    ConveyorPusherConfig,
    ConveyorPusherPointBinding,
)
from .config import DigitalPointConfig, Direction, InterfaceConfig


class SceneConfigError(ValueError):
    """A scene definition is missing, inconsistent, or unsupported."""


class SceneEventAction(str, Enum):
    """Allowlisted actions supported by the first scene engine."""

    LOAD_OBJECT = "load_object"
    RESET_SCENE = "reset_scene"


@dataclass(frozen=True)
class ConveyorSceneConfig:
    """One configured conveyor/photoeye component and its point binding."""

    component_id: str
    model: ConveyorPhotoeyeConfig
    binding: ConveyorPointBinding
    component_type: str = "conveyor_photoeye"


@dataclass(frozen=True)
class ConveyorPusherSceneConfig:
    """One configured conveyor/pusher component and its point binding."""

    component_id: str
    model: ConveyorPusherConfig
    binding: ConveyorPusherPointBinding
    component_type: str = "conveyor_pusher"


SceneComponentConfig = ConveyorSceneConfig | ConveyorPusherSceneConfig


@dataclass(frozen=True)
class SceneEvent:
    """One deterministic action applied at a logical scene time."""

    at_ms: int
    component_id: str
    action: SceneEventAction


@dataclass(frozen=True)
class SceneConfig:
    """Complete validated scene tied to one interface exchange period."""

    version: int
    physics_step_ms: int
    max_catchup_steps: int
    plc_exchange_ms: int
    components: tuple[SceneComponentConfig, ...]
    events: tuple[SceneEvent, ...]

    @property
    def physics_steps_per_exchange(self) -> int:
        """Return the exact number of fixed physics steps per PLC exchange."""
        return self.plc_exchange_ms // self.physics_step_ms


_NAME_PATTERN = re.compile(r"^[a-z][a-z0-9_]*$")


def _mapping(value: Any, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise SceneConfigError(f"{path} must be a JSON object")
    return value


def _list(value: Any, path: str) -> list[Any]:
    if not isinstance(value, list):
        raise SceneConfigError(f"{path} must be a JSON array")
    return value


def _required(mapping: dict[str, Any], key: str, path: str) -> Any:
    if key not in mapping:
        raise SceneConfigError(f"{path}.{key} is required")
    return mapping[key]


def _reject_unknown(
    mapping: dict[str, Any],
    allowed: set[str],
    path: str,
) -> None:
    unknown = sorted(set(mapping) - allowed)
    if unknown:
        raise SceneConfigError(
            f"{path} has unknown field(s): {', '.join(unknown)}"
        )


def _integer(value: Any, path: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise SceneConfigError(f"{path} must be an integer")
    return value


def _bounded_integer(
    value: Any,
    path: str,
    minimum: int,
    maximum: int,
) -> int:
    parsed = _integer(value, path)
    if not minimum <= parsed <= maximum:
        raise SceneConfigError(
            f"{path} must be between {minimum} and {maximum}"
        )
    return parsed


def _number(value: Any, path: str) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise SceneConfigError(f"{path} must be a finite number")
    parsed = float(value)
    if not math.isfinite(parsed):
        raise SceneConfigError(f"{path} must be a finite number")
    return parsed


def _name(value: Any, path: str) -> str:
    if not isinstance(value, str) or not _NAME_PATTERN.fullmatch(value):
        raise SceneConfigError(
            f"{path} must use lowercase letters, digits, and underscores"
        )
    return value


def _parse_conveyor_parameters(
    parameters: dict[str, Any],
    parameters_path: str,
) -> ConveyorPhotoeyeConfig:
    """Parse the conveyor portion shared by both supported components."""
    try:
        return ConveyorPhotoeyeConfig(
            length_m=_number(
                _required(parameters, "length_m", parameters_path),
                f"{parameters_path}.length_m",
            ),
            speed_m_per_s=_number(
                _required(parameters, "speed_m_per_s", parameters_path),
                f"{parameters_path}.speed_m_per_s",
            ),
            object_length_m=_number(
                _required(parameters, "object_length_m", parameters_path),
                f"{parameters_path}.object_length_m",
            ),
            photoeye_position_m=_number(
                _required(
                    parameters,
                    "photoeye_position_m",
                    parameters_path,
                ),
                f"{parameters_path}.photoeye_position_m",
            ),
            minimum_photoeye_on_s=_number(
                parameters.get("minimum_photoeye_on_s", 0.1),
                f"{parameters_path}.minimum_photoeye_on_s",
            ),
        )
    except ComponentError as exc:
        raise SceneConfigError(f"{parameters_path}: {exc}") from exc


def _validate_binding_point(
    interface: InterfaceConfig,
    point_name: str,
    expected_direction: Direction,
    bindings_path: str,
    role: str,
) -> DigitalPointConfig:
    """Validate one scene binding against the typed interface."""
    try:
        point = interface.point(point_name)
    except KeyError as exc:
        raise SceneConfigError(
            f"{bindings_path}.{role} references unknown interface "
            f"point {point_name!r}"
        ) from exc
    if not isinstance(point, DigitalPointConfig):
        raise SceneConfigError(
            f"{bindings_path}.{role} must reference a digital point"
        )
    direction = interface.tag(point.tag).direction
    if direction is not expected_direction:
        raise SceneConfigError(
            f"{bindings_path}.{role} must reference a "
            f"{expected_direction.value} point"
        )
    return point


def _parse_component(
    value: Any,
    index: int,
    interface: InterfaceConfig,
) -> SceneComponentConfig:
    path = f"components[{index}]"
    raw = _mapping(value, path)
    _reject_unknown(raw, {"id", "type", "parameters", "bindings"}, path)

    component_id = _name(_required(raw, "id", path), f"{path}.id")
    component_type = _required(raw, "type", path)
    if component_type not in ("conveyor_photoeye", "conveyor_pusher"):
        raise SceneConfigError(
            f"{path}.type must be 'conveyor_photoeye' or "
            "'conveyor_pusher'"
        )

    parameters_path = f"{path}.parameters"
    parameters = _mapping(
        _required(raw, "parameters", path),
        parameters_path,
    )
    conveyor_parameter_names = {
        "length_m",
        "speed_m_per_s",
        "object_length_m",
        "photoeye_position_m",
        "minimum_photoeye_on_s",
    }
    parameter_names = set(conveyor_parameter_names)
    if component_type == "conveyor_pusher":
        parameter_names.update(
            {"pusher_stroke_time_s", "transfer_position_fraction"}
        )
    _reject_unknown(parameters, parameter_names, parameters_path)
    conveyor_model = _parse_conveyor_parameters(
        parameters,
        parameters_path,
    )

    bindings_path = f"{path}.bindings"
    bindings = _mapping(
        _required(raw, "bindings", path),
        bindings_path,
    )
    binding_names = {"run_command", "photoeye"}
    if component_type == "conveyor_pusher":
        binding_names.update(
            {
                "extend_command",
                "extended_sensor",
                "retracted_sensor",
            }
        )
    _reject_unknown(bindings, binding_names, bindings_path)
    run_command = _name(
        _required(bindings, "run_command", bindings_path),
        f"{bindings_path}.run_command",
    )
    photoeye = _name(
        _required(bindings, "photoeye", bindings_path),
        f"{bindings_path}.photoeye",
    )
    point_specs = [
        (run_command, Direction.PLC_TO_PC, "run_command"),
        (photoeye, Direction.PC_TO_PLC, "photoeye"),
    ]

    if component_type == "conveyor_photoeye":
        points = [
            _validate_binding_point(
                interface,
                point_name,
                direction,
                bindings_path,
                role,
            )
            for point_name, direction, role in point_specs
        ]
        if len({point.group for point in points}) != 1:
            raise SceneConfigError(
                f"{bindings_path} points must use the same interface group"
            )
        try:
            binding = ConveyorPointBinding(
                run_command_point=run_command,
                photoeye_point=photoeye,
            )
        except ComponentError as exc:
            raise SceneConfigError(f"{bindings_path}: {exc}") from exc
        return ConveyorSceneConfig(
            component_id=component_id,
            model=conveyor_model,
            binding=binding,
        )

    extend_command = _name(
        _required(bindings, "extend_command", bindings_path),
        f"{bindings_path}.extend_command",
    )
    extended_sensor = _name(
        _required(bindings, "extended_sensor", bindings_path),
        f"{bindings_path}.extended_sensor",
    )
    retracted_sensor = _name(
        _required(bindings, "retracted_sensor", bindings_path),
        f"{bindings_path}.retracted_sensor",
    )
    point_specs.extend(
        (
            (extend_command, Direction.PLC_TO_PC, "extend_command"),
            (extended_sensor, Direction.PC_TO_PLC, "extended_sensor"),
            (retracted_sensor, Direction.PC_TO_PLC, "retracted_sensor"),
        )
    )
    points = [
        _validate_binding_point(
            interface,
            point_name,
            direction,
            bindings_path,
            role,
        )
        for point_name, direction, role in point_specs
    ]
    if len({point.group for point in points}) != 1:
        raise SceneConfigError(
            f"{bindings_path} points must use the same interface group"
        )
    try:
        model = ConveyorPusherConfig(
            conveyor=conveyor_model,
            pusher_stroke_time_s=_number(
                _required(
                    parameters,
                    "pusher_stroke_time_s",
                    parameters_path,
                ),
                f"{parameters_path}.pusher_stroke_time_s",
            ),
            transfer_position_fraction=_number(
                parameters.get("transfer_position_fraction", 0.8),
                f"{parameters_path}.transfer_position_fraction",
            ),
        )
        binding = ConveyorPusherPointBinding(
            run_command_point=run_command,
            extend_command_point=extend_command,
            photoeye_point=photoeye,
            extended_sensor_point=extended_sensor,
            retracted_sensor_point=retracted_sensor,
        )
    except ComponentError as exc:
        raise SceneConfigError(f"{path}: {exc}") from exc
    return ConveyorPusherSceneConfig(
        component_id=component_id,
        model=model,
        binding=binding,
    )


def _parse_event(
    value: Any,
    index: int,
    component_ids: set[str],
    physics_step_ms: int,
) -> SceneEvent:
    path = f"events[{index}]"
    raw = _mapping(value, path)
    _reject_unknown(raw, {"at_ms", "component", "action"}, path)
    at_ms = _bounded_integer(
        _required(raw, "at_ms", path),
        f"{path}.at_ms",
        0,
        2_147_483_647,
    )
    if at_ms % physics_step_ms:
        raise SceneConfigError(
            f"{path}.at_ms must align to physics_step_ms "
            f"({physics_step_ms} ms)"
        )
    component_id = _name(
        _required(raw, "component", path),
        f"{path}.component",
    )
    if component_id not in component_ids:
        raise SceneConfigError(
            f"{path}.component references unknown component "
            f"{component_id!r}"
        )
    action_text = _required(raw, "action", path)
    try:
        action = SceneEventAction(action_text)
    except (TypeError, ValueError) as exc:
        supported = ", ".join(action.value for action in SceneEventAction)
        raise SceneConfigError(
            f"{path}.action must be one of: {supported}"
        ) from exc
    return SceneEvent(
        at_ms=at_ms,
        component_id=component_id,
        action=action,
    )


def parse_scene_config(
    value: Any,
    interface: InterfaceConfig,
) -> SceneConfig:
    """Validate a decoded scene against one typed PLC interface."""
    if interface.version != 2 or not interface.points:
        raise SceneConfigError(
            "scene runtime requires interface schema version 2 with points"
        )

    root = _mapping(value, "scene")
    _reject_unknown(
        root,
        {
            "version",
            "physics_step_ms",
            "max_catchup_steps",
            "components",
            "events",
        },
        "scene",
    )
    version = _integer(_required(root, "version", "scene"), "version")
    if version != 1:
        raise SceneConfigError("version must be 1")
    physics_step_ms = _bounded_integer(
        _required(root, "physics_step_ms", "scene"),
        "physics_step_ms",
        1,
        1_000,
    )
    exchange_ms = interface.connection.cycle_ms
    if exchange_ms % physics_step_ms:
        raise SceneConfigError(
            "connection.cycle_ms must be an exact multiple of "
            "physics_step_ms"
        )
    max_catchup_steps = _bounded_integer(
        _required(root, "max_catchup_steps", "scene"),
        "max_catchup_steps",
        1,
        1_000,
    )
    steps_per_exchange = exchange_ms // physics_step_ms
    if steps_per_exchange > max_catchup_steps:
        raise SceneConfigError(
            "max_catchup_steps must be at least the number of physics "
            f"steps per PLC exchange ({steps_per_exchange})"
        )

    raw_components = _list(
        _required(root, "components", "scene"),
        "components",
    )
    if not raw_components:
        raise SceneConfigError(
            "components must contain at least one component"
        )
    components = tuple(
        _parse_component(item, index, interface)
        for index, item in enumerate(raw_components)
    )
    component_ids = [component.component_id for component in components]
    if len(component_ids) != len(set(component_ids)):
        raise SceneConfigError("component ids must be unique")

    pc_point_names = [
        point_name
        for component in components
        for point_name in component.binding.pc_point_names
    ]
    if len(pc_point_names) != len(set(pc_point_names)):
        raise SceneConfigError(
            "each PC-owned scene point may have only one component writer"
        )

    raw_events = _list(_required(root, "events", "scene"), "events")
    events = tuple(
        sorted(
            (
                _parse_event(
                    item,
                    index,
                    set(component_ids),
                    physics_step_ms,
                )
                for index, item in enumerate(raw_events)
            ),
            key=lambda event: event.at_ms,
        )
    )
    return SceneConfig(
        version=version,
        physics_step_ms=physics_step_ms,
        max_catchup_steps=max_catchup_steps,
        plc_exchange_ms=exchange_ms,
        components=components,
        events=events,
    )


def load_scene_config(
    path: str | Path,
    interface: InterfaceConfig,
) -> SceneConfig:
    """Read, decode, and validate one scene file."""
    scene_path = Path(path)
    try:
        text = scene_path.read_text(encoding="utf-8")
    except OSError as exc:
        raise SceneConfigError(
            f"cannot read scene file {scene_path}: {exc}"
        ) from exc
    try:
        decoded = json.loads(text)
    except json.JSONDecodeError as exc:
        raise SceneConfigError(
            f"invalid JSON at line {exc.lineno}, column {exc.colno}: "
            f"{exc.msg}"
        ) from exc
    return parse_scene_config(decoded, interface)
