"""Dusman birlikleri: yedi tur, kuleler ve usle ayni toon + kontur stili.

Tema askeri (docs/notes/2026-08-21-askeri-tema-sozlugu.md): birlikler bir
ordunun araclaridir. Dusman rengi MOR-LACIVERT + turuncu goz: oyuncunun
hicbir kulesiyle (zeytin, pas, buz mavisi, kirmizi, beyaz) karismaz ve
kum/cim zeminde net okunur.

Her tur silueti MEKANIGINI soyler:
  infantry  Piyade     kucuk tekerlekli arac - siradan birlik
  scout     Izci       dar, sivri, antenli - hizli ve kesif yapar
  soldier   Asker      paletli agir tank - dayanikli
  blind     Kor        penceresiz, kapali zirh + on kepce - izi ve yemi gormez
  brood     Kulucka    sirtinda kapsuller tasiyan arac - olunce bolunur
  queen     Kralice    buyuk komuta araci, parlayan kubbe - en iri hedef
  flyer     Ucan       dort pervaneli insansiz hava araci - koridor tanimaz

Burun yerel -Y'ye (resimde asagi) bakar; UnitAgent bu kurala gore dondurur.
Boyut farki kodda verilir (UnitSpawner: izci x0,7, asker x1,35, kralice
x1,8); burada hepsi kadraji benzer oranda doldurur.

Koleksiyonlar: UN_<tur>     Cikti: Assets/Sprites/Frontline/u_<tur>.png
"""
import math
import os

import fl_lib as fl

fl.PALETTE.update({
    "foe":        "#5a5fa8",
    "foe_dark":   "#3a3d75",
    "foe_light":  "#8c91d6",
    "foe_eye":    "#ff8a3d",
    "foe_glow":   "#ffd0a0",
    "pod":        "#e0a83a",
    "pod_dark":   "#a8761f",
    "tread":      "#23262a",
    "plate":      "#555b66",
    "plate_light": "#8a919c",
    "rotor":      "#c9d2dc",
})

Z = 0.12


def _wheels(c, t, xs, ys, size=(0.07, 0.15, 0.08)):
    i = 0
    for x in xs:
        for y in ys:
            fl.box(c, "%s_wheel%d" % (t, i), size, (x, y, 0.05), "tread", bevel=0.02)
            i += 1


def _eye(c, t, y, r=0.05, x=0.0, z=0.22):
    """One bakan 'goz': birligin nereye gittigini soyleyen tek sicak renk."""
    fl.cyl(c, t + "_eyering", r + 0.02, 0.02, (x, y, z), "foe_dark", verts=14)
    fl.cyl(c, t + "_eye", r, 0.02, (x, y, z + 0.015), "foe_eye", verts=14)


def infantry(c):
    t = "inf"
    _wheels(c, t, (-0.21, 0.21), (-0.16, 0.18))
    fl.box(c, t + "_hull", (0.34, 0.56, 0.14), (0, 0, Z), "foe", bevel=0.07)
    fl.box(c, t + "_cab", (0.24, 0.20, 0.07), (0, -0.10, 0.215), "foe_light", bevel=0.03)
    fl.box(c, t + "_bed", (0.24, 0.18, 0.05), (0, 0.16, 0.20), "foe_dark", bevel=0.02)
    _eye(c, t, -0.235, r=0.04, z=0.185)


def scout(c):
    t = "sct"
    _wheels(c, t, (-0.20, 0.20), (0.22,), size=(0.07, 0.17, 0.08))
    fl.box(c, t + "_fork", (0.07, 0.17, 0.08), (0, -0.24, 0.05), "tread", bevel=0.02)
    # Ok ucu govde: ucgen prizma, sivri ucu -Y'de (one). create_cone'un
    # ilk kosesi +Y'de dogdugu icin 180 derece cevrilir.
    fl.cyl(c, t + "_hull", 0.34, 0.12, (0, 0.02, Z), "foe", verts=3, bevel=0.04,
           rot=(0, 0, math.radians(180)), smooth=False)
    fl.box(c, t + "_tail", (0.34, 0.14, 0.10), (0, 0.24, 0.13), "foe_dark", bevel=0.03)
    fl.box(c, t + "_spine", (0.06, 0.36, 0.03), (0, 0.02, 0.195), "foe_light", bevel=0.0)
    fl.box(c, t + "_mast", (0.02, 0.22, 0.02), (0.12, 0.30, 0.20), "tread", bevel=0.0)
    fl.cyl(c, t + "_masttip", 0.03, 0.02, (0.12, 0.41, 0.21), "foe_eye", verts=10)
    _eye(c, t, -0.14, r=0.038, z=0.21)


def soldier(c):
    t = "sld"
    for i, x in enumerate((-0.30, 0.30)):
        fl.box(c, "%s_tread%d" % (t, i), (0.14, 0.78, 0.10), (x, 0, 0.05), "tread", bevel=0.03)
        for k in range(5):
            fl.box(c, "%s_link%d_%d" % (t, i, k), (0.15, 0.03, 0.012),
                   (x, -0.30 + 0.15 * k, 0.106), "plate", bevel=0.0)
    fl.box(c, t + "_hull", (0.48, 0.68, 0.14), (0, 0, Z + 0.02), "foe", bevel=0.06)
    fl.box(c, t + "_glacis", (0.40, 0.12, 0.05), (0, -0.27, 0.225), "foe_dark", bevel=0.02)
    fl.cyl(c, t + "_turret", 0.19, 0.08, (0, 0.06, 0.25), "foe_light", verts=8, bevel=0.025,
           rot=(0, 0, math.radians(22.5)), smooth=False)
    fl.cyl(c, t + "_gun", 0.045, 0.34, (0, -0.24, 0.25), "tread", verts=12, rot=fl.along_y())
    fl.cyl(c, t + "_muzzle", 0.06, 0.05, (0, -0.40, 0.25), "plate", verts=12, rot=fl.along_y())
    _eye(c, t, 0.06, r=0.045, z=0.30)


def blind(c):
    t = "bld"
    _wheels(c, t, (-0.25, 0.25), (-0.20, 0.0, 0.20), size=(0.08, 0.15, 0.08))
    # Kapali zirh: pencere yok, goz yok - "gormuyor".
    fl.box(c, t + "_hull", (0.44, 0.66, 0.16), (0, 0.03, Z + 0.01), "plate", bevel=0.05)
    fl.box(c, t + "_roof", (0.32, 0.50, 0.05), (0, 0.05, 0.235), "plate_light", bevel=0.03)
    # On kepce: onune ne cikarsa surer.
    fl.box(c, t + "_blade", (0.56, 0.08, 0.12), (0, -0.36, 0.09), "foe_dark", bevel=0.025)
    for i, x in enumerate((-0.17, 0.17)):
        fl.box(c, "%s_arm%d" % (t, i), (0.05, 0.12, 0.05), (x, -0.30, 0.13), "tread", bevel=0.01)
    # Capraz baglama plakalari: kapali, muhurlu arac.
    for i, a in enumerate((35.0, -35.0)):
        fl.box(c, "%s_x%d" % (t, i), (0.36, 0.045, 0.012), (0, 0.05, 0.265), "foe_dark",
               bevel=0.0, rot=(0, 0, math.radians(a)))
    for i, (x, y) in enumerate(((-0.12, -0.16), (0.12, -0.16), (-0.12, 0.26), (0.12, 0.26))):
        fl.cyl(c, "%s_rivet%d" % (t, i), 0.022, 0.015, (x, y, 0.265), "tread", verts=6, smooth=False)


def brood(c):
    t = "brd"
    _wheels(c, t, (-0.24, 0.24), (-0.22, 0.02, 0.26), size=(0.08, 0.15, 0.08))
    fl.box(c, t + "_hull", (0.42, 0.76, 0.13), (0, 0.02, Z), "foe", bevel=0.05)
    fl.box(c, t + "_cab", (0.30, 0.16, 0.07), (0, -0.26, 0.205), "foe_light", bevel=0.03)
    fl.box(c, t + "_rack", (0.36, 0.48, 0.03), (0, 0.11, 0.195), "foe_dark", bevel=0.01)
    # Sirtindaki kapsuller: olunce yavrulara bolunecegini soyler.
    spots = ((-0.09, -0.05), (0.09, -0.05), (-0.09, 0.11), (0.09, 0.11), (0.0, 0.27))
    for i, (x, y) in enumerate(spots):
        fl.ball(c, "%s_pod%d" % (t, i), 0.075, (x, y, 0.24), "pod", squash=0.7)
        fl.cyl(c, "%s_podcap%d" % (t, i), 0.028, 0.012, (x, y, 0.295), "pod_dark", verts=10)
    _eye(c, t, -0.345, r=0.038, z=0.20)


def queen(c):
    t = "qun"
    _wheels(c, t, (-0.33, 0.33), (-0.24, 0.0, 0.24), size=(0.10, 0.17, 0.09))
    fl.cyl(c, t + "_hull", 0.37, 0.14, (0, 0, Z + 0.01), "foe", verts=6, bevel=0.05,
           rot=(0, 0, math.radians(30)), smooth=False)
    fl.cyl(c, t + "_deck", 0.28, 0.05, (0, 0, 0.225), "foe_dark", verts=6, bevel=0.02,
           rot=(0, 0, math.radians(30)), smooth=False)
    # Parlayan komuta kubbesi: en iri, en degerli hedef.
    fl.cyl(c, t + "_ring", 0.17, 0.04, (0, 0, 0.27), "tread", verts=24)
    fl.ball(c, t + "_dome", 0.135, (0, 0, 0.29), "foe_eye", squash=0.6)
    fl.cyl(c, t + "_core", 0.055, 0.02, (0, 0, 0.375), "foe_glow", verts=16)
    # Tac: dort anten.
    for i in range(4):
        a = math.radians(45 + 90 * i)
        x, y = 0.27 * math.cos(a), 0.27 * math.sin(a)
        fl.cyl(c, "%s_post%d" % (t, i), 0.04, 0.06, (x, y, 0.27), "foe_light", verts=10)
        fl.cyl(c, "%s_tip%d" % (t, i), 0.022, 0.02, (x, y, 0.31), "foe_eye", verts=8)
    fl.box(c, t + "_prow", (0.20, 0.10, 0.08), (0, -0.37, 0.14), "foe_light", bevel=0.03)


def flyer(c):
    t = "fly"
    # X govde: dort kol, uclarinda pervane diski.
    for i in range(4):
        a = math.radians(45 + 90 * i)
        ca, sa = math.cos(a), math.sin(a)
        fl.box(c, "%s_arm%d" % (t, i), (0.36, 0.05, 0.04), (ca * 0.18, sa * 0.18, 0.20),
               "foe_dark", bevel=0.01, rot=(0, 0, a))
        fl.cyl(c, "%s_rotor%d" % (t, i), 0.15, 0.012, (ca * 0.33, sa * 0.33, 0.25), "rotor", verts=24)
        fl.box(c, "%s_blade%d" % (t, i), (0.28, 0.03, 0.008), (ca * 0.33, sa * 0.33, 0.262),
               "plate", bevel=0.0, rot=(0, 0, a + math.radians(60)))
        fl.cyl(c, "%s_hub%d" % (t, i), 0.035, 0.03, (ca * 0.33, sa * 0.33, 0.27), "tread", verts=10)
    fl.box(c, t + "_body", (0.20, 0.34, 0.10), (0, -0.01, 0.22), "foe", bevel=0.05)
    fl.box(c, t + "_nose", (0.12, 0.10, 0.07), (0, -0.20, 0.21), "foe_light", bevel=0.03)
    fl.box(c, t + "_fin", (0.24, 0.05, 0.03), (0, 0.17, 0.25), "foe_dark", bevel=0.01)
    _eye(c, t, -0.07, r=0.04, z=0.285)


UNITS = [
    ("infantry", infantry, 0.86),
    ("scout",    scout,    0.98),
    ("soldier",  soldier,  1.02),
    ("blind",    blind,    0.98),
    ("brood",    brood,    1.02),
    ("queen",    queen,    1.04),
    ("flyer",    flyer,    1.10),
]


def build_all(render=True):
    out = []
    for name, make, canvas in UNITS:
        c = fl.fresh_collection("UN_" + name)
        make(c)
        if render:
            out.append(fl.render_collection(
                c.name, os.path.join(fl.OUT_SPRITES, "u_%s.png" % name), canvas=canvas))
    return out
