# Dot powder accessory placement audit — V12

Read-only source plus individually reopened actual 14-lab-11-19-powder-batch-mixer.scene.png (SHA256 33948303BE8E3CCA5878C6497D07D774F109E9E39438C8F919B5F4B1CDC4D103, D6 V12 qualified identity).

## Findings

training_accessory_8 is exactly training.accessory.load_cell.v1 at (-4,0,3.6). The independent asset recognition identifies a single-ended bending/shear-beam load cell, with simplified fixed-end/load-introduction details. The actual image shows the corresponding small gray beam resting alone on the floor left of the valve/selector, disconnected from a load path. This is a credible placement/purpose defect, not a floating-light defect. A load cell is not itself a floor-standing machine.

The scene explicitly states an illustrative static layout; machineGuide says the separate equipment do not simulate transfer, weighing or mixing. No pointBinding targets accessory_8 or accessory_9. Therefore placing the beam underneath an existing hopper/tank would visually imply a working weighing load path that this source does not establish. Do not silently install it as a functional weighing system.

training_accessory_9 is exactly training.accessory.recipe_selector.v1 at (-1.1,0,3.6). Independent recognition identifies a pedestal-mounted rotary selector/control station; catalog size 0.7 x 1.7 x 0.6 m. In the actual image its face sits atop its own substantial vertical post and foot. This is already a supported freestanding station, unlike a loose panel head or DIN relay. No extra cabinet is required solely because its semantic name is recipe selector. Its source has no binding; recipe_valid remains switch_11's manual Boolean action. Do not imply the decorative selector edits a recipe or attach it to the operator cabinet as though it owns that action.

## Concrete bounded proposal

Provide a small grounded teaching bench/plinth at the beam's existing local X/Z location, clear of valve/selector/hopper access, and elevate the retained beam by its measured base-to-surface delta. Prefer a cutaway fixed-end demonstration fixture: clamp/support only its fixed end on the bench, keep sensing beam/free end visible, and identify it as an unbound load-cell training exhibit. A simple nonfunctional display cradle is acceptable when fixed-end details cannot be reliably identified. Preserve ID, asset, selection path, binding absence and all existing field equipment; support geometry must use a distinct environment/support namespace. Verify actual GLB mesh bounds/contact and detail readability after the approved repair. No loaded hopper, added measurement values, powder transfer, wiring or calibrated mass behavior is implied.

Keep the recipe-selector pedestal in place; only add an honest small exhibit label or operator/detail capture if purpose/readability remains ambiguous. The requested next detail should frame beam fixture, selector foot and face, and neighboring valve together, so support and preserved spacing are observable. Generic material restyling remains HELD. No product edits, build or engine actions performed.
