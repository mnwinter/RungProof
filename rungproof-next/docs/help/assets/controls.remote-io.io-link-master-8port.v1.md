# Eight-Port IO-Link Master help

Asset ID: `controls.remote-io.io-link-master-8port.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/remote-io`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.9 m
- Height: 1.0 m
- Depth: 0.55 m
- Source: `res://assets/controls_sensors/eight_port_io_link_master/source/eight_port_io_link_master.blend`
- Delivery: `res://assets/controls_sensors/eight_port_io_link_master/delivery/eight_port_io_link_master.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `connected` | `bool` | `output` |  | Connected. |
| `faulted` | `bool` | `output` |  | Faulted. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **IP67 eight-port IO-Link field master**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the ifm DataLine eight-port master family: the field enclosure has a distinct port face with eight M12-style sockets, per-port indicator treatment, power/network connector context, and industrial mounting form. The exaggerated attached cables are review context, not wiring or network topology.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm electronic: IO-Link masters](https://www.ifm.com/us/en/us/learn-more/io-link/io-link-masters) | oem-product-page | 2026-09-22 |

Modeled family features:
- eight M12-style ports
- port status indicators
- field enclosure
- network/power connector context
- mounting form

Intentionally generic / not claimed:
- No ifm mark, protocol, port class, IODD, voltage, current, IP rating, address, or network behavior is reproduced.
- All master and device I/O remain isolated symbolic simulator signals with no physical writes.
