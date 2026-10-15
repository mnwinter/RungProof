"""Read-only PLC setup diagnostics for RungProof.

This module deliberately imports only the validated configuration model and
Snap7 transport from the established Siemens PLC/PC interface project.  It
never creates InterfaceRuntime and never calls a transport write method.
"""

from __future__ import annotations

from pathlib import Path
import sys
import time
from typing import Any, Callable


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
        load_config,
    )
    from siemens_plc_pc_interface.transport import Snap7Transport
    from snap7 import Client as _Snap7Client
except ImportError as exc:  # pragma: no cover - exercised by packaged fallback.
    ConfigError = ValueError  # type: ignore[assignment,misc]
    Direction = None  # type: ignore[assignment]
    load_config = None  # type: ignore[assignment]
    Snap7Transport = None  # type: ignore[assignment]
    _Snap7Client = None  # type: ignore[assignment]
    ADAPTER_IMPORT_ERROR: str | None = str(exc)
else:
    ADAPTER_IMPORT_ERROR = None


class PlcDiagnosticError(RuntimeError):
    """A profile or adapter problem prevented the read-only test."""


def _require_adapter() -> None:
    if (
        ADAPTER_IMPORT_ERROR is not None
        or load_config is None
        or _Snap7Client is None
    ):
        raise PlcDiagnosticError(
            "The guarded PLC diagnostic adapter is not installed in this "
            f"player build. Import error: {ADAPTER_IMPORT_ERROR}"
        )


def _profile_path(profile_dir: Path, profile_id: str) -> Path:
    if not profile_id or Path(profile_id).name != profile_id:
        raise PlcDiagnosticError("PLC profile id must be one local file name.")
    if not profile_id.lower().endswith(".json"):
        raise PlcDiagnosticError("PLC profile must be a .json file.")
    resolved_dir = profile_dir.resolve()
    candidate = (resolved_dir / profile_id).resolve()
    if candidate.parent != resolved_dir:
        raise PlcDiagnosticError("PLC profile path escaped the profile folder.")
    if not candidate.is_file():
        raise PlcDiagnosticError(f"PLC profile was not found: {profile_id}")
    return candidate


def _profile_label(path: Path) -> str:
    return path.stem.replace("-", " ").replace("_", " ").title()


def list_plc_profiles(profile_dir: Path) -> list[dict[str, Any]]:
    """Return validated local PLC profiles without opening a PLC connection."""

    profile_dir.mkdir(parents=True, exist_ok=True)
    descriptors: list[dict[str, Any]] = []
    for path in sorted(profile_dir.glob("*.json")):
        descriptor: dict[str, Any] = {
            "id": path.name,
            "label": _profile_label(path),
            "valid": False,
            "readOnly": True,
        }
        try:
            _require_adapter()
            assert load_config is not None
            config = load_config(path)
        except (OSError, ValueError, PlcDiagnosticError) as exc:
            descriptor["error"] = str(exc)
        else:
            descriptor.update(
                {
                    "valid": True,
                    "ip": config.connection.ip,
                    "rack": config.connection.rack,
                    "slot": config.connection.slot,
                    "tagCount": len(config.tags),
                    "writeCount": 0,
                }
            )
        descriptors.append(descriptor)
    return descriptors


def _item(
    status: str,
    label: str,
    detail: str,
    fix: str | None = None,
) -> dict[str, str]:
    item = {"status": status, "label": label, "detail": detail}
    if fix:
        item["fix"] = fix
    return item


def _display_value(value: object) -> str:
    if isinstance(value, bool):
        return "TRUE" if value else "FALSE"
    return repr(value)


def run_read_only_plc_test(
    profile_dir: Path,
    profile_id: str,
    *,
    transport_factory: Callable[[], Any] | None = None,
    wait: Callable[[float], None] = time.sleep,
) -> dict[str, Any]:
    """Connect, read every configured tag twice, and disconnect without writes."""

    _require_adapter()
    profile_path = _profile_path(profile_dir, profile_id)
    assert load_config is not None
    config = load_config(profile_path)
    if transport_factory is None:
        if Snap7Transport is None:
            raise PlcDiagnosticError("Snap7 transport is unavailable.")
        transport_factory = Snap7Transport

    started = time.perf_counter()
    items: list[dict[str, str]] = []
    transport = transport_factory()
    connected = False
    connect_attempted = False
    first_values: dict[str, Any] = {}
    second_values: dict[str, Any] = {}

    try:
        connect_attempted = True
        transport.connect(config.connection)
        connected = True
        items.append(
            _item(
                "pass",
                "S7 connection",
                (
                    f"Connected read-only to {config.connection.ip}, "
                    f"rack {config.connection.rack}, slot "
                    f"{config.connection.slot}."
                ),
            )
        )
        first_values = transport.read_many_diagnostic(config.tags)
        sample_seconds = min(
            max(config.connection.cycle_ms * 3 / 1000.0, 0.10),
            0.25,
        )
        wait(sample_seconds)
        second_values = transport.read_many_diagnostic(config.tags)
    except Exception as exc:
        items.append(
            _item(
                "fail",
                "Configured tag readback",
                f"{type(exc).__name__}: {exc}",
                (
                    "Confirm the PLC IP, TCP port 102, rack/slot, CPU RUN "
                    "state, PUT/GET permission, and a standard non-optimized "
                    "DB14 matching this profile."
                ),
            )
        )
    finally:
        if connect_attempted:
            try:
                transport.disconnect()
            except Exception as exc:
                items.append(
                    _item(
                        "fail",
                        "S7 disconnect",
                        f"{type(exc).__name__}: {exc}",
                        "Confirm the PLC/network path is stable, then test again.",
                    )
                )

    if second_values:
        for tag in config.tags:
            if tag.name not in second_values:
                items.append(
                    _item(
                        "fail",
                        f"{tag.address} {tag.plc_symbol}",
                        "No value was returned.",
                        (
                            f"Verify {tag.address} exists as "
                            f"{tag.data_type.value} in the standard, "
                            "non-optimized simulation DB."
                        ),
                    )
                )
                continue
            items.append(
                _item(
                    "pass",
                    f"{tag.address} {tag.plc_symbol}",
                    (
                        f"read={_display_value(second_values[tag.name])}; "
                        f"configured as {tag.data_type.value} "
                        f"{tag.direction.value}"
                    ),
                )
            )

        status_expectations = (
            (
                "simulation_enable",
                True,
                "fail",
                "Set Simulation_Enable TRUE in TIA.",
            ),
            (
                "simulation_comm_ok",
                True,
                "warning",
                (
                    "This diagnostic is read-only, so COMM_OK may remain FALSE "
                    "until the authorized guarded writer advances heartbeat."
                ),
            ),
            (
                "simulation_timeout",
                False,
                "warning",
                (
                    "This diagnostic is read-only, so TIMEOUT may remain TRUE "
                    "until the authorized guarded writer advances heartbeat."
                ),
            ),
        )
        for tag_name, expected, mismatch_status, fix in status_expectations:
            if tag_name not in second_values:
                continue
            actual = second_values[tag_name]
            passed = actual is expected
            items.append(
                _item(
                    "pass" if passed else mismatch_status,
                    tag_name,
                    (
                        f"read={_display_value(actual)}, "
                        f"expected={_display_value(expected)}"
                    ),
                    None if passed else fix,
                )
            )

        pc_name = config.heartbeat.pc_tag
        echo_name = config.heartbeat.echo_tag
        if (
            pc_name in first_values
            and pc_name in second_values
            and echo_name in first_values
            and echo_name in second_values
        ):
            pc_changed = first_values[pc_name] != second_values[pc_name]
            echo_changed = first_values[echo_name] != second_values[echo_name]
            if pc_changed and echo_changed:
                items.append(
                    _item(
                        "pass",
                        "Heartbeat progress",
                        (
                            f"{pc_name} {_display_value(first_values[pc_name])}"
                            f" -> {_display_value(second_values[pc_name])}; "
                            f"{echo_name} "
                            f"{_display_value(first_values[echo_name])} -> "
                            f"{_display_value(second_values[echo_name])}"
                        ),
                    )
                )
            else:
                items.append(
                    _item(
                        "warning",
                        "Heartbeat progress",
                        (
                            f"No progress during the read-only sample: "
                            f"{pc_name}={_display_value(second_values[pc_name])}, "
                            f"{echo_name}="
                            f"{_display_value(second_values[echo_name])}."
                        ),
                        (
                            "This test never writes the PC heartbeat. Start the "
                            "authorized guarded live runtime if heartbeat "
                            "progress must be proven."
                        ),
                    )
                )

        items.append(
            _item(
                "info",
                "PC-owned value comparison",
                (
                    "Not run because this player is not yet the authorized "
                    "live point-binding writer."
                ),
            )
        )

    failures = sum(item["status"] == "fail" for item in items)
    warnings = sum(item["status"] == "warning" for item in items)
    passed = sum(item["status"] == "pass" for item in items)
    if failures:
        result_status = "failed"
        summary = f"FIX REQUIRED — {failures} check(s) failed."
    elif warnings:
        result_status = "passed_with_warnings"
        summary = (
            f"READBACK PASSED — {passed} checks passed with "
            f"{warnings} warning(s)."
        )
    else:
        result_status = "passed"
        summary = f"PLC TEST PASSED — {passed} checks passed."

    return {
        "status": result_status,
        "summary": summary,
        "profile": {
            "id": profile_path.name,
            "label": _profile_label(profile_path),
            "ip": config.connection.ip,
            "rack": config.connection.rack,
            "slot": config.connection.slot,
            "tagCount": len(config.tags),
        },
        "items": items,
        "readOnly": True,
        "writeAttempted": False,
        "durationMs": round((time.perf_counter() - started) * 1000, 1),
    }
