"""Behavior tests for RungProof's guarded live PLC session."""

from __future__ import annotations

import json
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from tools.plc_live import LivePlcController, LivePlcError


PROFILE_DIR = Path(__file__).resolve().parents[1] / "prototype" / "plc-profiles"


class FakeLiveTransport:
    """In-memory PLC transport proving the public live-session boundary."""

    instances: list["FakeLiveTransport"] = []

    def __init__(self) -> None:
        self._connected = False
        self.writes: list[tuple[str, object]] = []
        self.disconnect_count = 0
        self.plc_values: dict[str, object] = {
            "conveyor_running": False,
            "pusher_extend": False,
            "plc_heartbeat_echo": 0,
            "simulation_enable": True,
            "simulation_comm_ok": False,
            "simulation_timeout": True,
        }
        self.__class__.instances.append(self)

    @property
    def connected(self) -> bool:
        return self._connected

    def connect(self, _connection: object) -> None:
        self._connected = True

    def read_many(self, tags: tuple[object, ...]) -> dict[str, object]:
        if not self._connected:
            raise AssertionError("read occurred before connect")
        return {tag.name: self.plc_values[tag.name] for tag in tags}

    def write_many(self, values: list[tuple[object, object]]) -> None:
        if not self._connected:
            raise AssertionError("write occurred before connect")
        self.writes.extend((tag.name, value) for tag, value in values)

    def disconnect(self) -> None:
        self.disconnect_count += 1
        self._connected = False


class LivePlcControllerTests(unittest.TestCase):
    def setUp(self) -> None:
        FakeLiveTransport.instances.clear()
        self.clock = 0.0
        self.controller = LivePlcController(
            PROFILE_DIR,
            transport_factory=FakeLiveTransport,
            monotonic=lambda: self.clock,
        )

    def tearDown(self) -> None:
        self.controller.close()

    def test_authorized_scene_two_session_exchanges_only_declared_points(
        self,
    ) -> None:
        profile_id = "scene-2-db14-pusher-interface.json"
        descriptor = self.controller.describe_profile(profile_id)

        connected = self.controller.connect(
            profile_id=profile_id,
            scene_id="scene-2-conveyor-pusher",
            execute=True,
            authorized_write_scope=descriptor["writeScope"],
        )
        first = self.controller.cycle(
            session_id=connected["sessionId"],
            scene_id="scene-2-conveyor-pusher",
            pc_points={
                "part_at_pusher": False,
                "pusher_extended": False,
                "pusher_retracted": True,
            },
        )

        transport = FakeLiveTransport.instances[-1]
        self.assertEqual(first["health"], "starting")
        self.assertEqual(
            {name for name, _value in transport.writes},
            {
                "simulated_photoeye",
                "simulated_pusher_extended",
                "simulated_pusher_retracted",
                "pc_heartbeat",
            },
        )
        self.assertEqual(
            first["plcPoints"],
            {
                "conveyor_running": False,
                "pusher_extend": False,
            },
        )

        transport.plc_values["conveyor_running"] = True
        transport.plc_values["pusher_extend"] = True
        transport.plc_values["plc_heartbeat_echo"] = 1
        transport.plc_values["simulation_comm_ok"] = True
        transport.plc_values["simulation_timeout"] = False
        self.clock = 0.1
        second = self.controller.cycle(
            session_id=connected["sessionId"],
            scene_id="scene-2-conveyor-pusher",
            pc_points={
                "part_at_pusher": True,
                "pusher_extended": False,
                "pusher_retracted": True,
            },
        )

        self.assertEqual(second["health"], "healthy")
        self.assertEqual(
            [
                value
                for name, value in transport.writes
                if name == "simulated_photoeye"
            ],
            [False, True],
            "part_at_pusher must reach the DB14.DBX0.0 transport write as a "
            "FALSE-to-TRUE transition",
        )
        self.assertEqual(
            second["plcPoints"],
            {
                "conveyor_running": True,
                "pusher_extend": True,
            },
        )

    def test_profile_descriptor_exposes_scene_point_to_db_mapping(self) -> None:
        descriptor = self.controller.describe_profile(
            "scene-1-db14-interface.json"
        )

        self.assertEqual(
            descriptor["pcPointScope"],
            [
                {
                    "name": "simulated_photoeye",
                    "tag": "pc_to_plc",
                    "address": "DB14.DBX0.0",
                    "dataType": "BOOL",
                }
            ],
        )
        self.assertEqual(
            descriptor["plcPointScope"],
            [
                {
                    "name": "conveyor_running",
                    "tag": "plc_to_pc",
                    "address": "DB14.DBX0.1",
                    "dataType": "BOOL",
                }
            ],
        )

    def test_profile_missing_required_live_status_tag_is_rejected(self) -> None:
        source = (
            PROFILE_DIR / "scene-2-db14-pusher-interface.json"
        )
        document = json.loads(source.read_text(encoding="utf-8"))
        document["tags"] = [
            tag
            for tag in document["tags"]
            if tag["name"] != "simulation_comm_ok"
        ]

        with TemporaryDirectory() as temp_dir:
            profile_path = Path(temp_dir) / source.name
            profile_path.write_text(
                json.dumps(document),
                encoding="utf-8",
            )
            controller = LivePlcController(
                Path(temp_dir),
                transport_factory=FakeLiveTransport,
                monotonic=lambda: self.clock,
            )
            with self.assertRaisesRegex(
                LivePlcError,
                "simulation_comm_ok",
            ):
                controller.describe_profile(source.name)

    def test_scope_mismatch_is_rejected_before_the_transport_connects(
        self,
    ) -> None:
        profile_id = "scene-2-db14-pusher-interface.json"
        descriptor = self.controller.describe_profile(profile_id)
        incomplete_scope = descriptor["writeScope"][:-1]

        with self.assertRaisesRegex(LivePlcError, "exactly match"):
            self.controller.connect(
                profile_id=profile_id,
                scene_id="scene-2-conveyor-pusher",
                execute=True,
                authorized_write_scope=incomplete_scope,
            )

        self.assertEqual(FakeLiveTransport.instances, [])

    def test_malformed_profile_and_session_ids_fail_closed(self) -> None:
        with self.assertRaisesRegex(LivePlcError, "profileId"):
            self.controller.connect(
                profile_id=7,  # type: ignore[arg-type]
                scene_id="scene-2-conveyor-pusher",
                execute=True,
                authorized_write_scope=[],
            )
        self.assertEqual(FakeLiveTransport.instances, [])

        profile_id = "scene-2-db14-pusher-interface.json"
        descriptor = self.controller.describe_profile(profile_id)
        self.controller.connect(
            profile_id=profile_id,
            scene_id="scene-2-conveyor-pusher",
            execute=True,
            authorized_write_scope=descriptor["writeScope"],
        )
        with self.assertRaisesRegex(LivePlcError, "sessionId"):
            self.controller.cycle(
                session_id=7,  # type: ignore[arg-type]
                scene_id="scene-2-conveyor-pusher",
                pc_points={},
            )

        self.assertTrue(FakeLiveTransport.instances[-1].connected)

    def test_browser_cycle_timeout_closes_the_transport(self) -> None:
        profile_id = "scene-2-db14-pusher-interface.json"
        descriptor = self.controller.describe_profile(profile_id)
        self.controller.connect(
            profile_id=profile_id,
            scene_id="scene-2-conveyor-pusher",
            execute=True,
            authorized_write_scope=descriptor["writeScope"],
        )
        transport = FakeLiveTransport.instances[-1]

        self.clock = 1.001
        expired = self.controller.expire_stale()

        self.assertTrue(expired)
        self.assertFalse(transport.connected)
        self.assertEqual(transport.disconnect_count, 1)

    def test_partial_pc_point_cycle_fails_closed_and_disconnects(self) -> None:
        profile_id = "scene-2-db14-pusher-interface.json"
        descriptor = self.controller.describe_profile(profile_id)
        connected = self.controller.connect(
            profile_id=profile_id,
            scene_id="scene-2-conveyor-pusher",
            execute=True,
            authorized_write_scope=descriptor["writeScope"],
        )
        transport = FakeLiveTransport.instances[-1]

        with self.assertRaisesRegex(LivePlcError, "exactly match"):
            self.controller.cycle(
                session_id=connected["sessionId"],
                scene_id="scene-2-conveyor-pusher",
                pc_points={"part_at_pusher": True},
            )

        self.assertFalse(transport.connected)
        self.assertEqual(transport.writes, [])

    def test_scene_change_rejects_the_cycle_and_closes_the_transport(
        self,
    ) -> None:
        profile_id = "scene-2-db14-pusher-interface.json"
        descriptor = self.controller.describe_profile(profile_id)
        connected = self.controller.connect(
            profile_id=profile_id,
            scene_id="scene-2-conveyor-pusher",
            execute=True,
            authorized_write_scope=descriptor["writeScope"],
        )
        transport = FakeLiveTransport.instances[-1]

        with self.assertRaisesRegex(LivePlcError, "Scene changed"):
            self.controller.cycle(
                session_id=connected["sessionId"],
                scene_id="lab-2-01-workstation-call",
                pc_points={},
            )

        self.assertFalse(transport.connected)
        self.assertEqual(transport.writes, [])


if __name__ == "__main__":
    unittest.main()
