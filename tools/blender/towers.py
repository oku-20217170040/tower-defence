"""
Frontline — kule sprite ureteci (Blender).

Sanat yonu: sade askeri siluet, dusuk poli (21 Agu 2026 karari).

GORSEL DIL KURALI (spec 9.1) — bu dosyanin asil isi:
Ordunun GOREBILDIGI kule ile GOREMEDIGI kule bakista ayrilmali.

  batarya      -> tehdit YAZAR. Tek "gurultulu" kule: yuksek, dik namlu,
                  agresif siluet. Titresim halkasi da yalnizca onda
                  (halkayi kod ciziyor, ThreatRingView).
  pusu mevzisi -> tehdit YAZMAZ. Alcak, yassi, gomulu. Asil katil odur
                  ama gorunmemeli - oyunun tersine cevrilmis sezgisi bu
                  (spec 3.2).
  mayin tarlasi-> tehdit YAZMAZ. Yerde dagilmis kucuk tumsekler; dikey
                  yapi YOK.
  yem          -> hasar vermez, suruyu ceker. Yayin yapan bir fener:
                  ince govde + tepede parlak baslik.
  isin         -> periyodik YATAY hat. Govde X ekseninde uzun; iki ucta
                  mercek. Yonu mekanigini anlatir.

Yukseklik bu yuzden bir SUS degil, bilgi: yuksek kule gorunur, yassi
kule gizli.

Kullanim:
    exec(open(r"<repo>/tools/blender/towers.py").read())
    setup_rig(); build_all()
"""

import bpy
import os

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Frontline/Art/Towers"

# Birliklerle ayni govde ailesi.
# 0,20 denendi ve KOYU KALDI: haritanin zemini de koyu oldugu icin kule
# gomuluyordu, ustelik emisyonlu parcalarin yanindaki govde neredeyse
# siyah cikiyordu. Birliklerin govdesiyle ayni banda cekildi.
BODY = (0.30, 0.32, 0.29, 1.0)
METAL = (0.44, 0.45, 0.43, 1.0)


def setup_rig(res=512, ortho=1.0):
    for o in list(bpy.data.objects):
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
        if o.type == 'MESH':
            bpy.data.objects.remove(o, do_unlink=True)


def plate(name, pts, z, thick, material, bevel=0.02):
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


def ngon(name, sides, radius, z, thick, material, bevel=0.02, rot=0.0):
    import math
    pts = [(radius * math.cos(rot + i * 2 * math.pi / sides),
            radius * math.sin(rot + i * 2 * math.pi / sides)) for i in range(sides)]
    return plate(name, pts, z, thick, material, bevel)


def cyl(name, r, depth, loc, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=loc,
                                        rotation=rot, vertices=16)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
    bpy.ops.object.shade_auto_smooth() if hasattr(bpy.ops.object, "shade_auto_smooth") else None
    return o


def box(name, loc, scale, material, bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    bm = o.modifiers.new("bev", 'BEVEL')
    bm.width, bm.segments = bevel, 2
    o.data.materials.append(material)
    return o


def render_to(name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


# ---------------------------------------------------------------- kuleler

def build_battery():
    """GURULTULU. Yuksek, dik namlu, genis taban. Ordu onu gorur ve kacar."""
    clear()
    body = mat("T_Body", BODY)
    metal = mat("T_Metal", METAL)
    # Vurgu rengi: render doygunlugu emiyordu, hafif emisyon hue'yu tutar.
    acc = mat("T_Acc_bat", (1.0, 0.45, 0.12, 1.0), rough=0.3, emit=0.25)

    ngon("Base", 6, 0.50, 0.0, 0.12, body, 0.03)
    ngon("Ring", 6, 0.36, 0.12, 0.10, metal, 0.02, rot=0.5)
    cyl("Turret", 0.20, 0.28, (0, 0, 0.31), acc)

    # TEK namlu, one (-Y) bakar ve taretin kenarindan cikar.
    # Iki deneme elendi: dikey namlu tepeden "delik" gibi okunuyordu,
    # karsilikli iki namlu ise govdeyi delip gecen bir cubuga donusuyordu.
    cyl("Barrel", 0.06, 0.26, (0.0, -0.30, 0.34), metal, rot=(1.5708, 0, 0))
    box("Muzzle", (0.0, -0.42, 0.34), (0.13, 0.07, 0.10), metal, 0.015)

    # omuz plakalari: siluete agirlik katar
    for sx in (-1, 1):
        box("Shoulder%d" % sx, (sx * 0.34, 0.0, 0.20), (0.16, 0.30, 0.12), metal, 0.02)


def build_ambush():
    """SESSIZ. Alcak, yassi, gomulu. Asil katil ama gorunmemeli."""
    clear()
    body = mat("T_Body", BODY)
    acc = mat("T_Acc_amb", (0.45, 0.52, 0.58, 1.0), rough=0.7)

    # genis ve COK alcak plaka: yukseklik yok = gorunurluk yok
    ngon("Pad", 8, 0.44, 0.0, 0.05, body, 0.02)
    # ortada dar bir yarik: namlu degil, mazgal
    box("Slit", (0, 0, 0.06), (0.30, 0.07, 0.04), acc, 0.01)
    # kamuflaj kenarlari
    for sx in (-1, 1):
        box("Flap%d" % sx, (sx * 0.30, 0, 0.04), (0.16, 0.30, 0.03), body, 0.01)


def build_decoy():
    """Yayin yapan fener: ince govde, tepede parlak baslik."""
    clear()
    body = mat("T_Body", BODY)
    metal = mat("T_Metal", METAL)
    acc = mat("T_Acc_dec", (0.22, 0.85, 0.42, 1.0), rough=0.35, emit=0.35)

    ngon("Base", 6, 0.36, 0.0, 0.10, body, 0.025)
    cyl("Mast", 0.09, 0.34, (0, 0, 0.26), metal)
    cyl("Emitter", 0.19, 0.14, (0, 0, 0.48), acc)
    # yayin kollari: cekim yaydigini anlatir
    for i, a in enumerate((0.0, 1.05, 2.09, 3.14, 4.19, 5.24)):
        import math
        box("Arm%d" % i, (0.26 * math.cos(a), 0.26 * math.sin(a), 0.44),
            (0.14, 0.035, 0.03), acc, 0.008)


def build_minefield():
    """Yerde dagilmis tumsekler. DIKEY YAPI YOK -- ordu onu algilayamaz."""
    clear()
    body = mat("T_Body", BODY)
    acc = mat("T_Acc_min", (0.95, 0.78, 0.25, 1.0), rough=0.45)

    ngon("Field", 4, 0.46, 0.0, 0.035, body, 0.02, rot=0.785)
    spots = [(-0.20, 0.18), (0.16, 0.22), (0.24, -0.10),
             (-0.06, -0.02), (-0.24, -0.18), (0.04, -0.26)]
    for i, (x, y) in enumerate(spots):
        cyl("Mine%d" % i, 0.065, 0.06, (x, y, 0.05), acc)


def build_beam():
    """YATAY hat. Govde X ekseninde uzun; iki ucta mercek."""
    clear()
    body = mat("T_Body", BODY)
    metal = mat("T_Metal", METAL)
    acc = mat("T_Acc_beam", (0.30, 0.70, 0.92, 1.0), rough=0.3, emit=0.35)

    ngon("Base", 6, 0.34, 0.0, 0.10, body, 0.025)
    # uzun yatay govde: mekanigi (satir boyunca ates) siluet anlatir
    # Ray ve mercekler kadraj icinde kalmali: disari tasinca sprite'in
    # gorunur genisligi kareyi asiyor ve oyunda komsu hucreye giriyor.
    box("Rail", (0, 0, 0.20), (0.62, 0.15, 0.13), metal, 0.02)
    for sx in (-1, 1):
        cyl("Lens%d" % sx, 0.075, 0.08, (sx * 0.30, 0, 0.20), acc,
            rot=(0, 1.5708, 0))
    box("Core", (0, 0, 0.28), (0.26, 0.09, 0.06), acc, 0.012)


def build_all():
    out = []
    for name, fn in (("batarya", build_battery),
                     ("pusu", build_ambush),
                     ("yem", build_decoy),
                     ("mayin_tarlasi", build_minefield),
                     ("isin", build_beam)):
        fn()
        out.append(render_to(name + ".png"))
    return out
