"""Index existing native captures; never generates or changes screenshot pixels."""
import argparse
import hashlib
import html
import json
import re
from pathlib import Path

from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument("directory", type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
directory = args.directory.resolve()
manifest = json.loads((directory / "capture-manifest.json").read_text(encoding="utf-8-sig"))
catalog = json.loads((root / "scenes/catalog/original-scenes.catalog.json").read_text(encoding="utf-8-sig"))
profiles = dict(re.findall(r'\["([^"]+)"\] = P\(SceneEnvironmentFamily\.(\w+)',
                          (root / "src/App/SceneEnvironmentProfiles.cs").read_text()))
assert len(manifest) == 154, f"Expected 154 captures, got {len(manifest)}"
assert len({(entry["sceneId"], entry["view"]) for entry in manifest}) == 154
assert {entry["sceneId"] for entry in manifest} == set(profiles)
assert catalog["sceneCount"] == len(profiles) == 77
cards = []
assembly_hashes = set()
for index in range(0, len(manifest), 2):
    pair = manifest[index:index + 2]
    assert pair[0]["sceneId"] == pair[1]["sceneId"]
    assert {entry["view"] for entry in pair} == {"scene", "split"}
    for entry in pair:
        image_path = Path(entry["image"])
        assert hashlib.sha256(image_path.read_bytes()).hexdigest().upper() == entry["sha256"].upper()
        if "identity" in entry:
            identity = json.loads(Path(entry["identity"]).read_text(encoding="utf-8-sig"))
            assert identity["sceneId"] == entry["sceneId"] and identity["view"] == entry["view"]
            assert identity["imageSha256"].upper() == entry["sha256"].upper()
            assert identity["assemblyUnchangedDuringCapture"] is True
            assembly_hashes.add(identity["assemblySha256"])
        with Image.open(image_path) as picture:
            assert picture.size == (1600, 900), image_path
        log = Path(entry["log"]).read_text(encoding="utf-8-sig", errors="replace")
        assert "SCENE_ENVIRONMENT_READY" in log and entry["sceneId"] in log
        assert not re.search(r"^(?:ERROR:|SCRIPT ERROR:|Unhandled exception)", log, re.MULTILINE), image_path
    first = pair[0]
    scene_id = first["sceneId"]
    family = profiles[scene_id]
    title = html.escape(first["name"])
    pictures = "".join(f'<a href="{html.escape(Path(entry["image"]).name)}"><img loading="lazy" src="{html.escape(Path(entry["image"]).name)}" alt="{title}: {entry["view"]}"><span>{entry["view"]} view · open full image</span></a>' for entry in pair)
    cards.append(f'<article data-search="{html.escape((title + " " + scene_id + " " + family).lower())}"><h2>{index // 2 + 1:02d} · {title}</h2><p>{html.escape(scene_id)} · {family}</p><div class="pair">{pictures}</div></article>')
assert len(assembly_hashes) <= 1, "Catalog capture mixed different application assemblies"
document = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>RungProof — all 77 scene environments</title>
<style>body{margin:0;background:#202420;color:#ecebe4;font:16px system-ui,sans-serif}header,main{max-width:1440px;margin:auto;padding:24px}header{position:sticky;top:0;background:#202420;z-index:1;border-bottom:1px solid #4e5749}h1{margin:0;font-size:26px}p{color:#c6cbbf}input{width:min(600px,90%);padding:12px;background:#323b30;color:white;border:1px solid #88967e;border-radius:6px;font:inherit}article{margin-bottom:28px;padding:18px;background:#2d342b;border-radius:8px}h2{font-size:20px;margin:0}.pair{display:flex;gap:16px}.pair a{width:50%;color:#d8dfcb;text-decoration:none}.pair img{width:100%;height:auto;display:block}.pair span{display:block;padding:8px}article[hidden]{display:none}@media(max-width:800px){.pair{display:block}.pair a{width:100%;display:block}}</style>
<header><h1>RungProof · complete catalog environment review</h1><p>77 scenes and labs · 154 native stopped-state Compatibility captures. Click an image for its full 1600 × 900 view. Static presentation evidence; motion and release qualification are separate.</p><input id="search" aria-label="Filter scenes" placeholder="Filter by scene, lab, or environment family"><p id="count">77 scenes</p></header><main>'''
document += "\n".join(cards)
document += '''</main><script>document.querySelector('#search').addEventListener('input',event=>{const query=event.target.value.toLowerCase();let count=0;document.querySelectorAll('article').forEach(card=>{card.hidden=!card.dataset.search.includes(query);if(!card.hidden)count++});document.querySelector('#count').textContent=count+' scenes'});</script></html>'''
(directory / "gallery.html").write_text(document, encoding="utf-8")
print("CATALOG_CAPTURE_INDEX_PASS scenes=77 images=154 hashes=pass dimensions=1600x900 logs=clean")
