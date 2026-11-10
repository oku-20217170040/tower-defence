"""Cevre: agac, kaya, krater, dag. Kulelerle ayni toon + kontur stili.

Dosya adlari sahne kurucunun (FrontlineSandboxSceneBuilder) zaten okudugu
adlardir; cikti Assets/Sprites/Frontline altina yazilir, kod degismez.

Koleksiyonlar: EN_tree, EN_tree_large, EN_rock, EN_rock_large, EN_crater,
EN_mountain
"""
import math
import os
import random

import bmesh
import bpy

import fl_lib as fl

fl.PALETTE.update({
    "leaf_dark":  "#3c7a3c",
    "leaf":       "#57a046",
    "leaf_light": "#8acb5c",
    "stone_dark": "#666d76",
    "stone":      "#8b929a",
    "stone_light": "#b7bdc3",
    "dirt":       "#8f6f48",
    "dirt_dark":  "#5f472e",
    "pit":        "#33291f",
    "snow":       "#f1f5f9",
})


def _tree(name, size, seed):
    c = fl.fresh_collection(name)
    rng = random.Random(seed)
    outer = 6 if size > 1.0 else 5
    for i in range(outer):
        a = 2 * math.pi * i / outer + rng.uniform(-0.2, 0.2)
        r = 0.29 * size
        fl.ball(c, "%s_o%d" % (name, i), 0.15 * size * rng.uniform(0.9, 1.1),
                (math.cos(a) * r, math.sin(a) * r, 0.06), "leaf_dark", squash=0.6)
    inner = 5 if size > 1.0 else 4
    for i in range(inner):
        a = 2 * math.pi * i / inner + 0.5 + rng.uniform(-0.2, 0.2)
        r = 0.16 * size
        fl.ball(c, "%s_i%d" % (name, i), 0.16 * size * rng.uniform(0.9, 1.1),
                (math.cos(a) * r, math.sin(a) * r, 0.16), "leaf", squash=0.6)
    fl.ball(c, name + "_top", 0.17 * size, (-0.03 * size, 0.03 * size, 0.27), "leaf_light",
            squash=0.6)
    return c


def _rock(name, size, seed):
    c = fl.fresh_collection(name)
    rng = random.Random(seed)
    fl.rock(c, name + "_main", 0.30 * size, (0, 0, 0.12), "stone", seed=seed)
    n = 4 if size > 1.0 else 2
    for i in range(n):
        a = 2 * math.pi * i / n + rng.uniform(0.2, 1.0)
        r = 0.30 * size
        key = "stone_dark" if i % 2 == 0 else "stone_light"
        fl.rock(c, "%s_s%d" % (name, i), 0.16 * size * rng.uniform(0.8, 1.15),
                (math.cos(a) * r, math.sin(a) * r, 0.06), key, seed=seed * 7 + i)
    return c


def _crater(name, seed):
    c = fl.fresh_collection(name)
    rng = random.Random(seed)
    fl.cyl(c, name + "_rim", 0.40, 0.03, (0, 0, 0.015), "dirt", verts=11, smooth=False)
    fl.cyl(c, name + "_bowl", 0.31, 0.02, (0.01, -0.01, 0.035), "dirt_dark", verts=10, smooth=False)
    fl.cyl(c, name + "_pit", 0.20, 0.02, (0.02, -0.02, 0.05), "pit", verts=9, smooth=False)
    for i in range(5):
        a = 2 * math.pi * i / 5 + rng.uniform(-0.3, 0.3)
        fl.rock(c, "%s_p%d" % (name, i), rng.uniform(0.04, 0.07),
                (math.cos(a) * 0.37, math.sin(a) * 0.37, 0.05), "stone_dark", seed=seed + i)
    return c


def _peak(c, name, radius, height, loc, seed, cap=True):
    """Tepeden bakinca yuzleri farkli tonlanan cok kenarli koni."""
    rng = random.Random(seed)
    spin = rng.uniform(0, 6.28)
    parts = [("body", "stone", radius, 0.0, height)]
    if cap:
        parts.append(("cap", "snow", radius * 0.42, height * 0.60, height * 0.42))
    for part, key, r, z0, h in parts:
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=7, radius1=r, radius2=0.0, depth=h)
        for v in bm.verts:
            if v.co.z < 0:
                k = 1.0 + rng.uniform(-0.18, 0.18)
                v.co.x *= k
                v.co.y *= k
        mesh = bpy.data.meshes.new(name + "_" + part)
        bm.to_mesh(mesh)
        bm.free()
        mesh.materials.append(fl.toon(key))
        ob = bpy.data.objects.new(name + "_" + part, mesh)
        c.objects.link(ob)
        ob.location = (loc[0], loc[1], z0 + h / 2)
        ob.rotation_euler = (0, 0, spin)


def _mountain(name, seed):
    c = fl.fresh_collection(name)
    _peak(c, name + "_a", 0.40, 0.55, (-0.04, 0.05, 0), seed)
    _peak(c, name + "_b", 0.24, 0.30, (0.25, -0.20, 0), seed + 1, cap=False)
    _peak(c, name + "_c", 0.20, 0.24, (-0.28, -0.24, 0), seed + 2, cap=False)
    return c


ASSETS = [
    ("EN_tree",       lambda n: _tree(n, 1.0, 3),   "scenery_tree",       1.05),
    ("EN_tree_large", lambda n: _tree(n, 1.25, 8),  "scenery_tree_large", 1.25),
    ("EN_rock",       lambda n: _rock(n, 1.0, 5),   "scenery_rock",       1.05),
    ("EN_rock_large", lambda n: _rock(n, 1.3, 11),  "scenery_rock_large", 1.30),
    ("EN_crater",     lambda n: _crater(n, 2),      "scenery_crater",     0.95),
    ("EN_mountain",   lambda n: _mountain(n, 4),    "scenery_mountain",   1.10),
]


def build_all(render=True):
    out = []
    for coll_name, make, file_name, canvas in ASSETS:
        make(coll_name)
        if render:
            out.append(fl.render_collection(
                coll_name, os.path.join(fl.OUT_SPRITES, file_name + ".png"), canvas=canvas))
    return out
