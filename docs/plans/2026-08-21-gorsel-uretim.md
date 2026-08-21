# Görsel Üretim — Plan 6

**Tarih:** 21 Ağustos 2026
**Durum:** Birlikler üretildi ve oyuna bağlandı (4/4). Sırada kuleler.
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

### Birlikler (4) — **BİTTİ (22 Ağustos)**

Üreteç: `tools/blender/units.py` (tekrarlanabilir; Blender'da `exec` edilir).

**Tasarım kararı — silüet, anatomi değil.** İlk iki deneme anatomik asker (omuz + kask + tüfek) idi ve 30 pikselde blob oluyordu. Gövde **yön belirten bir kamaya** çevrildi: burun −Y'de, yani ekranda aşağı — birliklerin yürüyüş yönü. Okunabilirlik bundan sonra geldi.

Tür ayrımı **üç kanaldan birden**: silüet + vurgu rengi + ölçek. Üçü de aynı anda çalışır, yani renk körlüğünde bile tür ayırt edilir.

| Tür | Silüet | Vurgu rengi | Ölçek |
|---|---|---|---|
| **Piyade** | Dengeli kama, omuzluklu | bej | 1,0 |
| **İzci** | Dar ve uzun, omuzluk yok, anten var | açık mavi | 0,7 |
| **Asker** | Geniş, kalın omuzluklu | koyu kırmızı | 1,35 |
| **Kraliçe** | En iri, taçlı | mor | 1,8 |

**Ayarlanan render değerleri:** ortografik `ortho_scale = 1.0` (birlik kareyi doldurur), key 220 / fill 70 (ilk deneme aşırı aydınlıktı, renkler patlıyordu), `view_transform = Standard` (Filmic/AgX renkleri soldurur — sprite'ta renk olduğu gibi kalmalı).

**Gövde rengi orta tonlu asker yeşili**, koyu değil: haritanın zemini zaten koyu, koyu gövde kaybolurdu.

Unity import: Sprite, PPU 256 (sprite tam 1 dünya birimi — kod ile üretilen daireyle aynı), alphaIsTransparency açık. `ArtLibrary` sahneye kuruldu ve dört tür atandı; tint alpha 0 bırakıldı, yani sprite'lar kendi renkleriyle çiziliyor.

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
