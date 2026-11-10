"""Mermi, namlu cakmasi, isabet ve patlama gorselleri.

Dosya adlari kodun zaten okudugu adlardir (ArtLibrary.fx):
  battery = Topcu (agir, turuncu)   ambush = Makineli Tufek (kucuk, sari)
  beam    = Keskin Nisanci (soguk mavi; mermisi yok, hat cizilir)
Mermilerin burnu -Y'ye (resimde asagi) bakar - eski gorsellerle ayni.

Koleksiyonlar: FX_<dosya adi>
"""
import math
import os

import fl_lib as fl

fl.PALETTE.update({
    "fire_out":  "#e8541e",
    "fire_mid":  "#ffb22e",
    "fire_core": "#fff3c4",
    "cold_out":  "#2f8fd0",
    "cold_mid":  "#7fd6ff",
    "cold_core": "#f0fbff",
    "dust":      "#8c7a66",
    "dust_light": "#b8a892",
    "pebble":    "#666d76",
    "shell":     "#d9a53c",
    "shell_dark": "#3a3f46",
})


def _bullet(name, heavy):
    c = fl.fresh_collection(name)
    r, length = (0.16, 0.50) if heavy else (0.10, 0.44)
    fl.cyl(c, name + "_case", r, length * 0.55, (0, length * 0.2, 0.2), "shell",
           verts=16, rot=fl.along_y())
    fl.cyl(c, name + "_band", r + 0.02, 0.05, (0, length * 0.42, 0.2), "shell_dark",
           verts=16, rot=fl.along_y())
    # Burun: -Y'ye sivrilen koni.
    fl.cyl(c, name + "_nose", r, length * 0.45, (0, -length * 0.30, 0.2),
           "shell_dark" if heavy else "fire_mid", verts=16,
           rot=fl.along_y(), radius2=r * 0.15)
    return c


def _muzzle(name, palette, size):
    c = fl.fresh_collection(name)
    out_k, mid_k, core_k = palette
    rot = math.radians(-90)   # bir kol -Y'ye (namlunun baktigi yone) uzar
    fl.star(c, name + "_out", 5, 0.42 * size, 0.17 * size, (0, -0.08, 0.00), out_k,
            rot_z=rot, stretch_y=1.35)
    fl.star(c, name + "_mid", 5, 0.29 * size, 0.13 * size, (0, -0.05, 0.01), mid_k,
            rot_z=rot, stretch_y=1.3)
    fl.star(c, name + "_core", 5, 0.16 * size, 0.09 * size, (0, -0.02, 0.02), core_k,
            rot_z=rot, stretch_y=1.2)
    return c


def _impact(name, palette, points, size):
    c = fl.fresh_collection(name)
    out_k, mid_k, core_k = palette
    fl.star(c, name + "_out", points, 0.46 * size, 0.27 * size, (0, 0, 0.00), out_k, rot_z=0.2)
    fl.star(c, name + "_mid", points, 0.33 * size, 0.20 * size, (0, 0, 0.01), mid_k, rot_z=0.55)
    fl.star(c, name + "_core", points, 0.19 * size, 0.12 * size, (0, 0, 0.02), core_k, rot_z=0.2)
    return c


def _dust_burst(name):
    c = fl.fresh_collection(name)
    fl.star(c, name + "_out", 9, 0.46, 0.28, (0, 0, 0.0), "dust", rot_z=0.3)
    fl.star(c, name + "_mid", 9, 0.30, 0.19, (0, 0, 0.01), "dust_light", rot_z=0.65)
    for i in range(6):
        a = 2 * math.pi * i / 6 + 0.4
        fl.rock(c, "%s_p%d" % (name, i), 0.05,
                (math.cos(a) * 0.36, math.sin(a) * 0.36, 0.05), "pebble", seed=i)
    return c


FIRE = ("fire_out", "fire_mid", "fire_core")
COLD = ("cold_out", "cold_mid", "cold_core")

ASSETS = [
    ("fx_battery_bullet", lambda n: _bullet(n, True), 1.0),
    ("fx_ambush_bullet",  lambda n: _bullet(n, False), 1.0),
    ("fx_battery_muzzle", lambda n: _muzzle(n, FIRE, 1.0), 1.25),
    ("fx_ambush_muzzle",  lambda n: _muzzle(n, FIRE, 0.8), 1.25),
    ("fx_beam_muzzle",    lambda n: _muzzle(n, COLD, 0.85), 1.25),
    ("fx_battery_impact", lambda n: _impact(n, FIRE, 9, 1.0), 1.0),
    ("fx_ambush_impact",  lambda n: _impact(n, FIRE, 6, 0.8), 1.0),
    ("fx_beam_impact",    lambda n: _impact(n, COLD, 7, 0.9), 1.0),
    ("fx_scenery_burst",  _dust_burst, 1.0),
]


def build_all(render=True):
    out = []
    for file_name, make, canvas in ASSETS:
        coll_name = "FX_" + file_name
        make(coll_name)
        if render:
            out.append(fl.render_collection(
                coll_name, os.path.join(fl.OUT_SPRITES, file_name + ".png"), canvas=canvas))
    return out
