# Two-station board association source handoff

Product scope: exact lab-2-04-two-station-call only. No build/engine/native verification by this owner.

Changed files and frozen SHA256:

- src/App/DemoEnvironmentPresentation.TeachingSuites.cs: 5B8FD47AA508637D65E2BF0BB732994AD0F9407A66648CCD78FA33B2EBFE8EA2
- src/App/Main.DemoEnvironment.cs: AA52E7AE0A35E32F11A75807C107507487F00A8C73B49195337EDB90A576371C

Before copies beside this note: teaching-suites-before-two-station-v15.cs and main-demo-environment-before-two-station-v15.cs. No other product file touched by this owner.

Public ConfigureSharedCallBoardAssociation(SceneDefinition,Node3D) executes after environment attachment via a narrowly guarded Main.DemoEnvironment call. It checks exact scene/layout, two switch IDs, config action/face, unique toggle action/point, unique switch binding, and unique PC BOOL point. It sorts the actual attached field controls' GlobalPosition.X and the actual board backplates' GlobalPosition.X independently, then associates them one-to-one. Equal/nonfinite X rejects ambiguity. Mirrored room u and screen coordinates never select station identity.

Only passive board text, sibling names and provenance metadata change. All four board parts (Backplate, Instruction, LightBracket, SupportedFixture) receive coherent final North/South names after temporary unique names avoid rename collisions. Metadata records sourceEquipmentId, sourcePoint, sourceAction, associatedControlWorldX and bounded associationScope. Board geometry, controls, pick targets, bench and runtime are untouched. Main.cs and root's capture file are untouched.

Root acceptance pending: compile; native same-world-X ordering with both room mirror signs; exact retained control actions/bindings and source guards; actual purpose/operator render showing coherent North/South association. This is logical presentation correspondence, not physical station separation or clearance proof. Handle OPEN; material75 HELD.
