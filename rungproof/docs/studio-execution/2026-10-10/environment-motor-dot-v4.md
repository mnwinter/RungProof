# Dot motor fixture review V4

Bounded placement disposition: PASS WITH LIMITS. Human final visual approval remains false; this does not supersede the all77 V3 CHANGES_REQUIRED verdict for other environments.

Personally inspected six individual images from motor-rigs-refinement-v4/capture-manifest.json: scene and split for lab-10-04-motor-enum-state, lab-10-05-motor-struct-data, lab-10-06-ten-motor-array-startup. All six PNG hashes match their manifest and identity records. All identities name the requested scene/view, 1600x900, unchanged assembly C69F1C772CF0752B6587D44AB7DBC2184F9DCB3135369FB778F502974E7844AC. Normalized individual logs contain RUNTIME_CAPTURE_SAVED and no ERROR/FATAL diagnostic.

Single motors now visibly rest on grounded steel rigs; the array has two coherent continuous grounded benches. This resolves the observed loose-floor presentation issue. No new cabinet-head obstruction is visible. Lab10.5 split hides most of its motor behind the existing blue machine; array split shafts and numeric readouts are too small to claim literal legibility. Close/side bearing evidence and actual motion fixtures are separate from these stopped overview frames.

## Read-only runtime audit

SceneSimulationRuntime.ApplyBindings explicitly opts into combineRunningCommands for Lab10.6. Each distinct motor_i receives OR(motor_array_run, motor_i_run, motor_commands[i]); there is no priority/fallback or last-false override. CommitAggregateSceneOutputs expands the typed ARRAY into leaves; ProjectAggregatePoints reconstructs aggregate roots from leaves. Individual scalar aliases are independent. Command-count text reports requested commands, not measured shaft feedback.

Lab10.5 running binds motor_effective_enable = OR(motor_enable, motor_command.enable). record_ready contributes separate effective feedback, not an additional visualization gate. Lab10.4 running binds motor_running.

ConfigureMotorTestFixtures runs before SceneComposer.Compose returns. Main adds the composition root, allowing controller Ready to cache local KIN transforms, then constructs SceneSimulationRuntime, which captures direct equipment root Position. Pre-return root-Y placement therefore becomes runtime reset home without rewriting local shaft geometry. EquipmentMotionController.ResetMotion clears RunCommand, resets speed fraction to1, and restores authored local transforms/scales. Stop clears motion command without homing; actual Main virtual-session Stop/Reset must be tested separately from native direct-output fixtures.

## Required focused verification

Keep all12 motor IDs and controller identities distinct. Exercise all10 isolated scalar commands, all10 isolated ARRAY indices, group-only all10, mixed group/scalar/ARRAY commands, and clearing one true alias while another remains true. Only selected local shaft transforms may rotate; stationary stator/root XZ/basis and support remain fixed. Assert lifted root-Y remains the measured foot datum through runtime Reset and every shaft returns to its authored local transform. Verify actual Main controller Stop/Reset separately, and label direct-output projection fixtures honestly. No physical wiring, torque, load, safety, or commissioning proof is inferred.