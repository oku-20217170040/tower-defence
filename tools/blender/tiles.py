"""
Frontline — harita dosemesi ureteci (Blender).

PAZARLIKSIZ KURAL: zemin DIKKAT CEKMEMELI.
GridTileRenderer'in yorumu bunu zaten soyluyordu ve dogru: oyuncunun
bakmasi gereken sey kesif izi izleridir. Zemin fazla kontrastli olursa
izler kaybolur ve oyunun ANA GERI BILDIRIM KANALI korelir.

Bu yuzden doseme render'lari:
  - dusuk doygunluk (renk degil, doku)
  - dusuk kontrast (detaylar govde renginin +-%15'i icinde)
  - kenarlarda duz kenar payi -> yan yana dizilince dikis gorunmez

Uc doseme:
  koridor  -> birliklerin yurudugu yer. En koyu, en sakin.
  kurulum  -> kule kurulabilen serit. Bir tik acik + plaka hissi ki
              oyuncu "buraya kurabilirim" bilgisini zeminden okusun.
  duvar    -> gecilemez. Kutleli bloklar; koridordan bakisla ayrilmali.

Kullanim:
    exec(open(r"<repo>/tools/blender/tiles.py").read())
    setup_tile_rig(); build_all_tiles()
"""

import bpy
import os
import math
import random

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Frontline/Art/Tiles"
TILE_PX = 64

CORRIDOR = (0.115, 0.108, 0.095, 1.0)
BUILD = (0.215, 0.200, 0.170, 1.0)
BLOCKED = (0.075, 0.082, 0.075, 1.0)


def setup_tile_rig(res=TILE_PX):
    """Tam 1x1 birimlik kareyi dolduran ortografik tepeden kamera."""
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)

    sc = bpy.context.scene
    engines = [e.identifier for e in
               bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = ('BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines
                        else 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in engines else 'CYCLES')
    sc.render.film_transparent = False      # zemin OPAK olmali
    sc.render.resolution_x = res
    sc.render.resolution_y = res
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGB'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'

    cam_data = bpy.data.cameras.new("TileCam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 1.0               # kare tam 1 birim
    cam = bpy.data.objects.new("TileCam", cam_data)
    cam.location = (0, 0, 6)
    cam.rotation_euler = (0, 0, 0)
    sc.collection.objects.link(cam)
    sc.camera = cam

    # Tek, yumusak ve neredeyse tepeden isik: sert golge dikis yaratir.
    key = bpy.data.lights.new("TileKey", type='AREA')
    key.energy, key.size = 160, 8
    ko = bpy.data.objects.new("TileKey", key)
    ko.location, ko.rotation_euler = (1.2, 1.6, 5), (-0.22, 0.16, 0)
    sc.collection.objects.link(ko)


def mat(name, rgba, rough=0.85):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = rgba
    b.inputs["Roughness"].default_value = rough
    return m


def clear():
    for o in list(bpy.data.objects):
        if o.type == 'MESH':
            bpy.data.objects.remove(o, do_unlink=True)


def slab(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    if bevel > 0.0:
        bm = o.modifiers.new("bev", 'BEVEL')
        bm.width, bm.segments = bevel, 2
    o.data.materials.append(material)
    return o


def shade(base, k):
    """Govde renginin +-%15 bandinda kalan ton. Kontrast bilincli dusuk."""
    return (max(0.0, base[0] * k), max(0.0, base[1] * k), max(0.0, base[2] * k), 1.0)


def render_tile(name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


# ----------------------------------------------------------------- dosemeler

MARGIN = 0.10   # kenar payi: detay buraya girmez, dikis gorunmesin


def build_corridor(seed=1):
    """Birliklerin yurudugu yer: en koyu, en sakin. Hafif tekerlek izleri."""
    clear()
    random.seed(seed)
    base = mat("Tile_Corridor", CORRIDOR)
    dark = mat("Tile_CorridorDark", shade(CORRIDOR, 0.86))
    light = mat("Tile_CorridorLight", shade(CORRIDOR, 1.14))

    slab("Ground", (0, 0, 0), (1.2, 1.2, 0.1), base)

    # iki soluk iz: yuruyus yonunu (dikey) sezdirir ama okumayi bolmez
    for sx in (-0.22, 0.22):
        slab("Rut%d" % int(sx * 100), (sx, 0, 0.055), (0.16, 1.2, 0.02), dark)

    # birkac cakil: dokuyu kirar, tekrari gizler
    for i in range(5):
        x = random.uniform(-0.5 + MARGIN, 0.5 - MARGIN)
        y = random.uniform(-0.5 + MARGIN, 0.5 - MARGIN)
        s = random.uniform(0.035, 0.065)
        slab("Grit%d" % i, (x, y, 0.06), (s, s, 0.02), light)


def build_build_strip(seed=2):
    """Kule kurulabilen serit: plaka hissi. Oyuncu 'buraya kurabilirim'i
    zeminden okumali - bu bir SUS degil, arayuz."""
    clear()
    random.seed(seed)
    base = mat("Tile_Build", BUILD)
    edge = mat("Tile_BuildEdge", shade(BUILD, 0.82))
    rivet = mat("Tile_BuildRivet", shade(BUILD, 1.18))

    slab("Ground", (0, 0, 0), (1.2, 1.2, 0.1), base)

    # icten cerceve: plakayi kenardan ayirir, dikis kenarinda kalmaz
    inset = 0.5 - MARGIN * 1.6
    for sx, sy, w, h in ((0, inset, 2 * inset, 0.03),
                         (0, -inset, 2 * inset, 0.03),
                         (inset, 0, 0.03, 2 * inset),
                         (-inset, 0, 0.03, 2 * inset)):
        slab("Frame_%s_%s" % (sx, sy), (sx, sy, 0.055), (w, h, 0.02), edge)

    # kose perçinleri
    for sx in (-1, 1):
        for sy in (-1, 1):
            slab("Rivet%d%d" % (sx, sy), (sx * inset * 0.72, sy * inset * 0.72, 0.065),
                 (0.055, 0.055, 0.03), rivet, bevel=0.01)


def build_blocked(seed=3):
    """Gecilemez: kutleli bloklar. Koridordan BAKISLA ayrilmali."""
    clear()
    random.seed(seed)
    base = mat("Tile_Blocked", BLOCKED)
    block = mat("Tile_BlockedBlock", shade(BLOCKED, 1.25))
    dark = mat("Tile_BlockedDark", shade(BLOCKED, 0.75))

    slab("Ground", (0, 0, 0), (1.2, 1.2, 0.1), base)

    # duzensiz bloklar: duvar oldugunu kutle anlatir, renk degil
    cells = [(-0.24, 0.24), (0.20, 0.26), (0.26, -0.18), (-0.22, -0.24), (0.0, 0.0)]
    for i, (x, y) in enumerate(cells):
        w = random.uniform(0.20, 0.30)
        h = random.uniform(0.20, 0.30)
        slab("Block%d" % i, (x, y, 0.09), (w, h, 0.10), block, bevel=0.015)
        slab("Shadow%d" % i, (x + 0.02, y - 0.02, 0.055), (w, h, 0.02), dark)


def build_all_tiles():
    out = []
    for name, fn in (("koridor", build_corridor),
                     ("kurulum", build_build_strip),
                     ("duvar", build_blocked)):
        fn()
        out.append(render_tile(name + ".png"))
    return out
