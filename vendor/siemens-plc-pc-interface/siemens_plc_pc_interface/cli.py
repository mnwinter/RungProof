"""Command-line entry point for offline validation and guarded runtime work."""

from __future__ import annotations

import argparse
import json
import sys
import time
from collections.abc import Sequence
from dataclasses import replace
from typing import Any

from .config import (
    AnalogPointConfig,
    ConfigError,
    DigitalPointConfig,
    Direction,
    InterfaceConfig,
    load_config,
)
from .points import build_address_groups
from .runtime import InterfaceRuntime, SafeStatePolicy
from .scene import SceneCycleReport, SceneEngine, SceneRunner
from .scene_config import SceneConfig, SceneConfigError, load_scene_config
from .transport import Snap7Transport
from .update_loop import SimulationUpdateLoop


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="siemens-plc-pc-interface",
        description="Siemens PLC-PC Interface utilities",
    )
    commands = parser.add_subparsers(dest="command", required=True)

    validate = commands.add_parser(
        "validate",
        help="validate a JSON interface configuration without connecting",
    )
    validate.add_argument("config", help="path to the JSON configuration")

    run = commands.add_parser(
        "run",
        help="run the configured PLC exchange loop with explicit write approval",
    )
    run.add_argument("config", help="path to the JSON configuration")
    run.add_argument(
        "--execute",
        action="store_true",
        help="authorize writes only to configured pc_to_plc DB tags",
    )
    run.add_argument(
        "--cycles",
        type=_positive_integer,
        help="stop after this many cycles; omit to run until Ctrl+C",
    )
    run.add_argument(
        "--set",
        action="append",
        default=[],
        metavar="NAME=JSON_VALUE",
        help=(
            "set a non-heartbeat PC-owned value, for example "
            "--set pc_to_plc=true"
        ),
    )
    run.add_argument(
        "--write-safe-state-on-exit",
        action="store_true",
        help=(
            "best-effort write configured safe values before disconnecting; "
            "the PLC watchdog remains authoritative"
        ),
    )

    scene_validate = commands.add_parser(
        "scene-validate",
        help="validate an interface and scene without connecting",
    )
    scene_validate.add_argument(
        "interface",
        help="path to the schema-v2 JSON interface configuration",
    )
    scene_validate.add_argument(
        "scene",
        help="path to the JSON scene configuration",
    )

    scene_run = commands.add_parser(
        "scene-run",
        help="run a deterministic scene with explicit PLC write approval",
    )
    scene_run.add_argument(
        "interface",
        help="path to the schema-v2 JSON interface configuration",
    )
    scene_run.add_argument(
        "scene",
        help="path to the JSON scene configuration",
    )
    scene_run.add_argument(
        "--execute",
        action="store_true",
        help="authorize writes only to configured pc_to_plc DB tags",
    )
    scene_run.add_argument(
        "--cycles",
        type=_positive_integer,
        help="stop after this many exchanges; omit to run until Ctrl+C",
    )
    scene_run.add_argument(
        "--report-every",
        type=_positive_integer,
        default=25,
        metavar="CYCLES",
        help=(
            "print periodic cycle detail at this interval; state and health "
            "changes are always printed (default: 25)"
        ),
    )
    scene_run.add_argument(
        "--cycle-ms",
        type=_cycle_milliseconds,
        metavar="MS",
        help=(
            "override connection.cycle_ms for a controlled rate test; "
            "the value must still align to the scene physics step"
        ),
    )
    scene_run.add_argument(
        "--write-safe-state-on-exit",
        action="store_true",
        help=(
            "best-effort write configured safe values before disconnecting; "
            "the PLC watchdog remains authoritative"
        ),
    )

    scene_visualize = commands.add_parser(
        "scene-visualize",
        help="open a graphical scene with explicit PLC write approval",
    )
    scene_visualize.add_argument(
        "interface",
        help="path to the schema-v2 JSON interface configuration",
    )
    scene_visualize.add_argument(
        "scene",
        help="path to the JSON scene configuration",
    )
    scene_visualize.add_argument(
        "--execute",
        action="store_true",
        help="authorize writes only to configured pc_to_plc DB tags",
    )
    scene_visualize.add_argument(
        "--cycles",
        type=_positive_integer,
        help="stop after this many exchanges; omit to run until the window closes",
    )
    scene_visualize.add_argument(
        "--cycle-ms",
        type=_cycle_milliseconds,
        metavar="MS",
        help=(
            "override connection.cycle_ms; the value must still align to "
            "the scene physics step"
        ),
    )
    scene_visualize.add_argument(
        "--write-safe-state-on-exit",
        action="store_true",
        help=(
            "best-effort write configured safe values before disconnecting; "
            "the PLC watchdog remains authoritative"
        ),
    )
    return parser


def _positive_integer(text: str) -> int:
    try:
        value = int(text)
    except ValueError as exc:
        raise argparse.ArgumentTypeError("must be an integer") from exc
    if value <= 0:
        raise argparse.ArgumentTypeError("must be greater than zero")
    return value


def _cycle_milliseconds(text: str) -> int:
    value = _positive_integer(text)
    if value < 5 or value > 5_000:
        raise argparse.ArgumentTypeError("must be from 5 through 5000")
    return value


def _run_validate(path: str) -> int:
    try:
        config = load_config(path)
    except ConfigError as exc:
        print(f"CONFIG_INVALID: {exc}", file=sys.stderr)
        return 2

    pc_tags = [
        tag for tag in config.tags
        if tag.direction is Direction.PC_TO_PLC
    ]
    plc_tags = [
        tag for tag in config.tags
        if tag.direction is Direction.PLC_TO_PC
    ]
    digital_points = [
        point
        for point in config.points
        if isinstance(point, DigitalPointConfig)
    ]
    analog_points = [
        point
        for point in config.points
        if isinstance(point, AnalogPointConfig)
    ]
    address_groups = build_address_groups(config)

    print("CONFIG_VALID: True")
    print(f"VERSION: {config.version}")
    print(
        "CONNECTION: "
        f"{config.connection.cpu_family} "
        f"{config.connection.ip} "
        f"rack={config.connection.rack} "
        f"slot={config.connection.slot}"
    )
    print(f"CYCLE_MS: {config.connection.cycle_ms}")
    print(f"PC_TO_PLC_TAGS: {len(pc_tags)}")
    print(f"PLC_TO_PC_TAGS: {len(plc_tags)}")
    print(f"DIGITAL_POINTS: {len(digital_points)}")
    print(f"ANALOG_POINTS: {len(analog_points)}")
    print(f"ADDRESS_GROUPS: {len(address_groups)}")
    print(
        "MIXED_OWNER_BYTE_GROUPS: "
        f"{sum(group.shares_byte_with_opposite_owner for group in address_groups)}"
    )
    print(
        "HEARTBEAT: "
        f"{config.heartbeat.pc_tag} -> "
        f"{config.heartbeat.echo_tag} "
        f"timeout={config.heartbeat.timeout_ms}ms"
    )
    print("PLC_CONNECTION_ATTEMPTED: False")
    return 0


def _load_for_command(path: str) -> Any:
    try:
        return load_config(path)
    except ConfigError as exc:
        print(f"CONFIG_INVALID: {exc}", file=sys.stderr)
        return None


def _apply_assignments(
    runtime: InterfaceRuntime,
    assignments: list[str],
) -> None:
    seen: set[str] = set()
    for assignment in assignments:
        name, separator, raw_value = assignment.partition("=")
        name = name.strip()
        if not separator or not name or not raw_value.strip():
            raise ConfigError(
                f"invalid --set {assignment!r}; expected NAME=JSON_VALUE"
            )
        if name in seen:
            raise ConfigError(f"duplicate --set for tag {name!r}")
        seen.add(name)
        try:
            value = json.loads(raw_value)
        except json.JSONDecodeError as exc:
            raise ConfigError(
                f"invalid JSON value in --set {assignment!r}: {exc.msg}"
            ) from exc
        runtime.set_pc_value(name, value)


def _scope_text(runtime: InterfaceRuntime) -> str:
    return ", ".join(
        f"{tag.name}={tag.snap7_tag}"
        for tag in runtime.write_scope
    )


def _cycle_text(
    result: Any,
    pc_heartbeat_name: str,
    echo_name: str,
) -> str:
    heartbeat = result.pc_values_written.get(pc_heartbeat_name)
    echo = result.plc_values_read.get(echo_name)
    plc_values = json.dumps(
        result.plc_values_read,
        sort_keys=True,
        separators=(",", ":"),
    )
    return (
        f"CYCLE {result.cycle_number}: "
        f"heartbeat={heartbeat} echo={echo} "
        f"healthy={result.heartbeat.healthy} "
        f"reason={result.heartbeat.reason} "
        f"age_ms={result.heartbeat.age_ms} "
        f"plc={plc_values}"
    )


def _run_interface(args: argparse.Namespace) -> int:
    config = _load_for_command(args.config)
    if config is None:
        return 2

    policy = (
        SafeStatePolicy.BEST_EFFORT_WRITE
        if args.write_safe_state_on_exit
        else SafeStatePolicy.PLC_WATCHDOG_ONLY
    )
    runtime = InterfaceRuntime(
        config,
        Snap7Transport(),
        safe_state_policy=policy,
        start_time=time.monotonic(),
    )
    try:
        _apply_assignments(runtime, args.set)
    except ConfigError as exc:
        print(f"RUNTIME_CONFIG_INVALID: {exc}", file=sys.stderr)
        print("PLC_CONNECTION_ATTEMPTED: False")
        return 2

    print("MODE: CONFIGURED PLC RUNTIME")
    print(
        f"TARGET: {config.connection.ip} "
        f"rack={config.connection.rack} "
        f"slot={config.connection.slot}"
    )
    print(f"CYCLE_MS: {config.connection.cycle_ms}")
    print(f"WRITE_SCOPE: {_scope_text(runtime)}")
    print(
        "SAFE_STATE_POLICY: "
        f"{runtime.safe_state_policy.value}"
    )

    if not args.execute:
        print("PLC_CONNECTION_ATTEMPTED: False")
        print(
            "NOT RUN: add --execute to authorize only the listed "
            "configured DB writes"
        )
        return 2

    exit_code = 0
    try:
        runtime.connect()
        print("S7_SESSION_CONNECTED: True")

        deadline = time.monotonic()
        completed_cycles = 0
        saw_healthy_heartbeat = False
        last_result = None
        while (
            args.cycles is None
            or completed_cycles < args.cycles
        ):
            result = runtime.cycle(time.monotonic())
            last_result = result
            saw_healthy_heartbeat = (
                saw_healthy_heartbeat or result.heartbeat.healthy
            )
            completed_cycles += 1
            print(
                _cycle_text(
                    result,
                    config.heartbeat.pc_tag,
                    config.heartbeat.echo_tag,
                )
            )

            deadline += config.connection.cycle_ms / 1_000
            delay = deadline - time.monotonic()
            if delay > 0:
                time.sleep(delay)

        heartbeat_passed = (
            saw_healthy_heartbeat
            and last_result is not None
            and last_result.heartbeat.healthy
        )
        print(
            "HEARTBEAT_PROOF: "
            f"{'PASS' if heartbeat_passed else 'FAIL'}"
        )
        if not heartbeat_passed:
            exit_code = 1
    except KeyboardInterrupt:
        print("STOP_REQUESTED: Ctrl+C")
    except Exception as exc:
        print(
            f"RUNTIME_ERROR: {type(exc).__name__}: {exc}",
            file=sys.stderr,
        )
        exit_code = 1
    finally:
        shutdown = runtime.close()
        if shutdown.safe_state_attempted:
            print(
                "SAFE_STATE_WRITE: "
                f"{'PASS' if shutdown.safe_state_succeeded else 'FAIL'}"
            )
        else:
            print("SAFE_STATE_WRITE: NOT_REQUESTED")
        if shutdown.safe_state_error is not None:
            print(
                f"SAFE_STATE_ERROR: {shutdown.safe_state_error}",
                file=sys.stderr,
            )
            exit_code = 1
        if shutdown.disconnect_error is not None:
            print(
                f"DISCONNECT_ERROR: {shutdown.disconnect_error}",
                file=sys.stderr,
            )
            exit_code = 1

    return exit_code


def _load_scene_for_command(
    interface_path: str,
    scene_path: str,
    *,
    cycle_ms: int | None = None,
) -> tuple[InterfaceConfig, SceneConfig] | None:
    interface = _load_for_command(interface_path)
    if interface is None:
        return None
    if cycle_ms is not None:
        if interface.heartbeat.timeout_ms < cycle_ms * 2:
            print(
                "SCENE_INVALID: heartbeat.timeout_ms must be at least "
                "twice the overridden cycle period",
                file=sys.stderr,
            )
            return None
        interface = replace(
            interface,
            connection=replace(
                interface.connection,
                cycle_ms=cycle_ms,
            ),
        )
    try:
        scene = load_scene_config(scene_path, interface)
    except SceneConfigError as exc:
        print(f"SCENE_INVALID: {exc}", file=sys.stderr)
        return None
    return interface, scene


def _print_scene_scope(
    interface: InterfaceConfig,
    scene: SceneConfig,
    runtime: InterfaceRuntime,
) -> None:
    print("MODE: DETERMINISTIC PLC SCENE")
    print(
        f"TARGET: {interface.connection.ip} "
        f"rack={interface.connection.rack} "
        f"slot={interface.connection.slot}"
    )
    print(f"PLC_EXCHANGE_MS: {scene.plc_exchange_ms}")
    print(f"PHYSICS_STEP_MS: {scene.physics_step_ms}")
    print(
        "PHYSICS_STEPS_PER_EXCHANGE: "
        f"{scene.physics_steps_per_exchange}"
    )
    print(f"MAX_CATCHUP_STEPS: {scene.max_catchup_steps}")
    print(f"COMPONENTS: {len(scene.components)}")
    print(f"EVENTS: {len(scene.events)}")
    print(f"WRITE_SCOPE: {_scope_text(runtime)}")
    print(
        "SAFE_STATE_POLICY: "
        f"{runtime.safe_state_policy.value}"
    )


def _run_scene_validate(interface_path: str, scene_path: str) -> int:
    loaded = _load_scene_for_command(interface_path, scene_path)
    if loaded is None:
        return 2
    interface, scene = loaded
    runtime = InterfaceRuntime(
        interface,
        Snap7Transport(),
        start_time=time.monotonic(),
    )
    print("SCENE_VALID: True")
    _print_scene_scope(interface, scene, runtime)
    for component in scene.components:
        binding = component.binding
        detail = (
            f"run={binding.run_command_point} "
            f"extend={binding.extend_command_point} "
            f"photoeye={binding.photoeye_point} "
            f"extended={binding.extended_sensor_point} "
            f"retracted={binding.retracted_sensor_point}"
            if component.component_type == "conveyor_pusher"
            else (
                f"run={binding.run_command_point} "
                f"sensor={binding.photoeye_point}"
            )
        )
        print(
            "COMPONENT: "
            f"{component.component_id} "
            f"type={component.component_type} "
            f"{detail}"
        )
    print("PLC_CONNECTION_ATTEMPTED: False")
    return 0


def _scene_cycle_text(report: SceneCycleReport) -> str:
    update = report.snapshot.update
    heartbeat = update.cycle.heartbeat
    component_values = {}
    for component_id, snapshot in report.snapshot.components.items():
        values = {
            "state": snapshot.state.value,
            "motor_running": snapshot.motor_running,
            "object_present": snapshot.object_present,
            "object_leading_edge_m": snapshot.object_leading_edge_m,
            "photoeye_blocked": snapshot.photoeye_blocked,
            "completed_count": snapshot.completed_count,
        }
        if hasattr(snapshot, "pusher_position"):
            values.update(
                {
                    "pusher_position": snapshot.pusher_position,
                    "pusher_extended": snapshot.pusher_extended,
                    "pusher_retracted": snapshot.pusher_retracted,
                }
            )
        component_values[component_id] = values
    components = json.dumps(
        component_values,
        sort_keys=True,
        separators=(",", ":"),
    )
    return (
        f"SCENE_CYCLE {update.cycle.cycle_number}: "
        f"scene_ms={report.snapshot.scene_time_ms} "
        f"health={update.health.value} "
        f"heartbeat_healthy={heartbeat.healthy} "
        f"heartbeat_reason={heartbeat.reason} "
        f"duration_ms={report.cycle_duration_ms:.3f} "
        f"deadline_overrun={report.deadline_overrun} "
        f"components={components}"
    )


def _run_scene(args: argparse.Namespace) -> int:
    loaded = _load_scene_for_command(
        args.interface,
        args.scene,
        cycle_ms=args.cycle_ms,
    )
    if loaded is None:
        print("PLC_CONNECTION_ATTEMPTED: False")
        return 2
    interface, scene = loaded
    policy = (
        SafeStatePolicy.BEST_EFFORT_WRITE
        if args.write_safe_state_on_exit
        else SafeStatePolicy.PLC_WATCHDOG_ONLY
    )
    runtime = InterfaceRuntime(
        interface,
        Snap7Transport(),
        safe_state_policy=policy,
        start_time=time.monotonic(),
    )
    _print_scene_scope(interface, scene, runtime)
    if not args.execute:
        print("PLC_CONNECTION_ATTEMPTED: False")
        print(
            "NOT RUN: add --execute to authorize only the listed "
            "configured DB writes"
        )
        return 2

    exit_code = 0
    saw_healthy_heartbeat = False
    last_report: SceneCycleReport | None = None
    previous_health: tuple[str, bool, str] | None = None
    previous_components: tuple[tuple[object, ...], ...] | None = None
    print(f"REPORT_EVERY_CYCLES: {args.report_every}")

    def report_cycle(report: SceneCycleReport) -> None:
        nonlocal saw_healthy_heartbeat, last_report
        nonlocal previous_health, previous_components
        last_report = report
        update = report.snapshot.update
        heartbeat = update.cycle.heartbeat
        saw_healthy_heartbeat = (
            saw_healthy_heartbeat
            or heartbeat.healthy
        )
        health = (
            update.health.value,
            heartbeat.healthy,
            heartbeat.reason,
        )
        components = tuple(
            (
                component_id,
                snapshot.state.value,
                snapshot.motor_running,
                snapshot.object_present,
                snapshot.photoeye_blocked,
                snapshot.completed_count,
                getattr(snapshot, "pusher_position", None),
                getattr(snapshot, "pusher_extended", None),
                getattr(snapshot, "pusher_retracted", None),
            )
            for component_id, snapshot in sorted(
                report.snapshot.components.items()
            )
        )
        cycle_number = update.cycle.cycle_number
        should_print = (
            cycle_number == 1
            or cycle_number % args.report_every == 0
            or health != previous_health
            or components != previous_components
            or (
                args.cycles is not None
                and cycle_number == args.cycles
            )
        )
        if should_print:
            print(_scene_cycle_text(report))
        previous_health = health
        previous_components = components

    runner: SceneRunner | None = None
    try:
        runtime.connect()
        print("S7_SESSION_CONNECTED: True")
        engine = SceneEngine(
            scene,
            SimulationUpdateLoop(runtime),
        )
        runner = SceneRunner(engine)
        timing = runner.run(
            cycles=args.cycles,
            on_cycle=report_cycle,
        )
        print(
            "TIMING: "
            f"cycles={timing.cycles} "
            f"overruns={timing.deadline_overruns} "
            f"resyncs={timing.schedule_resyncs} "
            f"average_ms={timing.average_ms:.3f} "
            f"maximum_ms={timing.maximum_ms:.3f} "
            f"p99_ms={timing.p99_ms:.3f}"
        )
        heartbeat_passed = (
            saw_healthy_heartbeat
            and last_report is not None
            and last_report.snapshot.update.cycle.heartbeat.healthy
        )
        print(
            "HEARTBEAT_PROOF: "
            f"{'PASS' if heartbeat_passed else 'FAIL'}"
        )
        if not heartbeat_passed:
            exit_code = 1
    except KeyboardInterrupt:
        print("STOP_REQUESTED: Ctrl+C")
        if runner is not None:
            timing = runner.timing
            print(
                "TIMING: "
                f"cycles={timing.cycles} "
                f"overruns={timing.deadline_overruns} "
                f"resyncs={timing.schedule_resyncs} "
                f"average_ms={timing.average_ms:.3f} "
                f"maximum_ms={timing.maximum_ms:.3f} "
                f"p99_ms={timing.p99_ms:.3f}"
            )
    except Exception as exc:
        print(
            f"SCENE_RUNTIME_ERROR: {type(exc).__name__}: {exc}",
            file=sys.stderr,
        )
        exit_code = 1
    finally:
        shutdown = runtime.close()
        if shutdown.safe_state_attempted:
            print(
                "SAFE_STATE_WRITE: "
                f"{'PASS' if shutdown.safe_state_succeeded else 'FAIL'}"
            )
        else:
            print("SAFE_STATE_WRITE: NOT_REQUESTED")
        if shutdown.safe_state_error is not None:
            print(
                f"SAFE_STATE_ERROR: {shutdown.safe_state_error}",
                file=sys.stderr,
            )
            exit_code = 1
        if shutdown.disconnect_error is not None:
            print(
                f"DISCONNECT_ERROR: {shutdown.disconnect_error}",
                file=sys.stderr,
            )
            exit_code = 1
    return exit_code


def _run_scene_visualizer(args: argparse.Namespace) -> int:
    loaded = _load_scene_for_command(
        args.interface,
        args.scene,
        cycle_ms=args.cycle_ms,
    )
    if loaded is None:
        print("PLC_CONNECTION_ATTEMPTED: False")
        return 2
    interface, scene = loaded
    policy = (
        SafeStatePolicy.BEST_EFFORT_WRITE
        if args.write_safe_state_on_exit
        else SafeStatePolicy.PLC_WATCHDOG_ONLY
    )
    runtime = InterfaceRuntime(
        interface,
        Snap7Transport(),
        safe_state_policy=policy,
        start_time=time.monotonic(),
    )
    _print_scene_scope(interface, scene, runtime)
    print("VIEW: GRAPHICAL CONVEYOR AND PHOTOEYE")
    print("VIEW_COMPONENTS: CONVEYOR, PHOTOEYE, AND OPTIONAL PUSHER")
    print(
        "VIEW_REFRESH: independent of the configured PLC exchange"
    )

    if not args.execute:
        print("PLC_CONNECTION_ATTEMPTED: False")
        print(
            "NOT RUN: add --execute to authorize only the listed "
            "configured DB writes"
        )
        return 2

    from .visualizer import (
        VisualizerUnavailableError,
        run_scene_visualizer,
    )

    try:
        return run_scene_visualizer(
            scene,
            runtime,
            cycles=args.cycles,
        )
    except VisualizerUnavailableError as exc:
        print(
            f"SCENE_VISUALIZER_UNAVAILABLE: {exc}",
            file=sys.stderr,
        )
        print("PLC_CONNECTION_ATTEMPTED: False")
        return 2


def main(argv: Sequence[str] | None = None) -> int:
    """Run one requested command."""
    parser = _build_parser()
    args = parser.parse_args(argv)

    if args.command == "validate":
        return _run_validate(args.config)
    if args.command == "run":
        return _run_interface(args)
    if args.command == "scene-validate":
        return _run_scene_validate(args.interface, args.scene)
    if args.command == "scene-run":
        return _run_scene(args)
    if args.command == "scene-visualize":
        return _run_scene_visualizer(args)

    parser.error(f"unsupported command: {args.command}")
    return 2
