"""Check the five M0 glTF binary pieces without Blender or third-party Python packages."""
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
NAMES = ("tent", "chu", "generator", "fuel-bladder", "light-truck")


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
    for name in NAMES:
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
