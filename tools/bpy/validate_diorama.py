"""Check the composition recipe and exported mesh without Blender or Godot."""
import json
import math
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/validate"))
from validate_kit import validate  # Reuse the kit's complete embedded GLB checks.


def finite_vector(value, positive=False):
    return (isinstance(value, list) and len(value) == 3
            and all(type(x) in (int, float) and math.isfinite(x)
                    and (not positive or x > 0) for x in value))


def check():
    folder = ROOT / "assets/diorama"
    layout = json.loads((folder / "spoke-b.layout.json").read_text())
    instances = layout["instances"]
    names = set()
    for item in instances:
        name = item["id"]
        if not isinstance(name, str) or not name or name in names:
            raise ValueError("Instance ids must be nonempty and unique")
        names.add(name)
        asset = item["asset"]
        if asset not in {p.stem for p in (ROOT / "assets/kit").glob("*.glb")}:
            raise ValueError(f"{name}: unknown kit asset")
        if not finite_vector(item["at"]) or not finite_vector(item.get("scale", [1, 1, 1]), True):
            raise ValueError(f"{name}: finite positions and positive scales required")
        if not math.isfinite(item.get("yaw", 0)):
            raise ValueError(f"{name}: finite yaw required")
    if not {"tent", "chu", "generator", "fuel-bladder", "light-truck"} <= {i["asset"] for i in instances}:
        raise ValueError("Missing an original five-piece kit asset")
    camera = layout["camera"]
    if (not finite_vector(camera["position"]) or not finite_vector(camera["target"])
            or camera["position"] == camera["target"] or not math.isfinite(camera["size"]) or camera["size"] <= 0):
        raise ValueError("Camera needs distinct finite position/target and positive size")
    if not finite_vector(layout["light"]["rotation"]) or not 0 < layout["light"]["energy"] <= 4:
        raise ValueError("One finite key-light configuration is required")
    path = folder / "spoke-b.glb"
    count = validate(path)
    data = path.read_bytes()
    size = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + size])
    exported = {node.get("name") for node in doc["nodes"]}
    if not names <= exported:
        raise ValueError(f"Export lost instances: {sorted(names - exported)}")
    if doc.get("animations") or doc.get("skins") or doc.get("cameras") or doc.get("extensionsRequired"):
        raise ValueError("Diorama geometry must have no rig, animation, camera or required extension")
    print(f"DIORAMA_VALID: {len(instances)} instances; {count} colored, texture-free primitives")


if __name__ == "__main__":
    try:
        check()
    except (OSError, ValueError, KeyError, TypeError, struct.error) as error:
        print(f"DIORAMA_INVALID: {error}", file=sys.stderr)
        sys.exit(1)
