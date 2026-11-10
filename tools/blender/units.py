"""
Frontline — birlik sprite ureteci (Blender).

Sanat yonu: sade askeri siluet, dusuk poli (21 Agu 2026 karari).

TASARIM KURALI: telefonda bir birlik ~30 piksel. O olcekte okunan sey
anatomi degil, DIS HATTIR. Bu yuzden govde yon belirten bir KAMA:
burun -Y'de, yani ekranda asagi -- birliklerin yuruyus yonu.

Tur ayrimi UC kanaldan birden gelir:
  1. siluet (dar/genis, anten, tac)
  2. vurgu rengi (kask)
  3. olcek (Unity tarafinda: izci 0,7 · asker 1,35 · kralice 1,8)
Uc kanal da ayni anda calisir; renk korlugunde bile tur ayirt edilir.

Kullanim (Blender Python konsolu ya da MCP):
    exec(open(r"<repo>/tools/blender/units.py").read())
    setup_rig(); build_all()
"""

import bpy
import os

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Sprites/Frontline"

# Haritanin zemini KOYU. Govde de koyu olursa birlik kaybolur;
# orta tonlu asker yesili secildi.
BODY = (0.24, 0.26, 0.22, 1.0)


def setup_rig():
    """Ortografik tepeden kamera + seffaf film + sert tepe isigi."""
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)

    sc = bpy.context.scene
    engines = [e.identifier for e in
               bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = ('BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines
                        else 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in engines else 'CYCLES')
    sc.render.film_transparent = True
    sc.render.resolution_x = 512
    sc.render.resolution_y = 512
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    # Filmic/AgX renkleri soldururdu; sprite'ta renk oldugu gibi kalmali.
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'

    cam_data = bpy.data.cameras.new("TopCam")
    cam_data.type = 'ORTHO'          # izgara oyununda perspektif hizayi bozar
    # 3 Eylul 2026: TowerVariety seti ile ayni dolgunluk hedefi (~0.85
    # alfa-bbox fill) icin kamera yaklastirildi (1.0 -> 0.86). Kule setinde
    # oldugu gibi ayni res (512) ve ayni isik rigi kullaniliyor - tek fark
    # kalan sey artik yalnizca siluet, dolgunluk degil.
    cam_data.ortho_scale = 0.86
    cam = bpy.data.objects.new("TopCam", cam_data)
    cam.location = (0, 0, 10)
    cam.rotation_euler = (0, 0, 0)   # -Z'ye bakar; dunya +Y = ekranda yukari
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


def mat(name, rgba, rough=0.6):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = rgba
    b.inputs["Roughness"].default_value = rough
    return m


def clear_units():
    for o in list(bpy.data.objects):
        if o.type == 'MESH':
            bpy.data.objects.remove(o, do_unlink=True)


def plate(name, pts, z, thick, material, bevel=0.02):
    """Tepeden verilen cokgeni kati bir plakaya cevirir. Siluet buradan gelir."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x, y, z) for (x, y) in pts], [], [list(range(len(pts)))])
    me.update()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    sm = o.modifiers.new("sol", 'SOLIDIFY')
    sm.thickness, sm.offset = thick, 0
    bm = o.modifiers.new("bev", 'BEVEL')
    bm.width, bm.segments = bevel, 2
    o.data.materials.append(material)
    return o


def dome(name, loc, r, flat, material):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=18, ring_count=9)
    o = bpy.context.object
    o.name = name
    o.scale = (1, 1, flat)
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.object.shade_smooth()
    o.data.materials.append(material)
    return o


def build(accent, w=1.0, l=1.0, crown=False, antenna=False, pauldrons=True):
    """Yon belirten kama silueti; burun -Y'de (ekranda asagi = yuruyus yonu)."""
    clear_units()
    body_m = mat("Body", BODY)
    acc_m = mat("Acc_%s" % str(accent[:3]), accent, rough=0.42)

    plate("Body", [
        (0.00, -0.62 * l),          # burun
        (0.34 * w, -0.18 * l),
        (0.30 * w, 0.30 * l),
        (0.00, 0.44 * l),           # arka kertik: yon asimetrisi
        (-0.30 * w, 0.30 * l),
        (-0.34 * w, -0.18 * l),
    ], 0.0, 0.16, body_m, 0.025)

    if pauldrons:
        for sx in (-1, 1):
            plate("Pauldron%d" % sx, [
                (sx * 0.30 * w, -0.10 * l), (sx * 0.50 * w, -0.02 * l),
                (sx * 0.48 * w, 0.20 * l), (sx * 0.28 * w, 0.22 * l),
            ], 0.02, 0.13, body_m, 0.02)

    # Kask: TEK vurgu noktasi. Tur rengi burada okunur.
    dome("Helmet", (0, 0.02 * l, 0.14), 0.17 * w, 0.55, acc_m)

    if antenna:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.022, depth=0.42,
                                            location=(0.16 * w, 0.22 * l, 0.30))
        bpy.context.object.name = "Antenna"
        bpy.context.object.data.materials.append(acc_m)

    if crown:
        # Tac govdenin ICINDE kalir: disari tasarsa kadraj kesiyor ve
        # sprite'in merkezi kayiyor (bir kez yasandi).
        for sx, off in ((-1, 0.0), (1, 0.0), (0, 1.0)):
            plate("Spike_%s_%s" % (sx, off), [
                (sx * 0.22 * w - 0.045, 0.16 * l + off * 0.06),
                (sx * 0.22 * w + 0.045, 0.16 * l + off * 0.06),
                (sx * 0.22 * w + 0.028, 0.34 * l + off * 0.08),
                (sx * 0.22 * w - 0.028, 0.34 * l + off * 0.08),
            ], 0.10, 0.10, acc_m, 0.012)


def render_to(name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


def build_all():
    """Yedi birlik (Frontline.Core.UnitType). Dosya adlari kod tarafinin
    ArtLibrary/FrontlineSandboxSceneBuilder icinde aradigi 'u_<tur>' adlariyla
    BIREBIR eslesir - degistirilirse sprite'lar sessizce daireye duser."""
    out = []

    # Infantry: taban. Dengeli kama, bej kask.
    build((0.78, 0.72, 0.54, 1.0), w=1.00, l=1.00)
    out.append(render_to("u_infantry.png"))

    # Scout: dar ve uzun, anten var, omuzluk YOK -> hizli ve kirilgan.
    build((0.38, 0.78, 0.93, 1.0), w=0.78, l=1.08, antenna=True, pauldrons=False)
    out.append(render_to("u_scout.png"))

    # Soldier: genis ve kalin omuzluklu -> agir, durdurulmasi zor.
    build((0.80, 0.26, 0.22, 1.0), w=1.30, l=0.92)
    out.append(render_to("u_soldier.png"))

    # Blind: kesif izini/yemi gormez - soguk gri, omuzluksuz, antensiz;
    # "korlugu" duz ve sade siluetle anlatir, digerlerinden vurgu rengiyle ayrisir.
    build((0.58, 0.60, 0.62, 1.0), w=0.96, l=1.00, pauldrons=False)
    out.append(render_to("u_blind.png"))

    # Brood: olunce ikiye ayrilir - turuncu/amber vurgu, orta govde,
    # kucuk omuzluk (yavrulari tasiyan "sisirilmis" gorunum).
    build((0.90, 0.55, 0.18, 1.0), w=1.05, l=0.98)
    out.append(render_to("u_brood.png"))

    # Queen: en iri, tacli -> ayriksi siluet.
    build((0.68, 0.36, 0.88, 1.0), w=1.18, l=1.12, crown=True)
    out.append(render_to("u_queen.png"))

    # Flyer: kucuk ve hafif - dar govde, anten (radar/kanat hissi), acik
    # mavi-beyaz vurgu; UnitRoster'da en gec giren en seyrek turdur.
    build((0.75, 0.90, 0.98, 1.0), w=0.66, l=0.80, antenna=True, pauldrons=False)
    out.append(render_to("u_flyer.png"))

    return out
