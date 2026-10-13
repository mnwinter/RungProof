# V22b material render review — part 1

Scope: catalogue rows 1–26, scene and split, 52 individual actual PNG inspections. Human material rollout authorization reported by root. Candidate DLL SHA256: `7D1F6FC463D885BF6076EB5C4E32ABAFC74B9FDDDC2EBFA950A76B53A41E7813`. Root reports 77 native environment checks passed; this review does not independently rerun that gate.

Result: no hard material-related visibility blocker observed within these 52 stopped overview frames. Color differentiation is visible across rooms and equipment, and critical lenses, controls, belts, machine silhouettes and supports remain distinguishable. This is bounded visual review, not a whole-product completion claim or dynamic picking/behavior test. No pending images within assigned rows.

Texture quality remains uneven: broad painted floors in several rooms read almost flat at this camera scale, while the motor-STRUCT wood floor/wall and workstation tabletop show strong grain. Earlier V12 images inspected for the first rooms showed more obvious floor mottle; intervening geometry/camera changes mean that comparison is qualitative, not proof of a material-code defect. Small overview legends, cropped contextual boards and projection overlaps are recorded below without claiming they were newly caused by materials.

| Row / exact scene ID | Actual scene + split observations |
|---|---|
| 01 conveyor-cell | Terracotta/ochre walls and muted green floor separate blue conveyor, cartons and pale cabinet. Retained E-stop, roof tower and grounded legs visible. Inspection counter visible through opening. Broad floor reads nearly flat; contextual text tiny in split. |
| 02 equipment-gallery | Olive floor and cream/metal structure retain blue equipment and orange robot contrast. Full gallery remains visible; overview scale makes individual controls and boards small. No new support loss observed. |
| 03 lab-10-01-drive-alarm-code-string | Cream diagnostic room/brown floor distinguish blue CNC and mint cabinet. Diagnostic cupboard details visible in left recess. CNC/cabinet projection obscures some context in split; readout needs detail view. |
| 04 lab-10-02-chicken-label-print | Pale washdown walls, green floor and blue printer distinguish retained conveyor/scale/supports. Supply cupboard visible. Broad floor visually flat; readouts too small to validate text at overview scale. |
| 05 lab-10-03-vision-package-sorter | Gray walls/red trim distinguish four lanes, scanners and rotary machinery. Reference shelves visible in recess, lower parts partly behind sill. Cabinet small in split; no new material occlusion. |
| 06 lab-10-04-motor-enum-state | Cool gray/blue walls, dark teal floor, warm cabinet and blue motor bench remain distinct. Review recess board partly masked by cabinet projection. Broad floor smooth; numeric text needs detail view. |
| 07 lab-10-05-motor-struct-data | Clearly visible wood grain on floor and rear wall; cream wall and blue CNC separate from mint cabinet. Strongest texture expression in this group. Split machine projection hides some motor context, not a demonstrated material regression. |
| 08 lab-10-06-ten-motor-array-startup | Ten supported motor zones remain countable, blue/gray machinery clear against pale green room. Broad floor only subtle grain; split labels tiny. |
| 09 lab-11-06-wastewater-collection | Tan grain apron, green hut, gray tanks and silver header retain contrast. Cream route, pumps and cabinet visible. Radar caption tiny in overview; pipe silhouettes retained. |
| 10 lab-11-07-multi-conveyor-pallet-route | Gray/khaki hall and olive floor retain silver/blue conveyors and two loading bays. Route remains legible. Split overview wide, cabinet plates very small; texture restrained. |
| 11 lab-11-11-service-elevator | Tan/dark khaki room and grain floor contrast blue stairs, gray hoist and pale cabinet. Rope/landing/support geometry visible. Floating/context labels small at overview scale. |
| 12 lab-11-12-mobile-traffic-lights | Blue-gray apron, tan verge and white crossing retain strong roadway separation. Both mobile heads, plinths and wheels visible; one head faces away in default pose. Cabinet legend tiny in split. |
| 13 lab-11-13-xy-palletizing | Green frame/lower wall and muted floor distinguish orange robot, blue gantry, pallets and cabinet. Magazine/cabinet projection overlap persists in split. Existing Demo 5 header alias is outside material scope. |
| 14 lab-11-19-powder-batch-mixer | Cream/brown/green hall distinguishes gray tanks/hoppers and blue components. Passive load-cell display table retained left. Cabinet masks one hopper in split projection. Broad floor subtly textured. |
| 15 lab-2-01-workstation-call | Brown tabletop grain visible; teal/cream room and slate floor distinguish white backboard and blue controls/beacon. Black/white fascia contrast retained; oblique text small in split. |
| 16 lab-2-02-dual-confirmation | Dark tabletop, pale board and teal room retain green/blue button and beacon contrast. Understructure in deep shadow but recognizable. Split contextual boards partly clipped by framing. |
| 17 lab-2-03-service-marker-inhibit | Taupe room/wood rear and charcoal table preserve bright white beacon/red control separation. Dark support frame still visible. Split contextual board cropped. |
| 18 lab-2-04-two-station-call | Warm textured tabletop and mint room distinguish yellow controls/amber beacon. Thin front edge shows dense grain streaking/aliasing in both views: minor finish-quality concern, not evidence of collision. Black/white fascia remains visible; rear station boards retained. |
| 19 lab-2-05-bay-light-selector | Deep green room, rear wood grain and black tabletop distinguish white selector. Stopped beacon lenses are dark on dark top but silhouettes/ribbing remain visible. Dynamic active-lens proof outside this review. |
| 20 lab-2-06-ready-attention | Red rear accent, cream walls and pale olive floor distinguish navy tabletop, blue legs and white board. White/amber indications visible; labels small in split. |
| 21 lab-2-07-dual-contact-permissive | Cream room/dark brown floor separate white board, green/red controls and dark table. Grounded frame visible despite shadows. Split contextual plaque partial. |
| 22 lab-2-08-inspection-vote | Deep green wall/cool floor, dark frame and pale board retain yellow/green control and stack visibility. Reference cartons/benches visible behind; small physical markings remain overview-limited. |
| 23 lab-2-09-maintenance-beacon | Cream/tan room with red bay accent separates black table, tan board, white dial and three lens silhouettes. Context title visible; detailed text small and partly clipped in split. |
| 24 lab-2-10-dust-collector-seal-in | Ochre/terracotta alcove and pale green rear distinguish blue fan, warm cabinet and green/red controls. Silver grille visible. Floor shows broad low-contrast patches; this image alone does not identify whether finish or lighting causes them. Extraction plaque readable in scene, cropped in split. |
| 25 lab-2-11-inbound-tote-stop | Brown/cream room and olive floor retain saturated blue tote, blue belt rails, yellow accessories and cyan drive. Legs, scanner pole and cabinet/tower silhouettes visible; small legends remain overview-limited. |
| 26 lab-2-12-assembly-lift | Green/cream walls and subtle floor grain retain bright yellow worktop, dark scissor members, blue cylinder and warm cabinet. All controls and two pilots visible; split cabinet projection covers part of base, without demonstrated material change. |

Evidence audit: all 52 PNG hashes match their identity sidecars; exact scene IDs, scene/split views, candidate DLL hash and assemblyUnchangedDuringCapture=true match. Requested state is stopped; actualRuntimeState is null in inspected sidecar schema, so UI stopped presentation is not an independent runtime-state witness. No engine, builds, input or product-source edits performed by this reviewer.

Image directory: `rungproof-next/.tools/studio-verification/all77-material-v22b`. Machine-readable audit: `part1-review-identity-audit.json` in that directory. The table below records the exact bytes reviewed.

| File | SHA256 |
|---|---|
| 01-conveyor-cell.scene.png | `7C499CD65735B27E2207F12F62A628C0C476DDFF7350CC054D6BF1A190F0826A` |
| 01-conveyor-cell.split.png | `4750EA8D1B5D8B5503443ADBB4C9F7F506DE54DB9738BFD6FE41886FD6CFC4E7` |
| 02-equipment-gallery.scene.png | `0587A4A59950450F695D8D848FFBDE3087F910D9D63C5B3E0D69557624F62CAD` |
| 02-equipment-gallery.split.png | `A23F09459D0D4A5547C77F12C12FE77452F9FB13ADC0184F579377741F003560` |
| 03-lab-10-01-drive-alarm-code-string.scene.png | `F41AF9C74E805D9F66BF00417C5C000C399CBEBBCC169ED0112B58A3E715E079` |
| 03-lab-10-01-drive-alarm-code-string.split.png | `76F6EB3B66EDAF22D0EF9E2E19E1DF693CEA9AF6F3CDB8C81DDFA222F5E7D6E7` |
| 04-lab-10-02-chicken-label-print.scene.png | `8CE40904E2EB89D3380C5714DD2D550C2205EC1899775E50EF3656F45F29DFA9` |
| 04-lab-10-02-chicken-label-print.split.png | `97AF3090BC4A1E083D5DAEA298071409D357B690963890DB4FFE686BB70C2E87` |
| 05-lab-10-03-vision-package-sorter.scene.png | `D69F6ADA2914FE5BDC504347D7C3A55390C5D731C425E4A3A2D6C3DBF0C51B30` |
| 05-lab-10-03-vision-package-sorter.split.png | `7F9E0ED4B5E2BB557002BB811BCF792D50A726B8D1549DC1BDA91629186EAB07` |
| 06-lab-10-04-motor-enum-state.scene.png | `B9BF8DA45DC38738F22835E961381A52739E29083EDF3D9ABB902DF9F8B3488B` |
| 06-lab-10-04-motor-enum-state.split.png | `977E8118F0E473D5F23BF5A5106C8D3481418BC11C3E5AE13D42013B667400F5` |
| 07-lab-10-05-motor-struct-data.scene.png | `FD6F6A8038114090E5496A09BE52B7121123000D1BFB340EBE42520D7237D190` |
| 07-lab-10-05-motor-struct-data.split.png | `A904FE5AD9BDEFB28F1F1ECB2D966EE76B7BA0A07FAB6F3B36D9129CD7917FDF` |
| 08-lab-10-06-ten-motor-array-startup.scene.png | `A54E7C4664BC0916A9E83850DA4AF7BFEFCF58E8422980CC7843ADE3167208A7` |
| 08-lab-10-06-ten-motor-array-startup.split.png | `0E0694E92CC7462608347BAF1B2F4846D3D9E573016424DDDF11657E78193581` |
| 09-lab-11-06-wastewater-collection.scene.png | `78A21823795B9CB81CA8D900B7EB965E3B5884D03672800AA226FF380E2B28F0` |
| 09-lab-11-06-wastewater-collection.split.png | `CB03BA633370F66F816C2405201240853F0FEA1133AE4625AF404F9F9A1B772C` |
| 10-lab-11-07-multi-conveyor-pallet-route.scene.png | `7B6D65E6AD59834C50401DF17FCBA0EAC0F6256BAA4DDBCD9CAC5061B29E0B8A` |
| 10-lab-11-07-multi-conveyor-pallet-route.split.png | `DDDA663DCAC9308A10EC56F20425B2B447A2D1DBEAF331898107D99C4F615045` |
| 11-lab-11-11-service-elevator.scene.png | `4FE8F519B51842ECABD0EB908675C3A9305DA1A506D443404DCF4ECE08C93325` |
| 11-lab-11-11-service-elevator.split.png | `3A66FCCD392FA08C17FD76374F86B7DA36EB1018BC150E63C5E927BBD57066D1` |
| 12-lab-11-12-mobile-traffic-lights.scene.png | `EA9AE9AFEE34A1DB165EEF39ACECDBEA6CB17DC47DDED410A47EFB3154EC5A24` |
| 12-lab-11-12-mobile-traffic-lights.split.png | `3C24F583088C85A5FB11A7D3E08258C75A4C293315EFFB6F9EE9565A0DC45E8C` |
| 13-lab-11-13-xy-palletizing.scene.png | `A51CD42BFF9C37AFC0E52342D74D2489BE30537FCD77D4DEEE31277EEC979BE0` |
| 13-lab-11-13-xy-palletizing.split.png | `88FB28B5BCB7016E578ECDF0DB2CDAF5D20C34FAE4B386F0A1EFCAB9B86BB816` |
| 14-lab-11-19-powder-batch-mixer.scene.png | `6157646559BE1949EB3466615753E9DA50AC3E545BBB242586D33D53FEA9844D` |
| 14-lab-11-19-powder-batch-mixer.split.png | `A04B1187A3885CB3DE804C7FD65452EA7EF4EC805C0C476C3D27D850D6D484E1` |
| 15-lab-2-01-workstation-call.scene.png | `EDC6A2098016ABF39D551B6E8B8BE57EF4F9BB3C74D5D536D012C2C3B060DA9B` |
| 15-lab-2-01-workstation-call.split.png | `C22955511B811073D62634FA8ED9BF58ACFB1EC76CDFBB63337676AEF657AB6E` |
| 16-lab-2-02-dual-confirmation.scene.png | `B8A4A82847D3D6D5CE1E772E5873B811B7D624F55ECA41B28B73B07A2E478BE4` |
| 16-lab-2-02-dual-confirmation.split.png | `9FC43C701141EF87C1A9A494CFF1928B01C5BC30BA78BFB12B31A6CDE0BCBF8F` |
| 17-lab-2-03-service-marker-inhibit.scene.png | `C4A94BE3A1BCBA2D1343A723307E68D6909CBB7A7E88B850AAD00A4EA820C129` |
| 17-lab-2-03-service-marker-inhibit.split.png | `F61873D36BB7DF1B65E473F5074D28AAB02526709EC56B5D0AFF5D311BF3D37F` |
| 18-lab-2-04-two-station-call.scene.png | `C2DB7F9363108C0AFECF21355D020C885F249A060B0F7C351E3A350DDE56AB9F` |
| 18-lab-2-04-two-station-call.split.png | `9E8036BD117CB9653770EF920C1E54F2F926E71BC50BBD9FC135C9E84A9309AC` |
| 19-lab-2-05-bay-light-selector.scene.png | `AEFB1E0E0C2799FC6F905AAEEA936CA09CD7D01766D51CBBE47C4E4BD570A07B` |
| 19-lab-2-05-bay-light-selector.split.png | `B6662D994D004F02BBA1AA7B0FC2BDC36691B35B313AB0433D7284BFB7A7BB0E` |
| 20-lab-2-06-ready-attention.scene.png | `2CF4B8FD949F03BE617FB55E670CFF534A834C11C870747E7A57B067F8289E44` |
| 20-lab-2-06-ready-attention.split.png | `FAA3CD1E755DF531030DD67F2B333E7C4E39EAFF4740B0E8E054834F44BD5B71` |
| 21-lab-2-07-dual-contact-permissive.scene.png | `A158758022F1D0957C139BEF4854774919839487CA65949CD5039902B96CC61D` |
| 21-lab-2-07-dual-contact-permissive.split.png | `24484155EF3AEE38A40FFB11A01529B5FCB625BEFF7EAF83CB0582D1CBF5941E` |
| 22-lab-2-08-inspection-vote.scene.png | `E62AC648029AAE20CE9BEDA5EBA634CC5FFFF9EE8DB6206925966A03E71178CD` |
| 22-lab-2-08-inspection-vote.split.png | `0AED43D4430DF59C79EE16A8BFC2CF014552651957ED858C7862E16A0223BFF9` |
| 23-lab-2-09-maintenance-beacon.scene.png | `EBD5921DF862D1E1368F53051619AE46D7BA4E06FB6852EE561F77D5C77E0F53` |
| 23-lab-2-09-maintenance-beacon.split.png | `756129AEEEB71105CCEBE063ACAAD978EDE0C89CA06A5DDBF1372C45841CAF14` |
| 24-lab-2-10-dust-collector-seal-in.scene.png | `3E1EAEF1F341E63AD7C2CC21A950DF0F5DEA4D723002F8D525CF2548CB536B2A` |
| 24-lab-2-10-dust-collector-seal-in.split.png | `4C68E13349F3C5F83C7BCDE0B476845A7D71AA50D532E78B1335FB9F67D86826` |
| 25-lab-2-11-inbound-tote-stop.scene.png | `3AC13BD46FC62254C219F8FC57723DAAA7E613563F4702E3E47F7AA389F2C6B3` |
| 25-lab-2-11-inbound-tote-stop.split.png | `CA8710FEB496453A0193C4EE6812DE03D0AF017F36ADA8242DFA286A1DBCB7F5` |
| 26-lab-2-12-assembly-lift.scene.png | `D9BF0EABEB7FA4A0DA0F72796E408E11FC37E69F71F59F5B36C8780BFF2728F2` |
| 26-lab-2-12-assembly-lift.split.png | `9DE075F5DA857F03580379F661793E6E0DC07EC82B9AF380E83D056B2D3ADEDB` |

