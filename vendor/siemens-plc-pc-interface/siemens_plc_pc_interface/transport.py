"""PLC transport boundary and the python-snap7 implementation."""

from __future__ import annotations

import struct
from collections.abc import Callable, Sequence
from typing import Any, Protocol

from .config import (
    ConnectionConfig,
    DataType,
    Direction,
    TagConfig,
    validate_data_value,
)


TagValue = bool | int | float


class TransportError(RuntimeError):
    """A PLC connection, read, write, or disconnect operation failed."""


class OwnershipError(TransportError):
    """A caller attempted an operation contrary to configured ownership."""


class PlcTransport(Protocol):
    """Small client contract used by the runtime and fake unit-test clients."""

    @property
    def connected(self) -> bool:
        """Return whether this transport currently has an S7 session."""

    def connect(self, connection: ConnectionConfig) -> None:
        """Connect using validated endpoint settings."""

    def disconnect(self) -> None:
        """Close the current S7 session."""

    def read(self, tag: TagConfig) -> TagValue:
        """Read one PLC-owned configured tag."""

    def write(self, tag: TagConfig, value: TagValue) -> None:
        """Write one PC-owned configured tag."""

_READ_BATCH_MAX_SPAN_BYTES = 200
_READ_BATCH_MAX_GAP_BYTES = 16
_STRUCT_FORMATS = {
    DataType.BYTE: ">B",
    DataType.WORD: ">H",
    DataType.DWORD: ">I",
    DataType.INT: ">h",
    DataType.DINT: ">i",
    DataType.REAL: ">f",
}


def _decode_tag(
    tag: TagConfig,
    data: bytearray,
    block_start: int,
) -> TagValue:
    offset = tag.memory.start_byte - block_start
    if tag.data_type is DataType.BOOL:
        bit = tag.memory.start_bit % 8
        return bool(data[offset] & (1 << bit))
    return struct.unpack_from(_STRUCT_FORMATS[tag.data_type], data, offset)[0]


def _encode_tag(
    tag: TagConfig,
    value: TagValue,
    data: bytearray,
    block_start: int,
) -> None:
    offset = tag.memory.start_byte - block_start
    if tag.data_type is DataType.BOOL:
        bit_mask = 1 << (tag.memory.start_bit % 8)
        if value:
            data[offset] |= bit_mask
        else:
            data[offset] &= ~bit_mask & 0xFF
        return
    struct.pack_into(_STRUCT_FORMATS[tag.data_type], data, offset, value)


def _read_groups(
    tags: Sequence[TagConfig],
) -> tuple[tuple[TagConfig, ...], ...]:
    ordered = sorted(
        tags,
        key=lambda tag: (
            tag.memory.db_number,
            tag.memory.start_byte,
            tag.memory.end_byte,
        ),
    )
    groups: list[list[TagConfig]] = []
    for tag in ordered:
        if not groups:
            groups.append([tag])
            continue
        current = groups[-1]
        first = current[0]
        current_end = max(item.memory.end_byte for item in current)
        candidate_end = max(current_end, tag.memory.end_byte)
        gap = max(0, tag.memory.start_byte - current_end)
        span = candidate_end - first.memory.start_byte
        if (
            tag.memory.db_number == first.memory.db_number
            and gap <= _READ_BATCH_MAX_GAP_BYTES
            and span <= _READ_BATCH_MAX_SPAN_BYTES
        ):
            current.append(tag)
        else:
            groups.append([tag])
    return tuple(tuple(group) for group in groups)


def _write_groups(
    values: Sequence[tuple[TagConfig, TagValue]],
) -> tuple[tuple[tuple[TagConfig, TagValue], ...], ...]:
    """
    Group only consecutive, gap-free writes without reordering them.

    Preserving caller order keeps the generated heartbeat last. A gap starts
    a new DB write so an unowned byte is never included in a write request.
    """
    groups: list[list[tuple[TagConfig, TagValue]]] = []
    for item in values:
        tag, _ = item
        if not groups:
            groups.append([item])
            continue
        current = groups[-1]
        first_tag = current[0][0]
        current_end = max(
            current_tag.memory.end_byte
            for current_tag, _ in current
        )
        if (
            tag.memory.db_number == first_tag.memory.db_number
            and tag.memory.start_byte >= first_tag.memory.start_byte
            and tag.memory.start_byte <= current_end
        ):
            current.append(item)
        else:
            groups.append([item])
    return tuple(tuple(group) for group in groups)


class Snap7Transport:
    """
    Adapt python-snap7 behind the narrow runtime transport contract.

    The dependency is imported lazily so offline configuration validation and
    unit tests cannot connect merely by importing this package.
    """

    def __init__(
        self,
        client_factory: Callable[[], Any] | None = None,
    ) -> None:
        self._client_factory = client_factory
        self._client: Any | None = None

    @property
    def connected(self) -> bool:
        if self._client is None:
            return False
        try:
            return bool(self._client.get_connected())
        except Exception:
            return False

    def _make_client(self) -> Any:
        if self._client_factory is not None:
            return self._client_factory()

        try:
            from snap7 import Client
        except ImportError as exc:
            raise TransportError(
                "python-snap7 is not installed; install requirements.txt"
            ) from exc
        return Client()

    def connect(self, connection: ConnectionConfig) -> None:
        if self._client is not None:
            raise TransportError("transport is already connected or in use")

        client = self._make_client()
        self._client = client
        try:
            # python-snap7 3.1 exposes public timeout parameters, while its
            # pure-Python connection object also owns the socket receive
            # timeout. Configure both because this adapter is pinned to that
            # exact version and application shutdown must remain bounded.
            set_param = getattr(client, "set_param", None)
            if callable(set_param):
                from snap7.type import Parameter

                for parameter in (
                    Parameter.PingTimeout,
                    Parameter.SendTimeout,
                    Parameter.RecvTimeout,
                ):
                    set_param(parameter, connection.connect_timeout_ms)
            client.connect(
                connection.ip,
                connection.rack,
                connection.slot,
            )
            if not client.get_connected():
                raise TransportError(
                    "python-snap7 returned without an active S7 session"
                )
            raw_connection = getattr(client, "connection", None)
            if raw_connection is not None:
                timeout_s = connection.connect_timeout_ms / 1_000
                if hasattr(raw_connection, "timeout"):
                    raw_connection.timeout = timeout_s
                raw_socket = getattr(raw_connection, "socket", None)
                if raw_socket is not None:
                    raw_socket.settimeout(timeout_s)
        except Exception as exc:
            try:
                client.disconnect()
            except Exception:
                pass
            self._client = None
            if isinstance(exc, TransportError):
                raise
            raise TransportError(
                f"failed to connect to {connection.ip} "
                f"rack={connection.rack} slot={connection.slot}: {exc}"
            ) from exc

    def disconnect(self) -> None:
        client = self._client
        self._client = None
        if client is None:
            return
        try:
            client.disconnect()
        except Exception as exc:
            raise TransportError(f"failed to disconnect cleanly: {exc}") from exc

    def _require_client(self) -> Any:
        if self._client is None or not self.connected:
            raise TransportError("transport is not connected")
        return self._client

    def read(self, tag: TagConfig) -> TagValue:
        if tag.direction is not Direction.PLC_TO_PC:
            raise OwnershipError(
                f"refusing to read PC-owned tag {tag.name!r}; "
                "runtime reads are limited to plc_to_pc tags"
            )

        try:
            return self._require_client().read_tag(tag.snap7_tag)
        except OwnershipError:
            raise
        except Exception as exc:
            raise TransportError(
                f"failed to read {tag.name} at {tag.snap7_tag}: {exc}"
            ) from exc

    def write(self, tag: TagConfig, value: TagValue) -> None:
        if tag.direction is not Direction.PC_TO_PLC:
            raise OwnershipError(
                f"refusing to write PLC-owned tag {tag.name!r}; "
                "runtime writes are limited to pc_to_plc tags"
            )

        try:
            self._require_client().write_tag(tag.snap7_tag, value)
        except OwnershipError:
            raise
        except Exception as exc:
            raise TransportError(
                f"failed to write {tag.name} at {tag.snap7_tag}: {exc}"
            ) from exc

    def read_many(
        self,
        tags: Sequence[TagConfig],
    ) -> dict[str, TagValue]:
        requested = tuple(tags)
        for tag in requested:
            if tag.direction is not Direction.PLC_TO_PC:
                raise OwnershipError(
                    f"refusing to read PC-owned tag {tag.name!r}; "
                    "runtime reads are limited to plc_to_pc tags"
                )
        return self._read_many_unchecked(requested)

    def read_many_diagnostic(
        self,
        tags: Sequence[TagConfig],
    ) -> dict[str, TagValue]:
        """Read configured tags in both ownership directions without writes."""

        return self._read_many_unchecked(tuple(tags))

    def _read_many_unchecked(
        self,
        requested: tuple[TagConfig, ...],
    ) -> dict[str, TagValue]:
        if not requested:
            return {}

        client = self._require_client()
        values: dict[str, TagValue] = {}
        for group in _read_groups(requested):
            db_number = group[0].memory.db_number
            start = min(tag.memory.start_byte for tag in group)
            end = max(tag.memory.end_byte for tag in group)
            names = ", ".join(tag.name for tag in group)
            try:
                data = bytearray(client.db_read(db_number, start, end - start))
                if len(data) != end - start:
                    raise TransportError(
                        f"DB{db_number} read returned {len(data)} bytes; "
                        f"expected {end - start}"
                    )
                for tag in group:
                    values[tag.name] = _decode_tag(tag, data, start)
            except OwnershipError:
                raise
            except Exception as exc:
                if isinstance(exc, TransportError):
                    raise
                raise TransportError(
                    f"failed batched read of {names} from "
                    f"DB{db_number}.DBB{start}..{end - 1}: {exc}"
                ) from exc
        return values

    def write_many(
        self,
        values: Sequence[tuple[TagConfig, TagValue]],
    ) -> None:
        pending = tuple(values)
        validated: list[tuple[TagConfig, TagValue]] = []
        for tag, value in pending:
            if tag.direction is not Direction.PC_TO_PLC:
                raise OwnershipError(
                    f"refusing to write PLC-owned tag {tag.name!r}; "
                    "runtime writes are limited to pc_to_plc tags"
                )
            validated.append(
                (
                    tag,
                    validate_data_value(
                        value,
                        tag.data_type,
                        f"value for {tag.name}",
                    ),
                )
            )
        if not validated:
            return

        client = self._require_client()
        for group in _write_groups(validated):
            tags = tuple(tag for tag, _ in group)
            db_number = tags[0].memory.db_number
            start = min(tag.memory.start_byte for tag in tags)
            end = max(tag.memory.end_byte for tag in tags)
            names = ", ".join(tag.name for tag in tags)
            try:
                if any(tag.data_type is DataType.BOOL for tag in tags):
                    data = bytearray(
                        client.db_read(db_number, start, end - start)
                    )
                    if len(data) != end - start:
                        raise TransportError(
                            f"DB{db_number} read returned {len(data)} bytes; "
                            f"expected {end - start}"
                        )
                else:
                    data = bytearray(end - start)
                for tag, value in group:
                    _encode_tag(tag, value, data, start)
                client.db_write(db_number, start, data)
            except OwnershipError:
                raise
            except Exception as exc:
                if isinstance(exc, TransportError):
                    raise
                raise TransportError(
                    f"failed batched write of {names} to "
                    f"DB{db_number}.DBB{start}..{end - 1}: {exc}"
                ) from exc
