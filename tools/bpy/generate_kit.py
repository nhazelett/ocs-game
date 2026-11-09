"""Generate the kit. Proven with Blender 4.3.2 --background --factory-startup.

Example: blender --background --factory-startup --python tools/bpy/generate_kit.py -- --scale 1
M3-11 replaces the M0 primitive fallback; see tools/bpy/README.md.
"""
import argparse
import json
import math
from datetime import date
from pathlib import Path
import sys

import bpy

ROOT = Path(__file__).resolve().parents[2]
PALETTE = {
    "sand": (0.64, 0.58, 0.43, 1),
    "olive": (0.42, 0.43, 0.31, 1),
    "dark": (0.16, 0.18, 0.17, 1),
    "glass": (0.26, 0.32, 0.34, 1),
}


def paint(obj, color):
    attr = obj.data.color_attributes.new(name="Color", type="FLOAT_COLOR", domain="CORNER")
    for entry in attr.data:
        entry.color = PALETTE[color]
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    material = bpy.data.materials.get("KitVertexColor")
    if material is None:
        material = bpy.data.materials.new("KitVertexColor")
        material.use_nodes = True
        nodes = material.node_tree.nodes
        vertex_color = nodes.new("ShaderNodeVertexColor")
        vertex_color.layer_name = "Color"
        shader = nodes.get("Principled BSDF")
        shader.inputs["Roughness"].default_value = 1
        material.node_tree.links.new(vertex_color.outputs["Color"], shader.inputs["Base Color"])
    obj.data.materials.append(material)
    return obj


def box(name, size, position, color):
    bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return paint(obj, color)


def cylinder(name, radius, length, position, color, wheel=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=length, location=position)
    obj = bpy.context.object
    obj.name = name
    if wheel:
        obj.rotation_euler.y = 1.5707963267948966
    return paint(obj, color)


def tent():
    vertices = [(-1.4, -1.6, 0), (1.4, -1.6, 0), (0, -1.6, 1.8),
                (-1.4, 1.6, 0), (1.4, 1.6, 0), (0, 1.6, 1.8)]
    faces = [(0, 2, 1), (3, 4, 5), (0, 3, 5, 2), (1, 2, 5, 4), (0, 1, 4, 3)]
    mesh = bpy.data.meshes.new("tentMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("tent", mesh)
    bpy.context.collection.objects.link(obj)
    return [paint(obj, "sand")]


def chu():
    return [box("chu", (2.4, 3.5, 2.2), (0, 0, 1.1), "sand"),
            box("chuDoor", (.75, .03, 1.7), (0, -1.76, .85), "dark")]


def generator():
    return [box("generator", (1.5, 1, 1.1), (0, 0, .55), "olive"),
            cylinder("exhaust", .08, .8, (.45, 0, 1.4), "dark")]


def fuel_bladder():
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=1, location=(0, 0, .45))
    obj = bpy.context.object
    obj.name = "fuelBladder"
    obj.scale = (1.4, 1, .45)
    return [paint(obj, "olive")]


def light_truck():
    result = [box("truckChassis", (1.6, 3, .35), (0, 0, .65), "dark"),
              box("truckBed", (1.6, 1.7, .45), (0, .55, 1), "sand"),
              box("truckCab", (1.6, 1.15, 1.1), (0, -.9, 1.25), "sand"),
              box("windshield", (1.3, .03, .45), (0, -1.48, 1.5), "glass")]
    for side in (-.88, .88):
        for axle in (-1, 1):
            result.append(cylinder("wheel", .37, .25, (side, axle, .38), "dark", wheel=True))
    return result


BUILDERS = {"tent": tent, "chu": chu, "generator": generator,
            "fuel-bladder": fuel_bladder, "light-truck": light_truck}


def recipe(name, parts):
    """Shared M0-14 recipe uses meters, X right/Y up/Z back (Godot coordinates)."""
    result = []
    for index, part in enumerate(parts):
        x, y, z = part["at"]
        position = (x, -z, y)  # Blender is Z-up; glTF export converts back to Y-up.
        label = f"{name}-{index}"
        if part["shape"] == "box":
            sx, sy, sz = part["size"]
            obj = box(label, (sx, sz, sy), position, part["color"])
        elif part["shape"] == "cylinder":
            obj = cylinder(label, part["radius"], part["length"], position, part["color"])
            if part.get("axis", "y") == "x":
                obj.rotation_euler.y = math.pi / 2
            elif part.get("axis", "y") == "z":
                obj.rotation_euler.x = math.pi / 2
        else:
            raise ValueError(f"Unknown recipe primitive: {part['shape']}")
        result.append(obj)
    return result


RECIPES = json.loads(Path(__file__).with_name("kit_parts.json").read_text(encoding="utf-8"))
for _name, _parts in RECIPES.items():
    BUILDERS[_name] = lambda name=_name, parts=_parts: recipe(name, parts)


def provenance(paths, added):
    path = ROOT / "assets" / "PROVENANCE.md"
    lines = path.read_text(encoding="utf-8").splitlines()
    keys = {p.relative_to(ROOT).as_posix() for p in paths}
    lines = [line for line in lines if not any(line.startswith(f"| {key} |") for key in keys)]
    for key in sorted(keys):
        lines.append(f"| {key} | glTF binary mesh | generated | Blender {bpy.app.version_string}; Codex generator | tools/bpy/generate_kit.py; parametric, flat-shaded, vertex-color palette | Project license TBD | {added} |")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scale", type=float, default=1)
    parser.add_argument("--asset", choices=["all", *BUILDERS], default="all")
    parser.add_argument("--date", default=date.today().isoformat())
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    if not 0 < args.scale <= 100:
        parser.error("--scale must be positive and at most 100")
    date.fromisoformat(args.date)
    out = ROOT / "assets" / "kit"
    out.mkdir(parents=True, exist_ok=True)
    paths = []
    for name, builder in BUILDERS.items():
        if args.asset not in ("all", name):
            continue
        objects = builder()
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.location *= args.scale
            obj.scale *= args.scale
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        target = out / f"{name}.glb"
        result = bpy.ops.export_scene.gltf(filepath=str(target), export_format="GLB", use_selection=True,
            export_apply=True, export_animations=False, export_materials="EXPORT",
            export_vertex_color="MATERIAL", export_all_vertex_colors=True, export_yup=True)
        if "FINISHED" not in result:
            raise RuntimeError(f"glTF export failed for {name}: {result}")
        paths.append(target)
    provenance(paths, args.date)
    print(f"Generated {len(paths)} pieces with Blender {bpy.app.version_string}: {out}")


if __name__ == "__main__":
    main()
