"""Load and validate the PLC interface JSON configuration."""

from __future__ import annotations

import json
import math
import re
from dataclasses import dataclass
from enum import Enum
from ipaddress import IPv4Address
from pathlib import Path
from typing import Any


class ConfigError(ValueError):
    """A configuration value is missing, inconsistent, or unsafe."""


class Direction(str, Enum):
    """The only component allowed to write a configured PLC tag."""

    PC_TO_PLC = "pc_to_plc"
    PLC_TO_PC = "plc_to_pc"


class DataType(str, Enum):
    """PLC data types supported by the first configuration format."""

    BOOL = "BOOL"
    BYTE = "BYTE"
    WORD = "WORD"
    DWORD = "DWORD"
    INT = "INT"
    DINT = "DINT"
    REAL = "REAL"


class PointKind(str, Enum):
    """Scene-facing point behavior."""

    DIGITAL = "digital"
    ANALOG = "analog"


class OutOfRangePolicy(str, Enum):
    """How an analog point handles values outside its configured range."""

    CLAMP = "clamp"
    FAULT = "fault"


@dataclass(frozen=True)
class TagAddress:
    """Validated DB memory interval used for grouping and diagnostics."""

    db_number: int
    start_bit: int
    end_bit: int

    @property
    def start_byte(self) -> int:
        return self.start_bit // 8

    @property
    def end_byte(self) -> int:
        """Return the exclusive ending byte."""
        return (self.end_bit + 7) // 8


@dataclass(frozen=True)
class ConnectionConfig:
    """S7 endpoint and timing settings."""

    cpu_family: str
    ip: str
    rack: int
    slot: int
    cycle_ms: int
    connect_timeout_ms: int


@dataclass(frozen=True)
class TagConfig:
    """One typed tag in the dedicated PLC simulation data block."""

    name: str
    plc_symbol: str
    address: str
    data_type: DataType
    direction: Direction
    safe_value: bool | int | float | None
    memory: TagAddress

    @property
    def snap7_tag(self) -> str:
        """Return the tag string consumed by python-snap7."""
        return f"{self.address}:{self.data_type.value}"


@dataclass(frozen=True)
class HeartbeatConfig:
    """Names of the PC heartbeat and PLC echo tags."""

    pc_tag: str
    echo_tag: str
    timeout_ms: int


@dataclass(frozen=True)
class DigitalPointConfig:
    """One Boolean scene point backed by a configured BOOL tag."""

    name: str
    tag: str
    group: str
    inverted: bool
    kind: PointKind = PointKind.DIGITAL


@dataclass(frozen=True)
class AnalogPointConfig:
    """One scaled scene point backed by a configured numeric tag."""

    name: str
    tag: str
    group: str
    raw_min: int | float
    raw_max: int | float
    engineering_min: float
    engineering_max: float
    unit: str
    out_of_range: OutOfRangePolicy
    kind: PointKind = PointKind.ANALOG


PointConfig = DigitalPointConfig | AnalogPointConfig


@dataclass(frozen=True)
class InterfaceConfig:
    """Complete, validated interface configuration."""

    version: int
    connection: ConnectionConfig
    heartbeat: HeartbeatConfig
    tags: tuple[TagConfig, ...]
    points: tuple[PointConfig, ...]

    def tag(self, name: str) -> TagConfig:
        """Return a tag by configured name."""
        for tag in self.tags:
            if tag.name == name:
                return tag
        raise KeyError(name)

    def point(self, name: str) -> PointConfig:
        """Return a scene point by configured name."""
        for point in self.points:
            if point.name == name:
                return point
        raise KeyError(name)


_NAME_PATTERN = re.compile(r"^[a-z][a-z0-9_]*$")
_ADDRESS_PATTERN = re.compile(
    r"^DB(?P<db>\d+)\."
    r"(?P<area>DBX|DBB|DBW|DBD)"
    r"(?P<byte>\d+)"
    r"(?:\.(?P<bit>[0-7]))?$"
)

_SUPPORTED_CPU_FAMILIES = {"s7-1200", "s7-1500"}
_AREA_TYPES: dict[str, set[DataType]] = {
    "DBX": {DataType.BOOL},
    "DBB": {DataType.BYTE},
    "DBW": {DataType.WORD, DataType.INT},
    "DBD": {DataType.DWORD, DataType.DINT, DataType.REAL},
}
_TYPE_BITS: dict[DataType, int] = {
    DataType.BOOL: 1,
    DataType.BYTE: 8,
    DataType.WORD: 16,
    DataType.DWORD: 32,
    DataType.INT: 16,
    DataType.DINT: 32,
    DataType.REAL: 32,
}


def _mapping(value: Any, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ConfigError(f"{path} must be a JSON object")
    return value


def _list(value: Any, path: str) -> list[Any]:
    if not isinstance(value, list):
        raise ConfigError(f"{path} must be a JSON array")
    return value


def _string(value: Any, path: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise ConfigError(f"{path} must be a non-empty string")
    return value.strip()


def _integer(value: Any, path: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise ConfigError(f"{path} must be an integer")
    return value


def _number(value: Any, path: str) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ConfigError(f"{path} must be numeric")
    try:
        parsed = float(value)
    except OverflowError as exc:
        raise ConfigError(f"{path} must be finite") from exc
    if not math.isfinite(parsed):
        raise ConfigError(f"{path} must be finite")
    return parsed


def _boolean(value: Any, path: str) -> bool:
    if not isinstance(value, bool):
        raise ConfigError(f"{path} must be true or false")
    return value


def _required(mapping: dict[str, Any], key: str, path: str) -> Any:
    if key not in mapping:
        raise ConfigError(f"{path}.{key} is required")
    return mapping[key]


def _reject_unknown(
    mapping: dict[str, Any],
    allowed: set[str],
    path: str,
) -> None:
    unknown = sorted(set(mapping) - allowed)
    if unknown:
        joined = ", ".join(unknown)
        raise ConfigError(f"{path} has unknown field(s): {joined}")


def _bounded_integer(
    value: Any,
    path: str,
    minimum: int,
    maximum: int,
) -> int:
    parsed = _integer(value, path)
    if not minimum <= parsed <= maximum:
        raise ConfigError(
            f"{path} must be between {minimum} and {maximum}"
        )
    return parsed


def _parse_connection(value: Any) -> ConnectionConfig:
    path = "connection"
    raw = _mapping(value, path)
    _reject_unknown(
        raw,
        {
            "cpu_family",
            "ip",
            "rack",
            "slot",
            "cycle_ms",
            "connect_timeout_ms",
        },
        path,
    )

    cpu_family = _string(
        _required(raw, "cpu_family", path),
        f"{path}.cpu_family",
    ).lower()
    if cpu_family not in _SUPPORTED_CPU_FAMILIES:
        supported = ", ".join(sorted(_SUPPORTED_CPU_FAMILIES))
        raise ConfigError(
            f"{path}.cpu_family must be one of: {supported}"
        )

    ip = _string(_required(raw, "ip", path), f"{path}.ip")
    try:
        IPv4Address(ip)
    except ValueError as exc:
        raise ConfigError(f"{path}.ip must be a valid IPv4 address") from exc

    rack = _bounded_integer(
        _required(raw, "rack", path),
        f"{path}.rack",
        0,
        7,
    )
    slot = _bounded_integer(
        _required(raw, "slot", path),
        f"{path}.slot",
        0,
        31,
    )
    cycle_ms = _bounded_integer(
        _required(raw, "cycle_ms", path),
        f"{path}.cycle_ms",
        5,
        5_000,
    )
    connect_timeout_ms = _bounded_integer(
        _required(raw, "connect_timeout_ms", path),
        f"{path}.connect_timeout_ms",
        100,
        30_000,
    )

    return ConnectionConfig(
        cpu_family=cpu_family,
        ip=ip,
        rack=rack,
        slot=slot,
        cycle_ms=cycle_ms,
        connect_timeout_ms=connect_timeout_ms,
    )


def _parse_data_type(value: Any, path: str) -> DataType:
    text = _string(value, path).upper()
    try:
        return DataType(text)
    except ValueError as exc:
        supported = ", ".join(item.value for item in DataType)
        raise ConfigError(f"{path} must be one of: {supported}") from exc


def _parse_direction(value: Any, path: str) -> Direction:
    text = _string(value, path).lower()
    try:
        return Direction(text)
    except ValueError as exc:
        supported = ", ".join(item.value for item in Direction)
        raise ConfigError(f"{path} must be one of: {supported}") from exc


def validate_data_value(
    value: Any,
    data_type: DataType,
    path: str,
) -> bool | int | float:
    """Validate one Python value against a configured PLC scalar type."""
    if data_type is DataType.BOOL:
        if not isinstance(value, bool):
            raise ConfigError(f"{path} must be true or false for BOOL")
        return value

    if isinstance(value, bool):
        raise ConfigError(f"{path} must be numeric for {data_type.value}")

    if data_type is DataType.REAL:
        if not isinstance(value, (int, float)):
            raise ConfigError(f"{path} must be numeric for REAL")
        try:
            parsed = float(value)
        except OverflowError as exc:
            raise ConfigError(
                f"{path} must be a finite 32-bit REAL value"
            ) from exc
        if not math.isfinite(parsed) or abs(parsed) > 3.402_823_5e38:
            raise ConfigError(
                f"{path} must be a finite 32-bit REAL value"
            )
        return parsed

    if not isinstance(value, int):
        raise ConfigError(f"{path} must be an integer for {data_type.value}")

    ranges: dict[DataType, tuple[int, int]] = {
        DataType.BYTE: (0, 255),
        DataType.WORD: (0, 65_535),
        DataType.DWORD: (0, 4_294_967_295),
        DataType.INT: (-32_768, 32_767),
        DataType.DINT: (-2_147_483_648, 2_147_483_647),
    }
    minimum, maximum = ranges[data_type]
    if not minimum <= value <= maximum:
        raise ConfigError(
            f"{path} must be between {minimum} and {maximum} "
            f"for {data_type.value}"
        )
    return value


def _address_interval(
    address: str,
    data_type: DataType,
    path: str,
) -> TagAddress:
    match = _ADDRESS_PATTERN.fullmatch(address)
    if match is None:
        raise ConfigError(
            f"{path} must use an absolute DB address such as "
            "DB14.DBX0.0 or DB14.DBD2"
        )

    db_number = int(match.group("db"))
    byte_offset = int(match.group("byte"))
    area = match.group("area")
    bit_text = match.group("bit")

    if db_number < 1:
        raise ConfigError(f"{path} must use DB number 1 or greater")

    if data_type not in _AREA_TYPES[area]:
        allowed = ", ".join(
            item.value for item in sorted(
                _AREA_TYPES[area],
                key=lambda item: item.value,
            )
        )
        raise ConfigError(
            f"{path} area {area} requires data type: {allowed}"
        )

    if area == "DBX":
        if bit_text is None:
            raise ConfigError(f"{path} BOOL address requires a bit number")
        start_bit = byte_offset * 8 + int(bit_text)
    else:
        if bit_text is not None:
            raise ConfigError(
                f"{path} must not include a bit number for {data_type.value}"
            )
        start_bit = byte_offset * 8

    return TagAddress(
        db_number=db_number,
        start_bit=start_bit,
        end_bit=start_bit + _TYPE_BITS[data_type],
    )


def _parse_tag(value: Any, index: int) -> TagConfig:
    path = f"tags[{index}]"
    raw = _mapping(value, path)
    _reject_unknown(
        raw,
        {
            "name",
            "plc_symbol",
            "address",
            "data_type",
            "direction",
            "safe_value",
        },
        path,
    )

    name = _string(_required(raw, "name", path), f"{path}.name").lower()
    if _NAME_PATTERN.fullmatch(name) is None:
        raise ConfigError(
            f"{path}.name must start with a lowercase letter and contain "
            "only lowercase letters, digits, and underscores"
        )

    plc_symbol = _string(
        _required(raw, "plc_symbol", path),
        f"{path}.plc_symbol",
    )
    address = _string(
        _required(raw, "address", path),
        f"{path}.address",
    ).upper()
    data_type = _parse_data_type(
        _required(raw, "data_type", path),
        f"{path}.data_type",
    )
    direction = _parse_direction(
        _required(raw, "direction", path),
        f"{path}.direction",
    )
    interval = _address_interval(address, data_type, f"{path}.address")

    if direction is Direction.PC_TO_PLC:
        if "safe_value" not in raw:
            raise ConfigError(
                f"{path}.safe_value is required for pc_to_plc tags"
            )
        safe_value = validate_data_value(
            raw["safe_value"],
            data_type,
            f"{path}.safe_value",
        )
    else:
        if "safe_value" in raw and raw["safe_value"] is not None:
            raise ConfigError(
                f"{path}.safe_value must be omitted or null "
                "for plc_to_pc tags"
            )
        safe_value = None

    return TagConfig(
        name=name,
        plc_symbol=plc_symbol,
        address=address,
        data_type=data_type,
        direction=direction,
        safe_value=safe_value,
        memory=interval,
    )


def _validate_unique_and_non_overlapping(
    tags: list[TagConfig],
) -> None:
    names: set[str] = set()
    symbols: set[str] = set()
    addresses: set[str] = set()

    for index, tag in enumerate(tags):
        for value, seen, label in (
            (tag.name, names, "name"),
            (tag.plc_symbol, symbols, "plc_symbol"),
            (tag.address, addresses, "address"),
        ):
            if value in seen:
                raise ConfigError(
                    f"tags[{index}].{label} duplicates {value!r}"
                )
            seen.add(value)

    occupied: list[tuple[int, int, int, str]] = []
    for tag in tags:
        db_number = tag.memory.db_number
        start_bit = tag.memory.start_bit
        end_bit = tag.memory.end_bit
        for used_db, used_start, used_end, used_name in occupied:
            overlaps = (
                db_number == used_db
                and start_bit < used_end
                and used_start < end_bit
            )
            if overlaps:
                raise ConfigError(
                    f"tag {tag.name!r} overlaps tag {used_name!r} "
                    f"in DB{db_number}"
                )
        occupied.append((db_number, start_bit, end_bit, tag.name))


def _parse_point_kind(value: Any, path: str) -> PointKind:
    text = _string(value, path).lower()
    try:
        return PointKind(text)
    except ValueError as exc:
        supported = ", ".join(item.value for item in PointKind)
        raise ConfigError(f"{path} must be one of: {supported}") from exc


def _parse_out_of_range(value: Any, path: str) -> OutOfRangePolicy:
    text = _string(value, path).lower()
    try:
        return OutOfRangePolicy(text)
    except ValueError as exc:
        supported = ", ".join(item.value for item in OutOfRangePolicy)
        raise ConfigError(f"{path} must be one of: {supported}") from exc


def _point_name(value: Any, path: str) -> str:
    name = _string(value, path).lower()
    if _NAME_PATTERN.fullmatch(name) is None:
        raise ConfigError(
            f"{path} must start with a lowercase letter and contain only "
            "lowercase letters, digits, and underscores"
        )
    return name


def _parse_point(
    value: Any,
    index: int,
    tags_by_name: dict[str, TagConfig],
    heartbeat: HeartbeatConfig,
) -> PointConfig:
    path = f"points[{index}]"
    raw = _mapping(value, path)
    kind = _parse_point_kind(
        _required(raw, "kind", path),
        f"{path}.kind",
    )

    common_fields = {"name", "kind", "tag", "group"}
    if kind is PointKind.DIGITAL:
        _reject_unknown(raw, common_fields | {"inverted"}, path)
    else:
        _reject_unknown(
            raw,
            common_fields
            | {
                "raw_min",
                "raw_max",
                "engineering_min",
                "engineering_max",
                "unit",
                "out_of_range",
            },
            path,
        )

    name = _point_name(_required(raw, "name", path), f"{path}.name")
    tag_name = _point_name(_required(raw, "tag", path), f"{path}.tag")
    group = _point_name(_required(raw, "group", path), f"{path}.group")

    try:
        tag = tags_by_name[tag_name]
    except KeyError as exc:
        raise ConfigError(
            f"{path}.tag references unknown tag {tag_name!r}"
        ) from exc

    if tag_name in {heartbeat.pc_tag, heartbeat.echo_tag}:
        raise ConfigError(
            f"{path}.tag cannot expose an internal heartbeat tag"
        )

    if kind is PointKind.DIGITAL:
        if tag.data_type is not DataType.BOOL:
            raise ConfigError(
                f"{path}.tag must reference a BOOL tag for a digital point"
            )
        inverted = _boolean(raw.get("inverted", False), f"{path}.inverted")
        return DigitalPointConfig(
            name=name,
            tag=tag_name,
            group=group,
            inverted=inverted,
        )

    if tag.data_type is DataType.BOOL:
        raise ConfigError(
            f"{path}.tag must reference a numeric tag for an analog point"
        )

    raw_min = validate_data_value(
        _required(raw, "raw_min", path),
        tag.data_type,
        f"{path}.raw_min",
    )
    raw_max = validate_data_value(
        _required(raw, "raw_max", path),
        tag.data_type,
        f"{path}.raw_max",
    )
    if raw_min >= raw_max:
        raise ConfigError(f"{path}.raw_min must be less than raw_max")
    if (
        tag.direction is Direction.PC_TO_PLC
        and tag.safe_value is not None
        and not raw_min <= tag.safe_value <= raw_max
    ):
        raise ConfigError(
            f"{path} raw range must contain the backing tag safe_value"
        )

    engineering_min = _number(
        _required(raw, "engineering_min", path),
        f"{path}.engineering_min",
    )
    engineering_max = _number(
        _required(raw, "engineering_max", path),
        f"{path}.engineering_max",
    )
    if engineering_min == engineering_max:
        raise ConfigError(
            f"{path}.engineering_min and engineering_max must differ"
        )
    if not math.isfinite(engineering_max - engineering_min):
        raise ConfigError(f"{path} engineering range is too large")

    unit = _string(_required(raw, "unit", path), f"{path}.unit")
    out_of_range = _parse_out_of_range(
        _required(raw, "out_of_range", path),
        f"{path}.out_of_range",
    )
    return AnalogPointConfig(
        name=name,
        tag=tag_name,
        group=group,
        raw_min=raw_min,
        raw_max=raw_max,
        engineering_min=engineering_min,
        engineering_max=engineering_max,
        unit=unit,
        out_of_range=out_of_range,
    )


def _parse_points(
    value: Any,
    tags_by_name: dict[str, TagConfig],
    heartbeat: HeartbeatConfig,
) -> tuple[PointConfig, ...]:
    raw_points = _list(value, "points")
    parsed = [
        _parse_point(item, index, tags_by_name, heartbeat)
        for index, item in enumerate(raw_points)
    ]

    names: set[str] = set()
    tag_names: set[str] = set()
    for index, point in enumerate(parsed):
        if point.name in names:
            raise ConfigError(
                f"points[{index}].name duplicates {point.name!r}"
            )
        names.add(point.name)
        if point.tag in tag_names:
            raise ConfigError(
                f"points[{index}].tag duplicates point tag {point.tag!r}"
            )
        tag_names.add(point.tag)

    return tuple(parsed)


def _parse_heartbeat(
    value: Any,
    connection: ConnectionConfig,
    tags_by_name: dict[str, TagConfig],
) -> HeartbeatConfig:
    path = "heartbeat"
    raw = _mapping(value, path)
    _reject_unknown(raw, {"pc_tag", "echo_tag", "timeout_ms"}, path)

    pc_tag_name = _string(
        _required(raw, "pc_tag", path),
        f"{path}.pc_tag",
    ).lower()
    echo_tag_name = _string(
        _required(raw, "echo_tag", path),
        f"{path}.echo_tag",
    ).lower()
    timeout_ms = _bounded_integer(
        _required(raw, "timeout_ms", path),
        f"{path}.timeout_ms",
        20,
        60_000,
    )

    if timeout_ms < connection.cycle_ms * 2:
        raise ConfigError(
            "heartbeat.timeout_ms must be at least twice "
            "connection.cycle_ms"
        )

    if pc_tag_name == echo_tag_name:
        raise ConfigError("heartbeat.pc_tag and echo_tag must be different")

    try:
        pc_tag = tags_by_name[pc_tag_name]
    except KeyError as exc:
        raise ConfigError(
            f"heartbeat.pc_tag references unknown tag {pc_tag_name!r}"
        ) from exc
    try:
        echo_tag = tags_by_name[echo_tag_name]
    except KeyError as exc:
        raise ConfigError(
            f"heartbeat.echo_tag references unknown tag {echo_tag_name!r}"
        ) from exc

    if (
        pc_tag.direction is not Direction.PC_TO_PLC
        or pc_tag.data_type is not DataType.DINT
    ):
        raise ConfigError(
            "heartbeat.pc_tag must reference a pc_to_plc DINT tag"
        )
    if (
        echo_tag.direction is not Direction.PLC_TO_PC
        or echo_tag.data_type is not DataType.DINT
    ):
        raise ConfigError(
            "heartbeat.echo_tag must reference a plc_to_pc DINT tag"
        )

    return HeartbeatConfig(
        pc_tag=pc_tag_name,
        echo_tag=echo_tag_name,
        timeout_ms=timeout_ms,
    )


def parse_config(value: Any) -> InterfaceConfig:
    """Validate a decoded JSON value and return an immutable configuration."""
    root = _mapping(value, "config")
    version = _integer(_required(root, "version", "config"), "version")
    if version not in {1, 2}:
        raise ConfigError("version must be 1 or 2")

    allowed_fields = {"version", "connection", "heartbeat", "tags"}
    if version == 2:
        allowed_fields.add("points")
    _reject_unknown(
        root,
        allowed_fields,
        "config",
    )

    connection = _parse_connection(
        _required(root, "connection", "config")
    )

    raw_tags = _list(_required(root, "tags", "config"), "tags")
    if not raw_tags:
        raise ConfigError("tags must contain at least one tag")

    parsed_tags: list[TagConfig] = []
    for index, raw_tag in enumerate(raw_tags):
        parsed_tags.append(_parse_tag(raw_tag, index))

    _validate_unique_and_non_overlapping(parsed_tags)
    tags_by_name = {tag.name: tag for tag in parsed_tags}
    heartbeat = _parse_heartbeat(
        _required(root, "heartbeat", "config"),
        connection,
        tags_by_name,
    )
    if version == 2:
        points = _parse_points(
            _required(root, "points", "config"),
            tags_by_name,
            heartbeat,
        )
        if not points:
            raise ConfigError(
                "points must contain at least one point for version 2"
            )
    else:
        points = ()

    return InterfaceConfig(
        version=version,
        connection=connection,
        heartbeat=heartbeat,
        tags=tuple(parsed_tags),
        points=points,
    )


def load_config(path: str | Path) -> InterfaceConfig:
    """Read, decode, and validate a configuration file."""
    config_path = Path(path)
    try:
        text = config_path.read_text(encoding="utf-8")
    except OSError as exc:
        raise ConfigError(
            f"cannot read configuration file {config_path}: {exc}"
        ) from exc

    try:
        decoded = json.loads(text)
    except json.JSONDecodeError as exc:
        raise ConfigError(
            f"invalid JSON at line {exc.lineno}, column {exc.colno}: "
            f"{exc.msg}"
        ) from exc

    return parse_config(decoded)
