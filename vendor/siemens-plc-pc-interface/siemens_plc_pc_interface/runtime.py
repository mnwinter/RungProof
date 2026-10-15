"""Configuration-driven PLC simulation runtime cycle."""

from __future__ import annotations

from dataclasses import dataclass
from enum import Enum

from .config import (
    ConfigError,
    Direction,
    InterfaceConfig,
    TagConfig,
    validate_data_value,
)
from .heartbeat import HeartbeatCounter, HeartbeatMonitor, HeartbeatStatus
from .points import PointModel, PointSample, PointValueError
from .transport import PlcTransport, TagValue


class RuntimeStateError(RuntimeError):
    """The runtime was used in an invalid state."""


class SafeStatePolicy(str, Enum):
    """Optional PC behavior when a runtime session closes."""

    PLC_WATCHDOG_ONLY = "plc_watchdog_only"
    BEST_EFFORT_WRITE = "best_effort_write"


@dataclass(frozen=True)
class CycleResult:
    """Values exchanged during one deterministic runtime cycle."""

    cycle_number: int
    pc_values_written: dict[str, TagValue]
    plc_values_read: dict[str, TagValue]
    heartbeat: HeartbeatStatus


@dataclass(frozen=True)
class ShutdownResult:
    """Outcome of optional safe writes and transport disconnect."""

    safe_state_attempted: bool
    safe_state_succeeded: bool
    safe_state_error: str | None
    disconnect_error: str | None


class InterfaceRuntime:
    """
    Exchange only configured tags while supervising heartbeat progress.

    Non-heartbeat PC values begin at their validated safe values. A future
    scene layer can update them through ``set_pc_value``. Those values are
    written only when dirty, which avoids needless Boolean read/modify/write
    traffic against adjacent DB bits. The PC heartbeat is always generated
    internally and written last.
    """

    def __init__(
        self,
        config: InterfaceConfig,
        transport: PlcTransport,
        *,
        safe_state_policy: SafeStatePolicy = (
            SafeStatePolicy.PLC_WATCHDOG_ONLY
        ),
        heartbeat_counter: HeartbeatCounter | None = None,
        start_time: float = 0.0,
    ) -> None:
        self.config = config
        self.transport = transport
        self.safe_state_policy = safe_state_policy
        self._heartbeat_counter = heartbeat_counter or HeartbeatCounter()
        self._point_model = PointModel(config)
        self._heartbeat_monitor = HeartbeatMonitor(
            timeout_ms=config.heartbeat.timeout_ms,
            start_time=start_time,
        )
        self._heartbeat_tag = config.tag(config.heartbeat.pc_tag)
        self._echo_tag = config.tag(config.heartbeat.echo_tag)
        self._pc_tags = tuple(
            tag
            for tag in config.tags
            if tag.direction is Direction.PC_TO_PLC
        )
        self._plc_tags = tuple(
            tag
            for tag in config.tags
            if tag.direction is Direction.PLC_TO_PC
        )
        self._pc_values: dict[str, TagValue] = {
            tag.name: tag.safe_value
            for tag in self._pc_tags
            if tag is not self._heartbeat_tag
        }
        self._dirty_pc_tags = set(self._pc_values)
        self._connected = False
        self._heartbeat_sent = False
        self._last_heartbeat_written: int | None = None
        self._cycle_number = 0

    @property
    def connected(self) -> bool:
        return self._connected and self.transport.connected

    @property
    def write_scope(self) -> tuple[TagConfig, ...]:
        """Return every tag this runtime can write."""
        return self._pc_tags

    @property
    def read_scope(self) -> tuple[TagConfig, ...]:
        """Return every tag this runtime can read."""
        return self._plc_tags

    @property
    def point_model(self) -> PointModel:
        """Return the typed point converter used by this runtime."""
        return self._point_model

    def set_pc_value(self, name: str, value: TagValue) -> None:
        """Set one non-heartbeat, PC-owned scene value."""
        try:
            tag = self.config.tag(name)
        except KeyError as exc:
            raise ConfigError(f"unknown configured tag {name!r}") from exc

        if tag.direction is not Direction.PC_TO_PLC:
            raise ConfigError(
                f"tag {name!r} is PLC-owned and cannot be set by the PC"
            )
        if tag is self._heartbeat_tag:
            raise ConfigError(
                f"tag {name!r} is generated internally by the heartbeat"
            )

        validated = validate_data_value(
            value,
            tag.data_type,
            f"value for {name}",
        )
        if self._pc_values[name] != validated:
            self._pc_values[name] = validated
            self._dirty_pc_tags.add(name)

    def set_pc_point(
        self,
        name: str,
        value: bool | int | float,
    ) -> PointSample:
        """Scale and set one configured PC-owned scene point."""
        sample = self._point_model.encode_pc_value(name, value)
        if not sample.conversion_succeeded:
            raise PointValueError(
                f"point {name!r} rejected the value with quality "
                f"{sample.quality.value}"
            )
        assert sample.raw_value is not None
        self.set_pc_value(sample.tag_name, sample.raw_value)
        return sample

    def connect(self) -> None:
        if self._connected:
            raise RuntimeStateError("runtime is already connected")
        self.transport.connect(self.config.connection)
        self._connected = True

    def _require_connected(self) -> None:
        if not self.connected:
            raise RuntimeStateError("runtime is not connected")

    def _read_plc_values(self) -> dict[str, TagValue]:
        batch_reader = getattr(self.transport, "read_many", None)
        if callable(batch_reader):
            raw_values = batch_reader(self._plc_tags)
        else:
            raw_values = {
                tag.name: self.transport.read(tag)
                for tag in self._plc_tags
            }

        values: dict[str, TagValue] = {}
        for tag in self._plc_tags:
            values[tag.name] = validate_data_value(
                raw_values[tag.name],
                tag.data_type,
                f"PLC value for {tag.name}",
            )
        return values

    def _write_pc_values(self) -> dict[str, TagValue]:
        pending: list[tuple[TagConfig, TagValue]] = []
        for tag in self._pc_tags:
            if (
                tag is self._heartbeat_tag
                or tag.name not in self._dirty_pc_tags
            ):
                continue
            value = self._pc_values[tag.name]
            pending.append((tag, value))

        heartbeat = self._heartbeat_counter.next()
        pending.append((self._heartbeat_tag, heartbeat))

        batch_writer = getattr(self.transport, "write_many", None)
        if callable(batch_writer):
            batch_writer(pending)
        else:
            for tag, value in pending:
                self.transport.write(tag, value)

        written = {tag.name: value for tag, value in pending}
        self._dirty_pc_tags.difference_update(
            tag.name
            for tag, _ in pending
            if tag is not self._heartbeat_tag
        )
        self._heartbeat_sent = True
        self._last_heartbeat_written = heartbeat
        return written

    def cycle(self, now: float) -> CycleResult:
        """
        Run one read/monitor/write cycle.

        PLC-owned values are read first. The echo therefore acknowledges a
        heartbeat written during a previous cycle. PC-owned scene values are
        then written, followed by the new heartbeat as the final write.
        """
        self._require_connected()
        plc_values = self._read_plc_values()
        echo = plc_values[self._echo_tag.name]

        if self._heartbeat_sent and self._last_heartbeat_written is not None:
            heartbeat_status = self._heartbeat_monitor.observe(
                int(echo),
                self._last_heartbeat_written,
                now,
            )
        else:
            heartbeat_status = self._heartbeat_monitor.status(now)

        pc_values = self._write_pc_values()
        self._cycle_number += 1
        return CycleResult(
            cycle_number=self._cycle_number,
            pc_values_written=pc_values,
            plc_values_read=plc_values,
            heartbeat=heartbeat_status,
        )

    def _write_safe_state(self) -> None:
        safe_values = [
            (tag, tag.safe_value)
            for tag in self._pc_tags
            if tag is not self._heartbeat_tag
        ]
        safe_values.append(
            (self._heartbeat_tag, self._heartbeat_tag.safe_value)
        )
        batch_writer = getattr(self.transport, "write_many", None)
        if callable(batch_writer):
            batch_writer(safe_values)
        else:
            for tag, value in safe_values:
                self.transport.write(tag, value)

    def close(self) -> ShutdownResult:
        """
        Close the session and optionally attempt PC-side safe-value writes.

        A best-effort write is not guaranteed after network failure. The PLC
        watchdog remains the authoritative communication-loss protection.
        """
        safe_attempted = False
        safe_succeeded = False
        safe_error: str | None = None
        disconnect_error: str | None = None

        if (
            self.safe_state_policy is SafeStatePolicy.BEST_EFFORT_WRITE
            and self._connected
        ):
            safe_attempted = True
            try:
                self._write_safe_state()
                safe_succeeded = True
            except Exception as exc:
                safe_error = f"{type(exc).__name__}: {exc}"

        try:
            self.transport.disconnect()
        except Exception as exc:
            disconnect_error = f"{type(exc).__name__}: {exc}"
        finally:
            self._connected = False

        return ShutdownResult(
            safe_state_attempted=safe_attempted,
            safe_state_succeeded=safe_succeeded,
            safe_state_error=safe_error,
            disconnect_error=disconnect_error,
        )
