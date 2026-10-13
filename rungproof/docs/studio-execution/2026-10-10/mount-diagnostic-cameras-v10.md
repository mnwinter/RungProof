# Mount diagnostic camera source, V10

Separate root-granted lease changed only Main.ControlInstallationCapture.cs and capture_demo_nonfocus.ps1. No product/saved camera, scene geometry, runtime, state, input or output behavior changed. No engine, screen, input or build used by implementer.

New exact StationView values:

| View | Scope | Actual geometry framed |
|---|---|---|
| machine-support | Chicken label scene only; early wrapper and native guard | Actual visible WEIGH_* scale frame/deck/feet, retained head and repaired post/arm |
| mount-detail | Existing installation required | Visible support enclosure plus all explicitly associated retained panel/roof heads, including complete four-tier towers |
| ev-readouts | EV charging scene only; early wrapper and native guard | Actual training_accessory_5 and ev_meter_b heads, close view of both retained meter screens |

Each view projects all eight actual visible mesh corners through GlobalTransform, measures width/height/depth relative to the chosen view direction and fits the real scene aperture at vertical FOV 35 with 16 percent margin. Hidden old stands cannot enlarge the fit. Existing SceneViewportRect, ChallengeSceneAperture and CaptureCurrentView synchronize the evidence camera to the current scene viewport. Machine-support uses an oblique open +Z-side direction to show the retained full scale and support connection; no mesh is moved or hidden for the picture. mount-detail uses the enclosure's actual operator-facing basis; EV faces its actual enclosure.

Verified APIs against local GodotSharp XML and existing repository patterns: Aabb.Expand, Node3D.IsVisibleInTree, Camera3D.KeepAspect.Height, Fov, GetViewport/GetVisibleRect, mesh GetAabb and GlobalTransform. Source whitespace check passed. Before/after copies and hashes are in the SDD before/after machine-support-capture-v10 directories. Build and actual framing/contact/ink readability await root's native captures. A fit calculation is not proof that an occluded connection becomes visible; root must inspect the resulting image and retain any unresolved limitation.

Root examples use the existing nonfocus secondary-display wrapper with -StationView machine-support, mount-detail or ev-readouts and a fresh CapturePath; normal teaching/operator/side/motor views remain available. Primary screen and keyboard/mouse remain reserved.
