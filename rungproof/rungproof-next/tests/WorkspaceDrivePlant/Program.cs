using System;
using RungProof.Next.Workspace;

internal static class Program
{
    private static int checks;
    private static WorkspaceDrivePlant NewDrive() => new();
    private static void Check(bool result, string name)
    {
        checks++;
        if (!result) throw new InvalidOperationException(name);
    }
    private static void Near(double actual, double expected, string name)
        => Check(Math.Abs(actual - expected) < 1e-6, name + $" actual={actual:R} expected={expected:R}");

    private static int Main()
    {
        try
        {
            foreach (var ms in new[] { 20, 40, 100 })
            {
                var period = ms / 1000d;
                var drive = NewDrive();
                Check(!drive.Running && drive.ActualSpeedMps == 0 && drive.TravelM == 0, "initial_stopped_" + ms);
                var distance = drive.Step(period, true);
                Near(drive.ActualSpeedMps, .75 * period, "first_scan_acceleration_" + ms);
                Near(distance, .75 * period * period, "existing_end_velocity_not_trapezoid_" + ms);
                Near(drive.TravelM, distance, "exactly_one_explicit_step_" + ms);
                Check(drive.Running, "actual_speed_running_despite_only_first_scan_" + ms);
                var count = (int)Math.Ceiling(.65 / (.75 * period));
                for (var i = 1; i < count; i++) drive.Step(period, true);
                Near(drive.ActualSpeedMps, .65, "default_speed_saturates_" + ms);
                var speedBeforePermissive = drive.ActualSpeedMps;
                var permissiveDistance = drive.Step(period, true, false);
                Near(drive.ActualSpeedMps, speedBeforePermissive - .75 * period, "permissive_loss_coasts_" + ms);
                Check(drive.Running && permissiveDistance > 0, "permissive_loss_does_not_fake_immediate_zero_" + ms);
                drive.Step(period, true);
                var beforeCoast = drive.TravelM;
                distance = drive.Step(period, false);
                Near(drive.ActualSpeedMps, .65 - .75 * period, "command_false_coasts_" + ms);
                Near(distance, drive.ActualSpeedMps * period, "coast_uses_end_velocity_" + ms);
                Check(drive.Running && drive.TravelM > beforeCoast, "measured_running_persists_without_command_" + ms);
                for (var i = 0; i < count; i++) drive.Step(period, false);
                Check(!drive.Running && drive.ActualSpeedMps == 0, "coast_eventually_zero_" + ms);

                drive.Step(period, true);
                var retainedTravel = drive.TravelM;
                drive.Stop();
                Check(!drive.Running && drive.ActualSpeedMps == 0 && drive.TravelM == retainedTravel, "explicit_stop_zero_holds_travel_" + ms);
                Near(drive.Step(period, false), 0, "stopped_false_command_does_not_creep_" + ms);
                var snapshotSpeed = drive.ActualSpeedMps;
                for (var i = 0; i < 100; i++) { _ = drive.TravelM; _ = drive.Running; }
                Check(drive.TravelM == retainedTravel && drive.ActualSpeedMps == snapshotSpeed, "reads_own_no_clock_or_extra_tick_" + ms);
                Near(drive.Step(0, true), 0, "zero_accepted_delta_no_tick_" + ms);
                Near(drive.TravelM, retainedTravel, "zero_step_holds_travel_" + ms);
                drive.Reset();
                Check(drive.TravelM == 0 && drive.ActualSpeedMps == 0 && !drive.Running, "reset_model_origin_" + ms);
                // Renderer home transforms and disabled Godot callbacks are separate
                // native acceptance; this test cannot certify an adapter's clock.
                drive.Step(period, true, false);
                Check(drive.ActualSpeedMps == 0 && drive.TravelM == 0, "estop_permissive_false_target_zero_" + ms);
            }
            var threshold = NewDrive();
            threshold.Step(1, true, true, .01, 1);
            Check(!threshold.Running, "running_threshold_is_strictly_greater_than_point01");
            threshold.Step(1, true, true, -.0101, 1);
            Check(threshold.Running && threshold.ActualSpeedMps < 0, "negative_speed_running_absolute_value");
            var reverse = NewDrive();
            Near(reverse.Step(1, true, true, -5, 3), -2, "negative_setpoint_clamps_at_minus2");
            Near(reverse.Step(1, true, true, 5, 3), 1, "reversal_ramps_without_target_overshoot");
            Near(reverse.Step(1, true, true, 5, 3), 2, "positive_setpoint_clamps_at2");
            foreach (var bad in new[] { double.NaN, double.PositiveInfinity, -.01 })
            {
                var previous = reverse.TravelM;
                var rejected = false;
                try { reverse.Step(bad, true); } catch (ArgumentException) { rejected = true; }
                Check(rejected && reverse.TravelM == previous, "invalid_delta_rejected_before_mutation");
            }
            foreach (var bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                var previous = reverse.TravelM;
                var speed = reverse.ActualSpeedMps;
                var rejected = false;
                try { reverse.Step(.02, true, true, bad); } catch (ArgumentException) { rejected = true; }
                Check(rejected && reverse.TravelM == previous && reverse.ActualSpeedMps == speed, "nonfinite_speed_preserves_state");
                rejected = false;
                try { reverse.Step(.02, true, true, .65, bad); } catch (ArgumentException) { rejected = true; }
                Check(rejected && reverse.TravelM == previous && reverse.ActualSpeedMps == speed, "nonfinite_acceleration_preserves_state");
            }
            var unchangedTravel = reverse.TravelM;
            var unchangedSpeed = reverse.ActualSpeedMps;
            var negativeAccelerationRejected = false;
            try { reverse.Step(.02, true, true, .65, -.75); } catch (ArgumentException) { negativeAccelerationRejected = true; }
            Check(negativeAccelerationRejected && reverse.TravelM == unchangedTravel && reverse.ActualSpeedMps == unchangedSpeed,
                "negative_acceleration_rejected_without_mutation");
            Console.WriteLine($"WORKSPACE_DRIVE_PLANT_PASS checks={checks} scope=pure_explicit_clock_end_velocity_integration");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("WORKSPACE_DRIVE_PLANT_FAIL " + error); return 1; }
    }
}
