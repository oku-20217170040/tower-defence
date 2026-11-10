"""Frontline asset kutuphanesi - ortak altyapi.

Butun asset'ler TEK bir ana dosyada yasar: frontline_assets.blend
(bu klasorde). Her sprite parcasi kendi koleksiyonundadir; ayni palet,
ayni toon malzeme, ayni isik ve ayni kamera kullanilir. Yeni bir asset
eklemek icin bu dosyadaki yardimcilari kullanan bir uretici script yaz
(ornek: tower_mg.py), calistir, sonra save_library() cagir.

Kullanim (Blender icinden):
    import sys; sys.path.insert(0, r"<repo>/tools/blender/library")
    import fl_lib, tower_mg
    fl_lib.open_library()      # varsa ana dosyayi acar, yoksa kurar
    tower_mg.build_all()       # koleksiyonlari (yeniden) kurar + render alir
    fl_lib.save_library()      # ana dosyayi diske yazar

Kurallar:
- Namlu yerel -Y'ye bakar (resimde asagi). TowerTurret'in taban kurali.
- Donme ekseni dunya orijinidir; kaide ve taret AYNI kadrajla render
  edilir ki Unity'de ust uste tam otursunlar.
- Tuval 1,4 birim genisliginde: 1 birim = 1 harita hucresi.
"""
import math
import os
import random

import bmesh
import bpy
from mathutils import Vector

LIB_DIR = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(os.path.dirname(LIB_DIR)))
LIB_BLEND = os.path.join(LIB_DIR, "frontline_assets.blend")
OUT_TOWERS = os.path.join(REPO, "Assets", "Frontline", "Art", "TowerSet")
# Cevre, efekt ve mermi gorselleri: sahne kurucunun zaten okudugu klasor.
OUT_SPRITES = os.path.join(REPO, "Assets", "Sprites", "Frontline")

ROOT = "FL_ASSETS"          # butun asset koleksiyonlarinin ebeveyni
CANVAS = 1.4                # kameranin gordugu genislik (birim)
RESOLUTION = 512
OUTLINE_PX = 5.0

# Ortak palet (sRGB hex). Yeni renk gerekiyorsa BURAYA ekle, uretici
# script'e gomme - butun asset'ler ayni paleti paylasmali.
PALETTE = {
    "steel_dark":  "#3b4148",
    "steel_mid":   "#6f7a84",
    "steel_light": "#aab4bd",
    "gunmetal":    "#4c535c",
    "olive":       "#6f8f3a",
    "olive_dark":  "#4f6a2a",
    "olive_light": "#93b455",
    "brass":       "#c9973a",
    "hazard":      "#f2c230",
    "rubber":      "#23262a",
    "outline":     "#1b1d21",
}


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def rgb(key):
    h = PALETTE[key].lstrip("#")
    return tuple(_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


# ---------------------------------------------------------------- dosya

def open_library():
    """Ana dosya varsa acar; yoksa bos bir sahne kurup rig'i hazirlar."""
    if os.path.exists(LIB_BLEND):
        if os.path.normcase(bpy.data.filepath) != os.path.normcase(LIB_BLEND):
            bpy.ops.wm.open_mainfile(filepath=LIB_BLEND)
    else:
        for ob in list(bpy.data.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
    setup_rig()


def save_library():
    os.makedirs(LIB_DIR, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=LIB_BLEND, check_existing=False)
    return LIB_BLEND


# ------------------------------------------------------------------ rig

def setup_rig():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = RESOLUTION
    scene.render.resolution_y = RESOLUTION
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"

    cam = bpy.data.objects.get("FL_Camera")
    if cam is None:
        cam = bpy.data.objects.new("FL_Camera", bpy.data.cameras.new("FL_Camera"))
        scene.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = CANVAS
    cam.location = (0.0, 0.0, 5.0)
    cam.rotation_euler = (0.0, 0.0, 0.0)
    scene.camera = cam

    # Isik resmin sol ustunden gelir (butun asset'lerde ayni).
    sun = bpy.data.objects.get("FL_Sun")
    if sun is None:
        sun = bpy.data.objects.new("FL_Sun", bpy.data.lights.new("FL_Sun", "SUN"))
        scene.collection.objects.link(sun)
    sun.data.energy = 3.2
    # Golge KAPALI: toon bantlari esik degerinde; dusen golge bir yuzeyi
    # rastgele alt banda itiyordu (L2 govdesi koyu cikmisti).
    sun.data.use_shadow = False
    sun.rotation_euler = Vector((0.45, -0.45, -0.77)).to_track_quat("-Z", "Y").to_euler()

    world = scene.world or bpy.data.worlds.new("FL_World")
    scene.world = world
    world.use_nodes = True
    bg = next((n for n in world.node_tree.nodes if n.type == "BACKGROUND"), None)
    if bg is not None:
        bg.inputs[0].default_value = (1.0, 1.0, 1.0, 1.0)
        bg.inputs[1].default_value = 0.25

    # Kontur: Freestyle, mutlak kalinlik.
    scene.render.use_freestyle = True
    scene.render.line_thickness_mode = "ABSOLUTE"
    scene.render.line_thickness = 1.0   # birim; asil kalinlik linestyle.thickness (carpilir)
    layer = bpy.context.view_layer
    layer.use_freestyle = True
    fs = layer.freestyle_settings
    ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("FL_Outline")
    ls.select_silhouette = True
    ls.select_border = True
    ls.select_crease = True
    ls.select_contour = True
    ls.select_external_contour = True
    ls.linestyle.thickness = OUTLINE_PX
    ls.linestyle.color = rgb("outline")

    if ROOT not in bpy.data.collections:
        scene.collection.children.link(bpy.data.collections.new(ROOT))


# ------------------------------------------------------------- malzeme

def toon(key):
    """Paletteki renk icin 4 bantli toon malzeme (golge/orta/acik/parlak)."""
    name = "FL_" + key
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs[0].default_value = (1.0, 1.0, 1.0, 1.0)
    to_rgb = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "CONSTANT"
    bands = [(0.0, 0.50), (0.30, 0.74), (0.62, 1.0), (0.93, 1.22)]
    els = ramp.color_ramp.elements
    while len(els) < len(bands):
        els.new(0.5)
    for el, (pos, val) in zip(els, bands):
        el.position = pos
        el.color = (val, val, val, 1.0)
    mul = nt.nodes.new("ShaderNodeVectorMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = rgb(key)
    emit = nt.nodes.new("ShaderNodeEmission")

    nt.links.new(diff.outputs[0], to_rgb.inputs[0])
    nt.links.new(to_rgb.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], mul.inputs[0])
    nt.links.new(mul.outputs[0], emit.inputs[0])
    nt.links.new(emit.outputs[0], out.inputs[0])
    return mat


# ---------------------------------------------------------- koleksiyon

def fresh_collection(name):
    """Ayni adli koleksiyonu bosaltip geri verir (tekrar calistirilabilir)."""
    root = bpy.data.collections[ROOT]
    coll = bpy.data.collections.get(name)
    if coll is None:
        coll = bpy.data.collections.new(name)
        root.children.link(coll)
    for ob in list(coll.objects):
        mesh = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if mesh is not None and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    return coll


# ------------------------------------------------------------- geometri

def _finish(name, bm, key, coll, loc, rot, bevel, smooth_sides=False):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    if smooth_sides == "all":
        for poly in mesh.polygons:
            poly.use_smooth = True
    elif smooth_sides:
        for poly in mesh.polygons:
            poly.use_smooth = abs(poly.normal.z) < 0.5
    mesh.materials.append(toon(key))
    ob = bpy.data.objects.new(name, mesh)
    coll.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = rot
    if bevel > 0.0:
        mod = ob.modifiers.new("bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
        mod.angle_limit = math.radians(40.0)
    return ob


def box(coll, name, size, loc, key, bevel=0.02, rot=(0.0, 0.0, 0.0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]
        v.co.y *= size[1]
        v.co.z *= size[2]
    return _finish(name, bm, key, coll, loc, rot, bevel)


def cyl(coll, name, radius, depth, loc, key, verts=24, bevel=0.0,
        rot=(0.0, 0.0, 0.0), radius2=None, smooth=None):
    """Z ekseninde silindir. Yatay namlu icin along_y() kullan."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=verts,
                          radius1=radius,
                          radius2=radius if radius2 is None else radius2,
                          depth=depth)
    if smooth is None:
        smooth = verts >= 12
    return _finish(name, bm, key, coll, loc, rot, bevel, smooth_sides=smooth)


def ball(coll, name, radius, loc, key, squash=1.0, subdiv=2):
    """Yumusak kure (agac yapragi, toz bulutu). squash: Z ezme orani."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    for v in bm.verts:
        v.co.z *= squash
    return _finish(name, bm, key, coll, loc, (0.0, 0.0, 0.0), 0.0, smooth_sides="all")


def rock(coll, name, radius, loc, key, seed=0, squash=0.7, rough=0.22):
    """Koseli tas: dusuk cozunurluklu kure + tohumlu rastgele bozma."""
    rng = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=radius)
    for v in bm.verts:
        v.co *= 1.0 + rng.uniform(-rough, rough)
        v.co.z *= squash
    return _finish(name, bm, key, coll, loc, (0.0, 0.0, rng.uniform(0.0, 6.28)), 0.0)


def star(coll, name, points, r_out, r_in, loc, key, rot_z=0.0, stretch_y=1.0):
    """Duz yildiz (namlu cakmasi, patlama). Yuzu +Z'ye bakar."""
    bm = bmesh.new()
    verts = []
    for i in range(points * 2):
        a = math.pi * i / points + rot_z
        r = r_out if i % 2 == 0 else r_in
        verts.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r * stretch_y, 0.0)))
    face = bm.faces.new(verts)
    bm.normal_update()
    if face.normal.z < 0:
        bmesh.ops.reverse_faces(bm, faces=[face])
    return _finish(name, bm, key, coll, loc, (0.0, 0.0, 0.0), 0.0)


def along_y():
    """cyl() icin: silindiri Y eksenine yatirir (namlu)."""
    return (math.radians(90.0), 0.0, 0.0)


def scale_collection(coll, factor):
    """Koleksiyondaki her parcayi orijine gore buyutur (taret/kaide orani)."""
    for ob in coll.objects:
        ob.location = ob.location * factor
        ob.scale = (factor, factor, factor)


# --------------------------------------------------------------- render

def render_collection(name, path, canvas=None, resolution=None):
    """Yalnizca verilen koleksiyonu render eder.

    canvas: kameranin gordugu genislik (birim). Kuleler ortak CANVAS'i
    kullanir (kaide ile taret ust uste otursun diye); cevre ve efektler
    kendi boyutuna gore kadraj verir.
    """
    root = bpy.data.collections[ROOT]
    for coll in root.children:
        coll.hide_render = coll.name != name
    os.makedirs(os.path.dirname(path), exist_ok=True)
    scene = bpy.context.scene
    scene.camera.data.ortho_scale = canvas or CANVAS
    scene.render.resolution_x = resolution or RESOLUTION
    scene.render.resolution_y = resolution or RESOLUTION
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    scene.camera.data.ortho_scale = CANVAS
    scene.render.resolution_x = RESOLUTION
    scene.render.resolution_y = RESOLUTION
    return path
