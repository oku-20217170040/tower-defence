# Görsel Üretim — Plan 6

**Tarih:** 21 Ağustos 2026
**Durum:** Birlikler (4/4), kuleler (5/5), harita döşemeleri (3/3) üretildi ve oyuna bağlandı. Dönme + atış animasyonları çalışıyor. Sırada üs görseli.
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

### Kuleler (5) — **BİTTİ (22 Ağustos)**

Üreteç: `tools/blender/towers.py`.

**Görsel dil kuralı (spec 9.1) bu dosyanın asıl işi.** Titreşim halkası yalnızca bataryada; halkasızlık "ordu beni göremiyor" demenin görsel yolu. Model bunu sürdürüyor — **yükseklik bir süs değil, bilgi:**

| Tür | Model | Neden |
|---|---|---|
| **Batarya** | Yüksek taret, öne bakan tek namlu, omuz plakaları, turuncu | Tehdit **yazar**. Tek "gürültülü" kule; ordu onu görür ve kaçar |
| **Pusu mevzisi** | Alçak sekizgen pad, ortada dar mazgal, kamuflaj kanatları | Tehdit **yazmaz**. Asıl katil ama görünmemeli (spec 3.2) |
| **Mayın tarlası** | Yassı plaka + dağılmış sarı tümsekler, **dikey yapı yok** | Tehdit **yazmaz**; ordu algılayamaz |
| **Yem** | İnce direk + tepede parlak yeşil başlık, yayın kolları | Çekim yaydığını anlatır |
| **Işın** | X ekseninde uzun ray, iki uçta mercek, açık mavi | Yatay hat mekaniğini silüet anlatır |

**Elenen üç deneme (yorumlara yazıldı):** bataryada dikey namlu tepeden "delik" gibi okunuyordu; karşılıklı iki namlu gövdeyi delip geçen bir çubuğa dönüşüyordu; emisyon 1,4-1,6'da renk beyaza patlıyordu (0,35'e indirildi, renk base'ten gelir).

**Gövde rengi 0,20'den 0,30'a açıldı:** koyu haritada kule gömülüyordu ve emisyonlu parçaların yanındaki gövde neredeyse siyah çıkıyordu.

Unity import: Sprite, PPU 512 (sprite tam 1 dünya birimi), ölçek `ArtLibrary`'de 0 bırakıldı — yani `TowerPlacer.towerScale` (0,8) geçerli.

### Sonraki tur

Üs, harita zemini (koridor / kurulum şeridi / duvar), keşif izi katmanı dokusu.

## Render boru hattı

1. Blender'da tepeden bakış, **ortografik** kamera (perspektif yok — ızgara oyununda perspektif hizayı bozar).
2. Şeffaf arka plan (Film → Transparent), PNG.
3. Birlik başına 256×256, kule başına 512×512.
4. Unity'ye import: Sprite (2D and UI), Pixels Per Unit hücre boyutuna göre ayarlanır, Filter Mode Bilinear, Compression yok (mobilde sonra ayarlanır).
5. `ArtLibrary` listesine ata. Tint alpha 0 bırak.

### Harita döşemeleri (3) — **BİTTİ (22 Ağustos)**

Üreteç: `tools/blender/tiles.py`. Koridor / kurulum şeridi / duvar.

**Pazarlıksız kural: zemin dikkat çekmemeli.** `GridTileRenderer`'ın kendi yorumu bunu zaten söylüyordu ve haklı — oyuncunun bakması gereken şey keşif izi; zemin kontrastlı olursa oyunun **ana geri bildirim kanalı** körelir. Döşemeler bu yüzden düşük doygunluk, düşük kontrast (detaylar gövde renginin ±%15 bandında) ve kenarlarda düz payla üretildi (dikiş görünmesin).

| Döşeme | İçerik | Neden |
|---|---|---|
| Koridor | En koyu, en sakin; soluk tekerlek izleri + birkaç çakıl | Birliklerin yürüdüğü yer |
| Kurulum şeridi | Plaka hissi: içten çerçeve + köşe perçinleri | Oyuncu "buraya kurabilirim"i zeminden okur — bu bir süs değil, **arayüz** |
| Duvar | Kütleli bloklar | Duvar olduğunu **renk değil kütle** anlatır |

`GridTileRenderer` atlasa yükseltildi: döşemeler tek dokuya blit edilir, ızgara ne kadar büyürse büyüsün **tek SpriteRenderer, tek çizim çağrısı** kalır. Üçü de atanmazsa eski tek-texel zemine düşülür; Read/Write kapalıysa da düz zemine düşülüp uyarı yazılır.

## Animasyon — **BİTTİ (22 Ağustos)**

Hepsi **kozmetiktir**: hasar `TowerWeapon` içinde uygulanır, görsel katman denge sayılarına dokunmaz.

**Birlik dönüşü.** Birlikler gittikleri yöne döner. Sprite'ın burnu yerel −Y'de üretildiği için hedef açı = yönün açısı + 90°. Doğum anında yön **anında** kurulur (yoksa her birlik yola yanlış bakarak çıkar), yürürken 720°/sn ile yumuşak döner, hedef hücreye varırken yön vektörü sıfıra yaklaştığı için dönüş durdurulur.

**Kule dönüşü (`TowerTurret`).** Namlu hedefi her karede takip eder — yalnızca ateş anında değil, yoksa kule cansız durur. **Sadece hasar veren kulelerde**: dönen bir şey oyuncuya "bu beni hedefliyor" der, hasarsız kule (yem, mayın tarlası) bunu söylememeli. Batarya ağır (260°/sn), pusu çevik (420°/sn).

**Atış izleri (`ShotFlash`).** Havuzlanmış; kalabalık dalgada onlarca iz doğar, her biri için GameObject yaratmak çöp toplayıcıyı boğar. Görsel dil kuralı burada da geçerli: **pusu soluk ve ince, batarya parlak ve kalın.** Oyuncu pusunun çalıştığını görür — "asıl katil pusu" keşfine ancak böyle varabilir — ama ekranda gürültü yapan hep batarya olur.

> **İz sıklığı hasardan ayrıldı:** hasar sürekli uygulanır, iz sabit aralıklarla çizilir. Böylece görselin sıklığı dengeye dokunmadan ayarlanabilir.

**Işın çakması (`BeamFlash`).** İnce çizgi, ucu **ekran dışında** biter — uzunluk sabit bir sayı değil kameradan hesaplanır, yoksa yakınlaştırma değişince uç görünür hale gelir. Görsel kalınlık hasar kalınlığından **ayrıdır**: biri denge sayısı (0,6 hücre), diğeri sanat kararı (0,05 hücre). İlk sürümde ikisi aynı sayıdan türetiliyordu ve ışın ekranı kaplayan bir bant gibi duruyordu. Vurduğu birlik sayısı arttıkça ışın **parlar, kalınlaşmaz** — kalınlaşma inceliği bozuyordu.

**Kule ölçeği** 0,8 → 1,0 (hücreyi tam doldurur).

### Sıradaki animasyonlar

Öncelik sırası: (1) **kraliçe üsse varınca keşif izinin silinmesi** — oyunun en dramatik anı olmalı ve şu an hiç görünmüyor, (2) üs görseli, (3) birlik ölüm efektinin iyileştirilmesi (bugün `DeathPuff` var).

## Blender bağlantısı

3D görünümde **N** → **BlenderMCP** sekmesi → **Connect to Claude** (port 9876). Bağlanmadan üretim yapılamaz.
