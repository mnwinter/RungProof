# Managed shutdown diagnosis: bounded evidence

Observed failures occur after RUNTIME_CAPTURE_SAVED, during engine teardown. Preserved logs include:
- `.tools/studio-verification/varied-thirtyeight-coherent-v1/24-lab-4-12-cable-cut-length.split.png.log`
- `.tools/studio-verification/varied-fifty-coherent-v1/06-lab-11-12-mobile-traffic-lights.scene.png.log`
- `.tools/studio-verification/all77-material-coherent-v2/25-lab-2-11-inbound-tote-stop.scene.png.log`
- `.tools/studio-verification/all77-material-continuation-v2/01-lab-2-11-inbound-tote-stop.scene.png.log`

Logs report native CSharp binding teardown failure and unsafe StyleBoxFlat/ButtonGroup references. Saved PNGs from failed processes are excluded from qualified render manifests. Their existence is not successful capture evidence.

The no-concurrent-process retry still failed, so concurrent verifier activity is not a sufficient explanation. A process-local DOTNET_gcConcurrent=0 experiment also failed after two successful isolated probes; background GC disabling did not prevent the problem. No production runtime configuration was changed.

Main.cs now drains unreachable managed wrappers with GC.Collect, WaitForPendingFinalizers, GC.Collect immediately before the capture-only quit. This occurs after PNG saving; normal startup and normal exit paths are unchanged. Six successive full/split inbound probes passed in `.tools/studio-verification/all77-inbound-finalizer-probe-v3-*`. The coherent catalog capture is exercising the same binary: `83E67BF2AA996B47142C9C67614696962047E9DC94EC2FBD22D2A835B0E606F4`.

This is a candidate mitigation for observed capture teardown. The root cause and ordinary product shutdown impact remain unknown. Six clean probes do not prove a fix or release stability. Current build/native environment77/native controls77/offline dryer checks pass; those gates are independent of normal product exit reliability.

Next action: after the exclusive capture process releases override.cfg, run a no-focus secondary-display normal-runtime probe with automatic engine exit and no --capture argument, retaining its entire log and exact binary hash. It will not use keyboard/mouse or change focus. Record automatic runtime teardown separately from manual user close/save prompts.

Current V3 follow-up: the coherent batch completed77 scenes /154 qualified captures with terminal exit0. Three no-capture normal-runtime automatic-exit probes passed on the same83E binary: inbound-01, inbound-02 and motor-02 logs under .tools/studio-verification/normal-product-shutdown-v3-*.log (identity sidecars retain hashes). These exercise normal startup/engine-requested exit without the capture-only finalizer drain. Manual close/save prompts and root cause remain unverified. motor-01 was excluded: an incorrect scene ID loaded the default conveyor; the probe correctly rejected it, rather than qualifying the wrong scene. No native teardown failure was observed in that rejected run.
