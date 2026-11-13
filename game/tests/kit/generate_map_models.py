"""ART-12 additions. Run: blender -b --factory-startup --python game/tests/kit/generate_map_models.py"""
import importlib.util
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("kit", ROOT / "tools/bpy/generate_kit.py")
kit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(kit)


def kitchen():
    pieces = kit.tent()
    # Open serving hatch and counter, stove and tall vent distinguish it from lodging.
    pieces += [kit.box("serving-hatch", (1.8, .035, .8), (0, -1.61, .75), "dark"),
               kit.box("serving-counter", (2.2, .6, .12), (0, -1.85, .5), "sand"),
               kit.box("stove", (.8, 1, .8), (1.15, .4, .4), "dark"),
               kit.cylinder("kitchen-flue", .14, 2.4, (1.15, .4, 1.2), "dark"),
               kit.cylinder("vent-cap", .24, .12, (1.15, .4, 2.45), "olive")]
    return pieces


def dumpster():
    # Broad bin, raised split lids, lifting pockets and four casters.
    pieces = [kit.box("bin", (2.8, 1.65, 1.45), (0, 0, 1), "olive"),
              kit.box("rim", (2.95, 1.8, .14), (0, 0, 1.75), "dark")]
    for x in (-.73, .73):
        lid = kit.box("lid", (1.4, 1.75, .13), (x, 0, 1.9), "olive")
        lid.rotation_euler.x = -.16
        pieces.append(lid)
        pieces.append(kit.box("lifting-pocket", (.45, .14, .22), (x, -.92, 1.2), "dark"))
    for x in (-1.05, 1.05):
        for y in (-.55, .55):
            pieces.append(kit.cylinder("caster", .24, .18, (x, y, .24), "dark", wheel=True))
    return pieces


for name, builder in (("field-kitchen", kitchen), ("waste-dumpster", dumpster)):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    objects = builder()
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    result = bpy.ops.export_scene.gltf(filepath=str(ROOT / "assets/kit" / (name + ".glb")),
        export_format="GLB", use_selection=True, export_apply=True, export_animations=False,
        export_materials="EXPORT", export_vertex_color="MATERIAL", export_all_vertex_colors=True,
        export_yup=True)
    assert "FINISHED" in result, result
print("MAP_MODELS_GENERATED: field kitchen and waste dumpster; Blender", bpy.app.version_string)
