"""Verify every catalog kinematic nodePath exists in its delivery GLB."""
from __future__ import annotations

import json
import struct
from pathlib import Path


ROOT=Path(__file__).resolve().parents[1]


def glb_nodes(path: Path) -> set[str]:
    raw=path.read_bytes()
    if raw[:4]!=b"glTF": raise ValueError(f"Not GLB: {path}")
    _,version,total=struct.unpack_from("<III",raw,0)
    if version!=2 or total!=len(raw): raise ValueError(f"Invalid GLB header: {path}")
    length,kind=struct.unpack_from("<I4s",raw,12)
    if kind!=b"JSON": raise ValueError(f"Missing JSON chunk: {path}")
    doc=json.loads(raw[20:20+length].decode("utf-8"))
    return {node.get("name","") for node in doc.get("nodes",[])}


def main() -> int:
    failures=[];checked=0
    for catalog_name in ("production.catalog.json","candidates.catalog.json"):
        doc=json.loads((ROOT/"assets"/"catalog"/catalog_name).read_text(encoding="utf-8"))
        for asset in doc["assets"]:
            axes=asset.get("kinematics",[])
            if not axes: continue
            path=ROOT/asset["model"]["deliveryGltf"].removeprefix("res://")
            names=glb_nodes(path)
            for axis in axes:
                checked+=1
                if axis["nodePath"] not in names:
                    failures.append(f'{asset["id"]}: {axis["id"]} -> {axis["nodePath"]}')
    for failure in failures: print("MISSING_KINEMATIC_NODE",failure)
    print("KINEMATIC_NODE_CHECKS",checked)
    print("KINEMATIC_NODE_FAILURES",len(failures))
    return 1 if failures else 0


if __name__=="__main__": raise SystemExit(main())
