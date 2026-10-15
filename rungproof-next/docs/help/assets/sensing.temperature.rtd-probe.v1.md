# Industrial RTD Temperature Probe help

Asset ID: `sensing.temperature.rtd-probe.v1`  
Catalog status: **candidate / candidate**  
Category: `sensing/temperature`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.7 m
- Height: 1.4 m
- Depth: 0.7 m
- Source: `res://assets/controls_sensors/rtd_temperature_probe/source/rtd_temperature_probe.blend`
- Delivery: `res://assets/controls_sensors/rtd_temperature_probe/delivery/rtd_temperature_probe.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `temperature_c` | `float32` | `output` | degC | Temperature c. |
| `healthy` | `bool` | `output` |  | Healthy. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **industrial resistance thermometer with connection head and insertion stem**.
Source-model review: **compared-pass** — Rebuilt review render shows the documented stem, fitting, neck extension, and connection-head family; display and connector remain intentionally generic.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [WIKA: TR10 resistance thermometer data sheet](https://www.wika.com/media/Data-sheets/Temperature/Resistance-thermometers/ds_tr10_2_en_us.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- slim stainless insertion stem
- compression/process fitting zone
- neck extension
- top connection head and cable entry

Intentionally generic / not claimed:
- No WIKA head style, exact stem diameter/length, thermowell selection, sensor element, temperature range, hazardous-area approval, wiring, or accuracy is reproduced.
- temperature_c and healthy remain symbolic simulator outputs only.
