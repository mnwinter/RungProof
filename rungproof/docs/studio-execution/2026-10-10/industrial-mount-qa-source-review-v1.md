# Industrial mount QA source review v1

2026-10-11. Verdict: CHANGES REQUESTED before accepting this verifier as its named baseline-contract/contact gate. Read-only review of Main.IndustrialMountReview.cs and frozen inventory record structure; no engine or product edits.

## Required fixes

1. P2: source_point_binding_contract_unchanged and config_action_retained compare the active definition against a second load of the same current source. A modified scene JSON can pass both. Compare each record's frozen config/type and pointBindings binding/declaredType/owner against the current definition instead, with canonical JSON comparison where serialization order is irrelevant. The inventory contains these fields. The existing comparison can additionally prove loader consistency, but must not be labeled preservation of the pre-repair source contract.

2. P2: machineFrameReadout contact currently considers every visible mesh of the complete checkweigher as a head. WEIGH_DECK/FRAME/LEG touching WEIGH_readout_frame_arm can make head_support_contact_12mm pass while the actual readout housing remains unsupported. Restrict head bearing to WEIGH_readout_housing or another explicitly reviewed structural part of that readout, and independently require the arm's connection to the actual machine frame/deck. Contact with screen/static text alone is not structural head support. Retain the full scale root for bindings and product pose.

3. P2: cabinetStatusTower contact permits any OperatorCabinet_Door/Body/bench mesh as well as its exact coupler. A low tower intersecting the cabinet side could pass without bearing on its named roof support. Use the exact StatusTowerCoupler_ID and require head-bottom to coupler-top bearing plus enclosure-roof association, or invoke existing VerifyCabinetStatusTowers equivalent checks. Existing full control-installation verification may supply this separately, but then the combined acceptance must explicitly require that gate and this verifier's broad contact check must not claim exact bearing.

## Useful checks and bounded limits

All 77 catalog entries are traversed with Single scene matching; counts 77/121/54 catch missing records and unexpected scene duplication by exception. Required mount classes fail absent metadata, unknown kind, absent visible support or visible recognized old floor namespaces. Typed binding label-path and configured indicator-lens checks are useful structural checks. A namespace with no matches passes All legitimately only when the asset never contained a floor base; frozen delivered geometry names should be compared for required support removal where practical.

Reset pose/path comparison proves that Reset retains the current mounted structure; it does not independently prove the pre-repair controller lookup paths survived composition. Root/controller/lens/readout expectations and frozen configured bindings supply complementary coverage. This verifier explicitly does not claim output/lens projection, typed value formatting, input clicks, actual rendered readability, or physical installation acceptance; run the corresponding existing functional/render gates separately.

Supported template checks compare actual sibling head transforms against the support delta and restore, rather than metadata alone. Their scope is the currently supported template scenes; they do not prove arbitrary future imported BYO heads or non-template tower assemblies follow an unsupported editor operation.

Final native evidence is pending. Source approval after these fixes still cannot establish actual mounted render readability or whole-product completion.
