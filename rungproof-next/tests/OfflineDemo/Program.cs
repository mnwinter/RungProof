using System;
using RungProof.Next.Connections;

var failures = 0;
var factoryCalls = 0;
void RejectsConstruction(Action construct, string label)
{
    try { construct(); failures++; Console.WriteLine($"FAIL {label}: external client constructed"); }
    catch (NotSupportedException) { Console.WriteLine($"PASS {label}"); }
}
RejectsConstruction(() => { using var client = new ExternalPlcRuntimeClient("untrusted-parent-path"); }, "path_constructor_disabled");
RejectsConstruction(() =>
{
    using var client = new ExternalPlcRuntimeClient(() =>
    {
        factoryCalls++;
        throw new InvalidOperationException("An offline build must never request a bridge channel");
    });
}, "injected_constructor_disabled");
if (factoryCalls != 0) failures++;
var disconnected = new DisconnectedRuntimeClient();
if (disconnected.State != ConnectionState.Disconnected) failures++;
Console.WriteLine($"OFFLINE_DEMO_POLICY failures={failures} bridgeFactoryCalls={factoryCalls}");
return failures == 0 ? 0 : 1;
