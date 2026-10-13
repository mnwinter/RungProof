# Cabinet legend visual fix, V10 source

Bounded source fix after root's actual V9 operator captures showed initial characters spilling off the black physical plates: Chicken LOAD & WEIGH / PRINTER READY and related drive/wastewater legends. Changed only the leased legend block in SceneComposer.ControlInstallations.cs. No build, engine or input was used by the implementer.

The original switch builder places the centred Label3D at the imported Blender text mesh's left anchor. Enlarging PixelSize and plate scale preserved that incorrect anchor. The fix measures the resized BUTTON_legend_plate through accumulated common-parent transforms, centres the existing OperatorFaceLabel on that physical plate and places it .003 m beyond its front face. Horizontal/vertical alignment are explicitly centred. Actual configured text is measured with the selected font (or ThemeDB.FallbackFont), and PixelSize is limited by plate width/height after inset margins. Text, child identity, tags, configuration, selection targets and bindings are unchanged.

APIs verified against installed GodotSharp 4.7.2 XML: Font.GetStringSize and Label3D.Font, HorizontalAlignment, VerticalAlignment. GetStringSize was also already used in this repository's LadderEditorCanvas. Single-line sizes are measured individually for multiline text; the measured Mg line box supplies minimum line height.

Before snapshot/hash: .superpowers/sdd/repeat-cycle-execution-tasks/before/mount-legends-v9. After snapshot/hash: corresponding after/mount-legends-v10. Source whitespace check passed. Build, true rendered text fit/readability and binding/geometry regressions remain root acceptance gates. Font fit is not proof that tiny overview text is comfortably readable.

Existing authored generic START/chain labels were not renamed under this visual lease. This change fixes positioning/contrast and text fitting only; it does not certify those existing operator semantics.

## Capture-only view proposal

Existing CLI permits teaching/operator/side/motor. Operator and side target ControlInstallation, so neither can guarantee a close Chicken machine-frame arm witness; motor explicitly requires motorTestMounted and must not be misused. A dedicated scale capture view would need a separate root-approved diagnostic camera lease. It should measure training_accessory_5 frame/readout bounds and aim from the room's existing open side without changing normal/saved cameras.

For four-tier tower and EV details, existing operator/side captures are useful first checks, but their fixed target/distance may crop taller assemblies or make ink small. If actual frames fail, use a bounded diagnostic camera-only fit based on the installation body plus retained heads/readouts. This proposal adds no CLI option or product camera change and has not been implemented.
