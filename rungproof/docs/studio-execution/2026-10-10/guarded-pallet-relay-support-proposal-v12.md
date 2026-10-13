# Guarded pallet transfer DIN relay support proposal V12

Read-only diagnosis and proposed bounded geometry repair. No source edit, engine, input or material rollout. Root must lease implementation after capture freeze and Dot must review the concrete design.

## Actual defect and semantic identity

Individually inspected current 40-lab-3-01-guarded-pallet-transfer.scene.png: green/black DIN relay module and rail stand alone on floor at front-left of guarded conveyor. Part2 also reports this in split. This reviewer inspected the scene frame only for this additional concern.

Exact equipment training_accessory_8 in scenes/migrated/lab-3-01-guarded-pallet-transfer.scene.json is type trainingAccessory, label Guarded Pallet Transfer - safety relay/status beacon, pose(-3.3,0,3.6), config assetName safety relay/status beacon and catalogAssetId training.accessory.safety_relay_status_beacon.v1. No simulation.pointBindings or config action is assigned to this ID. Its semantic role is static illustrative training hardware, not a bound beacon, safety feedback or field guard sensor.

Catalog maps it to safety.relay.dual-channel-din.v1. tools/modeling/build_electrical_controls_assets.py safety_relay builder provides DIN rail, RELAY_BASE, SAFETY_RELAY, green terminal decks, wire entries, terminal screws, static STATUS_0/1/2 artwork and RESET_DIAL. Builder artwork labels DUAL CHANNEL SAFETY RELAY / CH1 CH2 RESET. These are decorative illustrative parts; do not claim simulated dual-channel safety validation. Catalog dimensions .45/.45/1.05 require actual delivered mesh measurement before choosing pose and bay size.

## Credible bounded repair

Preferred: retain whole relay and rail in a visible open teaching/service compartment attached to the existing operator cabinet, with a passive backplate and shallow enclosure sides. Show a continuous relay/rail -> backplate -> cabinet/enclosure support chain. Retain root/child identities and static artwork. Position its complete measured visible bounds inside the compartment, separated from existing three operator pilots and two roof heads. If the front face is too crowded, attach a distinct small open training enclosure beside the existing cabinet, sharing a visibly grounded cabinet station and clear access.

Do not merely mount exposed DIN relay on a closed operator door as though it were a pilot. A purposeful open demonstration panel with a supported enclosure/backplate is a credible alternative if an internal compartment cannot fit; label it static training hardware only if a separately approved text change is needed. Do not modify scene JSON pose/config, bindings, controls or safety behavior to solve this geometry defect.

Likely implementation location is an exact scene/ID composer helper or new narrow passive mounting partial. Actual leases are required before editing. Include the associated equipment root in the installation move inventory once if support assembly transforms can move, preserving original transforms and preventing double application. Current operator plan keeps switch_4/switch_9/switch_10; these, roof indicators5/11, optical curtain and conveyor photoeyes must remain unchanged.

## Verification needed after a leased repair

1. Measure actual visible delivered relay/rail mesh bounds and orientation; verify complete rail/backplate contact and cabinet enclosure fit rather than relying on stale catalog dimensions.
2. Native assertions for floor-orphan absence, rail/backplate/enclosure bearing, retained node paths/static artwork and unchanged other points/actions/reset transforms. Extend explicit static-accessory inventory narrowly; existing true lamp/display checks do not establish this relay support.
3. Same frozen candidate scene40 scene/split plus cabinet/service detail from the open side, individually viewed by Root and Dot. Verify relay is visible within intended compartment, pilots/roof heads remain readable, no overlapping hardware, no UI clipping.
4. Fresh affected/all77 mounting/control and environment checks as Root determines; no runtime safety state or physical commissioning claim.

Material75 remains HELD, Handle issue OPEN and all77 realism review is not whole-product completion. This is a repair proposal awaiting root lease and Dot review, not implementation or authorization.
