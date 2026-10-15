using System;
using RungProof.Next.Scenes;

internal static class MobileTrafficTests
{
    private static int _checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); _checks++; Console.WriteLine("TRAFFIC_PLANT_CHECK " + name + "=True"); }
    public static int Main()
    {
        var p = new MobileTrafficPlantModel();
        Check(p.CrossingClear && !p.Vehicles[0].Visible && !p.Vehicles[1].Visible, "reset_empty_clear");
        Check(p.Request(0) && p.Request(1) && !p.Request(0), "queue_one_each_no_reentry");
        p.Step(.02, true, true, true, true, true);
        Check(p.SignalConflict && p.Vehicles[0].Progress == 0 && p.Vehicles[1].Progress == 0, "opposing_green_blocks_both_entries");
        foreach (var loss in new[] { 0, 1, 2 }) {
            p.Step(.02, loss != 0, loss != 1, loss != 2, true, false);
            Check(p.Vehicles[0].Progress == 0, "missing_entry_permissive_" + loss);
        }
        p.Step(2, true, true, true, true, false);
        Check(p.Occupied(0) && !p.CrossingClear && p.Vehicles[1].Progress == 0, "actual_crossing_occupancy");
        var before = p.Vehicles[0].Progress;
        p.Step(.02, false, false, false, false, true);
        Check(p.Vehicles[0].Progress > before && p.Vehicles[1].Progress == 0, "ready_loss_clears_committed_vehicle_blocks_opposite_entry");
        p.Step(.02, false, false, false, true, true);
        Check(p.SignalConflict && p.Vehicles[0].Progress > before && p.Vehicles[1].Progress == 0, "conflict_does_not_strand_committed_crossing");
        var supported = true;
        for (var i = 0; i < 500; i++) {
            p.Step(.02, false, false, false, false, false);
            supported &= Math.Abs(p.X(0)) + MobileTrafficPlantModel.HalfLength <= 5 && Math.Abs(p.X(1)) + MobileTrafficPlantModel.HalfLength <= 5;
        }
        Check(supported, "supported_across_500_accepted_route_samples");
        Check(p.Vehicles[0].Complete && p.CrossingClear && p.Vehicles[0].Visible, "retained_clear_endpoint");
        var endpoint = p.X(0); p.Step(1, true, true, true, true, false);
        Check(p.X(0) == endpoint && p.Request(0), "endpoint_stable_explicit_requeue");
        p.Reset(); Check(p.CrossingClear && !p.Vehicles[0].Visible && !p.SignalConflict, "reset_clears_requests_pose_fault");
        foreach (var seconds in new[] { -1d, double.NaN, double.PositiveInfinity }) {
            var rejected = false; try { p.Step(seconds, true, true, true, true, false); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "invalid_time_rejected");
        }
        Console.WriteLine("MOBILE_TRAFFIC_PLANT_VERIFY PASS checks=" + _checks + " model only; native/controller pending");
        return 0;
    }
}
