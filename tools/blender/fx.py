"""
Frontline - mermi / namlu cakmasi / carpma patlamasi sprite ureteci (Blender).

Sanat yonu kulelerle ayni: sade siluet, dusuk poli, tepeden ortografik.

YON KURALI (birlik ve kulelerle AYNI): burun yerel -Y'de uretilir, yani
ekranda asagi. Kod tarafinda hedef aci = yonun acisi + 90 derece
(bkz. TowerTurret.AngleFor). Mermi ve namlu cakmasi ayni kurala uyar,
yoksa merminin burnu ucus yonune bakmaz.

GORSEL DIL (spec 9.1) burada da surer:
  batarya -> GURULTULU. Kalin obus, buyuk turuncu cakma, genis patlama.
  pusu    -> SESSIZ. Ince dart, kucuk soluk cakma, kivilcimli ufak carpma.
  isin    -> mavi elektrik; mermisi YOK (hat kod ile cizilir), yalnizca
             namlu cakmasi ve carpma parlamasi uretilir.

EMISYON DERSI (22 Agu, kulelerde ogrenildi ve burada TEKRARLANDI):
1,0 ustu emisyonda renk beyaza patliyor - ilk tur mermi/patlama
render'lari renksiz cikti. Emisyon 0,3-0,8 bandinda tutulur, renk
BASE COLOR'dan gelir.

Patlamalar TEK KARE uretilir; animasyon Unity'de olcek + donme + sonme
ile yapilir (bkz. ImpactBurst). Kare kare sprite sheet mobilde bellek ve
import maliyeti demek; bu oyunda patlama 0,25 sn goruniyor, tek kare
yetiyor.

Kullanim:
    exec(open(r"<repo>/tools/blender/fx.py").read())
    setup_rig(); build_all()
"""

import bpy
import os
import math

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Frontline/Art/Fx"

METAL = (0.44, 0.45, 0.43, 1.0)

# Kule sprite'larindaki vurgu renkleriyle AYNI aile: oyuncu mermiyi
# hangi kulenin attigini renkten okur.
BAT = (1.0, 0.45, 0.12, 1.0)     # batarya turuncusu
AMB = (0.62, 0.72, 0.80, 1.0)    # pusu soluk celigi
BEAM = (0.35, 0.78, 1.0, 1.0)    # isin mavisi


def setup_rig(res=128, ortho=1.0):
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)

    sc = bpy.context.scene
    engines = [e.identifier for e in
               bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = ("BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines
                        else "BLENDER_EEVEE" if "BLENDER_EEVEE" in engines else "CYCLES")
    sc.render.film_transparent = True
    sc.render.resolution_x = res
    sc.render.resolution_y = res
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"

    cam_data = bpy.data.cameras.new("TopCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.new("TopCam", cam_data)
    cam.location = (0, 0, 10)
    cam.rotation_euler = (0, 0, 0)
    sc.collection.objects.link(cam)
    sc.camera = cam

    key = bpy.data.lights.new("Key", type="AREA")
    key.energy, key.size = 220, 6
    ko = bpy.data.objects.new("Key", key)
    ko.location, ko.rotation_euler = (2.5, 3.5, 6), (-0.5, 0.35, 0)
    sc.collection.objects.link(ko)

    fill = bpy.data.lights.new("Fill", type="AREA")
    fill.energy, fill.size = 70, 8
    fo = bpy.data.objects.new("Fill", fill)
    fo.location, fo.rotation_euler = (-3, -2.5, 5), (0.45, -0.35, 0)
    sc.collection.objects.link(fo)


def mat(name, rgba, rough=0.6, emit=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = rgba
    b.inputs["Roughness"].default_value = rough
    if emit > 0.0:
        if "Emission Color" in b.inputs:
            b.inputs["Emission Color"].default_value = rgba
        elif "Emission" in b.inputs:
            b.inputs["Emission"].default_value = rgba
        if "Emission Strength" in b.inputs:
            b.inputs["Emission Strength"].default_value = emit
    return m


def clear():
    for o in list(bpy.data.objects):
        if o.type == "MESH":
            bpy.data.objects.remove(o, do_unlink=True)


def plate(name, pts, z, thick, material, bevel=0.01):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x, y, z) for (x, y) in pts], [], [list(range(len(pts)))])
    me.update()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    sm = o.modifiers.new("sol", "SOLIDIFY")
    sm.thickness, sm.offset = thick, 0
    if bevel > 0:
        bm = o.modifiers.new("bev", "BEVEL")
        bm.width, bm.segments = bevel, 2
    o.data.materials.append(material)
    return o


def cyl(name, r, depth, loc, material, rot=(0, 0, 0), verts=16):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=loc,
                                        rotation=rot, vertices=verts)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
    return o


def box(name, loc, scale, material, bevel=0.01, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    if bevel > 0:
        bm = o.modifiers.new("bev", "BEVEL")
        bm.width, bm.segments = bevel, 2
    o.data.materials.append(material)
    return o


def star(name, spikes, inner, outer, z, thick, material, phase=0.0, jitter=None):
    """Isinsal patlama siluetı. Duzgun daire yerine yildiz: patlama
    'genisleyen bir yuzey' degil, 'firlayan parcalar' okunmali."""
    pts = []
    n = spikes * 2
    for i in range(n):
        a = phase + i * math.pi / spikes
        r = outer if i % 2 == 0 else inner
        if jitter:
            r *= jitter[i % len(jitter)]
        pts.append((r * math.cos(a), r * math.sin(a)))
    return plate(name, pts, z, thick, material, bevel=0.008)


def render_to(name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


# ------------------------------------------------------------------ mermi

def build_bullet_battery():
    """Kalin obus. Burun -Y'de. Arkasinda kisa turuncu itki izi."""
    clear()
    metal = mat("F_Metal", METAL)
    acc = mat("F_Bat", BAT, rough=0.3, emit=0.45)

    cyl("Shell", 0.115, 0.30, (0, 0.02, 0), metal, rot=(1.5708, 0, 0), verts=12)
    plate("Nose", [(-0.115, -0.13), (0.115, -0.13), (0.0, -0.33)], 0, 0.14, metal)
    plate("Flame", [(-0.085, 0.17), (0.085, 0.17), (0.0, 0.40)], 0, 0.12, acc)
    box("Band", (0, 0.06, 0.08), (0.24, 0.05, 0.05), acc, 0.008)


def build_bullet_ambush():
    """Ince dart. SOLUK -- pusu gorunmemeli, mermisi de gurultu yapmamali."""
    clear()
    metal = mat("F_Steel", (0.55, 0.60, 0.64, 1.0), rough=0.35)
    acc = mat("F_Amb", AMB, rough=0.3, emit=0.30)

    plate("Dart", [(-0.05, 0.22), (0.05, 0.22), (0.035, -0.20), (0.0, -0.36),
                   (-0.035, -0.20)], 0, 0.10, metal)
    for sx in (-1, 1):
        plate("Fin%d" % sx, [(sx * 0.05, 0.24), (sx * 0.16, 0.34),
                             (sx * 0.05, 0.12)], 0, 0.06, acc)


# ---------------------------------------------------------- namlu cakmasi

def build_muzzle_battery():
    """Buyuk turuncu koni + yanal kulaklar. Ates edeni ekranda gorunur yapar."""
    clear()
    hot = mat("F_MzB", (1.0, 0.55, 0.14, 1.0), rough=0.2, emit=0.55)
    core = mat("F_MzBc", (1.0, 0.88, 0.55, 1.0), rough=0.2, emit=0.75)

    plate("Cone", [(-0.24, 0.30), (0.24, 0.30), (0.10, -0.44), (-0.10, -0.44)],
          0, 0.10, hot)
    plate("Core", [(-0.10, 0.24), (0.10, 0.24), (0.045, -0.30), (-0.045, -0.30)],
          0.06, 0.08, core)
    for sx in (-1, 1):
        plate("Ear%d" % sx, [(sx * 0.16, 0.22), (sx * 0.44, 0.06),
                             (sx * 0.14, -0.06)], 0, 0.06, hot)


def build_muzzle_ambush():
    """Kucuk, soluk, dar. Oyuncu pusunun calistigini gorur ama goz almaz."""
    clear()
    hot = mat("F_MzA", (0.72, 0.82, 0.90, 1.0), rough=0.2, emit=0.40)

    plate("Cone", [(-0.11, 0.18), (0.11, 0.18), (0.045, -0.34), (-0.045, -0.34)],
          0, 0.08, hot)
    for sx in (-1, 1):
        plate("Ear%d" % sx, [(sx * 0.07, 0.14), (sx * 0.26, 0.02),
                             (sx * 0.06, -0.04)], 0, 0.05, hot)


def build_muzzle_beam():
    """Mavi bosalma halkasi + dar cekirdek: isin 'sarj bosaldi' der."""
    clear()
    hot = mat("F_MzL", BEAM, rough=0.2, emit=0.55)
    core = mat("F_MzLc", (0.75, 0.94, 1.0, 1.0), rough=0.2, emit=0.75)

    star("Ring", 8, 0.16, 0.38, 0.0, 0.07, hot, phase=0.2)
    plate("Core", [(-0.07, 0.14), (0.07, 0.14), (0.03, -0.40), (-0.03, -0.40)],
          0.05, 0.07, core)


# ------------------------------------------------------- carpma / patlama

def build_impact_battery():
    """Genis ates topu: cekirdek + isinsal parcalar + duman halkasi."""
    clear()
    smoke = mat("F_Smoke", (0.28, 0.24, 0.22, 1.0), rough=0.9)
    fire = mat("F_Fire", (1.0, 0.42, 0.10, 1.0), rough=0.25, emit=0.55)
    core = mat("F_Core", (1.0, 0.86, 0.45, 1.0), rough=0.2, emit=0.80)

    star("Smoke", 9, 0.30, 0.48, -0.02, 0.05, smoke, phase=0.3,
         jitter=(1.0, 0.86, 1.08, 0.92))
    star("Fire", 7, 0.20, 0.40, 0.04, 0.09, fire,
         jitter=(1.0, 0.88, 1.06, 0.94))
    cyl("Core", 0.17, 0.12, (0, 0, 0.10), core, verts=14)


def build_impact_ambush():
    """Kucuk kivilcim yildizi. Delici vurus: az kutle, cok cizgi."""
    clear()
    spark = mat("F_Spark", (0.78, 0.86, 0.94, 1.0), rough=0.2, emit=0.45)
    core = mat("F_SparkC", (0.95, 0.98, 1.0, 1.0), rough=0.2, emit=0.70)

    star("Spark", 6, 0.07, 0.34, 0.0, 0.05, spark, phase=0.5,
         jitter=(1.0, 0.7, 1.12, 0.8))
    cyl("Core", 0.075, 0.08, (0, 0, 0.06), core, verts=12)


def build_impact_beam():
    """Mavi elektrik carpmasi: kirikli kollar, ince cekirdek."""
    clear()
    arc = mat("F_Arc", BEAM, rough=0.2, emit=0.55)
    core = mat("F_ArcC", (0.70, 0.92, 1.0, 1.0), rough=0.2, emit=0.75)

    star("Arc", 8, 0.13, 0.42, 0.0, 0.06, arc, phase=0.15,
         jitter=(1.0, 0.62, 1.1, 0.7, 0.9))
    cyl("Core", 0.085, 0.08, (0, 0, 0.06), core, verts=12)


def build_all():
    out = []
    jobs = (
        ("mermi_batarya.png", build_bullet_battery, 128),
        ("mermi_pusu.png", build_bullet_ambush, 128),
        ("namlu_batarya.png", build_muzzle_battery, 128),
        ("namlu_pusu.png", build_muzzle_ambush, 128),
        ("namlu_isin.png", build_muzzle_beam, 128),
        ("carpma_batarya.png", build_impact_battery, 256),
        ("carpma_pusu.png", build_impact_ambush, 256),
        ("carpma_isin.png", build_impact_beam, 256),
    )
    for name, fn, res in jobs:
        bpy.context.scene.render.resolution_x = res
        bpy.context.scene.render.resolution_y = res
        fn()
        out.append(render_to(name))
    return out
