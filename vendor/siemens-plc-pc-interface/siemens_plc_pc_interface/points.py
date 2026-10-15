"""Typed scene points layered above validated PLC tags."""

from __future__ import annotations

import math
from dataclasses import dataclass
from enum import Enum

from .config import (
    AnalogPointConfig,
    ConfigError,
    DataType,
    DigitalPointConfig,
    Direction,
    InterfaceConfig,
    OutOfRangePolicy,
    PointConfig,
    TagConfig,
    validate_data_value,
)


class PointValueError(ValueError):
    """A point value cannot be converted safely."""


class PointQuality(str, Enum):
    """Diagnostic result of one raw/engineering conversion."""

    GOOD = "good"
    CLAMPED_LOW = "clamped_low"
    CLAMPED_HIGH = "clamped_high"
    FAULT_LOW = "fault_low"
    FAULT_HIGH = "fault_high"


@dataclass(frozen=True)
class PointSample:
    """One scene-facing value with its raw PLC value and quality."""

    point_name: str
    tag_name: str
    value: bool | float | None
    raw_value: bool | int | float | None
    quality: PointQuality
    unit: str | None

    @property
    def conversion_succeeded(self) -> bool:
        """Return whether both sides of the conversion are usable."""
        return self.value is not None and self.raw_value is not None


@dataclass(frozen=True)
class AddressGroup:
    """
    Contiguous DB byte range with one configured owner.

    ``shares_byte_with_opposite_owner`` warns future block-write code that a
    read/modify/write boundary exists. The current transport still performs
    typed single-tag operations and does not write these groups directly.
    """

    direction: Direction
    db_number: int
    start_byte: int
    byte_length: int
    tag_names: tuple[str, ...]
    shares_byte_with_opposite_owner: bool


def _point_tag(config: InterfaceConfig, point: PointConfig) -> TagConfig:
    return config.tag(point.tag)


def _quality_for_range(
    value: float,
    minimum: float,
    maximum: float,
    policy: OutOfRangePolicy,
) -> tuple[float | None, PointQuality]:
    low = min(minimum, maximum)
    high = max(minimum, maximum)
    if value < low:
        if policy is OutOfRangePolicy.FAULT:
            return None, PointQuality.FAULT_LOW
        return low, PointQuality.CLAMPED_LOW
    if value > high:
        if policy is OutOfRangePolicy.FAULT:
            return None, PointQuality.FAULT_HIGH
        return high, PointQuality.CLAMPED_HIGH
    return value, PointQuality.GOOD


def _scale(
    value: float,
    source_min: float,
    source_max: float,
    target_min: float,
    target_max: float,
) -> float:
    ratio = (value - source_min) / (source_max - source_min)
    return target_min + ratio * (target_max - target_min)


def _round_integer(value: float) -> int:
    """Round halves away from zero for predictable PLC integer conversion."""
    if value >= 0:
        return math.floor(value + 0.5)
    return math.ceil(value - 0.5)


def _validated_raw(
    tag: TagConfig,
    value: bool | int | float,
    path: str,
) -> bool | int | float:
    try:
        return validate_data_value(value, tag.data_type, path)
    except ConfigError as exc:
        raise PointValueError(str(exc)) from exc


class PointModel:
    """Convert between scene values and the configured PLC tag values."""

    def __init__(self, config: InterfaceConfig) -> None:
        self.config = config

    def direction(self, name: str) -> Direction:
        """Return the authoritative writer for one point."""
        point = self._point(name)
        return _point_tag(self.config, point).direction

    def _point(self, name: str) -> PointConfig:
        try:
            return self.config.point(name)
        except KeyError as exc:
            raise PointValueError(f"unknown configured point {name!r}") from exc

    def decode(
        self,
        name: str,
        raw_value: bool | int | float,
    ) -> PointSample:
        """Decode a validated raw PLC value into a scene-facing value."""
        point = self._point(name)
        tag = _point_tag(self.config, point)
        raw = _validated_raw(tag, raw_value, f"raw value for point {name}")

        if isinstance(point, DigitalPointConfig):
            value = bool(raw) ^ point.inverted
            return PointSample(
                point_name=point.name,
                tag_name=tag.name,
                value=value,
                raw_value=raw,
                quality=PointQuality.GOOD,
                unit=None,
            )

        bounded, quality = _quality_for_range(
            float(raw),
            float(point.raw_min),
            float(point.raw_max),
            point.out_of_range,
        )
        engineering_value = (
            None
            if bounded is None
            else _scale(
                bounded,
                float(point.raw_min),
                float(point.raw_max),
                point.engineering_min,
                point.engineering_max,
            )
        )
        return PointSample(
            point_name=point.name,
            tag_name=tag.name,
            value=engineering_value,
            raw_value=raw,
            quality=quality,
            unit=point.unit,
        )

    def encode_pc_value(
        self,
        name: str,
        value: bool | int | float,
    ) -> PointSample:
        """
        Convert a scene value into one PC-owned PLC tag value.

        A ``fault`` out-of-range policy returns a non-writable sample instead
        of silently issuing a PLC write.
        """
        point = self._point(name)
        tag = _point_tag(self.config, point)
        if tag.direction is not Direction.PC_TO_PLC:
            raise PointValueError(
                f"point {name!r} is PLC-owned and cannot be set by the PC"
            )

        if isinstance(point, DigitalPointConfig):
            if not isinstance(value, bool):
                raise PointValueError(
                    f"value for point {name} must be true or false"
                )
            raw = value ^ point.inverted
            return PointSample(
                point_name=point.name,
                tag_name=tag.name,
                value=value,
                raw_value=raw,
                quality=PointQuality.GOOD,
                unit=None,
            )

        if isinstance(value, bool) or not isinstance(value, (int, float)):
            raise PointValueError(f"value for point {name} must be numeric")
        engineering = float(value)
        if not math.isfinite(engineering):
            raise PointValueError(f"value for point {name} must be finite")

        bounded, quality = _quality_for_range(
            engineering,
            point.engineering_min,
            point.engineering_max,
            point.out_of_range,
        )
        if bounded is None:
            return PointSample(
                point_name=point.name,
                tag_name=tag.name,
                value=engineering,
                raw_value=None,
                quality=quality,
                unit=point.unit,
            )

        raw_float = _scale(
            bounded,
            point.engineering_min,
            point.engineering_max,
            float(point.raw_min),
            float(point.raw_max),
        )
        raw_candidate: int | float
        if tag.data_type is DataType.REAL:
            raw_candidate = raw_float
        else:
            raw_candidate = _round_integer(raw_float)
        raw = _validated_raw(
            tag,
            raw_candidate,
            f"scaled raw value for point {name}",
        )
        return PointSample(
            point_name=point.name,
            tag_name=tag.name,
            value=bounded,
            raw_value=raw,
            quality=quality,
            unit=point.unit,
        )

    def safe_sample(self, name: str) -> PointSample | None:
        """Return the scene interpretation of a PC-owned tag's safe value."""
        point = self._point(name)
        tag = _point_tag(self.config, point)
        if tag.safe_value is None:
            return None
        return self.decode(name, tag.safe_value)


def build_address_groups(
    config: InterfaceConfig,
) -> tuple[AddressGroup, ...]:
    """
    Compute contiguous DB ranges without crossing ownership boundaries.

    This is a planning/diagnostic model for a future block transport. It does
    not change the proven typed-tag transport behavior.
    """
    opposite_bytes: dict[Direction, set[tuple[int, int]]] = {
        Direction.PC_TO_PLC: set(),
        Direction.PLC_TO_PC: set(),
    }
    for tag in config.tags:
        for byte in range(tag.memory.start_byte, tag.memory.end_byte):
            opposite_bytes[tag.direction].add((tag.memory.db_number, byte))

    groups: list[AddressGroup] = []
    for direction in Direction:
        tags = sorted(
            (tag for tag in config.tags if tag.direction is direction),
            key=lambda tag: (
                tag.memory.db_number,
                tag.memory.start_byte,
                tag.memory.end_byte,
                tag.name,
            ),
        )
        current_db: int | None = None
        current_start = 0
        current_end = 0
        current_names: list[str] = []

        def append_current() -> None:
            if current_db is None:
                return
            opposite = (
                Direction.PLC_TO_PC
                if direction is Direction.PC_TO_PLC
                else Direction.PC_TO_PLC
            )
            shared = any(
                (current_db, byte) in opposite_bytes[opposite]
                for byte in range(current_start, current_end)
            )
            groups.append(
                AddressGroup(
                    direction=direction,
                    db_number=current_db,
                    start_byte=current_start,
                    byte_length=current_end - current_start,
                    tag_names=tuple(current_names),
                    shares_byte_with_opposite_owner=shared,
                )
            )

        for tag in tags:
            start = tag.memory.start_byte
            end = tag.memory.end_byte
            if (
                current_db == tag.memory.db_number
                and start <= current_end
            ):
                current_end = max(current_end, end)
                current_names.append(tag.name)
                continue

            append_current()
            current_db = tag.memory.db_number
            current_start = start
            current_end = end
            current_names = [tag.name]

        append_current()

    return tuple(groups)
