"""Focused lifecycle tests for the native Qt window."""

from __future__ import annotations

import importlib.util
import inspect
import json
import os
from pathlib import Path
import tempfile
import threading
import time
from types import SimpleNamespace
import unittest


os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")

from tools.rungproof_native import (  # noqa: E402
    CONVEYOR_DRIVE_LAYOUT,
    ConnectionState,
    _qt_imports,
    _capture_native_views,
    _VisualQaSession,
    _write_self_test,
    build_window_class,
    run_gui,
)
from tools.native_software_viewport import (  # noqa: E402
    SoftwareScene2Viewport,
)
from tools.native_event_history import EventSeverity  # noqa: E402


class StubViewport:
    def __init__(self, qt: dict[str, object]) -> None:
        self.container = qt["QtWidgets"].QWidget()  # type: ignore[attr-defined]

    def update(self, _snapshot: object) -> None:
        return


class StubReviewViewport(StubViewport):
    def __init__(self, qt: dict[str, object], definition: object) -> None:
        super().__init__(qt)
        self.definition = definition


class StubSession:
    def __init__(self) -> None:
        self.closed = False
        self.begin_close_called = False
        self.connect_calls = 0
        self.disconnect_calls = 0
        self.run_calls = 0
        self.step_calls = 0
        self.stop_calls = 0
        self.reset_calls = 0
        self.config = SimpleNamespace(
            connection=SimpleNamespace(
                ip="10.70.9.201",
                rack=0,
                slot=1,
            ),
            tags=(),
        )

    @property
    def is_closed(self) -> bool:
        return self.closed

    def begin_close(self) -> None:
        self.begin_close_called = True

    def connect(self) -> None:
        self.connect_calls += 1

    def disconnect(self) -> None:
        self.disconnect_calls += 1

    def run(self) -> None:
        self.run_calls += 1

    def step(self) -> None:
        self.step_calls += 1

    def stop(self) -> None:
        self.stop_calls += 1

    def reset(self) -> None:
        self.reset_calls += 1

    def close(self, timeout: float = 5.0) -> None:
        del timeout
        self.closed = True

    def snapshot(self) -> object:
        return SimpleNamespace(connection=ConnectionState.DISCONNECTED)


class NativeAssetGeometryContractTests(unittest.TestCase):
    def test_conveyor_drive_is_on_discharge_roller_centerline(self) -> None:
        drive_roller = CONVEYOR_DRIVE_LAYOUT["drive_roller_center"]
        gearbox = CONVEYOR_DRIVE_LAYOUT["gearbox_center"]
        motor = CONVEYOR_DRIVE_LAYOUT["motor_center"]

        self.assertEqual(CONVEYOR_DRIVE_LAYOUT["type"], "direct-head-drive")
        self.assertEqual(CONVEYOR_DRIVE_LAYOUT["shaft_axis"], "z")
        self.assertGreater(drive_roller[0], 0.0)
        self.assertEqual(gearbox[:2], drive_roller[:2])
        self.assertEqual(motor[:2], drive_roller[:2])
        self.assertGreater(gearbox[2], 0.75)
        self.assertGreater(motor[2], gearbox[2])

    def test_gui_entrypoint_shows_window_before_event_loop(self) -> None:
        source = inspect.getsource(run_gui)
        self.assertIn("window.show()", source)
        self.assertIn("return app.exec()", source)


@unittest.skipUnless(
    importlib.util.find_spec("PySide6") is not None,
    "PySide6 is installed only in the locked package environment",
)
class NativeQtLifecycleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.qt = _qt_imports()
        cls.app = (
            cls.qt["QtWidgets"].QApplication.instance()
            or cls.qt["QtWidgets"].QApplication([])
        )

    def test_viewport_failure_happens_before_session_creation(self) -> None:
        session_creations = 0

        def fail_viewport(_qt: object) -> object:
            raise RuntimeError("injected Qt 3D startup failure")

        def make_session() -> StubSession:
            nonlocal session_creations
            session_creations += 1
            return StubSession()

        window_class = build_window_class(
            self.qt,
            viewport_factory=fail_viewport,
            session_factory=make_session,
        )
        with self.assertRaisesRegex(
            RuntimeError,
            "injected Qt 3D startup failure",
        ):
            window_class()

        self.assertEqual(session_creations, 0)

    def test_constructor_closes_session_when_later_startup_fails(self) -> None:
        session = StubSession()

        def fail_workspace_client() -> object:
            raise RuntimeError("injected workspace startup failure")

        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            review_viewport_factory=StubReviewViewport,
            session_factory=lambda: session,
            workspace_client_factory=fail_workspace_client,
        )
        with self.assertRaisesRegex(
            RuntimeError,
            "injected workspace startup failure",
        ):
            window_class()

        self.assertTrue(session.closed)

    def test_fixture_workspace_is_hidden_from_normal_product_ui(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            review_viewport_factory=StubReviewViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()

        menu_titles = {
            action.text()
            for action in window.findChild(
                self.qt["QtWidgets"].QMenuBar,
                "applicationMenu",
            ).actions()
        }
        self.assertNotIn("Workspace", menu_titles)
        self.assertTrue(window.workspace_badge.isHidden())
        event_badges = {
            label.text()
            for label in window.event_history_panel.findChildren(
                self.qt["QtWidgets"].QLabel
            )
            if label.objectName() == "microBadge"
        }
        self.assertIn("LOCAL ONLY", event_badges)
        self.assertNotIn("workspace sync", window.event_history.text())

        session.closed = True
        window.close()

    def test_default_software_viewport_is_not_a_native_child_window(self) -> None:
        viewport = SoftwareScene2Viewport(self.qt)
        viewport.container.resize(720, 460)
        viewport.container.show()
        self.app.processEvents()

        self.assertFalse(
            viewport.container.testAttribute(
                self.qt["QtCore"].Qt.WidgetAttribute.WA_NativeWindow
            )
        )
        self.assertFalse(hasattr(viewport, "window"))
        image = viewport.container.grab().toImage()
        self.assertFalse(image.isNull())
        center = image.pixelColor(image.width() // 2, image.height() // 2)
        self.assertLess(center.lightness(), 240)

        viewport.container.close()

    def test_equipment_selection_updates_shell_without_plc_calls(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=SoftwareScene2Viewport,
            session_factory=lambda: session,
        )
        window = window_class(initial_view="A")
        window._timer.stop()
        window.viewport.update(
            SimpleNamespace(
                connection=ConnectionState.DISCONNECTED,
                ready=False,
                error=None,
                model=None,
            )
        )
        self.app.processEvents()
        target = next(
            target
            for target in window.viewport._targets
            if target.target_id == "main_conveyor"
        )
        window.viewport.container.equipmentSelected.emit(target)
        self.app.processEvents()

        self.assertIn("MAIN CONVEYOR", window.equipment_inspection.text())
        if window._compact_display:
            self.assertIn("SOURCE LOCAL MODEL", window.equipment_inspection.text())
        else:
            self.assertIn(
                "LOCAL MODEL / PLC STATE NOT VERIFIED",
                window.equipment_inspection.text(),
            )
        self.assertIn("TAG  conveyor_running", window.equipment_inspection.text())
        self.assertIn(
            "PERM NOT EVALUATED" if window._compact_display else "PERM CONTRACT  ·  NOT EVALUATED",
            window.equipment_inspection.text(),
        )
        self.assertIn("QUALITY OFFLINE", window.equipment_inspection.text())
        self.assertEqual(session.connect_calls, 0)
        self.assertEqual(session.run_calls, 0)
        self.assertEqual(session.stop_calls, 0)
        self.assertEqual(session.reset_calls, 0)

        session.closed = True
        window.close()

    def test_self_test_observes_default_renderer_and_diagnostic(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            report_path = Path(temp_dir) / "native-self-test.json"
            result = _write_self_test(report_path)
            report = json.loads(report_path.read_text(encoding="utf-8"))

        self.assertEqual(result, 0)
        self.assertEqual(report["rendererId"], "isometric-3d")
        self.assertFalse(report["nativeChildWindow"])
        self.assertTrue(report["readOnlyPlcTestAvailable"])
        self.assertFalse(report["browserEngine"])
        self.assertFalse(report["httpServer"])
        self.assertFalse(report["plcConnectionAttempted"])
        self.assertTrue(report["realPlcWritesEnabled"])

    def test_view_capture_writes_three_disconnected_views(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = Path(temp_dir)
            result = _capture_native_views(output_dir)
            report = json.loads(
                (output_dir / "CAPTURE-REPORT.json").read_text(
                    encoding="utf-8"
                )
            )

            self.assertEqual(result, 0)
            self.assertFalse(report["plcConnectionAttempted"])
            self.assertEqual(
                {entry["view"] for entry in report["captures"]},
                {"A", "B", "C"},
            )
            for entry in report["captures"]:
                self.assertTrue((output_dir / entry["file"]).is_file())

    def test_responsive_capture_records_requested_window_size(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = Path(temp_dir)
            result = _capture_native_views(
                output_dir,
                width=1280,
                height=720,
            )
            report = json.loads(
                (output_dir / "CAPTURE-REPORT.json").read_text(
                    encoding="utf-8"
                )
            )

        self.assertEqual(result, 0)
        self.assertEqual(
            report["captureSize"],
            {"width": 1280, "height": 720},
        )
        self.assertTrue(
            all(
                entry["width"] == 1280 and entry["height"] == 720
                for entry in report["captures"]
            )
        )

    def test_visual_qa_capture_can_prove_local_event_and_selection(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = Path(temp_dir)
            result = _capture_native_views(
                output_dir,
                width=1280,
                height=720,
                demo_event=True,
                demo_selection="main_conveyor",
                demo_workspace=True,
                demo_panel="events",
            )
            report = json.loads(
                (output_dir / "CAPTURE-REPORT.json").read_text(
                    encoding="utf-8"
                )
            )

        self.assertEqual(result, 0)
        self.assertTrue(report["demoEvent"])
        self.assertEqual(report["demoSelection"], "main_conveyor")
        self.assertTrue(report["demoWorkspace"])
        self.assertEqual(report["demoPanel"], "events")
        self.assertFalse(report["plcConnectionAttempted"])

    def test_visual_qa_plc_states_are_labeled_and_transportless(self) -> None:
        expected = {
            "disconnected": (ConnectionState.DISCONNECTED, False),
            "not-ready": (ConnectionState.CONNECTED, False),
            "healthy": (ConnectionState.CONNECTED, True),
        }
        for state, (connection, ready) in expected.items():
            session = _VisualQaSession(state)
            snapshot = session.snapshot()
            self.assertEqual(snapshot.connection, connection)
            self.assertEqual(snapshot.ready, ready)
            self.assertTrue(session.visual_qa_fixture)
            session.connect()
            session.run()
            session.reset()
            self.assertFalse(session.is_closed)
            session.close()
            self.assertTrue(session.is_closed)

        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = Path(temp_dir)
            result = _capture_native_views(
                output_dir,
                width=960,
                height=520,
                demo_plc_state="healthy",
            )
            report = json.loads(
                (output_dir / "CAPTURE-REPORT.json").read_text(
                    encoding="utf-8"
                )
            )
        self.assertEqual(result, 0)
        self.assertTrue(report["visualQaFixture"])
        self.assertEqual(report["demoPlcState"], "healthy")
        self.assertFalse(report["plcConnectionAttempted"])
        self.assertEqual(report["fixtureTransport"], "none")
        self.assertTrue(report["realPlcWritesCapabilityEnabled"])

    def test_equipment_gallery_opens_and_switches_representative_assets(
        self,
    ) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class(compact_display=False)
        window._timer.stop()
        dialog = window._build_equipment_gallery_dialog()
        asset_list = dialog.findChild(
            self.qt["QtWidgets"].QListWidget,
            "galleryAssetList",
        )
        details = dialog.findChild(
            self.qt["QtWidgets"].QTextBrowser,
            "galleryDetails",
        )
        asset_search = dialog.findChild(
            self.qt["QtWidgets"].QLineEdit,
            "galleryAssetSearch",
        )
        asset_count = next(
            label
            for label in dialog.findChildren(self.qt["QtWidgets"].QLabel)
            if label.objectName() == "dialogScope"
            and "ASSETS" in label.text()
        )
        self.assertIsNotNone(asset_list)
        self.assertIsNotNone(details)
        self.assertIsNotNone(asset_search)
        self.assertIn("PENDING VISUAL APPROVAL", asset_count.text())
        camera_menu_labels = {
            action.text()
            for menu in dialog.findChildren(self.qt["QtWidgets"].QMenu)
            for action in menu.actions()
        }
        self.assertTrue({"Right", "Rear"}.issubset(camera_menu_labels))
        self.assertIn(
            "REVIEW ONLY  ·  PLC DISABLED  ·  NO PLC TRANSPORT",
            " ".join(
                label.text()
                for label in dialog.findChildren(self.qt["QtWidgets"].QLabel)
            ),
        )
        camera_labels = {
            button.text()
            for button in dialog.findChildren(
                self.qt["QtWidgets"].QPushButton
            )
        }
        self.assertTrue(
            {
                "Zoom -",
                "Zoom +",
                "Front",
                "Top",
                "Left",
                "Views",
                "Reset",
            }.issubset(camera_labels)
        )
        for index in range(asset_list.count()):
            asset_list.setCurrentRow(index)
            self.app.processEvents()
            self.assertIn("Capabilities", details.toPlainText())
            self.assertNotIn("AttributeError", details.toPlainText())
        # Use the stable asset type for an exact single match. The broader
        # word "robot" now intentionally matches both robotArm and amr.
        asset_search.setText("robotArm")
        self.app.processEvents()
        visible_rows = [
            asset_list.item(index)
            for index in range(asset_list.count())
            if not asset_list.item(index).isHidden()
        ]
        self.assertEqual(len(visible_rows), 1)
        self.assertIn("robot", visible_rows[0].text().casefold())
        self.assertIn("1 MATCH", asset_count.text())
        asset_search.clear()
        self.app.processEvents()
        self.assertEqual(
            sum(
                not asset_list.item(index).isHidden()
                for index in range(asset_list.count())
            ),
            asset_list.count(),
        )
        asset_search.setText("does-not-exist")
        self.app.processEvents()
        empty_state = dialog.findChild(
            self.qt["QtWidgets"].QLabel,
            "galleryEmptyState",
        )
        self.assertIsNotNone(empty_state)
        self.assertFalse(empty_state.isHidden())
        self.assertIn("NO MATCHES", empty_state.text())
        self.assertIn(
            "Current preview retained",
            " ".join(
                label.text()
                for label in dialog.findChildren(self.qt["QtWidgets"].QLabel)
            ),
        )
        dialog.close()
        session.closed = True
        window.close()

        compact_session = StubSession()
        compact_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: compact_session,
        )
        compact_window = compact_class(compact_display=True)
        compact_window._timer.stop()
        compact_dialog = compact_window._build_equipment_gallery_dialog()
        info_tabs = compact_dialog.findChild(
            self.qt["QtWidgets"].QTabWidget,
            "galleryInfoTabs",
        )
        self.assertIsNotNone(info_tabs)
        self.assertEqual(
            [info_tabs.tabText(index) for index in range(info_tabs.count())],
            ["ASSETS", "DETAILS"],
        )
        compact_dialog.close()
        compact_session.closed = True
        compact_window.close()

    def test_event_rows_support_inline_acknowledge_and_clear(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class(demo_event=True, compact_display=False)
        window._timer.stop()
        window.compact_rail_nav.setCurrentIndex(1)
        window._render_event_rows()

        self.assertGreaterEqual(window.event_table.topLevelItemCount(), 1)
        self.assertIn("PHOTOEYE BLOCKED", window.event_inspector.text())
        self.assertIn("PUSHER PERMISSIVE HELD", window.event_inspector.text())
        self.assertIn("SOURCE  SCENE", window.event_inspector.text())
        self.assertIn("no PLC alarm or physical I/O", window.event_inspector.text())
        window.event_table.setCurrentItem(
            window.event_table.topLevelItem(0)
        )
        self.app.processEvents()
        self.assertTrue(window.event_ack_button.isEnabled())
        self.assertTrue(window.event_clear_button.isEnabled())

        window.event_ack_button.click()
        self.app.processEvents()
        event = window.event_history_store.all()[0]
        self.assertEqual(event.acknowledged_by, "local-operator")
        self.assertIn("ACTIVE / ACKNOWLEDGED", window.event_inspector.text())

        window.event_clear_button.click()
        self.app.processEvents()
        self.assertFalse(window.event_history_store.all()[0].active)
        self.assertIn("STATE  CLEARED", window.event_inspector.text())

        session.closed = True
        window.close()

    def test_active_event_keeps_selected_asset_context_visible(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class(demo_event=True, compact_display=False)
        window._timer.stop()
        target = SimpleNamespace(
            target_id="main_conveyor",
            label="Main conveyor",
            equipment_type="Roller conveyor",
        )
        window._on_equipment_selected(target)
        self.app.processEvents()

        self.assertFalse(window.event_asset_context.isHidden())
        self.assertIn("MAIN CONVEYOR", window.event_asset_context.text())
        self.assertIn("TYPE  Roller conveyor", window.event_asset_context.text())
        self.assertIn("PERM NOT EVALUATED", window.event_asset_context.text())

        window._on_equipment_cleared()
        self.assertTrue(window.event_asset_context.isHidden())
        session.closed = True
        window.close()

    def test_compact_event_actions_follow_newest_active_event(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class(demo_event=True)
        window._timer.stop()
        window.compact_rail_nav.setCurrentIndex(1)
        newer = window.event_history_store.record(
            code="PUSHER_INTERLOCK",
            title="Pusher interlock active",
            severity=EventSeverity.WARNING,
            source="scene-2-local-model",
            detail="Pusher permissive remains held by the local model.",
            occurred_at_utc="2026-08-08T00:01:00Z",
        )
        window._render_event_rows()

        self.assertIn("PUSHER INTERLOCK ACTIVE", window.event_inspector.text())
        self.assertEqual(window._selected_event_row().event_id, newer.event_id)
        self.assertTrue(window.event_clear_button.isEnabled())
        window.event_clear_button.click()
        self.assertFalse(window.event_history_store.get(newer.event_id).active)
        self.assertIn("CLEARED", window.event_inspector.text())

        session.closed = True
        window.close()

    def test_compact_active_alarm_promotes_events_without_overriding_user_tab(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class(demo_event=True, compact_display=True)
        window._timer.stop()
        window._on_equipment_selected(
            SimpleNamespace(
                target_id="main_conveyor",
                label="Main conveyor",
                equipment_type="Roller conveyor",
            )
        )
        window._render_event_rows()
        window._update_event_surface_visibility()
        self.app.processEvents()

        self.assertEqual(window.compact_rail_nav.currentIndex(), 1)
        self.assertIn("PHOTOEYE BLOCKED", window.event_inspector.text())
        self.assertIn("ASSET  MAIN CONVEYOR", window.event_inspector.text())
        self.assertIn("conveyor_running", window.event_inspector.text())
        self.assertFalse(window.event_priority_hint.isHidden())

        window.compact_rail_nav.setCurrentIndex(0)
        self.app.processEvents()
        window._render_event_rows()
        window._update_event_surface_visibility()
        self.assertEqual(window.compact_rail_nav.currentIndex(), 0)
        self.assertTrue(window.event_priority_hint.isHidden())

        window._render_review_points(window._scene2_source["simulation"]["points"])
        self.assertIn("SCROLL FOR", window.points_column_right.text())
        self.assertGreaterEqual(window.points_table.rowCount(), 3)
        self.assertGreaterEqual(window.points_table_right.rowCount(), 3)

        session.closed = True
        window.close()

    def test_inspection_contract_fails_closed_until_exchange_is_ready(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        target = SimpleNamespace(target_id="main_conveyor")
        not_ready = SimpleNamespace(
            connection=ConnectionState.CONNECTED,
            ready=False,
            points={},
            cycle=4,
        )
        _tag, _perm, quality = window._inspection_contract_lines(
            target,
            not_ready,
        )
        self.assertIn("SESSION NOT READY", quality)
        ready = SimpleNamespace(
            connection=ConnectionState.CONNECTED,
            ready=True,
            points={"pusher_retracted": True, "part_at_pusher": False},
            cycle=5,
        )
        _tag, perm, quality = window._inspection_contract_lines(
            target,
            ready,
        )
        self.assertIn("PERM EVAL PASS", perm)
        self.assertIn("PLC EXCHANGE ACTIVE", quality)
        session.closed = True
        window.close()

    def test_inspection_point_values_fail_closed_and_use_normalized_photoeye_point(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        disconnected = SimpleNamespace(
            connection=ConnectionState.DISCONNECTED,
            ready=False,
            points={"conveyor_running": None},
        )
        conveyor = SimpleNamespace(target_id="main_conveyor")
        self.assertIn(
            "VALUE  --  ·  UNAVAILABLE",
            window._inspection_point_line(conveyor, disconnected),
        )
        connected = SimpleNamespace(
            connection=ConnectionState.CONNECTED,
            ready=True,
            points={"part_at_pusher": True},
        )
        photoeye = SimpleNamespace(target_id="photoeye")
        self.assertEqual(
            window._inspection_point_line(photoeye, connected),
            "VALUE  part_at_pusher=TRUE",
        )
        session.closed = True
        window.close()

    def test_pilot_capability_hides_real_plc_connect(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
            real_plc_writes_enabled=False,
        )
        window = window_class()
        window._timer.stop()

        self.assertFalse(window.action_connect.isVisible())
        self.assertFalse(window.action_connect.isEnabled())
        self.assertTrue(window.action_test_plc.isVisible())
        window._connect()
        self.assertEqual(session.connect_calls, 0)

        session.closed = True
        window.close()

    def test_window_remains_open_until_worker_reports_closed(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()
        window.show()
        self.app.processEvents()

        closed_immediately = window.close()
        self.app.processEvents()

        self.assertFalse(closed_immediately)
        self.assertTrue(window.isVisible())
        self.assertTrue(session.begin_close_called)

        session.closed = True
        window._finish_close_when_safe()
        self.app.processEvents()
        self.assertFalse(window.isVisible())

    def test_initial_window_fits_available_screen(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()
        available = self.app.primaryScreen().availableGeometry()

        self.assertLessEqual(window.width(), available.width())
        self.assertLessEqual(window.height(), available.height())

        session.closed = True
        window.close()

    def test_all_three_views_reuse_session_viewport_and_commands(self) -> None:
        session = StubSession()
        viewport = StubViewport(self.qt)
        window_class = build_window_class(
            self.qt,
            viewport_factory=lambda _qt: viewport,
            session_factory=lambda: session,
        )
        window = window_class(initial_view="A")
        window._timer.stop()
        original_viewport_widget = window.viewport.container

        self.assertEqual(window.current_view_mode, "A")
        self.assertIs(window.session, session)
        self.assertIs(window.viewport.container, original_viewport_widget)
        self.assertIs(window.points_panel.parentWidget(), window.workspace)
        self.assertFalse(window.points_panel.isHidden())

        window.set_view_mode("B")
        self.assertEqual(window.current_view_mode, "B")
        self.assertIs(window.session, session)
        self.assertIs(window.viewport.container, original_viewport_widget)
        self.assertIs(window.points_panel.parentWidget(), window.workspace)
        self.assertFalse(window.points_panel.isHidden())

        window.set_view_mode("C")
        self.assertEqual(window.current_view_mode, "C")
        self.assertIs(window.session, session)
        self.assertIs(window.viewport.container, original_viewport_widget)
        self.assertIs(window.points_panel.parentWidget(), window.workspace)
        self.assertFalse(window.points_panel.isHidden())

        self.assertEqual(
            (
                session.connect_calls,
                session.disconnect_calls,
                session.run_calls,
                session.stop_calls,
                session.reset_calls,
            ),
            (0, 0, 0, 0, 0),
        )
        self.assertEqual(
            len(
                window.findChildren(
                    self.qt["QtWidgets"].QFrame,
                    "header",
                )
            ),
            1,
        )
        self.assertEqual(
            len(
                window.findChildren(
                    self.qt["QtWidgets"].QFrame,
                    "transportHud",
                )
            ),
            1,
        )

        session.closed = True
        window.close()

    def test_constructor_honors_requested_initial_view(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class(initial_view="C")
        window._timer.stop()

        self.assertEqual(window.current_view_mode, "C")
        self.assertTrue(window.view_actions["C"].isChecked())

        session.closed = True
        window.close()

    def test_read_only_plc_test_is_available_from_plc_menu_only(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()

        self.assertEqual(window.action_test_plc.text(), "Test PLC - Read-only")
        self.assertEqual(
            [action.text() for action in window.playback_menu.actions()],
            ["Run", "Stop", "Reset"],
        )
        self.assertTrue(window.action_test_plc.isEnabled())
        self.assertFalse(hasattr(window, "test_plc_button"))
        self.assertEqual(
            (
                session.connect_calls,
                session.disconnect_calls,
                session.run_calls,
                session.stop_calls,
                session.reset_calls,
            ),
            (0, 0, 0, 0, 0),
        )

        session.closed = True
        window.close()

    def test_disconnect_repaints_plc_telemetry_without_a_new_cycle(self) -> None:
        session = StubSession()

        def snapshot(
            connection: ConnectionState,
            *,
            health: str,
            conveyor_running: bool | None,
            comm_ok: bool | None,
            timeout: bool | None,
        ) -> SimpleNamespace:
            return SimpleNamespace(
                connection=connection,
                running=False,
                cycle=42,
                health=health,
                heartbeat_reason=(
                    "heartbeat acknowledged"
                    if connection is ConnectionState.CONNECTED
                    else "not connected"
                ),
                heartbeat_echo=(42 if connection is ConnectionState.CONNECTED else None),
                simulation_enable=(True if connection is ConnectionState.CONNECTED else None),
                simulation_comm_ok=comm_ok,
                simulation_timeout=timeout,
                ready=connection is ConnectionState.CONNECTED,
                scene_time_s=1.0,
                exchange_ms=3.0,
                recent_p99_ms=4.0,
                points={
                    "part_at_pusher": False,
                    "pusher_extended": False,
                    "pusher_retracted": True,
                    "conveyor_running": conveyor_running,
                    "pusher_extend": False if conveyor_running is not None else None,
                    "pusher_position": 0.0,
                    "component_state": "running_loaded",
                    "parts_completed": 1,
                },
                model=None,
                message=(
                    "Real PLC connected - healthy"
                    if connection is ConnectionState.CONNECTED
                    else "Real PLC disconnected"
                ),
                error=None,
            )

        current = snapshot(
            ConnectionState.CONNECTED,
            health="healthy",
            conveyor_running=True,
            comm_ok=True,
            timeout=False,
        )
        session.snapshot = lambda: current  # type: ignore[method-assign]
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()
        window._refresh()

        current = snapshot(
            ConnectionState.DISCONNECTED,
            health="disconnected",
            conveyor_running=None,
            comm_ok=None,
            timeout=None,
        )
        window._refresh()

        self.assertIn("Health: DISCONNECTED", window.health_details.text())
        self.assertIn("Comm OK: --", window.health_details.text())
        self.assertIn("Timeout: --", window.health_details.text())
        point_values = {
            table.item(row, 0).text(): table.item(row, 2).text()
            for table in (window.points_table, window.points_table_right)
            for row in range(table.rowCount())
            if table.item(row, 0) is not None
        }
        self.assertEqual(point_values["conveyor_running"], "--")
        self.assertEqual(point_values["pusher_extend"], "--")
        point_owners = {
            table.item(row, 0).text(): table.item(row, 3).text()
            for table in (window.points_table, window.points_table_right)
            for row in range(table.rowCount())
            if table.item(row, 0) is not None
        }
        self.assertEqual(point_owners["conveyor_running"], "PLC?")
        self.assertEqual(point_owners["pusher_extend"], "PLC?")

        session.closed = True
        window.close()

    def test_primary_screen_has_only_bottom_run_stop_reset_controls(
        self,
    ) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()

        visible_buttons = [
            button.text()
            for button in window.findChildren(
                self.qt["QtWidgets"].QPushButton
            )
            if not button.isHidden()
        ]

        self.assertEqual(
            (
                window.hud_run_button.text(),
                window.hud_stop_button.text(),
                window.hud_reset_button.text(),
            ),
            ("Run", "Stop", "Reset"),
        )
        expected_buttons = ["Setup", "Basic Logic"]
        if not window._compact_display:
            expected_buttons.append("Open Event Console")
        expected_buttons.extend(["Run", "Stop", "Reset"])
        self.assertEqual(visible_buttons, expected_buttons)
        self.assertEqual(window.action_test_plc.text(), "Test PLC - Read-only")

        session.closed = True
        window.close()

    def test_scene_catalog_is_header_only_and_fails_closed(
        self,
    ) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            review_viewport_factory=StubReviewViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()

        self.assertEqual(window.scene_menu.title(), "Scene")
        self.assertEqual(window.action_scene_editor.text(), "Scene Editor")
        self.assertEqual(
            window.action_initial_simulation_setup.text(),
            "Initial Simulation Setup",
        )
        self.assertEqual(
            window.action_equipment_gallery.text(),
            "Equipment Gallery",
        )
        self.assertEqual(
            window.production_scenes_menu.title(),
            "Production Scenes",
        )
        self.assertEqual(window.review_scenes_menu.title(), "Review Scenes")
        self.assertEqual(
            window.training_labs_menu.title(),
            "Training Labs",
        )
        self.assertFalse(hasattr(window, "scene_playlist"))
        self.assertFalse(hasattr(window, "scene_selector"))
        self.assertFalse(hasattr(window, "scene_combo"))
        self.assertEqual(window.scene_setup_button.text(), "Setup")
        self.assertEqual(
            window.scene_logic_button.text(),
            "Basic Logic",
        )
        self.assertEqual(len(window.production_scene_actions), 2)
        self.assertEqual(len(window.review_scene_actions), 4)
        # The training catalog has expanded beyond the original 25 Chapter 2
        # labs. Prove that every delivered lab is represented exactly once;
        # the assertions below separately keep all lab actions fail-closed.
        scene_root = Path(__file__).resolve().parents[1] / "prototype" / "scenes"
        expected_labs = {
            json.loads(path.read_text(encoding="utf-8"))["id"]
            for path in scene_root.iterdir()
            if path.is_file() and path.suffix.lower() in {".json", ".plcscene"}
            and path.name.startswith("lab-")
        }
        self.assertGreaterEqual(len(expected_labs), 25)
        self.assertEqual(set(window.training_lab_actions), expected_labs)
        self.assertTrue(
            window.scene_actions["scene-2-conveyor-pusher"].isChecked()
        )
        self.assertNotIn("equipment-gallery", window.scene_actions)
        self.assertEqual(
            {
                scene_id
                for scene_id, action in window.scene_actions.items()
                if action.isEnabled()
            },
            {
                "scene-2-conveyor-pusher",
                "conveyor-cell",
                "tank-level",
                "tank-high-low",
                "tank-radar",
            },
        )
        self.assertTrue(
            all(
                not action.isEnabled()
                for action in window.training_lab_actions.values()
            )
        )

        window.scene_actions["conveyor-cell"].trigger()
        self.app.processEvents()

        self.assertEqual(window.current_scene_id, "conveyor-cell")
        self.assertEqual(window.runtime_scene.text(), "S03")
        self.assertIn("Conveyor Inspection Cell", window.scene_title.text())
        self.assertIn(
            "S03_CONVEYOR_INSPECTION_CELL_SETUP.md",
            window.scene_scope.text(),
        )
        self.assertFalse(window.event_history_store.active())
        self.assertIn("REVIEW ONLY", window.compact_priority_bar.text())
        self.assertEqual(window.compact_rail_nav.tabText(2), "HEALTH DISABLED")
        self.assertEqual(window.compact_rail_nav.tabText(3), "RUNTIME REVIEW")
        self.assertGreaterEqual(
            window._active_viewport.container.height(),
            250,
        )
        point_names = tuple(
            window.points_table.item(row, 0).text()
            for row in range(window.points_table.rowCount())
        ) + tuple(
            window.points_table_right.item(row, 0).text()
            for row in range(window.points_table_right.rowCount())
        )
        self.assertEqual(
            set(point_names),
            {
                "conveyor_run",
                "photoeye_blocked",
                "conveyor_speed",
                "parts_completed",
            },
        )
        for control in (
            window.hud_run_button,
            window.hud_stop_button,
            window.hud_reset_button,
            window.action_connect,
            window.action_test_plc,
        ):
            self.assertFalse(control.isEnabled())
        self.assertEqual(
            (
                session.connect_calls,
                session.disconnect_calls,
                session.run_calls,
                session.step_calls,
                session.reset_calls,
            ),
            (0, 0, 0, 0, 0),
        )

        window.scene_actions["scene-2-conveyor-pusher"].trigger()
        self.app.processEvents()

        self.assertEqual(
            window.current_scene_id,
            "scene-2-conveyor-pusher",
        )
        self.assertTrue(window.action_connect.isEnabled())
        self.assertTrue(window.action_test_plc.isEnabled())

        session.closed = True
        window.close()

    def test_frozen_package_disables_source_only_scene_editor(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            review_viewport_factory=StubReviewViewport,
            session_factory=lambda: session,
        )
        import tools.rungproof_native as native_module

        had_frozen = hasattr(native_module.sys, "frozen")
        previous_frozen = getattr(native_module.sys, "frozen", None)
        setattr(native_module.sys, "frozen", True)
        try:
            window = window_class()
            window._timer.stop()
            self.assertFalse(window.action_scene_editor.isEnabled())
            window.close()
        finally:
            if had_frozen:
                setattr(native_module.sys, "frozen", previous_frozen)
            else:
                delattr(native_module.sys, "frozen")
            session.closed = True

    def test_panel_area_roles_add_subtle_hierarchy_in_all_views(self) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            review_viewport_factory=StubReviewViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()

        self.assertEqual(window.scene_panel.property("areaRole"), "left")
        self.assertEqual(window.runtime_panel.property("areaRole"), "left")
        self.assertEqual(window.health_panel.property("areaRole"), "right")
        self.assertEqual(window.equipment_panel.property("areaRole"), "right")
        self.assertEqual(window.workspace_state, "OFFLINE")
        self.assertEqual(window.workspace_badge.text(), "WORKSPACE OFFLINE")
        self.assertEqual(window.compact_rail_nav.count(), 4)
        self.assertEqual(
            window.compact_panel_stack.count(),
            4 if window._compact_display else 0,
        )
        self.assertEqual(
            [window.compact_rail_nav.tabText(index) for index in range(4)],
            ["EQUIPMENT", "EVENTS", "HEALTH", "RUNTIME"],
        )
        self.assertEqual(window.points_panel.findChildren(self.qt["QtWidgets"].QLabel)[0].text(), "LOCAL MODEL POINTS")
        self.assertIn("CONTROLS LOCKED", window.hud_lock_reason.text())
        self.assertFalse(window.hud_lock_reason.isHidden())
        self.assertFalse(window.action_workspace_sync.isEnabled())
        self.assertFalse(window.action_workspace_upload.isEnabled())
        self.assertEqual(window.points_panel.property("areaRole"), "data")
        self.assertEqual(window.left_rail.property("areaRole"), "left")
        self.assertEqual(window.right_rail.property("areaRole"), "right")
        self.assertEqual(
            window.left_rail.widget().property("areaRole"),
            "left",
        )
        self.assertEqual(
            window.right_rail.widget().property("areaRole"),
            "right",
        )

        stylesheet = window.styleSheet()
        for color in ("#10242B", "#111F26", "#0A1E25", "#09151A"):
            self.assertIn(color, stylesheet)
        self.assertIn("QScrollBar::add-page:vertical", stylesheet)
        self.assertIn("QScrollBar::sub-page:vertical", stylesheet)

        for mode in ("B", "C", "A"):
            window.set_view_mode(mode)
            self.assertEqual(window.scene_panel.property("areaRole"), "left")
            self.assertEqual(window.points_panel.property("areaRole"), "data")

        session.closed = True
        window.close()

    def test_read_only_plc_test_uses_separate_zero_write_runner(self) -> None:
        session = StubSession()
        calls: list[tuple[Path, str]] = []
        results: list[dict[str, object]] = []

        def diagnostic_runner(
            profile_dir: Path,
            profile_id: str,
        ) -> dict[str, object]:
            calls.append((profile_dir, profile_id))
            return {
                "status": "passed",
                "summary": "PLC TEST PASSED",
                "profile": {
                    "id": profile_id,
                    "ip": "10.70.9.201",
                    "rack": 0,
                    "slot": 1,
                    "tagCount": 10,
                },
                "items": [],
                "readOnly": True,
                "writeAttempted": False,
                "durationMs": 1.0,
            }

        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
            diagnostic_runner=diagnostic_runner,
        )
        window = window_class()
        window._timer.stop()
        window._confirm_action = lambda *_args, **_kwargs: True
        window._show_plc_diagnostic_result = results.append

        window._test_plc()
        deadline = time.monotonic() + 2.0
        while window._diagnostic_active and time.monotonic() < deadline:
            self.app.processEvents()
            time.sleep(0.01)
        self.app.processEvents()

        self.assertFalse(window._diagnostic_active)
        self.assertEqual(len(calls), 1)
        self.assertEqual(calls[0][1], "scene-2-db14-pusher-interface.json")
        self.assertEqual(results[0]["readOnly"], True)
        self.assertEqual(results[0]["writeAttempted"], False)
        self.assertEqual(
            (
                session.connect_calls,
                session.disconnect_calls,
                session.run_calls,
                session.stop_calls,
                session.reset_calls,
            ),
            (0, 0, 0, 0, 0),
        )

        session.closed = True
        window.close()

    def test_live_connect_is_blocked_while_read_only_test_is_active(
        self,
    ) -> None:
        session = StubSession()
        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
        )
        window = window_class()
        window._timer.stop()
        window._confirm_action = lambda *_args, **_kwargs: True
        window._diagnostic_active = True

        window._connect()

        self.assertEqual(session.connect_calls, 0)
        session.closed = True
        window._diagnostic_active = False
        window.close()

    def test_read_only_test_immediately_disables_all_plc_entry_points(
        self,
    ) -> None:
        session = StubSession()
        started = threading.Event()
        release = threading.Event()

        def blocking_diagnostic(
            _profile_dir: Path,
            _profile_id: str,
        ) -> dict[str, object]:
            started.set()
            if not release.wait(2.0):
                raise TimeoutError("test did not release diagnostic")
            return {
                "status": "passed",
                "summary": "PLC TEST PASSED",
                "profile": {},
                "items": [],
                "readOnly": True,
                "writeAttempted": False,
                "durationMs": 1.0,
            }

        window_class = build_window_class(
            self.qt,
            viewport_factory=StubViewport,
            session_factory=lambda: session,
            diagnostic_runner=blocking_diagnostic,
        )
        window = window_class()
        window._timer.stop()
        window._confirm_action = lambda *_args, **_kwargs: True
        window._show_plc_diagnostic_result = lambda _result: None

        window._test_plc()
        self.assertTrue(started.wait(1.0))
        self.app.processEvents()

        self.assertTrue(window._diagnostic_active)
        self.assertFalse(window.action_connect.isEnabled())
        self.assertFalse(window.action_test_plc.isEnabled())
        window._connect()
        self.assertEqual(session.connect_calls, 0)

        release.set()
        deadline = time.monotonic() + 2.0
        while window._diagnostic_active and time.monotonic() < deadline:
            self.app.processEvents()
            time.sleep(0.01)
        self.assertFalse(window._diagnostic_active)

        session.closed = True
        window.close()

    def test_native_ui_source_has_no_browser_or_http_runtime(self) -> None:
        source = (
            Path(__file__).with_name("rungproof_native.py")
            .read_text(encoding="utf-8")
        )

        for forbidden in (
            "QtWebEngine",
            "QWebEngine",
            "http.server",
            "webbrowser",
            "serve_player",
            "three.module",
            "player.js",
            "action_step",
            "Step blocked",
        ):
            self.assertNotIn(forbidden, source)


if __name__ == "__main__":
    unittest.main()
