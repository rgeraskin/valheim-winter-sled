"""Renders previews of the sled: the cart body plus the generated undercarriage.

The cart body mesh and the textures are the game's, extracted locally for
previews only (see README); they are not part of the repo.

blender -b --factory-startup -P model/render_preview.py -- \
    <undercarriage.obj> <cart_body.obj> <texture dir> <out prefix>
"""

import math
import sys

import bpy
import mathutils

BODY_DROP = 0.10
# where the cart's body mesh sits in the prefab (Vagon/new/default)
BODY_ORIGIN = (0.0, 0.47 - BODY_DROP, 0.39)

argv = sys.argv[sys.argv.index("--") + 1:]
under_obj, body_obj, tex_dir, out = argv

# material -> (texture, tiling) as the game's materials set them up
MATERIALS = {
    "body": ("Cart_d.png", (1, 1), 0.85),
    "wood": ("DarkWoodBeams_d.png", (1, 1), 0.75),
    "iron": ("metalwall.png", (2, 2), 0.35),
    "rope": ("rope_d.png", (4, 1), 0.9),
    "fur": ("rug_wolf.png", (1, 1), 1.0),
}

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def make_material(name):
    tex, tiling, rough = MATERIALS[name]
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    img = nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(f"{tex_dir}/{tex}")
    img.interpolation = "Closest"
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (tiling[0], tiling[1], 1)
    coords = nodes.new("ShaderNodeTexCoord")
    links.new(coords.outputs["UV"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], img.inputs["Vector"])
    links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = rough
    if name == "iron":
        bsdf.inputs["Metallic"].default_value = 0.8
    if name == "fur":
        # the rug shader is alpha-tested; the texture's alpha is the pelt's outline
        links.new(img.outputs["Alpha"], bsdf.inputs["Alpha"])
        mat.surface_render_method = "DITHERED"
    return mat


mats = {name: make_material(name) for name in MATERIALS}


def import_obj(path):
    # OBJ is y-up; Blender maps it to (x, -z, y), so the sled's front (unity +z) faces -Y
    bpy.ops.wm.obj_import(filepath=path, forward_axis="NEGATIVE_Z", up_axis="Y")
    return list(bpy.context.selected_objects)


def to_blender(p):
    return (-p[0], -p[2], p[1])


for o in import_obj(body_obj):
    o.location = to_blender(BODY_ORIGIN)
    o.data.materials.clear()
    o.data.materials.append(mats["body"])

for o in import_obj(under_obj):
    for slot in o.material_slots:
        base = slot.material.name.split(".")[0] if slot.material else ""
        if base in mats:
            slot.material = mats[base]

# ground and snow-ish backdrop
bpy.ops.mesh.primitive_plane_add(size=12, location=(0, 0, 0))
ground = bpy.context.active_object
gm = bpy.data.materials.new("ground")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.82, 0.86, 0.92, 1)
gm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.95
ground.data.materials.append(gm)

world = bpy.data.worlds.new("w")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.70, 0.82, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
sun = bpy.data.lights.new("sun", "SUN")
sun.energy = 3.2
sun.angle = math.radians(8)
sun_obj = bpy.data.objects.new("sun", sun)
scene.collection.objects.link(sun_obj)
sun_obj.rotation_euler = (math.radians(48), 0, math.radians(-35))

cam = bpy.data.cameras.new("cam")
cam.lens = 40
cam_obj = bpy.data.objects.new("cam", cam)
scene.collection.objects.link(cam_obj)
scene.camera = cam_obj
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 900
scene.render.resolution_y = 620
scene.view_settings.view_transform = "Standard"

target = mathutils.Vector(to_blender((0, 0.42, 0.45)))
# (azimuth from the front, elevation, distance, look-at offset in unity coords)
views = {
    "front34": (35, 22, 5.0, (0, 0, 0)),
    "rear34": (215, 25, 5.0, (0, 0, 0)),
    "side": (90, 4, 4.6, (0, -0.1, 0)),
    "low_front": (25, 8, 2.4, (0.55, -0.1, 0.85)),
}
for name, (az, el, dist, off) in views.items():
    a, e = math.radians(az), math.radians(el)
    look = target + mathutils.Vector(to_blender(off))
    cam_obj.location = look + mathutils.Vector((dist * math.cos(e) * math.sin(a), -dist * math.cos(e) * math.cos(a), dist * math.sin(e)))
    cam_obj.rotation_euler = (look - cam_obj.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = f"{out}_{name}.png"
    bpy.ops.render.render(write_still=True)
