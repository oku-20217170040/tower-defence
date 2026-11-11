"""Us: oyuncunun savundugu komuta sigingi.

Kulelerle AYNI yapi dili: sekizgen celik kaide, civatalar, zeytin yesili
zirh, sari uyari seritleri. Fark olcekte ve islevde: kule ates eder, us
korunur - o yuzden namlu yok, ortada parlayan bir cekirdek ve dusmanin
girdigi bir kapi var.

Yon: harita ussu EN ALT sirada tutar, birlikler YUKARIDAN gelir; kapi bu
yuzden resmin USTUNE (+Y) bakar. HomeBaseView ussu birliklerin ustunde
cizer, yani kapiya varan birlik binanin altina girer gibi gorunur.

HomeBaseView kalan cana gore sprite'i renklendirir (carpim); cekirdek ve
zirh bu yuzden ACIK tonlarda - koyu yuzeyde renk degisimi okunmuyor.

Koleksiyon: BS_home     Cikti: Assets/Sprites/Frontline/home_base.png
"""
import math
import os

import fl_lib as fl

fl.PALETTE.update({
    "core":      "#ffe27a",
    "core_dark": "#c98f1e",
    "gate":      "#1c1f23",
    "armor":     "#7d9a45",
    "armor_dark": "#58732f",
    "armor_light": "#a3c263",
})

OCT = (0.0, 0.0, math.radians(22.5))


def build():
    c = fl.fresh_collection("BS_home")
    n = "home"

    # Temel ve guverte: kule kaidesinin buyugu.
    fl.cyl(c, n + "_plate", 0.50, 0.06, (0, 0, 0.03), "steel_dark", verts=8, bevel=0.03, rot=OCT)
    fl.cyl(c, n + "_deck", 0.44, 0.05, (0, 0, 0.075), "steel_mid", verts=8, bevel=0.02, rot=OCT)

    # Kapi: ustte, guverteye gomulu karanlik gecit + rampa + uyari seritleri.
    fl.box(c, n + "_ramp", (0.30, 0.10, 0.03), (0, 0.455, 0.07), "steel_light", bevel=0.012)
    fl.box(c, n + "_gate", (0.24, 0.17, 0.02), (0, 0.375, 0.115), "gate", bevel=0.0)
    for i, x in enumerate((-0.155, 0.155)):
        fl.box(c, "%s_post%d" % (n, i), (0.07, 0.20, 0.13), (x, 0.375, 0.15), "steel_dark", bevel=0.02)
        fl.box(c, "%s_posthz%d" % (n, i), (0.05, 0.05, 0.012), (x, 0.44, 0.222), "hazard", bevel=0.0)

    # Zirhli govde: kapinin oldugu kenar acik kalan sekizgen.
    fl.cyl(c, n + "_hull", 0.335, 0.11, (0, -0.02, 0.155), "armor_dark", verts=8, bevel=0.03, rot=OCT)
    fl.cyl(c, n + "_roof", 0.27, 0.05, (0, -0.02, 0.235), "armor", verts=8, bevel=0.025, rot=OCT)

    # Dort kose burcu: kulelerin civatali kaide dilinin buyugu.
    for i in range(4):
        a = math.radians(45 + 90 * i)
        x, y = 0.385 * math.cos(a), 0.385 * math.sin(a) - 0.02
        fl.cyl(c, "%s_bastion%d" % (n, i), 0.085, 0.14, (x, y, 0.14), "steel_dark", verts=8,
               bevel=0.015, rot=OCT)
        fl.cyl(c, "%s_bastioncap%d" % (n, i), 0.045, 0.03, (x, y, 0.225), "armor_light", verts=12)

    # Cekirdek: usun "cani". En acik renk - can azaldikca kizaran yer.
    fl.cyl(c, n + "_ring", 0.165, 0.04, (0, -0.02, 0.28), "steel_dark", verts=24)
    fl.cyl(c, n + "_core", 0.125, 0.03, (0, -0.02, 0.31), "core", verts=24)
    fl.cyl(c, n + "_coreIn", 0.065, 0.02, (0, -0.02, 0.335), "core_dark", verts=18)
    for i in range(4):
        a = math.radians(90 * i)
        fl.box(c, "%s_clamp%d" % (n, i), (0.07, 0.035, 0.03),
               (0.165 * math.cos(a), 0.165 * math.sin(a) - 0.02, 0.31), "steel_dark",
               bevel=0.008, rot=(0, 0, a))

    # Yan zirh levhalari ve uyari seritleri: kulelerle ayni aksan.
    for i, x in enumerate((-0.455, 0.455)):
        fl.box(c, "%s_tab%d" % (n, i), (0.07, 0.30, 0.05), (x, -0.02, 0.09), "armor_dark", bevel=0.012)
    fl.box(c, n + "_stripe", (0.30, 0.035, 0.012), (0, -0.235, 0.262), "hazard", bevel=0.0)

    # Anten: usun kule degil KOMUTA yeri oldugunu soyler.
    fl.box(c, n + "_mast", (0.03, 0.16, 0.03), (-0.20, -0.30, 0.20), "steel_dark", bevel=0.0)
    fl.cyl(c, n + "_masttip", 0.035, 0.03, (-0.20, -0.385, 0.21), "hazard", verts=10)

    # Guverte civatalari.
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        if abs(math.cos(a)) < 0.45 and math.sin(a) > 0:
            continue   # kapinin oldugu kenar bos
        fl.cyl(c, "%s_bolt%d" % (n, i), 0.022, 0.02, (0.405 * math.cos(a), 0.405 * math.sin(a), 0.105),
               "steel_light", verts=6, smooth=False)
    return c


def build_all(render=True):
    c = build()
    if not render:
        return []
    return [fl.render_collection(c.name, os.path.join(fl.OUT_SPRITES, "home_base.png"), canvas=1.08)]
