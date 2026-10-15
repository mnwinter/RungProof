"""Offline tests for RungProof's read-only PLC diagnostic."""

from __future__ import annotations

from pathlib import Path
import unittest

try:
    from .plc_diagnostics import (
        PlcDiagnosticError,
        list_plc_profiles,
        run_read_only_plc_test,
    )
except ImportError:
    from plc_diagnostics import (
        PlcDiagnosticError,
        list_plc_profiles,
        run_read_only_plc_test,
    )


PROFILE_DIR = Path(__file__).resolve().parents[1] / "prototype" / "plc-profiles"


class FakeReadOnlyTransport:
    instances: list["FakeReadOnlyTransport"] = []

    def __init__(self) -> None:
        self.connected = False
        self.read_count = 0
        self.write_count = 0
        self.__class__.instances.append(self)

    def connect(self, _connection: object) -> None:
        self.connected = True

    def read_many_diagnostic(self, tags: tuple[object, ...]) -> dict[str, object]:
        if not self.connected:
            raise AssertionError("diagnostic read occurred before connect")
        self.read_count += 1
        counter = self.read_count
        values: dict[str, object] = {}
        for tag in tags:
            data_type = tag.data_type.value
            if data_type == "BOOL":
                values[tag.name] = False
            elif data_type == "REAL":
                values[tag.name] = 0.0
            else:
                values[tag.name] = 0
        values["pc_heartbeat"] = counter
        values["plc_heartbeat_echo"] = counter
        values["simulation_enable"] = True
        values["simulation_comm_ok"] = True
        values["simulation_timeout"] = False
        return values

    def write(self, *_args: object) -> None:
        self.write_count += 1
        raise AssertionError("read-only diagnostic attempted a PLC write")

    def write_many(self, *_args: object) -> None:
        self.write_count += 1
        raise AssertionError("read-only diagnostic attempted a PLC batch write")

    def disconnect(self) -> None:
        self.connected = False


class FailingConnectionTransport(FakeReadOnlyTransport):
    def connect(self, _connection: object) -> None:
        raise OSError("offline test connection failure")

class ReadOnlyWatchdogTransport(FakeReadOnlyTransport):
    def read_many_diagnostic(
        self,
        tags: tuple[object, ...],
    ) -> dict[str, object]:
        values = super().read_many_diagnostic(tags)
        values["simulation_comm_ok"] = False
        values["simulation_timeout"] = True
        values["pc_heartbeat"] = 0
        values["plc_heartbeat_echo"] = 0
        return values


class PartiallyOpenTransport(FakeReadOnlyTransport):
    disconnect_attempted = False

    def connect(self, _connection: object) -> None:
        self.connected = True
        raise OSError("connection opened then handshake failed")

    def disconnect(self) -> None:
        self.__class__.disconnect_attempted = True
        self.connected = False


class PlcDiagnosticsTests(unittest.TestCase):
    def setUp(self) -> None:
        FakeReadOnlyTransport.instances.clear()
        PartiallyOpenTransport.disconnect_attempted = False

    def test_profiles_are_validated_without_connecting(self) -> None:
        profiles = list_plc_profiles(PROFILE_DIR)

        self.assertEqual(len(profiles), 2)
        self.assertTrue(all(profile["valid"] for profile in profiles))
        self.assertTrue(all(profile["readOnly"] for profile in profiles))
        self.assertTrue(all(profile["writeCount"] == 0 for profile in profiles))
        self.assertEqual(FakeReadOnlyTransport.instances, [])

    def test_read_only_test_reads_twice_and_never_writes(self) -> None:
        result = run_read_only_plc_test(
            PROFILE_DIR,
            "scene-1-db14-interface.json",
            transport_factory=FakeReadOnlyTransport,
            wait=lambda _seconds: None,
        )

        transport = FakeReadOnlyTransport.instances[-1]
        self.assertEqual(transport.read_count, 2)
        self.assertEqual(transport.write_count, 0)
        self.assertFalse(transport.connected)
        self.assertEqual(result["status"], "passed")
        self.assertTrue(result["readOnly"])
        self.assertFalse(result["writeAttempted"])
        self.assertFalse(
            any(item["status"] == "fail" for item in result["items"])
        )

    def test_connection_failure_is_actionable_and_still_never_writes(self) -> None:
        result = run_read_only_plc_test(
            PROFILE_DIR,
            "scene-1-db14-interface.json",
            transport_factory=FailingConnectionTransport,
            wait=lambda _seconds: None,
        )

        transport = FakeReadOnlyTransport.instances[-1]
        self.assertEqual(transport.write_count, 0)
        self.assertEqual(result["status"], "failed")
        self.assertFalse(result["writeAttempted"])
        failure = next(
            item for item in result["items"] if item["status"] == "fail"
        )
        self.assertIn("offline test connection failure", failure["detail"])
        self.assertIn("TCP port 102", failure["fix"])

    def test_profile_path_cannot_escape_profile_folder(self) -> None:
        with self.assertRaises(PlcDiagnosticError):
            run_read_only_plc_test(
                PROFILE_DIR,
                "..\\scene-1-db14-interface.json",
                transport_factory=FakeReadOnlyTransport,
                wait=lambda _seconds: None,
            )

    def test_read_only_watchdog_state_is_warning_not_false_failure(self) -> None:
        result = run_read_only_plc_test(
            PROFILE_DIR,
            "scene-1-db14-interface.json",
            transport_factory=ReadOnlyWatchdogTransport,
            wait=lambda _seconds: None,
        )

        self.assertEqual(result["status"], "passed_with_warnings")
        warning_labels = {
            item["label"]
            for item in result["items"]
            if item["status"] == "warning"
        }
        self.assertIn("simulation_comm_ok", warning_labels)
        self.assertIn("simulation_timeout", warning_labels)
        self.assertFalse(
            any(item["status"] == "fail" for item in result["items"])
        )

    def test_partial_connect_failure_still_attempts_disconnect(self) -> None:
        result = run_read_only_plc_test(
            PROFILE_DIR,
            "scene-1-db14-interface.json",
            transport_factory=PartiallyOpenTransport,
            wait=lambda _seconds: None,
        )

        self.assertEqual(result["status"], "failed")
        self.assertTrue(PartiallyOpenTransport.disconnect_attempted)


if __name__ == "__main__":
    unittest.main()
