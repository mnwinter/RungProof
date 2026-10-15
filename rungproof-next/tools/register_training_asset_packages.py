"""Register lesson-specific training accessory packages as candidate assets.

The training scenes use a generic, copyright-safe accessory renderer. This
script gives each named requirement a stable asset package identity and links
it to an existing generic model family already present in the candidate or
production library. The package remains candidate-only until its own review
and provenance gates pass.
"""
from __future__ import annotations

import json
import re
import shutil
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = ROOT.parent
SCENE_ROOT = PROJECT_ROOT / "prototype" / "scenes"
CATALOG_ROOT = ROOT / "assets" / "catalog"
CANDIDATE_CATALOG = CATALOG_ROOT / "candidates.catalog.json"
PRODUCTION_CATALOG = CATALOG_ROOT / "production.catalog.json"
REFERENCE_REGISTER = CATALOG_ROOT / "industrial-reference-register.json"
PACKAGE_ROOT = ROOT / "assets" / "training_accessories"


def slugify(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "_", value.lower()).strip("_")


def collect_requirements() -> list[str]:
    requirements: set[str] = set()
    for path in SCENE_ROOT.glob("lab-*.plcscene"):
        scene = json.loads(path.read_text(encoding="utf-8"))
        requirements.update(scene.get("training", {}).get("assetRequirements", []))
    return sorted(requirements)


def load_assets() -> list[dict]:
    assets: list[dict] = []
    for name in ("production.catalog.json", "candidates.catalog.json"):
        assets.extend(json.loads((CATALOG_ROOT / name).read_text(encoding="utf-8"))["assets"])
    return assets


def choose_family(name: str, assets: list[dict]) -> dict:
    words = {word for word in re.findall(r"[a-z0-9]+", name.lower()) if len(word) > 3}
    scored = []
    for asset in assets:
        haystack = " ".join([asset.get("displayName", ""), *asset.get("tags", [])]).lower()
        score = sum(word in haystack for word in words)
        scored.append((score, asset))
    scored.sort(key=lambda item: (-item[0], item[1]["id"]))
    if not scored or scored[0][0] == 0:
        raise ValueError(f"No model family matches {name!r}; author a model or specify a reviewed basis.")
    if len(scored) > 1 and scored[0][0] == scored[1][0]:
        raise ValueError(f"Ambiguous model family for {name!r}; specify a reviewed basis instead of choosing by asset ID.")
    return scored[0][1]


def copy_package(source: dict, asset_id: str) -> dict:
    slug = asset_id.removeprefix("training.accessory.").removesuffix(".v1")
    destination = PACKAGE_ROOT / slug
    source_model = source["model"]
    source_root = ROOT / source_model["sourceBlend"].removeprefix("res://")
    source_asset_root = source_root.parent.parent
    destination.mkdir(parents=True, exist_ok=True)
    for folder in ("source", "delivery", "collision", "review"):
        (destination / folder).mkdir(exist_ok=True)

    file_map = {
        "sourceBlend": (source_root, destination / "source" / f"{slug}.blend"),
        "deliveryGltf": (ROOT / source_model["deliveryGltf"].removeprefix("res://"), destination / "delivery" / f"{slug}.glb"),
        "collisionFile": (ROOT / source_model["collisionFile"].removeprefix("res://"), destination / "collision" / f"{slug}_collision.glb"),
        "thumbnailFile": (ROOT / source_model["thumbnailFile"].removeprefix("res://"), destination / "thumbnail.png"),
    }
    for source_path, destination_path in file_map.values():
        if not source_path.is_file():
            raise FileNotFoundError(source_path)
        shutil.copy2(source_path, destination_path)

    source_review = source_asset_root / "review"
    if source_review.is_dir():
        for review_path in source_review.iterdir():
            if review_path.is_file() and review_path.name != "independent_recognition.json":
                shutil.copy2(review_path, destination / "review" / review_path.name)

    return {
        "sourceBlend": f"res://assets/training_accessories/{slug}/source/{slug}.blend",
        "deliveryGltf": f"res://assets/training_accessories/{slug}/delivery/{slug}.glb",
        "lodFiles": [],
        "collisionFile": f"res://assets/training_accessories/{slug}/collision/{slug}_collision.glb",
        "thumbnailFile": f"res://assets/training_accessories/{slug}/thumbnail.png",
    }


def main() -> int:
    candidate_document = json.loads(CANDIDATE_CATALOG.read_text(encoding="utf-8"))
    production_assets = json.loads(PRODUCTION_CATALOG.read_text(encoding="utf-8"))["assets"]
    existing_candidates = candidate_document["assets"]
    existing_ids = {asset["id"] for asset in production_assets + existing_candidates}
    family_assets = [
        asset for asset in production_assets + existing_candidates
        if not asset["id"].startswith("training.accessory.")
    ]
    reference_document = json.loads(REFERENCE_REGISTER.read_text(encoding="utf-8"))
    reference_by_id = {entry["assetId"]: entry for entry in reference_document["entries"]}

    for requirement in collect_requirements():
        slug = slugify(requirement)
        asset_id = f"training.accessory.{slug}.v1"
        if asset_id in existing_ids:
            # Existing packages may have a purpose-built replacement. Never
            # overwrite its identity or node contract with a new keyword guess.
            continue
        source = choose_family(requirement, family_assets)
        model = copy_package(source, asset_id)
        source_bounds = source["bounds"]
        candidate = {
            "id": asset_id,
            "displayName": requirement.title(),
            "category": "training/accessories",
            "tags": [requirement.lower(), "training accessory", "generic industrial visual"],
            "model": model,
            "bounds": source_bounds,
            "connectors": [],
            "kinematics": source.get("kinematics", []),
            "animationTags": [
                {"id": item["id"], "nodePath": item["nodePath"], "kind": item["kind"]}
                for item in source.get("kinematics", [])
            ],
            "signals": [],
            "quality": {
                "status": "candidate",
                "blindReviewId": None,
                "recognitionConfidence": None,
                "topologyReviewed": False,
                "materialReviewed": False,
                "scaleReviewed": False,
                "animationReviewed": True,
            },
            "trainingRequirement": requirement,
            "genericBasisAssetId": source["id"],
            "copyrightBoundary": "Generic training visual; no logo, trade dress, product number, or exact OEM claim.",
        }
        existing_candidates.append(candidate)
        existing_ids.add(asset_id)
        source_reference = reference_by_id.get(source["id"])
        if source_reference:
            reference_document["entries"].append({
                **source_reference,
                "assetId": asset_id,
                "comparisonStatus": "reference-identified",
                "comparisonNotes": "Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.",
            })

    candidate_document["assets"] = existing_candidates
    CANDIDATE_CATALOG.write_text(json.dumps(candidate_document, indent=2) + "\n", encoding="utf-8")
    REFERENCE_REGISTER.write_text(json.dumps(reference_document, indent=2) + "\n", encoding="utf-8")
    print(f"TRAINING_ASSET_PACKAGES: {len(existing_candidates)} candidate assets")
    print(f"TRAINING_REQUIREMENTS: {len(collect_requirements())}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
