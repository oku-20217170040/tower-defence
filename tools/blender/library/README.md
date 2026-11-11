# Frontline Asset Kütüphanesi

Blender'da üretilen her asset burada kalıcı olarak yaşar. **Yeni bir asset
işine başlamadan önce bu dosyayı oku; listede olan bir şeyi sıfırdan
yeniden modelleme, ana dosyayı açıp üzerinden devam et.**

## Dosyalar

| Dosya | Ne |
|---|---|
| `frontline_assets.blend` | Ana dosya. Bütün asset'ler, her parça ayrı koleksiyonda. |
| `fl_lib.py` | Ortak altyapı: palet, toon malzeme, ışık, kamera, kontur, render. |
| `tower_mg.py` | Makineli Tüfek kulesi. |
| `towers.py` | Topçu, Keskin Nişancı, Zaman Bozulması, Radar kuleleri. |
| `scenery.py` | Ağaç, kaya, krater, dağ. |
| `fx.py` | Mermi, namlu çakması, isabet, toz patlaması. |
| `base.py` | Üs (komuta sığınağı). |
| `tiles.py` | Zemin döşemeleri. Blender değil, sistem Python'u + PIL ile çalışır. |

`.blend` dosyası bilinçli olarak `Assets/` dışında: Unity içerideki
`.blend` dosyalarını model olarak içe aktarmaya çalışır ve editörü yavaşlatır.

## Nasıl çalıştırılır (Blender içinden)

```python
import sys; sys.path.insert(0, r"<repo>/tools/blender/library")
import fl_lib, tower_mg, towers, scenery, fx
fl_lib.open_library()     # ana dosyayı açar (yoksa kurar)
tower_mg.build_all()      # koleksiyonları yeniden kurar + render alır
towers.build("cn")        # tek kule; towers.build_all() dördünü de üretir
scenery.build_all(); fx.build_all()
fl_lib.save_library()     # ana dosyayı diske yazar - HER SEFERİNDE çağır
```

Üretici scriptler tekrar çalıştırılabilir: aynı koleksiyonu boşaltıp
yeniden kurarlar, başka asset'lere dokunmazlar.

## Stil kuralları

- Tam tepeden ortografik render, 512 px, şeffaf zemin.
- 4 bantlı toon boyama, ışık resmin sol üstünden; düşen gölge render'da
  kapalı (Unity'de `SpriteShadow` çizer).
- Koyu kontur: Freestyle, 5 px.
- Renkler yalnızca `fl_lib.PALETTE` içinden.
- Kaide ve taret ayrı sprite, aynı kadraj (tuval 1,4 birim; 1 birim = 1 hücre).
- Namlu yerel -Y'ye (resimde aşağı) bakar; dönme ekseni resmin merkezi.

## Envanter

| Asset | Koleksiyonlar | Çıktı | Script |
|---|---|---|---|
| Makineli Tüfek | `TW_mg_base`, `TW_mg_L{1,2,3}_turret` | `Art/TowerSet/mg_*.png` | `tower_mg.py` |
| Topçu | `TW_cn_base`, `TW_cn_L{1,2,3}_turret` | `Art/TowerSet/cn_*.png` | `towers.py` |
| Keskin Nişancı | `TW_sn_base`, `TW_sn_L{1,2,3}_turret` | `Art/TowerSet/sn_*.png` | `towers.py` |
| Zaman Bozulması | `TW_tm_base`, `TW_tm_L{1,2,3}_turret` | `Art/TowerSet/tm_*.png` | `towers.py` |
| Radar | `TW_rd_base`, `TW_rd_L{1,2,3}_turret` | `Art/TowerSet/rd_*.png` | `towers.py` |
| Ağaç (küçük, büyük) | `EN_tree`, `EN_tree_large` | `Sprites/Frontline/scenery_tree*.png` | `scenery.py` |
| Kaya (küçük, büyük) | `EN_rock`, `EN_rock_large` | `Sprites/Frontline/scenery_rock*.png` | `scenery.py` |
| Krater | `EN_crater` | `Sprites/Frontline/scenery_crater.png` | `scenery.py` |
| Dağ | `EN_mountain` | `Sprites/Frontline/scenery_mountain.png` | `scenery.py` |
| Mermiler | `FX_fx_battery_bullet`, `FX_fx_ambush_bullet` | `Sprites/Frontline/fx_*_bullet.png` | `fx.py` |
| Namlu çakmaları | `FX_fx_{battery,ambush,beam}_muzzle` | `Sprites/Frontline/fx_*_muzzle.png` | `fx.py` |
| İsabet patlamaları | `FX_fx_{battery,ambush,beam}_impact` | `Sprites/Frontline/fx_*_impact.png` | `fx.py` |
| Toz patlaması | `FX_fx_scenery_burst` | `Sprites/Frontline/fx_scenery_burst.png` | `fx.py` |
| Üs | `BS_home` | `Sprites/Frontline/home_base.png` | `base.py` |
| Zemin döşemeleri | (Blender'da değil) | `Sprites/Frontline/tile_*.png` | `tiles.py` |

Çıktı yolları `Assets/Frontline/` (kuleler) ve `Assets/` (diğerleri) altındadır.
Kod adları eski: `battery` = Topçu, `ambush` = Makineli Tüfek, `beam` = Keskin Nişancı.

Henüz yeni stile geçmeyenler: birlikler (`u_*.png`),
menzil halkası ve hedef işareti, yıldız rozeti, arayüz ikonları.

## Yeni asset eklerken

1. `fl_lib` yardımcılarını kullanan bir üretici script yaz (`tower_mg.py` örnek).
2. Çalıştır, render'ı gözle kontrol et.
3. `fl_lib.save_library()` çağır.
4. Yukarıdaki envanter tablosuna satır ekle.
