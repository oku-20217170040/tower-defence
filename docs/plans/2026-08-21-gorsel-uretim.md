# Görsel Üretim — Plan 6

**Tarih:** 21 Ağustos 2026
**Durum:** Unity tarafı hazır, Blender bağlantısı bekleniyor.
**Sanat yönü:** **Sade askeri silüet, düşük poli** (21 Ağustos kullanıcı kararı).

## Neden bu yön

Oyun dikey tutuşlu ve tepeden bakışlı. Telefonda bir birlik ~30 piksel yer kaplıyor; bu ölçekte detay değil **silüet** okunur. Koyu gövde + tek vurgu rengi, mevcut renk kodlamasını doğrudan taşır ve ilk sprite'lar aynı gün oyuna girebilir.

Elenen iki yön: böcek/sürü (oyunun metaforuna daha sadık ama bacaklı yürüyüş animasyonu işi uzatır), mekanik/drone (en hızlı modellenen ama soğuk ton).

## Unity tarafı — **BİTTİ**

`Runtime/View/ArtLibrary.cs` eklendi: birlik ve kule görsellerinin tek bağlı noktası.

- `UnitType` → sprite + ölçek + tint
- `TowerKind` → sprite + ölçek + tint
- **Sprite atanmamış her tür eski daireye düşer.** Yani asset'ler parça parça gelebilir, oyun hiçbir anda bozulmaz.
- Sahnede tek örneği bulunur; `UnitSpawner` ve `TowerPlacer` onu bulur. Bulamazlarsa hiçbir şey değişmez — **görsel katman oyunun çalışması için zorunlu değil.**
- Tint alpha 0 ise sprite kendi renkleriyle çizilir (Blender render'ı için doğrusu bu).

Kurulum: sahneye boş GameObject → `ArtLibrary` scripti → listelere tür/sprite ata.

## Üretim listesi

Öncelik sırası: birlikler önce, çünkü ekranda en çok onlar var ve tür ayrımı **oynanışın okunabilirliği** demek.

### Birlikler (4)

| Tür | Silüet yönü | Mevcut renk kodu | Ölçek |
|---|---|---|---|
| **Piyade** | Taban asker silüeti | açık gri-bej | 1,0 |
| **İzci** | Küçük, ince, hızlı hatlı | açık mavi | 0,7 |
| **Asker** | İri, ağır, geniş omuz | koyu kırmızı | 1,35 |
| **Kraliçe** | En iri, ayrıksı siluet | mor | 1,8 |

Ölçek farkları koddan geliyor ve **kasıtlı**: renk körlüğüne karşı ikinci bir ayrım kanalı.

### Kuleler (5)

| Tür | Rol | Mevcut renk |
|---|---|---|
| **Pusu mevzisi** | Tek hedef, sürekli. Ordu onu **göremez** — görsel olarak sessiz/gizli durmalı | gri-mavi |
| **Batarya** | Alan hasarı + tehdit yazar. Tek "gürültülü" kule; titreşim halkası var | turuncu-kırmızı |
| **Yem (sahte hedef)** | Hasar vermez, sürüyü çeker | yeşil |
| **Mayın tarlası** | Yavaşlatır, tehdit yazmaz | sarı |
| **Işın** | Periyodik yatay hat | açık mavi |

**Görsel dil kuralı (spec 9.1):** titreşim halkası YALNIZCA bataryada. Halkasızlık, "ordu beni göremiyor" demenin görsel yoludur. Bu, modelde de sürdürülmeli — pusu ve mayın tarlası bastırılmış, batarya öne çıkan olmalı.

### Sonraki tur

Üs, harita zemini (koridor / kurulum şeridi / duvar), keşif izi katmanı dokusu.

## Render boru hattı

1. Blender'da tepeden bakış, **ortografik** kamera (perspektif yok — ızgara oyununda perspektif hizayı bozar).
2. Şeffaf arka plan (Film → Transparent), PNG.
3. Birlik başına 256×256, kule başına 512×512.
4. Unity'ye import: Sprite (2D and UI), Pixels Per Unit hücre boyutuna göre ayarlanır, Filter Mode Bilinear, Compression yok (mobilde sonra ayarlanır).
5. `ArtLibrary` listesine ata. Tint alpha 0 bırak.

## Animasyon

Bu tur **statik sprite** yeterli — mevcut oyunda birlikler zaten pozisyon interpolasyonuyla hareket ediyor ve okunuyor.

Animasyon ikinci turda ve öncelik sırası şu: (1) kraliçe üsse varınca keşif izinin silinme efekti — oyunun en dramatik anı, (2) ışının atış çakması, (3) birlik ölüm efekti (bugün `DeathPuff` var).

## Blender bağlantısı

3D görünümde **N** → **BlenderMCP** sekmesi → **Connect to Claude** (port 9876). Bağlanmadan üretim yapılamaz.
