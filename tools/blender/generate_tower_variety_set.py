"""
Frontline - kule cesitliligi seti (Blender).

Kaynak: 2026-09-03 referans oyun (Mobirix "Tower Defense: War Tactics")
GUNCELLE (kalici yukseltme) ekraninin taranmasiyla dogrulanan 5 resmi
kule tipi + 3 kademeli gorsel yukseltme dili. Bkz.
docs/notes/2026-09-03-referans-oyun-tam-analiz.md

BU BIREBIR KOPYA DEGIL - ayni TASARIM DILI, kendi siluetlerimiz:
referans oyunun kule ROLLERINI (hizli-ates/agir-top/keskin-nisanci/
radar-destek/yavaslatma-destek) ve 3-kademeli yukseltme fikrini alip
mevcut Frontline stiline (towers.py/base.py ile ayni rig, malzeme,
helper fonksiyonlari - duz renk + kontur, dusuk poli) uyguluyoruz.

Okunabilirlik kurali (towers.py'deki spec 9.1 ile tutarli):
  - Saldiri kuleleri NAMLULU (govdeden -Y'ye tasan bir cubuk/kutu var).
  - Destek kuleleri (radar, zaman-bozulmasi) NAMLUSUZ - donen disk/
    kristal govde. Oyuncu saldiri kulesini destek kuleden silahtan
    once SILUETTEN ayirt edebilmeli.
  - Seviye 1->2->3: govde ayni kalir, her seviyede KUCUK bir detay
    eklenir (yan plaka / ikinci parca) + tepede seviye sayisi kadar
    yildiz rozeti + vurgu rengi biraz daha parlak (emit artar). Renk
    ailesi degismez - referans oyunda oldugu gibi "ayni kule, daha
    parlak/donanimli" hissi, yeni bir kule degil.

Kullanim:
    exec(open(r"<repo>/tools/blender/generate_tower_variety_set.py").read())
    setup_rig(); build_all()
"""

import bpy
import os
import math

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Frontline/Art/TowerVariety"

BODY = (0.30, 0.32, 0.29, 1.0)
METAL = (0.44, 0.45, 0.43, 1.0)
STAR_GOLD = (0.95, 0.78, 0.25, 1.0)

# --------------------------------------------------------------------
# 4 Eylul gunduz - piksel-olculu referans yeniden tasarim
# (com.mobirix.tdwt Makineli Tufek kulesi, ust-goruntum zoom crop).
# Telefon ekranindan dogrudan piksel renk ornegi alindi:
#   gunmetal palet/tekerlek blok  ~ #2D3339
#   bronz govde ust (parlak)      ~ #9B6742
#   bronz govde alt (golge)       ~ #845131
#   merkez lens/nisangah (koyu)   ~ #313131
#   gumus namlu                   ~ #C7CBD0
#   namlu ucu sicak highlight     ~ #E3C8B5
# Yapisal dil: SOL/SAG'da (barrel -Y'ye baktigi icin +X/-X eksenlerinde)
# gunmetal palet-tekerlek bloklari, merkezde bronz/bakir silindirik govde
# (iki katmanli - ust parlak / alt golgeli), govde ustunde koyu daire
# (lens), namlu govdeden -Y'ye uzanir. TUM 5 kule tipi bu ortak "govde
# dili"ni paylasir; namlu kalinligi/uzunlugu ve namlusuz destek
# versiyonlari (radar/zaman-bozulmasi) role gore degisir. Yildiz rozetesi
# GOVDEYE GOMULMEDI - TowerStars.cs zaten ayri bir yildiz sirasi
# bindiriyor (bkz. Assets/Frontline/Runtime/View/TowerStars.cs Build/
# SetLevel), sprite icine ikinci bir yildiz koymak cift-gorunume yol
# acardi.
GUNMETAL = (0.176, 0.200, 0.224, 1.0)
BRONZE_TOP = (0.608, 0.404, 0.259, 1.0)
BRONZE_BASE = (0.518, 0.318, 0.192, 1.0)
LENS_DARK = (0.192, 0.192, 0.192, 1.0)
SILVER = (0.780, 0.796, 0.816, 1.0)
MUZZLE_HL = (0.890, 0.784, 0.710, 1.0)


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
    pts = [(radius * math.cos(rot + i * 2 * math.pi / sides),
            radius * math.sin(rot + i * 2 * math.pi / sides)) for i in range(sides)]
    return plate(name, pts, z, thick, material, bevel)


def cyl(name, r, depth, loc, material, rot=(0, 0, 0), verts=16):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=loc,
                                        rotation=rot, vertices=verts)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
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


def cone(name, r, depth, loc, material, verts=6):
    bpy.ops.mesh.primitive_cone_add(radius1=r, radius2=0.0, depth=depth,
                                    location=loc, vertices=verts)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
    return o


def hull(track_w=0.13, track_l=0.30, track_gap=0.20, track_z=0.12,
         body_r=0.16, body_h_bot=0.10, body_h_top=0.10, body_z0=0.0,
         lens_r=0.055, lens_z=None):
    """Ortak govde dili: SOL/SAG gunmetal palet-tekerlek bloklari +
    merkez iki-katmanli bronz silindir (alt golge / ust parlak) + ust
    yuzeyde koyu lens dairesi. Barrel/role-ozel parcalar cagiran
    build_* fonksiyonlari bunun UZERINE ekler (barrel -Y yonune gider).
    """
    gm = mat("Hull_Gunmetal", GUNMETAL, rough=0.5)
    b_top = mat("Hull_BronzeTop", BRONZE_TOP, rough=0.35)
    b_bot = mat("Hull_BronzeBase", BRONZE_BASE, rough=0.45)
    lens = mat("Hull_Lens", LENS_DARK, rough=0.2)

    for sx in (-1, 1):
        box("Track%d" % sx, (sx * track_gap, 0.0, track_z),
            (track_w, track_l, track_z * 2), gm, 0.02)

    # ust-goruntumden BodyBase gorunsun diye BodyTop belirgin KUCUK
    # yaricapla oturtuluyor - referanstaki koyu bronz golge halkasi bu
    # sekilde elde ediliyor (govde tepeden bakildiginda duz tek renk
    # degil, kenarda golgeli bir halka + parlak ic disk gorunur).
    cyl("BodyBase", body_r, body_h_bot, (0, 0, body_z0 + body_h_bot / 2),
        b_bot)
    top_z = body_z0 + body_h_bot + body_h_top / 2
    cyl("BodyTop", body_r * 0.70, body_h_top, (0, 0, top_z), b_top)

    lz = lens_z if lens_z is not None else (body_z0 + body_h_bot + body_h_top + 0.01)
    cyl("Lens", lens_r, 0.02, (0, 0, lz), lens, verts=20)
    return top_z + body_h_top / 2


def render_to(name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


def stars(level, acc_material, radius_ring=0.30, z=0.5):
    """Tepede seviye sayisi kadar altin yildiz rozeti (kucuk 4-kollu
    piramit cifti). Seviye 1'de yok, referans oyundaki yildiz rozeti
    diliyle tutarli."""
    n = level - 1
    if n <= 0:
        return
    for i in range(n):
        a = (i - (n - 1) / 2.0) * 0.5
        x = radius_ring * math.sin(a)
        y = -radius_ring * 0.15
        cone("Star%d" % i, 0.035, 0.05, (x, y, z), acc_material, verts=4)


# ---------------------------------------------------------------- kuleler
# Her build_* fonksiyonu level (1-3) alir. Govde ailesi sabit kalir,
# seviyeyle detay/parlaklik artar (bkz. dosya basi aciklama).

def build_machine_gun(level=1):
    """Hizli-ates / makineli tufek. TEK gumus namlu (referans oyunda tek
    namlu, cift namlu DEGIL - piksel-olculu crop'ta net gorulen), govde
    ortak "hull" dili (gunmetal palet + bronz silindir + lens). Namlu
    ucunda sicak highlight. Referans: 'Makineli Tufek Taret'."""
    clear()
    silver = mat("MG_Silver", SILVER, rough=0.3)
    hl = mat("MG_MuzzleHL_L%d" % level, MUZZLE_HL, rough=0.25, emit=0.10 + 0.08 * level)

    top_z = hull(track_w=0.12, track_l=0.30, track_gap=0.19,
                 body_r=0.155, body_h_bot=0.09, body_h_top=0.09)

    # TEK namlu, govdeden -Y'ye uzanir (referansta gorulen tek gumus varil)
    cyl("Barrel", 0.055, 0.30, (0.0, -0.20, top_z - 0.02), silver,
        rot=(1.5708, 0, 0), verts=14)
    cyl("MuzzleTip", 0.062, 0.05, (0.0, -0.36, top_z - 0.02), hl,
        rot=(1.5708, 0, 0), verts=14)

    if level >= 2:
        # seviye 2: yan besleme kutusu (ammo box)
        box("AmmoBox", (0.20, 0.06, 0.13), (0.09, 0.11, 0.07), hl, 0.01)
    if level >= 3:
        # seviye 3: ikinci besleme kutusu
        box("AmmoBox2", (-0.20, 0.06, 0.13), (0.09, 0.11, 0.07), hl, 0.01)


def build_cannon(level=1):
    """Agir top. Ayni hull dili ama daha BUYUK palet/govde + KALIN-KISA
    namlu (zirhli/yavas hedeflere ezici hasar hissi). Referans: 'Top
    Taret' - MG'den daha agir/hantal siluet."""
    clear()
    silver = mat("CN_Silver", SILVER, rough=0.35)
    hl = mat("CN_MuzzleHL_L%d" % level, MUZZLE_HL, rough=0.3, emit=0.10 + 0.08 * level)

    top_z = hull(track_w=0.15, track_l=0.34, track_gap=0.22,
                 body_r=0.20, body_h_bot=0.11, body_h_top=0.11)

    # KALIN kisa namlu - agirlik/darbe hissi
    cyl("Barrel", 0.095, 0.26, (0.0, -0.17, top_z - 0.02), silver,
        rot=(1.5708, 0, 0), verts=16)
    cyl("MuzzleTip", 0.105, 0.05, (0.0, -0.31, top_z - 0.02), hl,
        rot=(1.5708, 0, 0), verts=16)

    if level >= 2:
        # seviye 2: omuz plakalari
        for sx in (-1, 1):
            box("Shoulder%d" % sx, (sx * 0.24, 0.04, top_z - 0.09), (0.06, 0.16, 0.06), hl, 0.012)
    if level >= 3:
        # seviye 3: arka dengeleyici agirlik
        box("Counterweight", (0.0, 0.22, top_z - 0.06), (0.16, 0.09, 0.07), hl, 0.012)


def build_sniper(level=1):
    """Keskin nisanci. Ayni hull dili ama DAR palet + UZUN INCE namlu
    (uzun menzil hissi). Kirmizi vurgu ayirt edici renk. Referans:
    'Keskin Nisanci Taret' - pahali, tum hatti delen."""
    clear()
    silver = mat("SN_Silver", SILVER, rough=0.25)
    acc = mat("SN_Acc_L%d" % level, (0.78, 0.18, 0.16, 1.0), rough=0.3, emit=0.15 + 0.12 * level)

    top_z = hull(track_w=0.10, track_l=0.24, track_gap=0.15,
                 body_r=0.13, body_h_bot=0.08, body_h_top=0.08)

    # UZUN ince namlu, -Y'ye cok tasar (uzun menzil hissi)
    cyl("Barrel", 0.032, 0.52, (0.0, -0.32, top_z - 0.02), silver,
        rot=(1.5708, 0, 0), verts=14)
    cyl("MuzzleTip", 0.038, 0.04, (0.0, -0.57, top_z - 0.02), acc,
        rot=(1.5708, 0, 0), verts=14)

    # gorunur spring/geri-tepme segmentleri (referansta net gozlendi)
    coil_count = 2 + level
    for i in range(coil_count):
        y = -0.04 - i * 0.055
        cyl("Coil%d" % i, 0.045, 0.018, (0.0, y, top_z - 0.02), acc,
            rot=(1.5708, 0, 0), verts=10)


def build_radar(level=1):
    """Destek: menzil/algi artirici. NAMLUSUZ - ayni hull (palet+bronz
    govde) ama namlu yerine ustte donen canak-anten. Referans: 'Radar
    Taret' - kendisi atmaz, yakindaki kulelerin menzilini artirir."""
    clear()
    metal = mat("RD_Metal", METAL)
    acc = mat("RD_Acc_L%d" % level, (0.55, 0.62, 0.68, 1.0), rough=0.25, emit=0.20 + 0.15 * level)

    top_z = hull(track_w=0.12, track_l=0.28, track_gap=0.19,
                 body_r=0.155, body_h_bot=0.09, body_h_top=0.09)

    cyl("Post", 0.04, 0.08, (0, 0, top_z + 0.04), metal, verts=12)
    # canak: yatik disk, gövde/palet icine sigacak kucuklukte - namlu
    # degil, saldiri kulesinden silueti farkli ama tracks'i ORTMEZ
    cyl("Dish", 0.135, 0.035, (0, 0, top_z + 0.10), acc, rot=(0.25, 0, 0), verts=20)
    cyl("DishRim", 0.135, 0.012, (0, 0, top_z + 0.118), metal, rot=(0.25, 0, 0), verts=20)

    if level >= 2:
        # seviye 2: yan destek kollari
        for sx in (-1, 1):
            box("Arm%d" % sx, (sx * 0.18, 0.0, top_z - 0.02), (0.03, 0.12, 0.03), metal, 0.01)
    if level >= 3:
        # seviye 3: ikinci kucuk uydu-canak (coklu-kule destek hissi)
        cyl("Dish2", 0.06, 0.02, (0.15, -0.075, top_z + 0.03), acc, rot=(0.25, 0, 0.6), verts=14)


def build_time_distortion(level=1):
    """Destek: yavaslatma aurasi. NAMLUSUZ - ayni hull ama namlu yerine
    ustte kristal/turbin kanatlar, mavi vurgu. Referans: 'Zaman Bozulmasi
    Taret' - yakindaki dusmanlari yavaslatir."""
    clear()
    acc = mat("TD_Acc_L%d" % level, (0.28, 0.55, 0.92, 1.0), rough=0.25, emit=0.25 + 0.15 * level)

    top_z = hull(track_w=0.12, track_l=0.26, track_gap=0.18,
                 body_r=0.145, body_h_bot=0.085, body_h_top=0.085)

    # kristal/turbin: 4 kollu doner kanat, namlu yerine bu okunur
    blade_count = 4
    bz = top_z + 0.05
    for i in range(blade_count):
        a = i * (2 * math.pi / blade_count)
        x, y = 0.18 * math.cos(a), 0.18 * math.sin(a)
        box("Blade%d" % i, (x, y, bz), (0.08, 0.035, 0.12), acc, 0.01)

    cyl("CoreTop", 0.07, 0.08, (0, 0, bz + 0.06), acc, verts=12)

    if level >= 2:
        # seviye 2: ikinci ic halka - "guclenmis alan" hissi
        cyl("InnerRing", 0.14, 0.02, (0, 0, top_z + 0.02), acc, verts=20)
    if level >= 3:
        # seviye 3: 4 kanat 8'e cikar (daha yogun doner gorunum)
        for i in range(blade_count):
            a = (i + 0.5) * (2 * math.pi / blade_count)
            x, y = 0.18 * math.cos(a), 0.18 * math.sin(a)
            box("BladeX%d" % i, (x, y, bz), (0.055, 0.028, 0.09), acc, 0.008)


TOWERS = (
    ("hizli_ates", build_machine_gun),
    ("agir_top", build_cannon),
    ("keskin_nisanci", build_sniper),
    ("radar_destek", build_radar),
    ("zaman_bozulmasi", build_time_distortion),
)

# 3 Eylul buyutme turu: kullanici "kucukler biraz daha buyuk olsun" dedi.
# Olcum (PowerShell/System.Drawing alpha-bbox taramasi, 512x512 render):
# eski ortho=1.0 sabitiyle dolgunluk (max(fillW,fillH)) su sekildeydi:
#   agir_top 0.89, hizli_ates 0.74, keskin_nisanci 0.76, radar_destek 0.63,
#   zaman_bozulmasi 0.59. Hedef: hepsi ~0.85 (agir_top zaten oradaydi,
#   kalanlar kamerayi yaklastirarak buyutuldu). Kamera ORTHO_SCALE'i
#   kucultmek modeli kadrajda buyutur (deger = 512x512 kanvas SABIT kalir,
#   sadece modelin kadraj icindeki payi artar). L2/L3'un L1'den daha genis
#   olmadigi ayrica olculdu (yildiz/plaka eklemeleri govdenin icinde
#   kaliyor), yani tek deger 3 seviyeye de guvenle uygulanabilir.
# 4 Eylul gunduz - hull yeniden tasarimindan sonra yeniden olculdu
# (ayni PowerShell/System.Drawing alpha-bbox taramasi, 512x512):
#   agir_top 0.56, hizli_ates 0.62, keskin_nisanci 0.64, radar_destek 0.67,
#   zaman_bozulmasi 0.69. Hedef yine ~0.85.
ORTHO_SCALE = {
    "hizli_ates": 0.635,
    "agir_top": 0.690,
    "keskin_nisanci": 0.546,
    "radar_destek": 0.584,
    "zaman_bozulmasi": 0.563,
}


def build_all():
    out = []
    cam = bpy.context.scene.camera
    for name, fn in TOWERS:
        cam.data.ortho_scale = ORTHO_SCALE.get(name, 1.0)
        for level in (1, 2, 3):
            fn(level)
            out.append(render_to("%s_L%d.png" % (name, level)))
    cam.data.ortho_scale = 1.0
    return out
