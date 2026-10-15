"""Tkinter viewer for the deterministic PLC scene runtime."""

from __future__ import annotations

import threading
from dataclasses import dataclass
from typing import Any

from .runtime import InterfaceRuntime, ShutdownResult
from .scene import (
    SceneCycleReport,
    SceneEngine,
    SceneRunner,
    TimingSnapshot,
)
from .scene_config import (
    ConveyorPusherSceneConfig,
    ConveyorSceneConfig,
    SceneComponentConfig,
    SceneConfig,
)
from .update_loop import LoopHealth, SimulationUpdateLoop


class VisualizerUnavailableError(RuntimeError):
    """The local Python runtime cannot create the graphical viewer."""


@dataclass(frozen=True)
class ConveyorVisualState:
    """Normalized conveyor state used by the Canvas renderer."""

    component_id: str
    state: str
    motor_running: bool
    object_present: bool
    object_leading_fraction: float | None
    object_trailing_fraction: float | None
    photoeye_fraction: float
    photoeye_blocked: bool
    completed_count: int


@dataclass(frozen=True)
class ConveyorPusherVisualState:
    """Normalized conveyor/pusher state used by the Canvas renderer."""

    component_id: str
    state: str
    motor_running: bool
    object_present: bool
    object_leading_fraction: float | None
    object_trailing_fraction: float | None
    photoeye_fraction: float
    photoeye_blocked: bool
    completed_count: int
    pusher_position: float
    pusher_extended: bool
    pusher_retracted: bool
    pusher_extending: bool


@dataclass(frozen=True)
class VisualizerOutcome:
    """Final worker result after runtime cleanup."""

    timing: TimingSnapshot | None
    shutdown: ShutdownResult
    error: str | None

    @property
    def succeeded(self) -> bool:
        return (
            self.error is None
            and self.shutdown.safe_state_error is None
            and self.shutdown.disconnect_error is None
        )


def project_conveyors(
    scene: SceneConfig,
    report: SceneCycleReport,
) -> tuple[ConveyorVisualState, ...]:
    """
    Convert authoritative scene snapshots into renderer-neutral positions.

    Fractions intentionally are not clamped. At a leading-edge position of
    zero, part of a newly loaded object is still outside the conveyor infeed.
    The Canvas renderer can therefore show the product entering the belt.
    """
    projected: list[ConveyorVisualState] = []
    for component in scene.components:
        if not isinstance(component, ConveyorSceneConfig):
            continue
        snapshot = report.snapshot.components[component.component_id]
        length = component.model.length_m
        leading = snapshot.object_leading_edge_m
        projected.append(
            ConveyorVisualState(
                component_id=component.component_id,
                state=snapshot.state.value,
                motor_running=snapshot.motor_running,
                object_present=snapshot.object_present,
                object_leading_fraction=(
                    leading / length if leading is not None else None
                ),
                object_trailing_fraction=(
                    (leading - component.model.object_length_m) / length
                    if leading is not None
                    else None
                ),
                photoeye_fraction=(
                    component.model.photoeye_position_m / length
                ),
                photoeye_blocked=snapshot.photoeye_blocked,
                completed_count=snapshot.completed_count,
            )
        )
    return tuple(projected)


def project_conveyor_pushers(
    scene: SceneConfig,
    report: SceneCycleReport,
) -> tuple[ConveyorPusherVisualState, ...]:
    """Project authoritative pusher snapshots into display fractions."""
    projected: list[ConveyorPusherVisualState] = []
    for component in scene.components:
        if not isinstance(component, ConveyorPusherSceneConfig):
            continue
        snapshot = report.snapshot.components[component.component_id]
        conveyor = component.model.conveyor
        leading = snapshot.object_leading_edge_m
        projected.append(
            ConveyorPusherVisualState(
                component_id=component.component_id,
                state=snapshot.state.value,
                motor_running=snapshot.motor_running,
                object_present=snapshot.object_present,
                object_leading_fraction=(
                    leading / conveyor.length_m
                    if leading is not None
                    else None
                ),
                object_trailing_fraction=(
                    (leading - conveyor.object_length_m)
                    / conveyor.length_m
                    if leading is not None
                    else None
                ),
                photoeye_fraction=(
                    conveyor.photoeye_position_m / conveyor.length_m
                ),
                photoeye_blocked=snapshot.photoeye_blocked,
                completed_count=snapshot.completed_count,
                pusher_position=snapshot.pusher_position,
                pusher_extended=snapshot.pusher_extended,
                pusher_retracted=snapshot.pusher_retracted,
                pusher_extending=snapshot.pusher_extending,
            )
        )
    return tuple(projected)


class _VisualWorker:
    """Own all PLC and scene work outside the Tk main thread."""

    def __init__(
        self,
        runtime: InterfaceRuntime,
        scene: SceneConfig,
        *,
        cycles: int | None,
    ) -> None:
        self._runtime = runtime
        self._scene = scene
        self._cycles = cycles
        self._stop = threading.Event()
        self._lock = threading.Lock()
        self._latest_report: SceneCycleReport | None = None
        self._outcome: VisualizerOutcome | None = None
        self._status = "Waiting to start"
        self._thread = threading.Thread(
            target=self._run,
            name="plc-scene-worker",
            daemon=False,
        )

    @property
    def is_alive(self) -> bool:
        return self._thread.is_alive()

    def start(self) -> None:
        self._thread.start()

    def request_stop(self) -> None:
        self._stop.set()
        with self._lock:
            if self._outcome is None:
                self._status = "Stopping and disconnecting..."

    def join(self, timeout: float | None = None) -> None:
        self._thread.join(timeout)

    def snapshot(
        self,
    ) -> tuple[
        str,
        SceneCycleReport | None,
        VisualizerOutcome | None,
    ]:
        with self._lock:
            return self._status, self._latest_report, self._outcome

    def _publish(self, report: SceneCycleReport) -> None:
        with self._lock:
            self._latest_report = report
            self._status = "Connected - scene running"

    def _run(self) -> None:
        timing: TimingSnapshot | None = None
        error: str | None = None
        with self._lock:
            self._status = "Connecting to PLC..."
        try:
            self._runtime.connect()
            print("S7_SESSION_CONNECTED: True")
            engine = SceneEngine(
                self._scene,
                SimulationUpdateLoop(self._runtime),
            )
            runner = SceneRunner(engine)
            timing = runner.run(
                cycles=self._cycles,
                on_cycle=self._publish,
                should_stop=self._stop.is_set,
            )
        except Exception as exc:
            error = f"{type(exc).__name__}: {exc}"
        finally:
            shutdown = self._runtime.close()
            with self._lock:
                self._status = (
                    "Stopped"
                    if error is None
                    else f"Runtime error: {error}"
                )
                self._outcome = VisualizerOutcome(
                    timing=timing,
                    shutdown=shutdown,
                    error=error,
                )


class _TkSceneViewer:
    """Render scene reports while a worker owns PLC communications."""

    _BACKGROUND = "#101820"
    _PANEL = "#182630"
    _TEXT = "#e7eef2"
    _MUTED = "#9fb0ba"
    _GOOD = "#35c96f"
    _WARN = "#f2bd42"
    _BAD = "#ef5350"
    _BELT = "#4e5d66"
    _PRODUCT = "#e7a33e"

    def __init__(
        self,
        tk: Any,
        scene: SceneConfig,
        worker: _VisualWorker,
    ) -> None:
        self._tk = tk
        self._scene = scene
        self._worker = worker
        self._last_cycle = -1
        self._handled_outcome = False
        self._close_requested = False
        self._exit_code = 0

        root = tk.Tk()
        self._root = root
        root.title("Siemens PLC Visual Simulator")
        root.geometry("1100x720")
        root.minsize(780, 520)
        root.configure(bg=self._BACKGROUND)
        root.protocol("WM_DELETE_WINDOW", self._request_close)

        header = tk.Frame(root, bg=self._PANEL, padx=18, pady=12)
        header.pack(fill="x")
        tk.Label(
            header,
            text="PLC VISUAL SIMULATOR",
            bg=self._PANEL,
            fg=self._TEXT,
            font=("Segoe UI", 18, "bold"),
        ).pack(side="left")
        self._connection_label = tk.Label(
            header,
            text="Starting...",
            bg=self._PANEL,
            fg=self._WARN,
            font=("Segoe UI", 11, "bold"),
        )
        self._connection_label.pack(side="right", padx=(12, 0))
        self._stop_button = tk.Button(
            header,
            text="Stop simulator",
            command=self._request_close,
            bg="#344955",
            fg=self._TEXT,
            activebackground="#49616d",
            activeforeground=self._TEXT,
            relief="flat",
            padx=14,
            pady=6,
        )
        self._stop_button.pack(side="right")

        status_panel = tk.Frame(root, bg=self._PANEL, padx=14, pady=10)
        status_panel.pack(fill="x", padx=14, pady=(14, 8))
        self._status_values: dict[str, Any] = {}
        status_names = (
            "Loop health",
            "Heartbeat",
            "Simulation enable",
            "Communication OK",
            "PLC timeout",
            "Scene time",
            "Exchange time",
            "Recent p99",
        )
        for index, name in enumerate(status_names):
            cell = tk.Frame(status_panel, bg=self._PANEL, padx=8, pady=4)
            cell.grid(row=index // 4, column=index % 4, sticky="ew")
            status_panel.grid_columnconfigure(index % 4, weight=1)
            tk.Label(
                cell,
                text=name,
                bg=self._PANEL,
                fg=self._MUTED,
                font=("Segoe UI", 9),
            ).pack(anchor="w")
            value = tk.StringVar(value="--")
            self._status_values[name] = value
            tk.Label(
                cell,
                textvariable=value,
                bg=self._PANEL,
                fg=self._TEXT,
                font=("Consolas", 11, "bold"),
            ).pack(anchor="w")

        self._canvas = tk.Canvas(
            root,
            bg=self._BACKGROUND,
            highlightthickness=0,
        )
        self._canvas.pack(fill="both", expand=True, padx=14, pady=(0, 14))

    def run(self) -> int:
        self._worker.start()
        self._root.after(16, self._poll)
        try:
            self._root.mainloop()
        finally:
            if self._worker.is_alive:
                self._worker.request_stop()
                self._worker.join(timeout=5.0)
        return self._exit_code

    def _request_close(self) -> None:
        if self._worker.is_alive:
            self._close_requested = True
            self._worker.request_stop()
            self._stop_button.configure(state="disabled")
            self._connection_label.configure(
                text="Stopping...",
                fg=self._WARN,
            )
            return
        self._root.destroy()

    def _poll(self) -> None:
        status, report, outcome = self._worker.snapshot()
        self._connection_label.configure(text=status)

        if report is not None:
            cycle = report.snapshot.update.cycle.cycle_number
            if cycle != self._last_cycle:
                self._last_cycle = cycle
                self._render(report)

        if outcome is not None and not self._handled_outcome:
            self._handled_outcome = True
            self._handle_outcome(outcome)
            if self._close_requested:
                self._root.destroy()
                return

        self._root.after(16, self._poll)

    def _handle_outcome(self, outcome: VisualizerOutcome) -> None:
        if outcome.timing is not None:
            timing = outcome.timing
            print(
                "TIMING: "
                f"cycles={timing.cycles} "
                f"overruns={timing.deadline_overruns} "
                f"resyncs={timing.schedule_resyncs} "
                f"average_ms={timing.average_ms:.3f} "
                f"maximum_ms={timing.maximum_ms:.3f} "
                f"p99_ms={timing.p99_ms:.3f}"
            )
        if outcome.error is not None:
            print(
                f"SCENE_VISUALIZER_ERROR: {outcome.error}",
            )
            self._connection_label.configure(fg=self._BAD)
        else:
            self._connection_label.configure(fg=self._MUTED)

        if outcome.shutdown.safe_state_attempted:
            result = (
                "PASS"
                if outcome.shutdown.safe_state_succeeded
                else "FAIL"
            )
            print(f"SAFE_STATE_WRITE: {result}")
        else:
            print("SAFE_STATE_WRITE: NOT_REQUESTED")
        if outcome.shutdown.safe_state_error is not None:
            print(
                "SAFE_STATE_ERROR: "
                f"{outcome.shutdown.safe_state_error}"
            )
        if outcome.shutdown.disconnect_error is not None:
            print(
                "DISCONNECT_ERROR: "
                f"{outcome.shutdown.disconnect_error}"
            )

        self._exit_code = 0 if outcome.succeeded else 1
        self._stop_button.configure(
            text="Close",
            command=self._root.destroy,
            state="normal",
        )

    @staticmethod
    def _bool_text(value: object) -> str:
        if value is True:
            return "TRUE"
        if value is False:
            return "FALSE"
        return "--"

    def _render(self, report: SceneCycleReport) -> None:
        update = report.snapshot.update
        cycle = update.cycle
        heartbeat = cycle.heartbeat
        plc = cycle.plc_values_read
        health = update.health

        self._status_values["Loop health"].set(health.value.upper())
        self._status_values["Heartbeat"].set(
            f"{heartbeat.reason} / echo={heartbeat.last_echo}"
        )
        self._status_values["Simulation enable"].set(
            self._bool_text(plc.get("simulation_enable"))
        )
        self._status_values["Communication OK"].set(
            self._bool_text(plc.get("simulation_comm_ok"))
        )
        self._status_values["PLC timeout"].set(
            self._bool_text(plc.get("simulation_timeout"))
        )
        self._status_values["Scene time"].set(
            f"{report.snapshot.scene_time_ms / 1_000:.2f} s"
        )
        self._status_values["Exchange time"].set(
            f"{report.cycle_duration_ms:.2f} ms"
        )
        self._status_values["Recent p99"].set(
            f"{report.timing.p99_ms:.2f} ms"
        )
        self._connection_label.configure(
            fg=(
                self._GOOD
                if health in (LoopHealth.HEALTHY, LoopHealth.DEGRADED)
                else self._WARN
                if health is LoopHealth.STARTING
                else self._BAD
            )
        )
        self._draw_components(
            project_conveyors(self._scene, report),
            project_conveyor_pushers(self._scene, report),
        )

    def _draw_components(
        self,
        conveyor_states: tuple[ConveyorVisualState, ...],
        pusher_states: tuple[ConveyorPusherVisualState, ...],
    ) -> None:
        canvas = self._canvas
        canvas.delete("all")
        width = max(canvas.winfo_width(), 760)
        height = max(canvas.winfo_height(), 300)
        total = len(conveyor_states) + len(pusher_states)
        row_height = max(190, height // max(1, total))
        components = {
            component.component_id: component
            for component in self._scene.components
        }

        for index, state in enumerate(conveyor_states):
            component = components[state.component_id]
            self._draw_conveyor(
                component,
                state,
                top=index * row_height,
                width=width,
                row_height=row_height,
            )

        offset = len(conveyor_states)
        for index, state in enumerate(pusher_states):
            component = components[state.component_id]
            top = (offset + index) * row_height
            self._draw_conveyor(
                component,
                state,
                top=top,
                width=width,
                row_height=row_height,
            )
            self._draw_pusher(
                component,
                state,
                top=top,
                width=width,
                row_height=row_height,
            )

    def _draw_conveyor(
        self,
        component: SceneComponentConfig,
        state: ConveyorVisualState | ConveyorPusherVisualState,
        *,
        top: int,
        width: int,
        row_height: int,
    ) -> None:
        canvas = self._canvas
        model = (
            component.model.conveyor
            if isinstance(component, ConveyorPusherSceneConfig)
            else component.model
        )
        left = 135.0
        right = max(left + 300.0, width - 70.0)
        belt_y = top + row_height * 0.56
        belt_height = 38.0
        span = right - left

        canvas.create_text(
            24,
            top + 24,
            anchor="w",
            text=state.component_id.replace("_", " ").upper(),
            fill=self._TEXT,
            font=("Segoe UI", 14, "bold"),
        )
        canvas.create_text(
            24,
            top + 50,
            anchor="w",
            text=(
                f"{state.state}  |  completed {state.completed_count}"
            ),
            fill=self._MUTED,
            font=("Consolas", 10),
        )

        canvas.create_rectangle(
            left,
            belt_y,
            right,
            belt_y + belt_height,
            fill=self._BELT,
            outline="#71818a",
            width=2,
        )
        roller_count = max(5, int(span // 90))
        for roller in range(roller_count + 1):
            x = left + span * roller / roller_count
            canvas.create_oval(
                x - 7,
                belt_y + 12,
                x + 7,
                belt_y + 26,
                fill="#263942",
                outline="#8a9aa2",
            )

        sensor_x = left + span * state.photoeye_fraction
        sensor_color = self._BAD if state.photoeye_blocked else "#42a5f5"
        canvas.create_line(
            sensor_x,
            belt_y - 65,
            sensor_x,
            belt_y + belt_height,
            fill=sensor_color,
            width=3,
            dash=() if state.photoeye_blocked else (5, 4),
        )
        canvas.create_rectangle(
            sensor_x - 15,
            belt_y - 82,
            sensor_x + 15,
            belt_y - 65,
            fill=sensor_color,
            outline="",
        )
        canvas.create_text(
            sensor_x,
            belt_y - 94,
            text=(
                "PHOTOEYE BLOCKED"
                if state.photoeye_blocked
                else "PHOTOEYE CLEAR"
            ),
            fill=sensor_color,
            font=("Segoe UI", 9, "bold"),
        )

        if (
            state.object_present
            and state.object_leading_fraction is not None
            and state.object_trailing_fraction is not None
        ):
            product_left = left + span * state.object_trailing_fraction
            product_right = left + span * state.object_leading_fraction
            canvas.create_rectangle(
                product_left,
                belt_y - 45,
                product_right,
                belt_y - 3,
                fill=self._PRODUCT,
                outline="#ffd28a",
                width=2,
            )
            canvas.create_text(
                (product_left + product_right) / 2,
                belt_y - 24,
                text="PRODUCT",
                fill="#2c2112",
                font=("Segoe UI", 9, "bold"),
            )

        motor_color = self._GOOD if state.motor_running else "#67757d"
        canvas.create_oval(
            45,
            belt_y - 1,
            105,
            belt_y + 59,
            fill=motor_color,
            outline="#c7d1d6",
            width=2,
        )
        canvas.create_text(
            75,
            belt_y + 29,
            text="M",
            fill="#0b151a",
            font=("Segoe UI", 18, "bold"),
        )
        canvas.create_text(
            75,
            belt_y + 75,
            text="RUN" if state.motor_running else "STOP",
            fill=motor_color,
            font=("Consolas", 10, "bold"),
        )

        canvas.create_text(
            left,
            belt_y + 62,
            anchor="w",
            text="0.0 m",
            fill=self._MUTED,
            font=("Consolas", 9),
        )
        canvas.create_text(
            right,
            belt_y + 62,
            anchor="e",
            text=f"{model.length_m:.1f} m",
            fill=self._MUTED,
            font=("Consolas", 9),
        )

    def _draw_pusher(
        self,
        component: SceneComponentConfig,
        state: ConveyorPusherVisualState,
        *,
        top: int,
        width: int,
        row_height: int,
    ) -> None:
        """Overlay one pusher cylinder, rod, and limit indication."""
        if not isinstance(component, ConveyorPusherSceneConfig):
            return
        canvas = self._canvas
        left = 135.0
        right = max(left + 300.0, width - 70.0)
        belt_y = top + row_height * 0.56
        span = right - left
        sensor_x = left + span * state.photoeye_fraction
        pusher_x = sensor_x - 48.0
        cylinder_top = belt_y - 135.0
        cylinder_bottom = belt_y - 104.0
        rod_end = cylinder_bottom + 58.0 * state.pusher_position
        pusher_color = (
            self._WARN if state.pusher_extending else "#9fb0ba"
        )

        canvas.create_rectangle(
            pusher_x - 22,
            cylinder_top,
            pusher_x + 22,
            cylinder_bottom,
            fill="#344955",
            outline="#c7d1d6",
            width=2,
        )
        canvas.create_line(
            pusher_x,
            cylinder_bottom,
            pusher_x,
            rod_end,
            fill=pusher_color,
            width=7,
        )
        canvas.create_rectangle(
            pusher_x - 24,
            rod_end,
            pusher_x + 24,
            rod_end + 8,
            fill=pusher_color,
            outline="",
        )
        canvas.create_text(
            pusher_x,
            cylinder_top - 15,
            text=(
                "PUSHER EXTENDED"
                if state.pusher_extended
                else "PUSHER RETRACTED"
                if state.pusher_retracted
                else f"PUSHER {state.pusher_position * 100:.0f}%"
            ),
            fill=pusher_color,
            font=("Segoe UI", 9, "bold"),
        )


def run_scene_visualizer(
    scene: SceneConfig,
    runtime: InterfaceRuntime,
    *,
    cycles: int | None = None,
) -> int:
    """Create the Tk viewer and run until the window closes."""
    try:
        import tkinter as tk
    except ImportError as exc:
        raise VisualizerUnavailableError(
            "Tkinter is not installed in this Python runtime"
        ) from exc

    try:
        worker = _VisualWorker(runtime, scene, cycles=cycles)
        viewer = _TkSceneViewer(tk, scene, worker)
    except tk.TclError as exc:
        raise VisualizerUnavailableError(
            f"Tk could not open a display: {exc}"
        ) from exc
    return viewer.run()
