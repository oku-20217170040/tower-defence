"""
Frontline - us (ana karargah) sprite ureteci (Blender).

Us haritanin ALT ortasinda durur; ordu yukaridan asagi akar. Yani usun
"on cephesi" +Y'dir (ekranda YUKARI) - kulelerin -Y kuralinin tersi,
cunku us donmez, savunma hatti hep orduya bakar.

Tasarim kararlari:

  * Us bir KULE DEGIL, bir YER. Kuleler hucreyi doldurur; us birden
    fazla hucre kaplar ve zeminle butunlesir. Bu yuzden alcak, genis,
    yatay katmanli.
  * Cephesi (kuzey) agir: beton bariyerler ve kum torbalari orada.
    Oyuncu neyin korundugu ve tehdidin nereden geldigini bakisla okur.
  * Merkezde YESIL komuta cekirdegi: oyuncunun rengi. Can azaldikca
    Unity tarafi bu sprite'i kirmiziya cekiyor (HomeBaseView) -
    o yuzden cekirdek tek ve buyuk, kismi kirmizilik parcali durmasin.
  * Emisyon dusuk (0,3-0,5): 22 Agu kule dersinde 1,4+ emisyon rengi
    beyaza patiriyordu.

Kullanim:
    exec(open(r"<repo>/tools/blender/base.py").read())
    setup_rig(); build_base(); render_to("us.png")
"""

import bpy
import os
import math

OUT = "C:/Users/serha/Unity/Unity/Tower Defence/Assets/Frontline/Art/Base"

BODY = (0.36, 0.38, 0.34, 1.0)
METAL = (0.52, 0.53, 0.50, 1.0)
CONCRETE = (0.50, 0.50, 0.47, 1.0)
GREEN = (0.26, 0.85, 0.45, 1.0)


def setup_rig(res=512, ortho=1.0):
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)

    sc = bpy.context.scene
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


def render_to(name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


def build_base():
    """Alcak, genis, katmanli karargah. Cephe +Y'ye (ekranda yukari) bakar."""
    clear()
    body = mat("B_Body", BODY)
    metal = mat("B_Metal", METAL)
    concrete = mat("B_Concrete", CONCRETE)
    green = mat("B_Core", GREEN, rough=0.35, emit=0.40)

    # taban plakasi: genis ve cok alcak - us bir YERDIR, kule degil
    ngon("Pad", 8, 0.48, 0.0, 0.05, body, 0.02, rot=0.3925)
    # ic avlu
    ngon("Yard", 8, 0.36, 0.05, 0.03, concrete, 0.015, rot=0.3925)

    # komuta cekirdegi: TEK ve buyuk. Can dustukce Unity bunu kirmiziya
    # ceker; parcali olsaydi renk gecisi kirli okunurdu.
    cyl("Core", 0.155, 0.16, (0, -0.02, 0.14), green, verts=18)
    cyl("CoreRing", 0.20, 0.07, (0, -0.02, 0.09), metal, verts=18)

    # cephe bariyerleri (kuzey): ordu yukaridan gelir, agirlik orada
    for i, x in enumerate((-0.30, -0.10, 0.10, 0.30)):
        box("Barrier%d" % i, (x, 0.36, 0.07), (0.17, 0.09, 0.10), concrete)
    box("Gate", (0.0, 0.26, 0.07), (0.22, 0.07, 0.09), metal)

    # yan hangarlar: siluete genislik verir, us tek blob olmaz
    for sx in (-1, 1):
        box("Hangar%d" % sx, (sx * 0.36, -0.06, 0.09), (0.16, 0.34, 0.12), body)
        box("Stripe%d" % sx, (sx * 0.36, -0.06, 0.16), (0.10, 0.05, 0.03), green)

    # arka depo ve anten: derinlik, ve "burasi yasiyor" hissi
    box("Depot", (0.0, -0.36, 0.06), (0.34, 0.14, 0.09), body)
    cyl("Mast", 0.028, 0.30, (0.24, -0.30, 0.18), metal, verts=10)
    cyl("Dish", 0.075, 0.03, (0.24, -0.30, 0.33), metal, verts=14)


def build_all():
    build_base()
    return [render_to("us.png")]
