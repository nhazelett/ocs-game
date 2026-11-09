"""Assemble exported kit meshes using the presentation-only Spoke B layout.

blender --background --factory-startup --python tools/bpy/assemble_diorama.py
Run generate_kit.py first. No scenario, rule or simulation state is read.
"""
import json
import math
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]


def main():
    layout = json.loads((ROOT / "assets/diorama/spoke-b.layout.json").read_text())
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for item in layout["instances"]:
        path = ROOT / "assets/kit" / (item["asset"] + ".glb")
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=str(path))
        imported = set(bpy.data.objects) - before
        parent = bpy.data.objects.new(item["id"], None)
        bpy.context.collection.objects.link(parent)
        for obj in imported:
            if obj.parent not in imported:
                obj.parent = parent
        # Layout uses glTF Y-up; Blender uses Z-up.
        x, y, z = item["at"]
        parent.location = (x, -z, y)
        parent.rotation_euler.z = math.radians(item.get("yaw", 0))
        sx, sy, sz = item.get("scale", [1, 1, 1])
        parent.scale = (sx, sz, sy)
    target = ROOT / "assets/diorama/spoke-b.glb"
    bpy.ops.object.select_all(action="SELECT")
    result = bpy.ops.export_scene.gltf(
        filepath=str(target), export_format="GLB", use_selection=True,
        export_animations=False, export_materials="EXPORT", export_apply=True,
        export_vertex_color="MATERIAL", export_all_vertex_colors=True, export_yup=True)
    if "FINISHED" not in result:
        raise RuntimeError(f"Diorama export failed: {result}")
    print(f"DIORAMA_EXPORTED: {len(layout['instances'])} kit instances; Blender {bpy.app.version_string}")


if __name__ == "__main__":
    main()
