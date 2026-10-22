# Independent Lab 2.6 review

One full native frame established scene/layout; lab206-evidence-compact.jpg contains physical output, visible symbolic values and runtime for all eight monitored cases. Full sources and hashes are preserved in the derived manifest. The extra native down/up captures and action plan were also inspected. No UI input or app/backend execution by this reviewer.

| Case | Actual request_held | Actual ready_light | Actual attention_light | Visible result |
|---|---|---|---|---|
| lab206-normal | False | True | False | White lit, amber dark / RUNNING |
| lab206-pressed | True | False | True | White dark, amber lit / RUNNING |
| lab206-released-toggle | False | True | False | White lit, amber dark / RUNNING |
| lab206-pressed-repeat | True | False | True | White dark, amber lit / RUNNING |
| lab206-app-stop | True | False | False | Both dark / STOPPED |
| lab206-app-run | True | False | True | Amber restored / RUNNING |
| lab206-active-reset | False | True | False | White lit / STOPPED, scan0 |
| lab206-reset-run | False | True | False | White lit / RUNNING |

The tested maintained toggles produce complementary indications while running. App Stop removes both outputs while retaining inputTrue; Run restores amber. Reset from an active request clears it and restores the declared initial white indication, which stays white on Run. These are scoped screen-supported cases with the GUI-loaded fixture, not proof of every lesson/editor path or an app-supplied solution.

## Native release discrepancy

lab206-held-ui.json records an OS mouse-down on the scene request button, 1.1seconds held, followed by OS mouse-up and a0.8second observation interval. lab206-native-held.png and lab206-native-mouseup.png both show amber on and white off; runtime remains RUNNING. The input does not visibly spring back after mouse release in this case.

The same screen description and help call the request button spring-return, while the help action table explicitly defines Press/release request as toggle. This establishes a documentation/control-contract discrepancy. Behavior agrees with the explicit maintained-toggle action; it is not evidence the PLC complement logic failed. Scope is appended to QA-SPEC-002 rather than creating a duplicate issue. A second-click toggle release was separately successful; it must not be called physical mouse-up release.

No additional functional failure observed and no repair plan selected. Missing cases include rapid/overlapping input, phase-specific input changes while stopped, exact accepted-scan response, navigation/persistence and keyboard access.
