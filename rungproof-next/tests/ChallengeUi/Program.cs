using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.App;
using RungProof.Next.Challenges;

internal static class Program
{
    private static void Require(bool value, string detail) { if (!value) throw new InvalidOperationException(detail); }
    private static int _passed;
    private static void Test(string name, Action action) { action(); _passed++; Console.WriteLine("PASS " + name); }
    private static readonly ChallengeRunIdentity Identity = new("RP-CANON-1", "conveyor", 1, "program", "packet", "plant", "engine", "engine-hash", "eval", "eval-hash", 20, 5);
    private static ChallengeDefinition Definition() => new(1, "conveyor", 1, "scene-1-conveyor-stop", "conveyor-v1", 20, "Conveyor", "Stop at beam", ["Release", "Remember", "Inhibit"], new(1, .5, .2, .5, .1), Enum.GetValues<ChallengeCaseKind>().Select((kind, index) => new ChallengeTestCase($"CV0{index + 1}", kind, 250)).ToArray(), [], "conveyor-bool-v1", new("starter", "hash", 20));
    private static void Main()
    {
        Test("Partial cancellation retains all seven ledger rows and Not Run", () =>
        {
            var result = new ChallengeRunResult(Identity, ChallengeOutcome.Cancelled, [new("CV01", ChallengeOutcome.Passed, "ok", "idle", "idle", null)], [], [], "cancelled", "user cancelled");
            var ledger = ChallengePresenterModel.Ledger(Definition(), result, []);
            Require(ledger.Contains("CV01: Passed") && ledger.Split("Not Run").Length - 1 == 6, "Missing trials must never appear passed");
            Require(ledger.Contains("Cancelled"), "Outcome is explicit");
        });
        Test("Passed persistence failure remains Passed with readable recovery status", () =>
        {
            var result = new ChallengeRunResult(Identity, ChallengeOutcome.Passed, [], [], [], "passed", "all seven passed");
            var state = new ChallengePresentationState(ChallengeUiState.Finished, Identity, result, true, true, false, false, ChallengePendingAction.None, ChallengePersistenceStatus.NotSaved, "Passed");
            var text = ChallengePresenterModel.Status(state);
            Require(text.Contains("Passed") && text.Contains("Progress not saved") && text.Contains("CURRENT PROOF"), "Persistence is separate from behavioral outcome");
        });
        Test("Retired proof never inferred from historical completion", () =>
        {
            var state = new ChallengePresentationState(ChallengeUiState.Finished, null, null, false, true, false, false, ChallengePendingAction.None, ChallengePersistenceStatus.Saved, "Draft changed");
            Require(!ChallengePresenterModel.Status(state).Contains("CURRENT PROOF"), "Authoritative false must remain false");
            var ledger = ChallengePresenterModel.Ledger(Definition(), null, [new(Identity, DateTimeOffset.UnixEpoch, [])]);
            Require(ledger.Contains("Historical local completions: 1") && ledger.Split("Not Run").Length - 1 == 7, "Disk history doesn't mark trial rows passed");
        });
        Test("Cleanup visibly retains read copy save and pending action", () =>
        {
            var state = new ChallengePresentationState(ChallengeUiState.Cancelling, Identity, null, false, false, true, true, ChallengePendingAction.Navigate, ChallengePersistenceStatus.NotAttempted, "Timed out");
            var text = ChallengePresenterModel.Status(state);
            Require(text.Contains("Navigate") && text.Contains("read/copy/save") && text.Contains("editing locked"), "Pending cleanup remains intelligible");
        });
        Test("Hints reveal only the requested prefix and clamp bounds", () =>
        {
            Require(ChallengePresenterModel.Hints(Definition(), 1).Contains("Release") && !ChallengePresenterModel.Hints(Definition(), 1).Contains("Remember"), "Sequential hints");
            Require(ChallengePresenterModel.Hints(Definition(), -1) == "" && ChallengePresenterModel.Hints(Definition(), 50).Contains("Hint 3: Inhibit"), "Bounded prefix");
        });
        Test("Trace includes binding values plus presence independent of coordinate zero", () =>
        {
            var inputs = new Dictionary<string, bool> { ["simulated_photoeye"] = true };
            var row = new ChallengeObservation(Identity, "CV03", 1, 51, 51, TimeSpan.FromMilliseconds(1020), inputs, new Dictionary<string, bool> { ["conveyor_running"] = false }, new(new Dictionary<string, bool>(), new Dictionary<string, double> { ["object_position"] = 0 }, new Dictionary<string, string> { ["component_state"] = "discharged" }, false, 1), ["diagnostic"]);
            inputs["simulated_photoeye"] = false;
            var text = ChallengePresenterModel.TraceRow(row);
            Require(text.Contains("HISTORICAL") && text.Contains("simulated_photoeye=TRUE") && text.Contains("conveyor_running=FALSE") && text.Contains("present: False; completed: 1") && text.Contains("diagnostic"), "Copied row includes actual state, not false pass from position zero");
        });
        Test("Keyboard walks insertion contact insertion contact insertion output and reverses", () =>
        {
            var expected = new ChallengeKeyboardPosition[] { new(-1, 0, false), new(0, -1, false), new(-1, 1, false), new(1, -1, false), new(-1, 2, false), new(-1, -1, true) };
            var selection = expected[0];
            foreach (var position in expected.Skip(1)) { selection = ChallengePresenterModel.Navigate(2, selection, ChallengeHorizontalNavigation.Right); Require(selection == position, "Correct logical selection"); }
            foreach (var position in expected.Reverse().Skip(1)) { selection = ChallengePresenterModel.Navigate(2, selection, ChallengeHorizontalNavigation.Left); Require(selection == position, "Reverse logical selection"); }
            Require(ChallengePresenterModel.Navigate(2, selection, ChallengeHorizontalNavigation.Left) == expected[0], "Left boundary");
        });
        Test("Empty branch Home End insertion and output stay reachable", () =>
        {
            var home = ChallengePresenterModel.Navigate(0, new(-1, -1, true), ChallengeHorizontalNavigation.Home);
            Require(home == new ChallengeKeyboardPosition(-1, 0, false), "Empty insertion");
            Require(ChallengePresenterModel.Navigate(0, home, ChallengeHorizontalNavigation.End).Output, "Empty output");
        });
        Test("Seven disclosure descriptions are distinct and cover priority reset", () =>
        {
            var descriptions = Enum.GetValues<ChallengeCaseKind>().Select(ChallengePresenterModel.CaseDisclosure).ToArray();
            Require(descriptions.Distinct().Count() == 7 && descriptions.Any(text => text.Contains("priority")) && descriptions.Any(text => text.Contains("repeat")), "Complete learning disclosure");
        });
        Test("Empty branch and selector counts retain explicit no-selection", () =>
        {
            Require(ChallengePresenterModel.ClampSelection(0, 0) == -1, "Never select zero from an empty selector");
            Require(ChallengePresenterModel.ClampSelection(1, -1) == 0 && ChallengePresenterModel.ClampSelection(2, 9) == 1, "Nonempty clamps safely");
        });
        Test("Deep branch keyboard selection reveals both operand and symbol bounds", () =>
        {
            var offset = ChallengePresenterModel.RevealVertical(700, 300, 0);
            Require(offset == 455 && offset <= 645 && offset + 300 >= 755, "Entire selected operand visible below");
            Require(ChallengePresenterModel.RevealVertical(200, 300, offset) == 145, "Entire selected operand visible above");
            Require(ChallengePresenterModel.RevealVertical(250, 300, 100) == 100, "Stable scroll if already visible");
        });
        Test("Challenge palette exposes ordinary BOOL structure and blocks prohibited instructions", () =>
        {
            foreach (var name in new[] { "AddNormallyOpen", "AddNormallyClosed", "AddAssignCoil", "AddBranch", "RemoveBranch", "AddRung", "RemoveInstruction" })
                Require(ChallengePresenterModel.IsPaletteInstruction(name), "Eligible authoring path visible");
            foreach (var name in new[] { "AddRisingEdge", "AddFallingEdge", "AddSetLatch", "AddResetUnlatch", "AddTimerOnDelay", "AddCountUp", "Unknown" })
                Require(!ChallengePresenterModel.IsPaletteInstruction(name), "Unsupported palette entry hidden");
        });
        Console.WriteLine($"CHALLENGE_UI_MODEL_TESTS passed={_passed} failures=0; native input and rendering not exercised");
    }
}
