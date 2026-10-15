"""Guarded live PLC session for RungProof.

This adapter reuses the pinned Siemens interface runtime.  It owns one
explicitly authorized S7 session, accepts only configured PC-owned scene
points, returns only configured PLC-owned scene points/status, and relies on
the PLC watchdog when browser cycles stop.
"""

from __future__ import annotations

from pathlib import Path
import re
import secrets
import sys
import threading
import time
from typing import Any, Callable, Mapping


INTERFACE_SOURCE_ROOT = (
    Path(__file__).resolve().parents[1]
    / "vendor"
    / "siemens-plc-pc-interface"
)
if INTERFACE_SOURCE_ROOT.is_dir():
    interface_source = str(INTERFACE_SOURCE_ROOT)
    if interface_source not in sys.path:
        sys.path.insert(0, interface_source)

try:
    from siemens_plc_pc_interface.config import (
        ConfigError,
        Direction,
        InterfaceConfig,
        load_config,
    )
    from siemens_plc_pc_interface.runtime import (
        InterfaceRuntime,
        SafeStatePolicy,
    )
    from siemens_plc_pc_interface.transport import Snap7Transport
    from siemens_plc_pc_interface.update_loop import (
        LoopHealth,
        SimulationUpdateLoop,
        update_log_record,
    )
except ImportError as exc:  # pragma: no cover - packaged fallback.
    ConfigError = ValueError  # type: ignore[assignment,misc]
    Direction = None  # type: ignore[assignment]
    InterfaceConfig = Any  # type: ignore[assignment,misc]
    InterfaceRuntime = None  # type: ignore[assignment]
    SafeStatePolicy = None  # type: ignore[assignment]
    SimulationUpdateLoop = None  # type: ignore[assignment]
    Snap7Transport = None  # type: ignore[assignment]
    LoopHealth = None  # type: ignore[assignment]
    load_config = None  # type: ignore[assignment]
    update_log_record = None  # type: ignore[assignment]
    ADAPTER_IMPORT_ERROR: str | None = str(exc)
else:
    ADAPTER_IMPORT_ERROR = None


_SCENE_ID = re.compile(r"^[a-z0-9][a-z0-9_-]{0,95}$")
_MAX_POINT_COUNT = 256
_REQUIRED_LIVE_STATUS_TAGS = (
    "simulation_enable",
    "simulation_comm_ok",
    "simulation_timeout",
)


class LivePlcError(RuntimeError):
    """The guarded live PLC session rejected or failed an operation."""


def _require_adapter() -> None:
    if (
        ADAPTER_IMPORT_ERROR is not None
        or load_config is None
        or InterfaceRuntime is None
        or SimulationUpdateLoop is None
        or Snap7Transport is None
    ):
        raise LivePlcError(
            "The guarded live PLC adapter is unavailable in this build. "
            f"Import error: {ADAPTER_IMPORT_ERROR}"
        )


def _profile_path(profile_dir: Path, profile_id: object) -> Path:
    if not isinstance(profile_id, str) or not profile_id:
        raise LivePlcError("profileId must be one local file name.")
    if Path(profile_id).name != profile_id:
        raise LivePlcError("profileId must be one local file name.")
    if not profile_id.lower().endswith(".json"):
        raise LivePlcError("PLC profile must be a .json file.")
    resolved_dir = profile_dir.resolve()
    candidate = (resolved_dir / profile_id).resolve()
    if candidate.parent != resolved_dir:
        raise LivePlcError("PLC profile path escaped the profile folder.")
    if not candidate.is_file():
        raise LivePlcError(f"PLC profile was not found: {profile_id}")
    return candidate


def _scope_item(tag: Any) -> dict[str, str]:
    return {
        "name": tag.name,
        "symbol": tag.plc_symbol,
        "address": tag.address,
        "dataType": tag.data_type.value,
    }


def _point_scope_item(point: Any, tag: Any) -> dict[str, str]:
    return {
        "name": point.name,
        "tag": tag.name,
        "address": tag.address,
        "dataType": tag.data_type.value,
    }


def _profile_descriptor(
    profile_id: str,
    config: InterfaceConfig,
) -> dict[str, Any]:
    assert Direction is not None
    write_scope = [
        _scope_item(tag)
        for tag in config.tags
        if tag.direction is Direction.PC_TO_PLC
    ]
    read_scope = [
        _scope_item(tag)
        for tag in config.tags
        if tag.direction is Direction.PLC_TO_PC
    ]
    pc_point_scope = [
        _point_scope_item(point, config.tag(point.tag))
        for point in config.points
        if config.tag(point.tag).direction is Direction.PC_TO_PLC
    ]
    plc_point_scope = [
        _point_scope_item(point, config.tag(point.tag))
        for point in config.points
        if config.tag(point.tag).direction is Direction.PLC_TO_PC
    ]
    return {
        "id": profile_id,
        "cpuFamily": config.connection.cpu_family,
        "ip": config.connection.ip,
        "rack": config.connection.rack,
        "slot": config.connection.slot,
        "cycleMs": config.connection.cycle_ms,
        "connectTimeoutMs": config.connection.connect_timeout_ms,
        "heartbeatTimeoutMs": config.heartbeat.timeout_ms,
        "writeScope": write_scope,
        "readScope": read_scope,
        "pcPointScope": pc_point_scope,
        "plcPointScope": plc_point_scope,
    }


def _validate_live_profile_contract(config: InterfaceConfig) -> None:
    """Require the status bits used to fail closed in the browser."""

    assert Direction is not None
    for name in _REQUIRED_LIVE_STATUS_TAGS:
        try:
            tag = config.tag(name)
        except KeyError as exc:
            raise LivePlcError(
                f'Live PLC profile is missing required status tag "{name}".'
            ) from exc
        if (
            tag.direction is not Direction.PLC_TO_PC
            or tag.data_type.value != "BOOL"
        ):
            raise LivePlcError(
                f'Live PLC status tag "{name}" must be a PLC-to-PC BOOL.'
            )


class LivePlcController:
    """Serialize one explicitly authorized live PLC session."""

    def __init__(
        self,
        profile_dir: Path,
        *,
        transport_factory: Callable[[], Any] | None = None,
        monotonic: Callable[[], float] = time.monotonic,
    ) -> None:
        _require_adapter()
        self._profile_dir = profile_dir
        self._transport_factory = transport_factory or Snap7Transport
        self._monotonic = monotonic
        self._lock = threading.RLock()
        self._runtime: Any | None = None
        self._loop: Any | None = None
        self._config: InterfaceConfig | None = None
        self._descriptor: dict[str, Any] | None = None
        self._session_id: str | None = None
        self._scene_id: str | None = None
        self._last_cycle_at: float | None = None

    def _load_profile(
        self,
        profile_id: str,
    ) -> tuple[InterfaceConfig, dict[str, Any]]:
        assert load_config is not None
        path = _profile_path(self._profile_dir, profile_id)
        try:
            config = load_config(path)
        except (OSError, ConfigError, ValueError) as exc:
            raise LivePlcError(str(exc)) from exc
        if not config.points:
            raise LivePlcError(
                "Live PLC profiles require typed scene points (schema v2)."
            )
        _validate_live_profile_contract(config)
        return config, _profile_descriptor(path.name, config)

    def describe_profile(self, profile_id: str) -> dict[str, Any]:
        """Return the exact read/write scope without connecting."""

        _config, descriptor = self._load_profile(profile_id)
        return descriptor

    def connect(
        self,
        *,
        profile_id: str,
        scene_id: str,
        execute: bool,
        authorized_write_scope: object,
    ) -> dict[str, Any]:
        """Connect only after the caller confirms the exact write scope."""

        if execute is not True:
            raise LivePlcError(
                "Live PLC connection requires explicit execute authorization."
            )
        if not isinstance(scene_id, str) or not _SCENE_ID.fullmatch(scene_id):
            raise LivePlcError("A valid scene id is required.")
        config, descriptor = self._load_profile(profile_id)
        if authorized_write_scope != descriptor["writeScope"]:
            raise LivePlcError(
                "Authorized write scope does not exactly match the profile."
            )

        with self._lock:
            if self._runtime is not None:
                raise LivePlcError("A live PLC session is already connected.")
            assert InterfaceRuntime is not None
            assert SimulationUpdateLoop is not None
            assert SafeStatePolicy is not None
            assert self._transport_factory is not None
            runtime = InterfaceRuntime(
                config,
                self._transport_factory(),
                safe_state_policy=SafeStatePolicy.PLC_WATCHDOG_ONLY,
                start_time=self._monotonic(),
            )
            try:
                runtime.connect()
                update_loop = SimulationUpdateLoop(runtime)
            except Exception as exc:
                runtime.close()
                raise LivePlcError(
                    f"Could not start guarded PLC session: "
                    f"{type(exc).__name__}: {exc}"
                ) from exc

            self._runtime = runtime
            self._loop = update_loop
            self._config = config
            self._descriptor = descriptor
            self._session_id = secrets.token_urlsafe(24)
            self._scene_id = scene_id
            self._last_cycle_at = self._monotonic()
            return {
                **descriptor,
                "sessionId": self._session_id,
                "sceneId": scene_id,
                "connected": True,
            }

    def cycle(
        self,
        *,
        session_id: str,
        scene_id: str,
        pc_points: Mapping[str, bool | int | float],
    ) -> dict[str, Any]:
        """Run one typed plant-to-PLC-to-plant exchange."""

        if not isinstance(session_id, str) or not session_id:
            raise LivePlcError("sessionId must be a non-empty string.")
        if not isinstance(pc_points, Mapping):
            raise LivePlcError("pcPoints must be an object.")
        if len(pc_points) > _MAX_POINT_COUNT:
            raise LivePlcError("pcPoints exceeds the configured point limit.")

        with self._lock:
            if (
                self._runtime is None
                or self._loop is None
                or self._config is None
                or self._session_id is None
            ):
                raise LivePlcError("No live PLC session is connected.")
            if not secrets.compare_digest(session_id, self._session_id):
                raise LivePlcError("Live PLC session id is invalid or stale.")
            if scene_id != self._scene_id:
                self._disconnect_locked()
                raise LivePlcError(
                    "Scene changed during live PLC exchange; session closed."
                )

            assert self._descriptor is not None
            expected_pc_points = {
                item["name"]
                for item in self._descriptor["pcPointScope"]
            }
            supplied_pc_points = set(pc_points)
            if supplied_pc_points != expected_pc_points:
                missing = sorted(expected_pc_points - supplied_pc_points)
                extra = sorted(supplied_pc_points - expected_pc_points)
                self._disconnect_locked()
                raise LivePlcError(
                    "pcPoints must exactly match the active scene contract; "
                    f"missing={missing}, extra={extra}. Session closed."
                )

            try:
                result = self._loop.step(self._monotonic(), pc_points)
                record = update_log_record(result)
            except Exception as exc:
                self._disconnect_locked()
                raise LivePlcError(
                    f"Guarded PLC cycle failed and disconnected: "
                    f"{type(exc).__name__}: {exc}"
                ) from exc

            self._last_cycle_at = self._monotonic()
            plc_points = {
                name: sample.value
                for name, sample in result.plc_point_samples.items()
            }
            point_tags = {
                point.tag
                for point in self._config.points
            }
            plc_status = {
                name: value
                for name, value in result.cycle.plc_values_read.items()
                if name not in point_tags
            }
            response = {
                "sessionId": self._session_id,
                "sceneId": self._scene_id,
                "cycle": result.cycle.cycle_number,
                "health": result.health.value,
                "heartbeat": record["heartbeat"],
                "plcPoints": plc_points,
                "plcStatus": plc_status,
                "diagnostics": record["diagnostics"],
            }
            if result.health is LoopHealth.FAULT:
                self._disconnect_locked()
                response["connected"] = False
                response["closedReason"] = "PLC cycle health fault"
            else:
                response["connected"] = True
            return response

    def disconnect(self, *, session_id: str | None = None) -> dict[str, Any]:
        """Close the active session; an optional id prevents stale teardown."""

        with self._lock:
            if self._runtime is None:
                return {"connected": False, "closed": False}
            if (
                session_id is not None
                and self._session_id is not None
                and not secrets.compare_digest(session_id, self._session_id)
            ):
                raise LivePlcError("Live PLC session id is invalid or stale.")
            result = self._disconnect_locked()
            return {"connected": False, "closed": True, **result}

    def expire_stale(self) -> bool:
        """Close a session when browser-driven cycles stop before the watchdog."""

        with self._lock:
            if (
                self._runtime is None
                or self._config is None
                or self._last_cycle_at is None
            ):
                return False
            stale_after = self._config.heartbeat.timeout_ms / 1_000
            if self._monotonic() - self._last_cycle_at <= stale_after:
                return False
            self._disconnect_locked()
            return True

    def _disconnect_locked(self) -> dict[str, Any]:
        runtime = self._runtime
        self._runtime = None
        self._loop = None
        self._config = None
        self._descriptor = None
        self._session_id = None
        self._scene_id = None
        self._last_cycle_at = None
        if runtime is None:
            return {}
        result = runtime.close()
        return {
            "safeStateAttempted": result.safe_state_attempted,
            "safeStateSucceeded": result.safe_state_succeeded,
            "safeStateError": result.safe_state_error,
            "disconnectError": result.disconnect_error,
        }

    def close(self) -> None:
        with self._lock:
            self._disconnect_locked()
