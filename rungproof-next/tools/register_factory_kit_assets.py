"""Register the visually inspected factory-kit tranche as catalog candidates.

Registration is deliberately separate from approval.  These assets remain
candidate-only until independent recognition and the rest of the production
quality gates have passed.
"""
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"

# slug: (asset id, display name, category, [width, height, depth], signals)
SPECS = {
    "sealed_concrete_floor_slab": ("facility.floor.concrete-6x6.v1", "Industrial Concrete Floor Slab - 6 m x 6 m", "facility/floors", [6.0, .20, 6.0], []),
    "insulated_wall_panel": ("safety.fence.solid-acoustic-panel-4x3.v1", "Solid Machine-Guarding / Acoustic Barrier Panel", "safety/guarding", [4.25, 3.15, .45], []),
    "personnel_door_wall_module": ("safety.enclosure.personnel-door-panel.v1", "Machine-Guarding Enclosure Panel with Personnel Door", "safety/guarding", [4.25, 3.15, 1.15], []),
    "observation_window_wall_module": ("safety.enclosure.observation-window-panel.v1", "Machine-Guarding Enclosure Panel with Observation Window", "safety/guarding", [4.25, 3.15, .55], []),
    "structural_w_column": ("structures.column.steel-4m.v1", "Structural Steel Support Column - 4 m", "structures/steel", [.92, 4.08, .92], []),
    "structural_w_beam": ("structures.beam.w-section-5m.v1", "Structural W-Section Beam - 5 m", "structures/steel", [5.0, .80, .55], []),
    "elevated_catwalk_platform": ("structures.catwalk.platform-4m.v1", "Elevated Industrial Catwalk Platform - 4 m", "facility/access", [4.1, 2.40, 1.45], []),
    "industrial_stair_flight": ("facility.access.stair-flight-2m.v1", "Industrial Stair Flight - 2 m Rise", "facility/access", [3.0, 3.05, 1.20], []),
    "fixed_caged_ladder": ("facility.access.caged-ladder-4m.v1", "Fixed Caged Ladder - 4 m", "facility/access", [1.1, 4.1, 1.0], []),
    "pedestrian_guardrail": ("safety.guardrail.pedestrian-4m.v1", "Pedestrian Guardrail - 4 m", "safety/guarding", [4.1, 1.15, .35], []),
    "machine_safety_fence_panel": ("safety.fence.mesh-panel-3m.v1", "Machine Safety Fence Panel - 3 m", "safety/guarding", [3.5, 2.15, .40], []),
    "machine_safety_swing_gate": ("safety.gate.mesh-swing-2m.v1", "Machine Safety Swing Gate - 2 m", "safety/guarding", [2.5, 2.15, .45], ["gate_closed"]),
    "safety_bollard": ("safety.bollard.120mm.v1", "Safety Bollard - 120 mm", "safety/guarding", [.50, 1.25, .50], []),
    "safety_light_curtain_pair": ("safety.light-curtain.1800mm.v1", "Safety Light Curtain Pair - 1800 mm", "safety/presence-sensing", [2.4, 2.10, .45], ["field_clear"]),
    "industrial_workbench": ("facility.workbench.2400mm.v1", "Industrial Workbench - 2400 mm", "facility/workstations", [2.5, 1.45, 1.2], []),
    "mobile_tool_cabinet": ("facility.storage.mobile-tool-cabinet.v1", "Mobile Tool Cabinet", "facility/storage", [1.35, 1.45, .80], []),
    "floorstanding_electrical_enclosure": ("controls.enclosure.floorstanding.v1", "Floorstanding Electrical Enclosure", "controls/enclosures", [1.30, 2.05, .55], []),
    "industrial_junction_box": ("controls.enclosure.junction-box.v1", "Industrial Junction Box", "controls/enclosures", [1.05, .90, .55], []),
    "ladder_cable_tray": ("utilities.cable-tray.ladder-4m.v1", "Ladder Cable Tray - 4 m", "utilities/cable-management", [4.1, .80, 1.1], []),
    "trapeze_pipe_support": ("process.support.multi-pipe-rack.v1", "Multi-Pipe Support Rack", "process/supports", [2.7, 1.85, .75], []),
    "gma_wood_pallet": ("loads.pallet.gma-48x40.v1", "GMA Wood Pallet - 48 x 40 in", "loads/pallets", [1.25, .30, 1.05], []),
    "reusable_plastic_tote": ("loads.tote.reusable-plastic.v1", "Reusable Plastic Tote", "loads/containers", [1.10, .82, .82], []),
    "steel_shipping_drum": ("loads.drum.steel-55gal.v1", "Steel Shipping Drum - 55 gal", "loads/containers", [.75, 1.20, .75], []),
    "ibc_tote": ("loads.ibc.1000l.v1", "Intermediate Bulk Container - 1000 L", "loads/containers", [1.25, 1.60, 1.25], []),
    "steel_coil_on_saddles": ("loads.coil.steel-banded.v1", "Banded Steel Coil on Saddles", "loads/metal", [1.75, 1.55, 1.25], []),
    "banded_sheet_metal_stack": ("loads.sheet-stack.banded.v1", "Banded Sheet-Metal Stack", "loads/metal", [2.60, .82, 1.30], []),
    "industrial_floor_scale": ("sensing.scale.floor-platform.v1", "Industrial Floor Platform Scale", "sensing/weight", [2.4, 1.65, 2.0], ["weight_kg", "overload"]),
    "fixed_barcode_scanner": ("sensing.identification.fixed-barcode-scanner.v1", "Fixed Industrial Barcode Scanner", "sensing/identification", [.65, 1.85, .75], ["trigger", "code_present"]),
}


def make_signal(name: str) -> dict:
    is_input = name == "trigger"
    data_type = "float32" if name == "weight_kg" else "bool"
    return {
        "id": name,
        "dataType": data_type,
        "direction": "input" if is_input else "output",
        "unit": "kg" if name == "weight_kg" else None,
        "kinematicAxis": None,
        "description": name.replace("_", " ").capitalize() + ".",
    }


def main() -> int:
    document = json.loads(CATALOG.read_text(encoding="utf-8"))
    aliases = {
        "facility.wall.insulated-panel-4x3.v1": "safety.fence.solid-acoustic-panel-4x3.v1",
        "facility.wall.personnel-door-module.v1": "safety.enclosure.personnel-door-panel.v1",
        "facility.wall.observation-window-module.v1": "safety.enclosure.observation-window-panel.v1",
    }
    for asset in document["assets"]:
        asset["id"] = aliases.get(asset["id"], asset["id"])
    by_id = {asset["id"]: asset for asset in document["assets"]}
    for slug, (asset_id, name, category, bounds, signals) in SPECS.items():
        by_id[asset_id] = {
            "id": asset_id,
            "displayName": name,
            "category": category,
            "tags": [name.lower(), slug.replace("_", " ")],
            "model": {
                "sourceBlend": f"res://assets/factory_kit/{slug}/source/{slug}.blend",
                "deliveryGltf": f"res://assets/factory_kit/{slug}/delivery/{slug}.glb",
                "lodFiles": [],
                "collisionFile": f"res://assets/factory_kit/{slug}/collision/{slug}_collision.glb",
                "thumbnailFile": f"res://assets/factory_kit/{slug}/thumbnail.png",
            },
            "bounds": {"widthM": bounds[0], "heightM": bounds[1], "depthM": bounds[2]},
            "connectors": [],
            "kinematics": [],
            "signals": [make_signal(signal) for signal in signals],
            "quality": {
                "status": "candidate",
                "blindReviewId": None,
                "recognitionConfidence": None,
                "topologyReviewed": True,
                "materialReviewed": True,
                "scaleReviewed": True,
                "animationReviewed": False,
            },
        }
    document["assets"] = list(by_id.values())
    CATALOG.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print("REGISTERED_FACTORY_KIT", len(SPECS))
    print("TOTAL_CANDIDATES", len(document["assets"]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
