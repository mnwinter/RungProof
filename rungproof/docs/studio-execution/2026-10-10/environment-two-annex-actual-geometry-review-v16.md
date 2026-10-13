# V16 actual two-annex geometry review

Read-only review of environments-v16.json (SHA F5A4308F03C83548631D5AF2C1B66426A32C0054BE9BFF75F7EBC52B7BCF28E1). Root reports DLL03D36DF05C938EF51650AB9FC56A98EF56189498626E87F4FBBA009D2F228328, full77 environment verifier exit0/PASS with zero errors/warnings. No source edit, build or engine from reviewer. Current leased-file hashes still exactly5031C48788A7A88D52E685E4FEB2B456BA500F85ECB7A47D19A1697DAC5666BB and1CB11F9F38E504C48C43ED7515A930448B08D8B14B209584445DDF01CE3CE44C.

## Inspection actual measurements

- WholeIndustrialFoundation actual minimum(-9.828,-.125,-6.006916),size(20.118,.120000005,11.890499):top-.004999995. Only this slab owns inspection bay/apron; no AnnexContinuousFoundation remains in this bay.
- Annex return actual depth2.000m; back facade outerX-8.518001,innerX-8.3380007. Counter actual X[-7.0280004,-6.62800078],Z[-2.4397662,-.8397661],Y[.99,1.049999943]. Actual clear working depth1.3100003m.
- Rear doorway inferred independently from actual flank meshes:Z[-2.2397661,-1.0397660],width1.2000001, height2.2. Both original portal cases verified by root; exported checkedOpenings2,checkedStaticRoutes1.
- Counter each of4foot→slab,4leg→foot,4table→leg contacts independently evaluated from measured faces, requiring abs(vertical gap)<=.012m AND positive X/Z overlaps. All12 pass: foot/slab residual5e-9m; leg/foot9e-9m; table/leg3.6e-8m. Foot X/Z footprint.0600004/.0599999m; leg-bearing overlaps~.035m each axis.
- Tray actual bottom1.0500001→table top1.049999943:gap.000000157m; positive footprint overlap .3000002x.5m. Contact passes same unchanged12mm tolerance, with no large floating gap hidden by tolerance.
- Actual back roof X[-8.428,-8.02800038],Y[3.22,3.37999985],Z[-2.964766,-.314766]. Back wall extends toY3.3, so roof/back overlap .09m inX/.08m inY; side-return bearings also retained. Roof clear above approach Y2.

Full approach is reconstructed from actual door center/back inner face/counter rear edge plus approved exterior start (node metadata is not exported): approximatelyX[-9.7280005,-7.0830005],Y0–2,Z[-2.2397661,-1.0397660].1.2m square endpoint working area has55mm clear to wall/counter. Floor outer bearing margin100mm, and complete route/endcaps lie inside floorXZ. An independent positive-overlap test against EVERY actual exported inspection upright environment mesh (all axes>10um) found ZERO route obstruction, including actual new counter/legs/tray/backfacade/returns. No new-bay namespace exclusion used in this final check. Jamb edges coincide with sweep edges: zero lateral tolerance remains explicit. Root's native gate consumes actual localRouteBounds and checks plant+visible environment; reviewer reconstruction does not replace an exported actual route record or root's plant checks.

## Vision actual measurements

- Four foot→ContinuousAnnexFoundation and4upright→foot contact pairs pass positiveXZ footprint plus12mm vertical-gap limit. Feet actualbottom-.005,foundationtop-.005; upright bottom.024999976 vsfeet top.024999997:residual2.1e-8m.
- Shelf actual footprintX[-15.2,-14.80000038],Z[-8.153822,-6.3538223],size.40x1.8m. Shelf tops approximately.45/.95/1.45. Each of3shelves overlaps ALL4uprights in positiveXYZ, with~.04m intersections peraxis. Thus no unsupported shelf inferred merely from node names.
- Three measured tray→shelf contacts pass same limit:gap magnitudes1e-8,4e-8,6e-8m; positiveXZ bearing .29999924x.39999962m each. All tray footprints remain within their actual shelf faces.
- Caption bearing actualX[-14.83,-14.809999542],Y[1.3100001,1.410000005],Z[-8.078822,-6.4288224]. Upper shelf underside1.4100001 gives95nm face-gap residual; positiveXZ bearing .020000458x1.6499996m. Plate stays INSIDE frame footprint and supported by upper shelf underside. It does not float between tiers.
- Caption Label3D ink is not a MeshInstance3D in this export. This review establishes its plate bounds/contact only; actual renderer/label-envelope check must qualify glyph fit/readability separately.
- Actual caption/shelf/foot extents fit existing connected recess. Existing lower tier top.45 remains below framed window sill.8375, so geometry cannot prove all3tiers visible in normal/split.

## Scope and remaining evidence

PASS bounded actual visible-AABB support/contact/route-footing measurements above. Root77 gate results are reported as root evidence, separate from these independent calculations. Export contains architectural meshes but no full plant list/route/camera record; previous V15b full-world candidate audit is before evidence, and current root gate checks plant obstruction. No triangle collision, dynamic clearance, physical accessibility/commissioning or safety claim follows. Actual15 normal/split/purpose captures and root/Dot individual visual acceptance remain pending at report creation; no rendered-purpose completion claim. No material75 approval or whole-product release claim.
