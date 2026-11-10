"""Topcu, Keskin Nisanci, Zaman Bozulmasi ve Radar kuleleri.

Her kule: sabit kaide + 3 seviye ust yapi (Makineli Tufek ile ayni kalip,
bkz. tower_mg.py). Mekanikler:
docs/specs/2026-11-11-kule-yetenek-ve-yukseltme-kararlari.md

Renk kimligi (bir bakista ayirt edilsin):
  cn Topcu            pas turuncusu - kalin kisa namlu, namlu freni
  sn Keskin Nisanci   soguk mavi-celik - cok uzun ince namlu, durbun
  tm Zaman Bozulmasi  koyu kirmizi - NAMLU YOK, cekirdek + yayici kollar
                      (yavaslayan dusmana dusen kirmizi isikla ayni renk)
  rd Radar            acik gri/beyaz - NAMLU YOK, canak anten

Koleksiyonlar: TW_<prefix>_base, TW_<prefix>_L<n>_turret
Cikti: Assets/Frontline/Art/TowerSet/<prefix>_base.png, <prefix>_L<n>_turret.png
"""
import math
import os

import fl_lib as fl

TURRET_SCALE = 1.2
OCT = (0.0, 0.0, math.radians(22.5))

fl.PALETTE.update({
    "rust":        "#c0692b",
    "rust_dark":   "#8f4a1e",
    "rust_light":  "#e08d4a",
    "ice":         "#3f7fa6",
    "ice_dark":    "#2b5a78",
    "ice_light":   "#7fb8d8",
    "lens":        "#bfeaff",
    "crimson":     "#a8323a",
    "crimson_dark": "#74222a",
    "crimson_light": "#d85a5f",
    "glow_red":    "#ff8f86",
    "white":       "#e3e8ec",
    "white_dark":  "#b3bcc4",
    "signal":      "#f2c230",
})


# ------------------------------------------------------------- kaideler

def _base(prefix, accent, feet=False, round_deck=False):
    c = fl.fresh_collection("TW_%s_base" % prefix)
    p = prefix + "_base"
    if feet:
        # Topcu: geri tepmeyi tasiyan dort destek ayagi.
        for i in range(4):
            a = math.radians(45 + 90 * i)
            fl.box(c, "%s_foot%d" % (p, i), (0.20, 0.11, 0.05),
                   (0.40 * math.cos(a), 0.40 * math.sin(a), 0.03),
                   "steel_dark", bevel=0.02, rot=(0, 0, a))
    fl.cyl(c, p + "_plate", 0.44, 0.08, (0, 0, 0.04), "steel_dark",
           verts=8, bevel=0.03, rot=OCT)
    if round_deck:
        fl.cyl(c, p + "_deck", 0.34, 0.05, (0, 0, 0.09), "steel_mid", verts=32)
    else:
        fl.cyl(c, p + "_deck", 0.345, 0.05, (0, 0, 0.09), "steel_mid",
               verts=8, bevel=0.02, rot=OCT)
    fl.cyl(c, p + "_ring", 0.24, 0.03, (0, 0, 0.125), "steel_dark", verts=32)
    for i, (x, y) in enumerate(((0.28, 0.28), (-0.28, 0.28), (0.28, -0.28), (-0.28, -0.28))):
        fl.cyl(c, "%s_bolt%d" % (p, i), 0.032, 0.03, (x, y, 0.10), "steel_light",
               verts=6, smooth=False)
    for i, x in enumerate((-0.38, 0.38)):
        fl.box(c, "%s_tab%d" % (p, i), (0.07, 0.24, 0.04), (x, 0, 0.10), accent, bevel=0.012)
    return c


def _stripe(c, name, size, loc):
    fl.box(c, name, size, loc, "hazard", bevel=0.0)


# ---------------------------------------------------------------- Topcu

def _cannon_turret(level):
    c = fl.fresh_collection("TW_cn_L%d_turret" % level)
    t = "cn%d" % level
    z = 0.23
    w, d, r, length = {1: (0.34, 0.34, 0.070, 0.27),
                       2: (0.40, 0.38, 0.082, 0.33),
                       3: (0.46, 0.42, 0.095, 0.38)}[level]

    fl.box(c, t + "_hull", (w, d, 0.17), (0, 0.06, 0.215), "rust", bevel=0.06)
    fl.box(c, t + "_mantlet", (w * 0.72, 0.09, 0.15), (0, -0.115, 0.215), "rust_dark", bevel=0.025)
    fl.cyl(c, t + "_cupola", 0.075 + 0.01 * level, 0.035, (0.05, 0.10, 0.315),
           "rust_light", verts=20)

    y0 = -0.13
    fl.cyl(c, t + "_barrel", r, length, (0, y0 - length / 2, z), "gunmetal",
           verts=18, rot=fl.along_y())
    fl.cyl(c, t + "_sleeve", r + 0.022, 0.09, (0, y0 - 0.045, z), "steel_dark",
           verts=14, rot=fl.along_y())
    # Namlu freni: kulenin imzasi.
    fl.box(c, t + "_brake", (r * 2 + 0.07, 0.085, r * 2), (0, y0 - length + 0.03, z),
           "steel_dark", bevel=0.02)
    if level == 3:
        fl.box(c, t + "_brake2", (r * 2 + 0.07, 0.05, r * 2), (0, y0 - length + 0.125, z),
               "steel_dark", bevel=0.015)

    if level >= 2:
        for i, x in enumerate((-(w / 2 + 0.02), w / 2 + 0.02)):
            fl.box(c, "%s_skirt%d" % (t, i), (0.07, d * 0.8, 0.12), (x, 0.06, 0.19),
                   "rust_dark", bevel=0.022)
        _stripe(c, t + "_stripe", (w * 0.8, 0.035, 0.012), (0, 0.06 + d / 2 - 0.05, 0.305))

    # Mermi rafi: seviye arttikca daha fazla mermi.
    shells = {1: 0, 2: 3, 3: 4}[level]
    if shells:
        fl.box(c, t + "_rack", (0.075 * shells + 0.04, 0.12, 0.10),
               (0, 0.06 + d / 2 + 0.04, 0.19), "steel_dark", bevel=0.02)
        for i in range(shells):
            x = (i - (shells - 1) / 2) * 0.075
            fl.cyl(c, "%s_shell%d" % (t, i), 0.028, 0.02, (x, 0.06 + d / 2 + 0.04, 0.25),
                   "brass", verts=12)
    else:
        fl.box(c, t + "_rear", (0.22, 0.09, 0.10), (0, 0.06 + d / 2 + 0.02, 0.19),
               "steel_dark", bevel=0.02)

    fl.scale_collection(c, TURRET_SCALE)
    return c


# ------------------------------------------------------ Keskin Nisanci

def _sniper_turret(level):
    c = fl.fresh_collection("TW_sn_L%d_turret" % level)
    t = "sn%d" % level
    z = 0.22
    w = {1: 0.20, 2: 0.24, 3: 0.28}[level]
    length = {1: 0.38, 2: 0.42, 3: 0.44}[level]
    r = {1: 0.026, 2: 0.030, 3: 0.034}[level]

    fl.box(c, t + "_body", (w, 0.40, 0.15), (0, 0.10, 0.205), "ice", bevel=0.045)
    fl.box(c, t + "_stock", (w * 0.6, 0.10, 0.10), (0, 0.33, 0.19), "ice_dark", bevel=0.02)
    fl.box(c, t + "_collar", (w * 0.7, 0.07, 0.13), (0, -0.115, 0.21), "steel_dark", bevel=0.02)

    y0 = -0.12
    fl.cyl(c, t + "_barrel", r, length, (0, y0 - length / 2, z), "gunmetal",
           verts=14, rot=fl.along_y())
    fl.cyl(c, t + "_muzzle", r + 0.018, 0.09, (0, y0 - length + 0.045, z), "steel_dark",
           verts=12, rot=fl.along_y())

    # Durbun: govdenin ustunde, namluya paralel.
    sr = {1: 0.030, 2: 0.036, 3: 0.042}[level]
    sx = w / 2 - 0.03
    fl.cyl(c, t + "_scope", sr, 0.24, (sx, 0.04, 0.31), "steel_dark", verts=14, rot=fl.along_y())
    fl.cyl(c, t + "_lens", sr + 0.012, 0.03, (sx, -0.085, 0.31), "lens", verts=14, rot=fl.along_y())

    if level >= 2:
        # Namlu boyunca iki kilavuz ray: zirh delen atisin gorseli.
        for i, x in enumerate((-(r + 0.03), r + 0.03)):
            fl.box(c, "%s_rail%d" % (t, i), (0.022, length * 0.62, 0.03),
                   (x, y0 - length * 0.36, z), "ice_light", bevel=0.0)
        _stripe(c, t + "_stripe", (w * 0.8, 0.03, 0.012), (0, 0.25, 0.285))
    if level == 3:
        # Guc hucreleri: oldurunce ikinci atis.
        for i, x in enumerate((-(w / 2 + 0.07), w / 2 + 0.07)):
            fl.cyl(c, "%s_cell%d" % (t, i), 0.062, 0.10, (x, 0.16, 0.20), "ice_dark", verts=16)
            fl.cyl(c, "%s_cellcap%d" % (t, i), 0.034, 0.02, (x, 0.16, 0.26), "lens", verts=12)
        for i, f in enumerate((0.30, 0.52, 0.74)):
            fl.box(c, "%s_clamp%d" % (t, i), ((r + 0.05) * 2, 0.03, 0.05),
                   (0, y0 - length * f, z), "steel_dark", bevel=0.008)

    fl.scale_collection(c, TURRET_SCALE)
    return c


# ---------------------------------------------------- Zaman Bozulmasi

def _time_turret(level):
    c = fl.fresh_collection("TW_tm_L%d_turret" % level)
    t = "tm%d" % level
    arms = {1: 3, 2: 4, 3: 6}[level]
    reach = {1: 0.27, 2: 0.30, 3: 0.33}[level]

    fl.cyl(c, t + "_hub", 0.17 + 0.01 * level, 0.10, (0, 0, 0.19), "crimson",
           verts=arms * 2 if arms > 3 else 12, bevel=0.02, smooth=False)
    for i in range(arms):
        a = math.radians(90 + 360.0 * i / arms)
        ca, sa = math.cos(a), math.sin(a)
        fl.box(c, "%s_arm%d" % (t, i), (reach, 0.07, 0.07),
               (ca * reach / 2, sa * reach / 2, 0.18), "crimson_dark", bevel=0.02, rot=(0, 0, a))
        fl.cyl(c, "%s_node%d" % (t, i), 0.055, 0.08, (ca * reach, sa * reach, 0.19),
               "steel_dark", verts=12)
        fl.cyl(c, "%s_tip%d" % (t, i), 0.03, 0.02, (ca * reach, sa * reach, 0.24),
               "glow_red", verts=10)

    if level >= 2:
        # Kollari baglayan halka: alan buyudu.
        fl.cyl(c, t + "_ring", reach + 0.012, 0.03, (0, 0, 0.145), "steel_mid", verts=40)
        fl.cyl(c, t + "_ringin", reach - 0.035, 0.032, (0, 0, 0.146), "steel_dark", verts=40)
    fl.cyl(c, t + "_core", 0.10 + 0.012 * level, 0.03, (0, 0, 0.255), "crimson_light", verts=24)
    fl.cyl(c, t + "_eye", 0.05 + 0.01 * level, 0.02, (0, 0, 0.28), "glow_red", verts=20)
    if level == 3:
        fl.cyl(c, t + "_pupil", 0.035, 0.02, (0, 0, 0.30), "crimson_dark", verts=16)

    fl.scale_collection(c, TURRET_SCALE)
    return c


# ---------------------------------------------------------------- Radar

def _radar_turret(level):
    c = fl.fresh_collection("TW_rd_L%d_turret" % level)
    t = "rd%d" % level
    dish = {1: 0.20, 2: 0.25, 3: 0.29}[level]
    dy = -0.05

    # Konsol + karsi agirlik: canak merkezden kacik durur ki donus okunsun.
    fl.box(c, t + "_console", (0.24 + 0.03 * level, 0.16, 0.12), (0, 0.24, 0.19),
           "steel_mid", bevel=0.03)
    fl.box(c, t + "_yoke", (0.10, 0.22, 0.08), (0, 0.12, 0.18), "steel_dark", bevel=0.02)
    fl.cyl(c, t + "_lamp", 0.03, 0.02, (0.07, 0.25, 0.26), "signal", verts=10)

    fl.cyl(c, t + "_dish", dish, 0.05, (0, dy, 0.24), "white_dark", verts=32)
    fl.cyl(c, t + "_bowl", dish - 0.035, 0.03, (0, dy, 0.26), "white", verts=32)
    fl.cyl(c, t + "_bowl2", dish * 0.52, 0.02, (0, dy, 0.275), "white_dark", verts=28)
    for i in range(3):
        a = math.radians(90 + 120 * i)
        length = dish - 0.05
        fl.box(c, "%s_strut%d" % (t, i), (length, 0.022, 0.02),
               (math.cos(a) * length / 2, dy + math.sin(a) * length / 2, 0.295),
               "steel_dark", bevel=0.0, rot=(0, 0, a))
    fl.cyl(c, t + "_feed", 0.045, 0.04, (0, dy, 0.31), "steel_dark", verts=14)
    fl.cyl(c, t + "_feedtip", 0.022, 0.02, (0, dy, 0.335), "signal", verts=10)

    if level >= 2:
        for i, x in enumerate((-(dish + 0.06), dish + 0.06)):
            fl.box(c, "%s_rod%d" % (t, i), (0.03, 0.26, 0.03), (x, 0.10, 0.18),
                   "steel_dark", bevel=0.0)
            fl.cyl(c, "%s_rodtip%d" % (t, i), 0.036, 0.03, (x, -0.03, 0.19), "signal", verts=10)
        _stripe(c, t + "_stripe", (0.22, 0.03, 0.012), (0, 0.29, 0.255))
    if level == 3:
        # Ikinci kucuk canak: hedef paylasimi.
        fl.cyl(c, t + "_dishB", 0.085, 0.04, (-0.17, 0.30, 0.25), "white_dark", verts=20)
        fl.cyl(c, t + "_bowlB", 0.058, 0.02, (-0.17, 0.30, 0.275), "white", verts=20)
        fl.cyl(c, t + "_feedB", 0.02, 0.02, (-0.17, 0.30, 0.295), "signal", verts=8)

    fl.scale_collection(c, TURRET_SCALE)
    return c


# ----------------------------------------------------------------- hepsi

TOWERS = {
    "cn": (lambda: _base("cn", "rust_dark", feet=True), _cannon_turret),
    "sn": (lambda: _base("sn", "ice_dark"), _sniper_turret),
    "tm": (lambda: _base("tm", "crimson_dark", round_deck=True), _time_turret),
    "rd": (lambda: _base("rd", "white_dark", round_deck=True), _radar_turret),
}


def build(prefix, render=True):
    make_base, make_turret = TOWERS[prefix]
    out = []
    base = make_base()
    if render:
        out.append(fl.render_collection(base.name,
                   os.path.join(fl.OUT_TOWERS, prefix + "_base.png")))
    for level in (1, 2, 3):
        turret = make_turret(level)
        if render:
            out.append(fl.render_collection(turret.name,
                       os.path.join(fl.OUT_TOWERS, "%s_L%d_turret.png" % (prefix, level))))
    return out


def build_all(render=True):
    out = []
    for prefix in TOWERS:
        out += build(prefix, render)
    return out
