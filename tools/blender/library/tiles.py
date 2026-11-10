"""Zemin dosemeleri: yol, kurulum seridi, kapali hucre.

Blender DEGIL, sistem Python'u ile calisir (PIL gerekir):
    python tools/blender/library/tiles.py

Neden Blender degil: doseme her hucrede TEKRAR eder, kenarlari dikissiz
olmali ve dikkat cekmemeli (spec: "zemin dikkat cekmemeli"). Duz renk +
seyrek, kenardan sarip devam eden lekeler bunu en temiz verir.

GridTileRenderer uc dosemenin de AYNI boyutta olmasini ister (64 px).
Cikti: Assets/Sprites/Frontline/tile_{corridor,buildstrip,blocked}.png
"""
import os
import random

from PIL import Image, ImageDraw

SIZE = 64
SS = 8   # ust ornekleme: once buyuk ciz, sonra kucult (yumusak kenar)

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "..", "Assets", "Sprites", "Frontline")

# (ad, zemin, koyu leke, acik leke, leke sayisi, tohum)
TILES = [
    ("tile_corridor",   "#b08d5c", "#9c7b4d", "#c2a06e", 9, 11),
    ("tile_buildstrip", "#7db24f", "#6da043", "#90c462", 10, 23),
    # Kapali hucre: koyu cim + tas benekleri. Duz gri kare dagin altinda
    # "kutu" gibi okunuyordu; dag artik cimin ustunde duruyor.
    ("tile_blocked",    "#6fa348", "#629440", "#8f9890", 9, 37),
]


def make(name, base, dark, light, count, seed):
    rng = random.Random(seed)
    big = SIZE * SS
    im = Image.new("RGB", (big, big), base)
    draw = ImageDraw.Draw(im)
    for i in range(count):
        color = dark if i % 2 == 0 else light
        x, y = rng.uniform(0, big), rng.uniform(0, big)
        w, h = rng.uniform(0.05, 0.11) * big, rng.uniform(0.03, 0.07) * big
        # Dikissizlik: lekeyi 9 komsu kopyada da ciz, kenardan saran devam eder.
        for dx in (-big, 0, big):
            for dy in (-big, 0, big):
                draw.ellipse((x - w + dx, y - h + dy, x + w + dx, y + h + dy), fill=color)
    im = im.resize((SIZE, SIZE), Image.LANCZOS).convert("RGBA")
    path = os.path.normpath(os.path.join(OUT, name + ".png"))
    im.save(path)
    return path


if __name__ == "__main__":
    for tile in TILES:
        print(make(*tile))
