# RungProof domain context

This file defines the project language used by architecture and code reviews.

## Plant Runtime

The **Plant Runtime** is the deep, renderer-neutral module that owns machine
physics, sensors, actuator motion, fixed time steps, and playback state.

Its interface accepts elapsed simulation time and typed PLC-owned point values.
It publishes one immutable snapshot containing PC-owned feedback, PLC-owned
commands currently applied, equipment state, and completion information.

The implementation must not import Qt, Snap7, Three.js, or perform file I/O.

## PLC Session

The **PLC Session** is the module that owns the only live S7 transport. Its
implementation cycles at the configured PLC exchange period, writes only
configured PC-owned points, reads only configured PLC-owned points/status, and
publishes typed exchange results.

The real `Snap7Transport` and an in-memory test adapter meet the same transport
interface. The test adapter is not an operator-selectable Fake PLC mode.

Connection state is independent from playback state:

- connection: `DISCONNECTED`, `CONNECTING`, `CONNECTED`, `RECONNECTING`;
- playback: `STOPPED`, `RUNNING`;
- readiness: heartbeat health plus exact PLC enable/communication/timeout bits.

Stop, Reset, and sequence completion never call the disconnect interface.
Readiness loss or a transport failure latches playback `STOPPED`; recovery or
reconnection requires a fresh operator Run.

## Renderer Adapter

The **Renderer Adapter** converts an immutable Plant Runtime snapshot into
native Qt 3D transforms, materials, text, and indicators. It is downstream
from the PLC Session and cannot delay or drive the PLC cycle.

This seam provides leverage: one Plant Runtime supports the native renderer,
deterministic tests, and future renderers without duplicating plant behavior.

## Current migration boundary

The packaged native application currently implements the Scene 2 conveyor
pusher vertical slice. The legacy browser player remains a source-only
behavior reference while the other scene implementations and assets are
ported behind the Plant Runtime interface.

The application must not ship or launch Edge, Chrome, Electron, WebView,
QtWebEngine, an HTML engine, or a local HTTP server.
