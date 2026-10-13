# Industrial mount QA source fix review v1

2026-10-11. Verdict: APPROVED FOR NATIVE EXECUTION. The three prior P2 findings are addressed in the exact reviewed source SHA-256 30A62EE5EC869AFBA6D1FC76B4CB4EA27509761FDB4EE5E92CFC8538C12B555F.

Compared Main.IndustrialMountReview.cs against the saved before-p2-v1.cs. No product edits, engine, build, screen or input use during review.

1. All 77 current authored scene files are now checked against their immutable V7 source SHA-256. Slash normalization matches the frozen manifest's Windows paths to inventory source paths. Frozen config and point bindings are compared through canonical property ordering, with actual binding arrays sorted for order-independent set comparison. Exact scene hashes additionally protect point types/owners and the two annotation-only records without binding snapshots. Same-current-source consistency checks remain complementary rather than the only preservation evidence.
2. The scale's retained head contact candidates are now specifically its readout housing/screen; general deck/frame/leg meshes cannot satisfy that head-to-arm predicate. Exact arm/post existence and separate arm-to-post and post-to-machine-frame contacts establish the intended support chain. Full machine/root pose still remains unchanged.
3. Roof head contact now accepts only its exact named coupler. Additional checks require head-bottom/coupler-top bearing within 3 mm and full retained-head X/Z envelope inside the cabinet body footprint in the cabinet's local frame. Transform composition is applied before forming the local AABB, avoiding a loose rotated world-AABB shortcut.

These checks establish source/structural native regression when executed successfully; they do not establish label readability, actual click targets, typed value formatting, lens output projection, product scan behavior or physical strength/safety. Existing functional and actual-render gates remain required. Native results are pending and no final acceptance is implied by this source approval.

## V9 compiler qualification and native evidence addendum

Reviewed the exact before-array-qualification-v1.cs delta independently. The only code change is System.Array.Empty<MeshInstance3D>() qualification, resolving the Main.Array name collision; one trailing blank line is nonsemantic. No QA predicate was weakened. Source approval applies to current SHA-256 1EACA2CB3B1C848C6DF1D5F6F6F0F7E16861FF747E085DB7E7C635216959DDE4, verified directly. The earlier source hash remains historical.

Independently read industrial-mount-all77-v9.log and industrial-mount-review-c44168841b274372994e39070180c0ec.json. The log reports 2205 passed/2205 cases, zero failures, ROOT_GODOT_EXIT=0 and no ERROR/FAIL/EXCEPTION lines. JSON independently records checks=2205, an empty failures array, 177 inventory evidence records spanning all 77 distinct scenes, and the explicit native-geometry/source-only scope. Evidence groups contain 66 cabinet status heads, one machine-frame readout, 43 operator-panel records, 47 student-bench records and 20 retained non-panel/annotation records. Counts include record-level overlap between light/display inventories; they are not all unique equipment counts.

This closes the bounded all-77 source-contract/mount-structure native gate on the root-designated V9 candidate. Root reports DLL SHA-256 4597A187B712DD314D10B8EE8A7C2994858DB3C79DB7CE254C9B5D509DDCAEA9; this review independently checked the QA source and execution artifacts, not that DLL's file hash. Existing control-installation functional regression, rendered mounting/readability and repeat-cycle final acceptance remain pending. No relabeling of historical source/candidate evidence.
