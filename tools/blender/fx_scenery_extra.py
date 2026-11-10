"""
Frontline - fx / scenery ek gorseller (Blender).
Mevcut tools/blender/*.py ile AYNI rig, malzeme ve helper fonksiyonlari
kullanilir (setup_rig/mat/plate/cyl/box/star) - stil birebir tutarli olsun diye.

Uretilenler:
  fx_scenery_burst.png  - agac/kaya yikilma ani patlamasi (toz + enkaz), 256
  fx_focus_marker.png   - hedef kilitleme lock-on gostergesi, 128
  fx_range_ring.png     - menzil halkasi, dolu yari saydam disk, 512, gercek
                           radyal gradyan shader ile
  scenery_tree_large.png / scenery_rock_large.png - buyuk boy varyasyon, 512
"""

import bpy
import os
import math

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Frontline/Art/Fx"
OUT_SCENERY = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Frontline/Art/Base"
FINAL = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Sprites/Frontline"

METAL = (0.44, 0.45, 0.43, 1.0)
TREE_GREEN = (0.20, 0.42, 0.18, 1.0)
ROCK_GREY = (0.30, 0.31, 0.30, 1.0)
DUST = (0.42, 0.36, 0.28, 1.0)
DEBRIS_WOOD = (0.28, 0.20, 0.13, 1.0)
DEBRIS_ROCK = (0.35, 0.35, 0.33, 1.0)
LOCK_RED = (0.95, 0.18, 0.10, 1.0)


def setup_rig(res=512, ortho=1.0):
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)

    sc = bpy.context.scene
    if hasattr(sc, "compositing_node_group"):
        sc.compositing_node_group = None
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


def gradient_disc_mat(name, edge_rgb, core_rgb, edge_alpha=0.85, core_alpha=0.35):
    """Merkezden kenara gercek radyal gradyan: renk ACIK ortadan KOYU kenara,
    alfa de kenarda daha belirgin, ortada daha silik - 'menzil alani' hissi."""
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexCoord")
    grad = nt.nodes.new("ShaderNodeTexGradient")
    grad.gradient_type = "SPHERICAL"
    ramp_col = nt.nodes.new("ShaderNodeValToRGB")
    ramp_col.color_ramp.elements[0].position = 0.0
    ramp_col.color_ramp.elements[0].color = (core_rgb[0], core_rgb[1], core_rgb[2], 1.0)
    ramp_col.color_ramp.elements[1].position = 1.0
    ramp_col.color_ramp.elements[1].color = (edge_rgb[0], edge_rgb[1], edge_rgb[2], 1.0)
    ramp_a = nt.nodes.new("ShaderNodeValToRGB")
    ramp_a.color_ramp.elements[0].position = 0.0
    ramp_a.color_ramp.elements[0].color = (core_alpha, core_alpha, core_alpha, 1.0)
    e1 = ramp_a.color_ramp.elements.new(0.82)
    e1.color = (edge_alpha, edge_alpha, edge_alpha, 1.0)
    ramp_a.color_ramp.elements[2].position = 1.0
    ramp_a.color_ramp.elements[2].color = (0.0, 0.0, 0.0, 1.0)

    emit_n = nt.nodes.new("ShaderNodeEmission")
    transp = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")

    nt.links.new(tex.outputs["Generated"], grad.inputs["Vector"])
    nt.links.new(grad.outputs["Fac"], ramp_col.inputs["Fac"])
    nt.links.new(grad.outputs["Fac"], ramp_a.inputs["Fac"])
    nt.links.new(ramp_col.outputs["Color"], emit_n.inputs["Color"])
    emit_n.inputs["Strength"].default_value = 1.0
    nt.links.new(ramp_a.outputs["Color"], mix.inputs["Fac"])
    nt.links.new(transp.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit_n.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])

    m.blend_method = "BLEND"
    m.show_transparent_back = False
    return m


def clear():
    for o in list(bpy.data.objects):
        if o.type == "MESH":
            bpy.data.objects.remove(o, do_unlink=True)


def plate(name, pts, z, thick, material, bevel=0.012):
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


def sphere(name, r, loc, material, segs=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=segs, ring_count=rings)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
    return o


def rock_chunk(name, r, loc, material, subdiv=1):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=r, location=loc, subdivisions=subdiv)
    o = bpy.context.object
    o.name = name
    # hafif duzensizlik: gercek kaya siluetli olsun
    import random
    random.seed(hash(name) & 0xffff)
    me = o.data
    for v in me.vertices:
        f = 1.0 + random.uniform(-0.18, 0.18)
        v.co.x *= f
        v.co.y *= f
        v.co.z *= (1.0 + random.uniform(-0.12, 0.12))
    me.update()
    o.data.materials.append(material)
    return o


def box(name, loc, scale, material, bevel=0.012, rot=(0, 0, 0)):
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
    pts = []
    n = spikes * 2
    for i in range(n):
        a = phase + i * math.pi / spikes
        r = outer if i % 2 == 0 else inner
        if jitter:
            r *= jitter[i % len(jitter)]
        pts.append((r * math.cos(a), r * math.sin(a)))
    return plate(name, pts, z, thick, material, bevel=0.008)


def apply_outline(path, color=(0.14, 0.10, 0.08), width_px=3, harden=True):
    """Render bittikten sonra PNG'ye ince koyu kahve dis hat ekler: alfa
    kanalini N piksel genislet (kare-kutu dilate), orijinal alfanin disinda
    kalan halkayi koyu renkle doldurup orijinal render'i ustune bindir.
    Mevcut sette gorulen 'stroke' etkisi budur - 3D golgeden bagimsiz,
    her geometri turunde ayni kalinlikta kalir. Blender 5.x compositor'u
    node-group API'siyle kirilgan oldugu icin dogrudan numpy ile, kayitli
    PNG uzerinde yapiliyor.

    harden=True  -> opak nesneler (kule/agac/kaya): sinir 0.5 esikle
                     sertlestirilir, kenar anti-alias artigi kalmaz.
    harden=False -> yari saydam nesneler (menzil halkasi gibi): ic alfa
                     GRADYANI korunur, yalnizca disina ince bir cerceve
                     eklenir (obje varligini 0.02 esigiyle test eder)."""
    import numpy as np

    img = bpy.data.images.load(path)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    alpha = px[:, :, 3]
    thresh = 0.5 if harden else 0.02
    obj_mask0 = (alpha > thresh).astype(np.float32)

    dilated = obj_mask0.copy()
    for _ in range(width_px):
        shifted = dilated.copy()
        shifted[1:, :] = np.maximum(shifted[1:, :], dilated[:-1, :])
        shifted[:-1, :] = np.maximum(shifted[:-1, :], dilated[1:, :])
        shifted[:, 1:] = np.maximum(shifted[:, 1:], dilated[:, :-1])
        shifted[:, :-1] = np.maximum(shifted[:, :-1], dilated[:, 1:])
        dilated = shifted

    obj_mask = alpha > thresh
    ring_mask = (dilated > 0.5) & (~obj_mask)

    out = px.copy()
    if harden:
        # Sinir netligi: orijinal alfa esikle SERTLESTIRILIR (anti-alias
        # ara degerleri kaldirilir), yoksa kenardaki yari-saydam pikseller
        # hem 'nesne' hem 'halka' sayilip halka rengini orijinal rengin
        # ustune yanlislikla ezebiliyordu.
        out[:, :, 3] = 0.0
        out[obj_mask] = px[obj_mask]
        out[obj_mask, 3] = 1.0
    # harden=False: out zaten px kopyasi, ic gradyan alfa dokunulmadan kalir.
    out[ring_mask, 0] = color[0]
    out[ring_mask, 1] = color[1]
    out[ring_mask, 2] = color[2]
    out[ring_mask, 3] = 1.0

    img.pixels[:] = out.reshape(-1).tolist()
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    return path


def render_to(folder, name, outline=True, outline_color=(0.14, 0.10, 0.08),
              outline_px=None, outline_harden=True):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, name)
    res = bpy.context.scene.render.resolution_x
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    if outline:
        wpx = outline_px if outline_px is not None else max(1, round(res / 160))
        apply_outline(path, outline_color, wpx, harden=outline_harden)
    return path


# ---------------------------------------------------------------- A: burst

def build_scenery_burst():
    """Ortu (agac/kaya) yok olma ani: merkezde toz bulutu + firlayan enkaz
    parcalari (ahsap kahve + tas gri karisik, cunku hem agac hem kaya icin
    kullanilacak tek gorsel). Radyal simetrik, tek kare - Unity tarafinda
    olcek+sonme ile animasyon yapilacak (fx.py'deki carpma mantigiyla ayni)."""
    clear()
    smoke = mat("Fx_Smoke", DUST, rough=0.9)
    smoke2 = mat("Fx_Smoke2", (0.52, 0.46, 0.38, 1.0), rough=0.85)
    wood = mat("Fx_Wood", DEBRIS_WOOD, rough=0.6)
    rockc = mat("Fx_RockChip", DEBRIS_ROCK, rough=0.5)

    star("SmokeOuter", 10, 0.28, 0.46, -0.03, 0.05, smoke, phase=0.25,
         jitter=(1.0, 0.82, 1.1, 0.9, 1.05))
    star("SmokeInner", 8, 0.18, 0.34, 0.01, 0.06, smoke2, phase=0.6,
         jitter=(1.0, 0.88, 1.06))

    import random
    random.seed(7)
    for i in range(6):
        a = i * (2 * math.pi / 6) + 0.3
        d = 0.30 + random.uniform(-0.03, 0.05)
        loc = (d * math.cos(a), d * math.sin(a), 0.05 + random.uniform(0.0, 0.03))
        m = wood if i % 2 == 0 else rockc
        rock_chunk("Debris%d" % i, 0.045 + random.uniform(0.0, 0.02), loc, m, subdiv=1)


# ------------------------------------------------------------- B: lock-on

def build_focus_marker():
    """Askeri lock-on: kirmizi capraz + 4 kose parantezi, merkez BOS (altindaki
    hedef gorunsun diye). Kirmizi/amber - 'bu hedeflendi' tehdidi tasir."""
    clear()
    red = mat("Fx_Lock", LOCK_RED, rough=0.3, emit=0.55)
    amber = mat("Fx_LockAmber", (1.0, 0.55, 0.15, 1.0), rough=0.3, emit=0.45)

    # kose parantezleri (L seklinde 4 kose)
    arm = 0.14
    thick_w = 0.045
    gap = 0.34  # merkezden koseye bosluk
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * gap, sy * gap
            plate("Corner_h_%d_%d" % (sx, sy),
                  [(cx, cy), (cx - sx * arm, cy), (cx - sx * arm, cy - sy * thick_w), (cx, cy - sy * thick_w)],
                  0, 0.05, red, bevel=0.006)
            plate("Corner_v_%d_%d" % (sx, sy),
                  [(cx, cy), (cx, cy - sy * arm), (cx - sx * thick_w, cy - sy * arm), (cx - sx * thick_w, cy)],
                  0, 0.05, red, bevel=0.006)

    # kisa ic tikler: kenar ortalarindan merkeze dogru, ama MERKEZE degmiyor -
    # capraz gibi ortadan gecip hedefi kapatmasin, sadece 'kilitlendi' isareti
    for ang in (0.0, math.pi / 2, math.pi, 3 * math.pi / 2):
        w = 0.028
        r0, r1 = 0.155, 0.24
        pts = [(r0, -w), (r1, -w), (r1, w), (r0, w)]
        cosr, sinr = math.cos(ang), math.sin(ang)
        rp = [(x * cosr - y * sinr, x * sinr + y * cosr) for (x, y) in pts]
        plate("Tick_%s" % ang, rp, 0, 0.045, amber, bevel=0.004)

    # ic bosluk halkasi: hedefin gorunecegi delik cevresinde ince cerceve.
    # ic yaricap yeterince buyuk tutulur ki merkez tamamen bos kalsin.
    star("InnerRing", 20, 0.12, 0.145, 0.01, 0.03, amber, phase=0.0)


# ---------------------------------------------------------- C: range ring

def build_range_ring():
    """Dolu yari saydam menzil diski: gercek shader-tabanli radyal gradyan
    (ortadan kenara), kenar biraz daha koyu/belirgin - PrimitiveSprites
    placeholder disc yerine tasarlanmis versiyon."""
    clear()
    gm = gradient_disc_mat(
        "Fx_RangeRing",
        edge_rgb=(0.30, 0.85, 0.55),
        core_rgb=(0.55, 0.95, 0.75),
        edge_alpha=0.9,
        core_alpha=0.22,
    )
    bpy.ops.mesh.primitive_circle_add(radius=0.47, vertices=64, fill_type="NGON",
                                       location=(0, 0, 0))
    o = bpy.context.object
    o.name = "RangeDisc"
    o.data.materials.append(gm)

    # UV: TexCoord Generated kullanildi, ekstra UV gerekmez ama guvenli olsun
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.uv.smart_project()
    bpy.ops.object.mode_set(mode="OBJECT")
    # NOT: kenara ayrica star() tabanli bir 'cerceve' EKLENMEDI - star()
    # ic/dis yaricap arasinda zikzak/disli bir seklidir (fx.py'de patlama
    # icin bilerek kullanilir), duzgun bir daire kenari icin uygun degil.
    # Kenardaki 'daha koyu/net' hissi zaten gradient_disc_mat'in alfa
    # rampasindan geliyor (0.82 -> 1.0 arasi netlesme).


# ------------------------------------------------------ D: buyuk agac/kaya

def build_tree_large():
    """Mevcut scenery_tree.png ile ayni aile (kume halinde yumru kureler,
    disk govde), ~%40 daha buyuk siluet + bir ekstra kume katmani (daha
    fazla detay/golge - buyuklugu vurgulasin)."""
    clear()
    green = mat("Tree_Green", TREE_GREEN, rough=0.55)
    green_dark = mat("Tree_GreenDark", (0.14, 0.30, 0.13, 1.0), rough=0.6)

    # merkez govde
    sphere("Core", 0.30, (0, 0, 0.16), green, segs=14, rings=10)
    # ana yaprak kumeleri (6 petal, buyuk versiyonda daha genis yaricap)
    n = 7
    for i in range(n):
        a = i * (2 * math.pi / n)
        r = 0.30
        loc = (r * math.cos(a), r * math.sin(a), 0.14)
        sphere("Petal%d" % i, 0.20, loc, green, segs=10, rings=8)
    # ekstra ust katman: buyuklugu ve hacmi vurgulayan ikinci, daha kucuk
    # ve daha koyu kume seti - golge/detay zenginligi
    for i in range(n):
        a = i * (2 * math.pi / n) + (math.pi / n)
        r = 0.16
        loc = (r * math.cos(a), r * math.sin(a), 0.30)
        sphere("PetalTop%d" % i, 0.13, loc, green_dark, segs=8, rings=6)


def build_rock_large():
    """Mevcut scenery_rock.png ile ayni aile (dusuk poli, koseli kume),
    ~%40 daha buyuk + ikinci, kucuk yardimci kaya parcasi (buyuk kaya
    yiginlarinda goruldugu gibi - detay katmani)."""
    clear()
    grey = mat("Rock_Grey", ROCK_GREY, rough=0.75)
    grey_dark = mat("Rock_GreyDark", (0.20, 0.20, 0.19, 1.0), rough=0.8)

    rock_chunk("Main", 0.34, (0.02, -0.02, 0.18), grey, subdiv=2)
    rock_chunk("Side", 0.20, (-0.24, 0.14, 0.10), grey_dark, subdiv=1)
    rock_chunk("Small", 0.13, (0.22, 0.20, 0.06), grey_dark, subdiv=1)


def build_all():
    out = []

    setup_rig(res=256, ortho=1.0)
    build_scenery_burst()
    out.append(render_to(OUT, "fx_scenery_burst.png"))

    setup_rig(res=128, ortho=1.0)
    build_focus_marker()
    out.append(render_to(OUT, "fx_focus_marker.png"))

    setup_rig(res=512, ortho=1.0)
    build_range_ring()
    out.append(render_to(OUT, "fx_range_ring.png",
                          outline_color=(0.10, 0.28, 0.19), outline_px=2,
                          outline_harden=False))

    setup_rig(res=512, ortho=1.0)
    build_tree_large()
    out.append(render_to(OUT_SCENERY, "scenery_tree_large.png"))

    setup_rig(res=512, ortho=1.0)
    build_rock_large()
    out.append(render_to(OUT_SCENERY, "scenery_rock_large.png"))

    return out
