"""Check every M0 glTF binary piece and the shared recipe without Blender or dependencies."""
import json
import math
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
NAMES = ("tent", "chu", "generator", "fuel-bladder", "light-truck")
EXTENSIONS = ("terrain-sand", "terrain-scrub", "terrain-coast", "road-straight", "road-corner",
              "route-straight", "route-corner", "warehouse", "town-block", "antenna-mast",
              "guard-tower", "barrier-set", "fuel-truck", "bus", "forklift", "pallet", "water-buffalo")


def validate_recipes():
    recipes = json.loads((ROOT / "tools/bpy/kit_parts.json").read_text(encoding="utf-8"))
    if set(recipes) != set(EXTENSIONS):
        raise ValueError("recipe must cover the complete M0-14 extension")
    for name, parts in recipes.items():
        if not parts:
            raise ValueError(f"{name}: empty piece")
        for part in parts:
            if part["color"] not in ("sand", "olive", "dark", "glass"):
                raise ValueError(f"{name}: color outside the shared palette")
            at = part["at"]
            if len(at) != 3 or not all(isinstance(x, (int, float)) and math.isfinite(x) for x in at):
                raise ValueError(f"{name}: position needs three finite coordinates")
            if part["shape"] == "box":
                dimensions = part["size"]
                if len(dimensions) != 3:
                    raise ValueError(f"{name}: box needs three dimensions")
            elif part["shape"] == "cylinder":
                dimensions = (part["radius"], part["length"])
                if part.get("axis", "y") not in ("x", "y", "z"):
                    raise ValueError(f"{name}: invalid cylinder axis")
            else:
                raise ValueError(f"{name}: unsupported shape")
            if not all(isinstance(x, (int, float)) and math.isfinite(x) and x > 0 for x in dimensions):
                raise ValueError(f"{name}: primitive dimensions must be positive and finite")


def validate(path):
    data = path.read_bytes()
    if len(data) < 20:
        raise ValueError("missing GLB header (check Git LFS checkout)")
    magic, version, length = struct.unpack_from("<4sII", data)
    if magic != b"glTF" or version != 2 or length != len(data):
        raise ValueError("not a complete glTF 2.0 binary (check Git LFS checkout)")
    size, kind = struct.unpack_from("<I4s", data, 12)
    if kind != b"JSON" or 20 + size > len(data):
        raise ValueError("missing JSON chunk")
    document = json.loads(data[20:20 + size])
    primitives = [p for mesh in document.get("meshes", []) for p in mesh.get("primitives", [])]
    if not primitives or any(not {"POSITION", "NORMAL", "COLOR_0"} <= p.get("attributes", {}).keys() for p in primitives):
        raise ValueError("every primitive must contain positions, normals and vertex colors")
    if document.get("images") or document.get("textures"):
        raise ValueError("M0 kit must use vertex colors, not textures")
    if any("uri" in buffer for buffer in document.get("buffers", [])):
        raise ValueError("GLB buffers must be embedded")
    return len(primitives)


def main():
    errors = []
    try:
        validate_recipes()
    except (OSError, ValueError, KeyError, TypeError) as error:
        errors.append(f"kit_parts.json: {error}")
    for name in (*NAMES, *EXTENSIONS):
        path = ROOT / "assets" / "kit" / f"{name}.glb"
        try:
            count = validate(path)
            print(f"OK {path.relative_to(ROOT)}: {count} colored primitive(s)")
        except (OSError, ValueError, KeyError, struct.error) as error:
            errors.append(f"{path}: {error}")
    for error in errors:
        print("ERROR", error, file=sys.stderr)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
