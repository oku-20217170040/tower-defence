"""
Frontline — kule sprite ureteci (Blender).

Sanat yonu: sade askeri siluet, dusuk poli (21 Agu 2026 karari).

GORSEL DIL KURALI (spec 9.1) — bu dosyanin asil isi:
Ordunun GOREBILDIGI kule ile GOREMEDIGI kule bakista ayrilmali.

  batarya      -> tehdit YAZAR. Tek "gurultulu" kule: yuksek, dik namlu,
                  agresif siluet. Titresim halkasi da yalnizca onda
                  (halkayi kod ciziyor, ThreatRingView).
  pusu mevzisi -> tehdit YAZMAZ. Alcak ve yassi govde, ama ONE BAKAN
                  ince bir namlu var (23 Agu kullanici karari): donen
                  kulenin neye nisan aldigi ancak namludan okunur.
                  Gizlilik YUKSEKLIKTE korunur - govde hala yerde.
  mayin tarlasi-> tehdit YAZMAZ. Yerde dagilmis kucuk tumsekler; dikey
                  yapi YOK.
  yem          -> hasar vermez, suruyu ceker. Yayin yapan bir fener:
                  ince govde + tepede parlak baslik.
  isin         -> periyodik delici hat. Yayici NISAN YONUNDE (-Y): uc
                  uca iki mercek + one bakan agiz (23 Agu). Once X
                  ekseninde yatay raydi; kule donunce nereye ates ettigi
                  okunmuyordu ve hat rayin ortasindan cikip onu
                  yariyordu.

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
    """SESSIZ ama artik GERCEK NAMLULU. Alcak govde + one bakan top.

    22 Agustos'ta mazgaldi: govdenin ortasinda dar bir yarik. Kullanici
    begenmedi ve hakliydi - donen bir kulenin neye nisan aldigi ancak
    NAMLUDAN okunur, mazgal her acidan ayni goruniyordu.

    Gizlilik siluetin YUKSEKLIGINDE korunur, namlunun yoklugunda degil:
    govde hala cok alcak ve dar, namlu ise yere yakin. Bataryanin yuksek
    taretiyle yan yana konunca fark bakista okunur - kural (spec 9.1)
    bozulmadi, tasindi.

    Namlu, silueti tasiyacak kadar KALIN olmali: ilk denemede 0,042
    yaricapli bir cubuktu ve 30 pikselde kayboluyordu. Govde de
    kucultuldu, yoksa namlu plakanin icinde kalip disari cikmiyordu.
    Ucu y = -0,48.
    """
    clear()
    body = mat("T_Body", BODY)
    metal = mat("T_Metal", METAL)
    acc = mat("T_Acc_amb", (0.45, 0.52, 0.58, 1.0), rough=0.7)

    # dar ve COK alcak plaka: yukseklik yok = gorunurluk yok
    ngon("Pad", 8, 0.32, 0.0, 0.05, body, 0.02)
    # kama (breech): namlunun arkasi, siluete agirlik ve yon verir
    box("Breech", (0.0, 0.05, 0.09), (0.26, 0.24, 0.08), body, 0.015)
    box("Hatch", (0.0, 0.14, 0.14), (0.14, 0.10, 0.03), metal, 0.01)

    # GERCEK NAMLU: -Y'ye bakar, plakanin disina TASAR. Bataryanin
    # kalin topundan ince olmasi tur farkini tasir.
    box("Barrel", (0.0, -0.20, 0.10), (0.10, 0.44, 0.09), metal, 0.015)
    box("Muzzle", (0.0, -0.44, 0.10), (0.15, 0.09, 0.10), acc, 0.012)

    # kamuflaj kenarlari: govde hala zemine yayilir, yukselmez
    for sx in (-1, 1):
        box("Flap%d" % sx, (sx * 0.26, 0.06, 0.03), (0.16, 0.22, 0.03), body, 0.01)


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
    """ONE BAKAN YAYICI. Ray -Y'ye cevrildi; hat namlunun ucundan cikar.

    Ilk surum X ekseninde yatay bir raydi ve "satir boyunca ates"
    fikrini anlatiyordu. Ama kule DONUYOR: yatay ray dondugunde nereye
    ates ettigini soylemiyor, ustelik hat govdenin ortasindan cikip rayi
    yariyordu (23 Agu kullanici karari).

    Ikinci deneme de elendi: arkadaki sarj halkalari silindirdi ve
    tepeden bakinca kulenin yarisini kaplayan mavi bir leke oluyordu.
    Vurgu rengi bu modelde INCE SERITLERE dagitilir - mavi, govdeyi
    ezmeden nereye bakildigini soyleyecek kadar olmali.

    Mekanigin siluetten okunmasi kaybolmadi, yer degistirdi: uzun ve
    dar govde hala "bu bir hat silahi" der, artik NEREYE dogru oldugunu
    da soyler. Agiz ucu y = -0,49.
    """
    clear()
    body = mat("T_Body", BODY)
    metal = mat("T_Metal", METAL)
    acc = mat("T_Acc_beam", (0.30, 0.70, 0.92, 1.0), rough=0.3, emit=0.35)

    ngon("Base", 6, 0.30, 0.0, 0.09, body, 0.025)

    # sarj govdesi: agir arka, "birikip bosalma" hissi
    box("Coil", (0.0, 0.20, 0.15), (0.28, 0.22, 0.13), body, 0.02)
    for sy in (0.13, 0.21, 0.29):
        box("Coil%.2f" % sy, (0.0, sy, 0.23), (0.30, 0.035, 0.03), acc, 0.008)

    # ILERI bakan ray: -Y'de uzun, namlunun govdesi
    box("Rail", (0.0, -0.15, 0.17), (0.17, 0.50, 0.12), metal, 0.02)
    # hizalanmis mercek seritleri: hat bunlarin ekseninden cikar
    for sy in (-0.08, -0.24):
        box("Lens%.2f" % sy, (0.0, sy, 0.24), (0.21, 0.05, 0.04), acc, 0.01)
    # agiz: en ondeki dudak, namlu ucunu isaret eder
    box("Muzzle", (0.0, -0.44, 0.17), (0.13, 0.10, 0.13), acc, 0.012)


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
