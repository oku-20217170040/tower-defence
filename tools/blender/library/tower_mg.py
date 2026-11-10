"""Makineli Tufek kulesi: sabit kaide + 3 seviye taret.

Mekanik (docs/specs/2026-11-11-kule-yetenek-ve-yukseltme-kararlari.md):
tek hedef, cok hizli ates. Seviye 2 ates hizi, seviye 3 CIFT NAMLU
(ayni anda iki hedef). Gorsel bunu soylemeli: L1 tek ince namlu,
L2 daha agir sogutma govdesi + sarjor tamburu, L3 cift namlu.

Koleksiyonlar: TW_mg_base, TW_mg_L1_turret, TW_mg_L2_turret, TW_mg_L3_turret
Cikti: Assets/Frontline/Art/TowerSet/mg_base.png, mg_L{1,2,3}_turret.png
"""
import math
import os

import fl_lib as fl

PREFIX = "mg"
GUN_Z = 0.22
TURRET_SCALE = 1.2   # taret kaideye baskin olmali; kaide yalnizca zemin


def build_base():
    c = fl.fresh_collection("TW_mg_base")
    oct_rot = (0.0, 0.0, math.radians(22.5))
    fl.cyl(c, "mg_base_plate", 0.44, 0.08, (0, 0, 0.04), "steel_dark",
           verts=8, bevel=0.03, rot=oct_rot)
    fl.cyl(c, "mg_base_deck", 0.345, 0.05, (0, 0, 0.09), "steel_mid",
           verts=8, bevel=0.02, rot=oct_rot)
    fl.cyl(c, "mg_base_ring", 0.24, 0.03, (0, 0, 0.125), "steel_dark", verts=32)
    for i, (x, y) in enumerate(((0.28, 0.28), (-0.28, 0.28), (0.28, -0.28), (-0.28, -0.28))):
        fl.cyl(c, "mg_base_bolt%d" % i, 0.032, 0.03, (x, y, 0.10), "steel_light",
               verts=6, smooth=False)
    for i, x in enumerate((-0.38, 0.38)):
        fl.box(c, "mg_base_tab%d" % i, (0.07, 0.24, 0.04), (x, 0, 0.10),
               "olive_dark", bevel=0.012)
    return c


def _gun(c, tag, x, length, r_barrel, r_jacket, jacket_len, fins):
    """Tek namlu: govde icinden -Y'ye uzar. Ucu y = -(0.14 + length)."""
    y0 = -0.14
    fl.cyl(c, tag + "_barrel", r_barrel, length, (x, y0 - length / 2, GUN_Z),
           "gunmetal", verts=16, rot=fl.along_y())
    fl.cyl(c, tag + "_jacket", r_jacket, jacket_len,
           (x, y0 - jacket_len / 2, GUN_Z), "steel_mid", verts=12, rot=fl.along_y())
    for i in range(fins):
        fy = y0 - jacket_len * (i + 0.5) / fins
        fl.cyl(c, "%s_fin%d" % (tag, i), r_jacket + 0.014, 0.018,
               (x, fy, GUN_Z), "steel_dark", verts=12, rot=fl.along_y())
    fl.cyl(c, tag + "_muzzle", r_barrel + 0.016, 0.05,
           (x, y0 - length + 0.01, GUN_Z), "steel_dark", verts=12, rot=fl.along_y())


def build_turret(level):
    c = fl.fresh_collection("TW_mg_L%d_turret" % level)
    t = "mg%d" % level

    if level == 1:
        fl.box(c, t + "_body", (0.30, 0.32, 0.14), (0, 0.03, 0.20), "olive", bevel=0.045)
        fl.box(c, t + "_rear", (0.20, 0.09, 0.10), (0, 0.215, 0.18), "olive_dark", bevel=0.02)
        fl.cyl(c, t + "_hatch", 0.07, 0.03, (0, 0.06, 0.285), "olive_light", verts=20)
        fl.box(c, t + "_ammo", (0.11, 0.16, 0.10), (-0.205, 0.04, 0.17), "brass", bevel=0.02)
        fl.box(c, t + "_feed", (0.07, 0.05, 0.04), (-0.13, -0.06, 0.21), "steel_dark", bevel=0.01)
        _gun(c, t + "_g", 0.0, 0.33, 0.034, 0.054, 0.15, 2)

    elif level == 2:
        fl.box(c, t + "_body", (0.34, 0.36, 0.16), (0, 0.04, 0.21), "olive", bevel=0.05)
        for i, x in enumerate((-0.20, 0.20)):
            fl.box(c, "%s_plate%d" % (t, i), (0.07, 0.30, 0.12), (x, 0.04, 0.19),
                   "olive_dark", bevel=0.022)
        fl.box(c, t + "_rear", (0.24, 0.10, 0.11), (0, 0.25, 0.19), "steel_dark", bevel=0.02)
        fl.cyl(c, t + "_hatch", 0.075, 0.03, (0.02, 0.08, 0.305), "olive_light", verts=20)
        fl.box(c, t + "_stripe", (0.30, 0.035, 0.012), (0, 0.185, 0.295), "hazard", bevel=0.0)
        fl.cyl(c, t + "_drum", 0.10, 0.09, (-0.30, 0.05, 0.20), "brass", verts=20)
        fl.cyl(c, t + "_drumcap", 0.045, 0.02, (-0.30, 0.05, 0.255), "steel_dark", verts=12)
        fl.box(c, t + "_feed", (0.09, 0.05, 0.04), (-0.19, -0.07, 0.22), "steel_dark", bevel=0.01)
        fl.box(c, t + "_sight", (0.045, 0.13, 0.05), (0.10, -0.07, 0.31), "steel_dark", bevel=0.012)
        _gun(c, t + "_g", 0.0, 0.38, 0.038, 0.064, 0.20, 3)

    else:
        fl.box(c, t + "_body", (0.44, 0.38, 0.18), (0, 0.05, 0.22), "olive", bevel=0.055)
        fl.box(c, t + "_shield", (0.54, 0.06, 0.15), (0, -0.155, 0.21), "olive_dark", bevel=0.022)
        fl.box(c, t + "_rear", (0.34, 0.11, 0.12), (0, 0.27, 0.20), "steel_dark", bevel=0.02)
        for i, x in enumerate((-0.09, 0.0, 0.09)):
            fl.box(c, "%s_vent%d" % (t, i), (0.05, 0.07, 0.02), (x, 0.275, 0.265),
                   "rubber", bevel=0.0)
        fl.cyl(c, t + "_hatch", 0.085, 0.035, (0, 0.07, 0.325), "olive_light", verts=20)
        fl.cyl(c, t + "_hatchcap", 0.04, 0.02, (0, 0.07, 0.35), "steel_dark", verts=12)
        for i, x in enumerate((-0.15, 0.15)):
            fl.box(c, "%s_stripe%d" % (t, i), (0.035, 0.26, 0.012), (x, 0.06, 0.315),
                   "hazard", bevel=0.0)
        for i, x in enumerate((-0.33, 0.33)):
            fl.cyl(c, "%s_drum%d" % (t, i), 0.10, 0.10, (x, 0.07, 0.21), "brass", verts=20)
            fl.cyl(c, "%s_drumcap%d" % (t, i), 0.045, 0.02, (x, 0.07, 0.27),
                   "steel_dark", verts=12)
        _gun(c, t + "_gl", -0.105, 0.40, 0.040, 0.066, 0.20, 3)
        _gun(c, t + "_gr", 0.105, 0.40, 0.040, 0.066, 0.20, 3)

    fl.scale_collection(c, TURRET_SCALE)
    return c


def build_all(render=True):
    out = []
    base = build_base()
    if render:
        out.append(fl.render_collection(base.name,
                   os.path.join(fl.OUT_TOWERS, PREFIX + "_base.png")))
    for level in (1, 2, 3):
        turret = build_turret(level)
        if render:
            out.append(fl.render_collection(turret.name,
                       os.path.join(fl.OUT_TOWERS, "%s_L%d_turret.png" % (PREFIX, level))))
    return out
