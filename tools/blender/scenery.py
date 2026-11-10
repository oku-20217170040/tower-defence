"""
Frontline - dogal ortu ureteci (Blender): kaya, agac, cukur, dag.

3 Eylul 2026 gorsel tutarlilik turu: TowerVariety setiyle (bkz.
generate_tower_variety_set.py) AYNI rig - ayni EEVEE motoru, ayni Key/Fill
isik kurulumu, ayni 512px seffaf render, ayni "cikinti/kontur YOK" kurali
(fx_scenery_extra.py'deki apply_outline burada BILEREK kullanilmiyor -
TowerVariety setinde de yok, iki aile arasindaki tek fark artik siluet,
isik/kontur dili degil).

Uretilenler (Assets/Sprites/Frontline'a dogrudan yazilir):
  scenery_rock.png / scenery_rock_large.png   - kucuk/buyuk kaya
  scenery_tree.png / scenery_tree_large.png   - kucuk/buyuk agac
  scenery_crater.png                          - yikilma sonrasi cukur
  scenery_mountain.png                        - harita kenari / duvar
                                                 hucresi icin buyuk dag kutlesi
                                                 (kullanici istegi: "daglar")

Kullanim:
    exec(open(r"<repo>/tools/blender/scenery.py").read())
    build_all()
"""

import bpy
import os
import math
import random

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Sprites/Frontline"

ROCK_GREY = (0.32, 0.33, 0.31, 1.0)
ROCK_DARK = (0.20, 0.20, 0.19, 1.0)
TREE_GREEN = (0.22, 0.44, 0.19, 1.0)
TREE_DARK = (0.14, 0.30, 0.13, 1.0)
TRUNK = (0.28, 0.20, 0.13, 1.0)
CRATER_DIRT = (0.30, 0.23, 0.16, 1.0)
CRATER_DARK = (0.16, 0.12, 0.09, 1.0)
MOUNTAIN_ROCK = (0.36, 0.37, 0.38, 1.0)
MOUNTAIN_SNOW = (0.82, 0.84, 0.86, 1.0)
MOUNTAIN_DARK = (0.22, 0.22, 0.23, 1.0)


def setup_rig(res=512, ortho=1.0):
    """TowerVariety ile birebir ayni: engine, isik, seffaf film.

    3 Eylul 2026 DUZELTME: eskiden burada TUM nesneler siliniyordu
    (kamera/isik/MESH dahil). render_to() sirasi build_X() -> setup_rig()
    -> render oldugu icin bu, az once insa edilen MESH'i render'dan HEMEN
    ONCE siliyordu - sonuc her zaman BOS/SEFFAF PNG (0 opak piksel). Simdi
    yalnizca kamera/isik siliniyor (bunlar zaten bu fonksiyon tarafindan
    yeniden yaratiliyor); MESH nesneleri clear() fonksiyonunun isi.
    """
    for o in list(bpy.data.objects):
        if o.type in ('CAMERA', 'LIGHT'):
            bpy.data.objects.remove(o, do_unlink=True)

    sc = bpy.context.scene
    engines = [e.identifier for e in
               bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = ('BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines
                        else 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in engines else 'CYCLES')
    sc.render.film_transparent = True
    sc.render.resolution_x = res
    sc.render.resolution_y = res
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'

    cam_data = bpy.data.cameras.new("TopCam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.new("TopCam", cam_data)
    cam.location = (0, 0, 10)
    cam.rotation_euler = (0, 0, 0)
    sc.collection.objects.link(cam)
    sc.camera = cam

    key = bpy.data.lights.new("Key", type='AREA')
    key.energy, key.size = 220, 6
    ko = bpy.data.objects.new("Key", key)
    ko.location, ko.rotation_euler = (2.5, 3.5, 6), (-0.5, 0.35, 0)
    sc.collection.objects.link(ko)

    fill = bpy.data.lights.new("Fill", type='AREA')
    fill.energy, fill.size = 70, 8
    fo = bpy.data.objects.new("Fill", fill)
    fo.location, fo.rotation_euler = (-3, -2.5, 5), (0.45, -0.35, 0)
    sc.collection.objects.link(fo)


def mat(name, rgba, rough=0.7, emit=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = rgba
    b.inputs["Roughness"].default_value = rough
    if emit > 0.0 and "Emission Strength" in b.inputs:
        if "Emission Color" in b.inputs:
            b.inputs["Emission Color"].default_value = rgba
        b.inputs["Emission Strength"].default_value = emit
    return m


def clear():
    for o in list(bpy.data.objects):
        if o.type == 'MESH':
            bpy.data.objects.remove(o, do_unlink=True)


def sphere(name, r, loc, material, segs=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=segs, ring_count=rings)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
    return o


def rock_chunk(name, r, loc, material, subdiv=2, seed=0):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=r, location=loc, subdivisions=subdiv)
    o = bpy.context.object
    o.name = name
    random.seed(seed)
    me = o.data
    for v in me.vertices:
        f = 1.0 + random.uniform(-0.18, 0.18)
        v.co.x *= f
        v.co.y *= f
        v.co.z *= (1.0 + random.uniform(-0.12, 0.12))
    me.update()
    o.data.materials.append(material)
    return o


def cyl(name, r, depth, loc, material, rot=(0, 0, 0), verts=12):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=loc,
                                        rotation=rot, vertices=verts)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
    return o


def plate(name, pts, z, thick, material, bevel=0.015):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x, y, z) for (x, y) in pts], [], [list(range(len(pts)))])
    me.update()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    sm = o.modifiers.new("sol", 'SOLIDIFY')
    sm.thickness, sm.offset = thick, 0
    if bevel > 0:
        bm = o.modifiers.new("bev", 'BEVEL')
        bm.width, bm.segments = bevel, 2
    o.data.materials.append(material)
    return o


def render_to(name, res=512, ortho=1.0):
    setup_rig(res=res, ortho=ortho)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


# ------------------------------------------------------------------ kaya

def build_rock(big=False):
    clear()
    grey = mat("Rock_Grey", ROCK_GREY, rough=0.75)
    dark = mat("Rock_Dark", ROCK_DARK, rough=0.8)
    s = 1.35 if big else 1.0
    rock_chunk("Main", 0.30 * s, (0.02, -0.02, 0.16 * s), grey, subdiv=2, seed=1)
    rock_chunk("Side", 0.18 * s, (-0.22 * s, 0.14 * s, 0.09 * s), dark, subdiv=1, seed=2)
    rock_chunk("Small", 0.12 * s, (0.20 * s, 0.18 * s, 0.05 * s), dark, subdiv=1, seed=3)
    if big:
        rock_chunk("Extra", 0.15 * s, (0.02, -0.26 * s, 0.07 * s), grey, subdiv=1, seed=4)


# ------------------------------------------------------------------ agac

def build_tree(big=False):
    clear()
    green = mat("Tree_Green", TREE_GREEN, rough=0.55)
    dark = mat("Tree_Dark", TREE_DARK, rough=0.6)
    trunk = mat("Tree_Trunk", TRUNK, rough=0.7)
    s = 1.35 if big else 1.0

    cyl("Trunk", 0.05 * s, 0.14 * s, (0, 0, 0.05 * s), trunk)
    sphere("Core", 0.26 * s, (0, 0, 0.16 * s), green, segs=14, rings=10)
    n = 6
    for i in range(n):
        a = i * (2 * math.pi / n)
        r = 0.24 * s
        loc = (r * math.cos(a), r * math.sin(a), 0.13 * s)
        sphere("Petal%d" % i, 0.17 * s, loc, green, segs=10, rings=8)
    if big:
        for i in range(n):
            a = i * (2 * math.pi / n) + (math.pi / n)
            r = 0.14 * s
            loc = (r * math.cos(a), r * math.sin(a), 0.28 * s)
            sphere("PetalTop%d" % i, 0.11 * s, loc, dark, segs=8, rings=6)


# ---------------------------------------------------------------- cukur

def build_crater():
    clear()
    dirt = mat("Crater_Dirt", CRATER_DIRT, rough=0.9)
    dark = mat("Crater_Dark", CRATER_DARK, rough=0.9)

    # asagi cukurlesen halka izlenimi: disk + kucuk ic karanlik disk
    cyl("Rim", 0.32, 0.03, (0, 0, 0.0), dirt, verts=20)
    cyl("Pit", 0.20, 0.02, (0, 0, -0.01), dark, verts=18)
    random.seed(9)
    for i in range(5):
        a = random.uniform(0, 2 * math.pi)
        r = random.uniform(0.20, 0.30)
        loc = (r * math.cos(a), r * math.sin(a), 0.02)
        rock_chunk("Debris%d" % i, 0.04, loc, dirt, subdiv=1, seed=100 + i)


# ------------------------------------------------------------------- dag

def build_mountain():
    """Harita kenari / blocked hucre icin buyuk dag kutlesi - kullanicinin
    'daglar' istegi. Kar tepeli, koseli, dusuk poli - kaya ailesiyle ayni
    malzeme dili ama cok daha buyuk ve daha yuksek (golge/derinlik icin
    tepe rengi acilir)."""
    clear()
    rock = mat("Mtn_Rock", MOUNTAIN_ROCK, rough=0.8)
    dark = mat("Mtn_Dark", MOUNTAIN_DARK, rough=0.85)
    snow = mat("Mtn_Snow", MOUNTAIN_SNOW, rough=0.5)

    plate("Peak1", [(-0.34, -0.30), (0.02, 0.38), (0.36, -0.20), (0.10, -0.36)],
          0.0, 0.30, rock, bevel=0.02)
    plate("Peak2", [(-0.10, -0.32), (-0.30, 0.16), (0.06, 0.30), (0.18, -0.10)],
          0.0, 0.20, dark, bevel=0.02)
    # kar sapkasi: tepe uclarina yakin kucuk beyaz plaka
    plate("Snow1", [(-0.06, 0.14), (0.02, 0.34), (0.16, 0.10)],
          0.28, 0.05, snow, bevel=0.01)
    plate("Snow2", [(-0.24, 0.02), (-0.28, 0.16), (-0.14, 0.10)],
          0.18, 0.04, snow, bevel=0.01)


def build_all():
    out = []

    build_rock(big=False)
    out.append(render_to("scenery_rock.png", res=512, ortho=0.90))

    build_rock(big=True)
    out.append(render_to("scenery_rock_large.png", res=512, ortho=0.90))

    build_tree(big=False)
    out.append(render_to("scenery_tree.png", res=512, ortho=0.90))

    build_tree(big=True)
    out.append(render_to("scenery_tree_large.png", res=512, ortho=0.90))

    build_crater()
    out.append(render_to("scenery_crater.png", res=512, ortho=1.0))

    build_mountain()
    out.append(render_to("scenery_mountain.png", res=512, ortho=0.85))

    return out
