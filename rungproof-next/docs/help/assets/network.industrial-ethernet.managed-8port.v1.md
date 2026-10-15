# Eight-Port Managed Industrial Ethernet Switch help

Asset ID: `network.industrial-ethernet.managed-8port.v1`  
Catalog status: **candidate / candidate**  
Category: `network/industrial-ethernet`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.2 m
- Height: 0.5 m
- Depth: 0.75 m
- Source: `res://assets/electrical_controls/managed_ethernet_switch/source/managed_ethernet_switch.blend`
- Delivery: `res://assets/electrical_controls/managed_ethernet_switch/delivery/managed_ethernet_switch.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `power_ok` | `bool` | `input` |  | Power ok. |
| `network_fault` | `bool` | `output` |  | Network fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **managed eight-port DIN-rail industrial Ethernet switch**.
Source-model review: **compared-pass** — Fresh blind render compared with Siemens SCALANCE XC208 managed switches: the industrial housing has eight repeated RJ45-port treatments with individual LEDs, lower uplink/fiber-like ports, and a management/diagnostic face. It reads as a generic managed eight-port industrial Ethernet switch.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Siemens: SCALANCE XC208 managed industrial Ethernet switch](https://mall.industry.siemens.com/goos/catalog/Pages/mmpdata.ashx?MLFB1=6GK52080BA102AA3&MLFB2=6GK5208-0BA00-2AC2&lang=en) | oem-product-page | 2026-09-22 |

Modeled family features:
- eight repeated port treatments
- per-port LEDs
- industrial enclosure
- lower uplink/fiber-like ports
- management face

Intentionally generic / not claimed:
- No Siemens mark, port speed, protocol, VLAN, redundancy, IP configuration, cybersecurity setting, power supply, or network configuration is reproduced.
- The simulator does not transmit network traffic or connect to plant equipment.
