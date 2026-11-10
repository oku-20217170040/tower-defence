# Referans Oyun Gözlemi — Mobirix "Tower Defense: War Tactics"

- Tarih: 2026-09-02
- Cihaz: Redmi Note 14 Pro, adb üzerinden 11 ekran görüntüsü (yatay/landscape)
- Gözlenen bölüm: kar/buz haritası, **Dalga 21 → 23 / 25** arası, can 9
- Not: Yalnızca tasarım/sayı gözlemi yapıldı. Hiçbir asset/APK indirilmedi veya çıkarılmadı.
- Etiketler: **[G] = ekranda gözlendi**, **[T] = tahmin/çıkarım**

---

## 1. HUD ve UI Yerleşimi (yatay referans)

| Bölge | İçerik | Kaynak |
|---|---|---|
| Sol üst şerit | Can (9) · Altın (252–917 arası dalgalandı) · "DALGA 21/25" | [G] |
| Üst orta | "KALAN DÜŞMAN : 1/34" — kalan/toplam sayacı | [G] |
| Üst ortanın sağı | Sarı ok = "sonraki dalgayı erken başlat" | [T] |
| Sağ üst şerit | 5 sarf malzemesi ikonu + mavi rozet sayaçları | [G] |
| Sol dikey sütun | Reklam-izle (1 video), "+100 altın" (kırmızı nokta), yıldız/yükselt (kırmızı nokta), **X3 hız**, ayarlar, duraklat | [G] |
| Sağ alt köşe | Yeşil çanta = envanter/ganimet düğmesi | [G] |
| Dünya üstü | Hasar sayıları ("15") düşmanın üstünde beliriyor | [G] |

**Bizim oyuna ne anlama geliyor:** Dikey ekranda bu şerit ikiye bölünmeli — üst şeride can/altın/dalga + kalan düşman, **alt şeride** sarf malzemeleri ve hız/duraklat sütunu (baş parmak erişimi). Sol dikey sütun dikeyde alt yatay bara dönüşmeli.

---

## 2. Kule Tipleri

Sahada eş zamanlı gözlenen **en az 6 farklı silüet** (yıldızlı seviye rozetleri ile):

| # | Görünüm | Rol (çıkarım) | Kaynak |
|---|---|---|---|
| 1 | Çift namlulu paletli taret (en yaygın, 8+ adet) | Tek hedef DPS / ana hat | [G] silüet, [T] rol |
| 2 | Gri "çanak/radar" | Yavaşlatma veya destek aurası | [T] |
| 3 | Kırmızı gövdeli, yaylı fırlatıcı | Havan / alan hasarı (AoE) | [T] |
| 4 | Yeşil kutu taret (üs yakını) | Uzun menzil / füze | [T] |
| 5 | Gri "artı/pervane" şeklinde (üs yanı) | Hava savunma veya sabit üs savunması | [T] |
| 6 | Bakır/altın renkli varyant | Aynı kulenin yükseltilmiş hali | [G] renk değişimi |

- Kule seviyesi **yıldız sayısıyla** gösteriliyor (gövde üzerinde 1–3 yıldız) [G]
- Yükseltilince kulenin **rengi ve silüeti değişiyor** (gri → bakır/yeşil) [G]

**Bizim oyuna ne anlama geliyor:** 5–6 kule tipi + 3 kademeli yıldız yükseltme yeterli çeşitlilik veriyor. Seviyeyi rakam yerine **yıldız + renk** ile göstermek okunabilirliği artırıyor; dikey ekranda küçük kulelerde bu şart.

---

## 3a. TUR 2 GÜNCELLEME (aynı gün, ikinci oturum — 12 yeni ekran görüntüsü, çöl/kaktüs haritası, Dalga 3→9)

adb bağlantısı bu turun sonunda koptu (cihaz `List of devices attached` boş döndü — kablo/yetki sorunu, kullanıcı tarafında). 18 hedeflenen görüntüden 12'si alınabildi. Kule yerleştirme anı yine YAKALANAMADI (oturum başladığında kuleler zaten kuruluydu, yeni boş kareye dokunma anına denk gelinmedi). Ama **hedef kilitleme sorusu bu turda net cevaplandı**:

### SORU 2 CEVABI: Otomatik/pasif, dokunmaya gerek yok

t4–t8 ekran görüntülerinde (Dalga 5-8) ardışık olarak şunlar gözlendi:
- Bir kayanın üstünde **kırmızı çember + artı işaretli nişangah (crosshair)** ikonu beliriyor — bu kulenin "şu an ateş ettiği hedef" göstergesi.
- Nişangahın olduğu kayanın hemen yanında bir kule (yeşil/gri taretli) **ateş kıvılcımı ile o kayaya doğru mermi/lazer çiziyor**.
- Bir sonraki karede aynı kayanın üstünde **siyah/kırmızı HP barı** beliriyor ve azalıyor.
- Birkaç kare sonra o kaya **tamamen kayboluyor**, nişangah **komşu kayaya kayıyor** — yani kuleler sırayla, otomatik olarak menzillerindeki örtüyü tarıyor.
- **Hiçbir noktada oyuncunun kayaya/ağaca dokunduğuna dair kanıt yok** — bu ekran görüntüleri pasif gözlem, dokunma anını yakalayamadık ama süreç kesintisiz ve kule zaten oradaydı, oyuncunun herhangi bir eylemi olmadan otomatik ilerledi.
- **Bonus bulgu:** kaya yok olduğunda yerinde **"★15" sarı altın rozeti** beliriyor (t8) — yıkılan örtü **altın düşürüyor** (15 gözlendi). Önceki turda "belirsiz" bıraktığımız obje-ekonomisi sorusu kısmen netleşti: en az bir obje tipi (kaya) parçalanınca sabit 15 altın veriyor. Büyük/küçük obje kıyası hâlâ net değil (yalnızca bir boyut/tip yıkılırken yakalandı).

**Sonuç:** Kapak/örtü mekaniği **pasif ve otomatik** — kule menzilindeki kayayı/ağacı kendiliğinden hedef alıp yok ediyor, oyuncu müdahalesi gerekmiyor. Nişangah ikonu sadece görsel geri bildirim (hangi obje şu an hedefte). Bu, oyuncunun bir objeye dokunarak kuleyi oraya yönlendirdiği aktif bir "hedef kilitleme" sistemi OLMADIĞINI gösteriyor — en azından örtü nesneleri için. Düşmanlara karşı manuel hedef seçimi (öncelik: en yakın/en güçlü/vs.) bu turda da gözlenmedi, kule menüsünde 3. buton (hedef önceliği) yine görülmedi.

**Kalan belirsizlik:** Oyuncunun bilerek bir ağaca/kayaya dokunup dokunmadığını, dokunursa ekstra bir şey olup olmadığını hâlâ test edemedik (adb koptu). Yani "dokunma tamamen etkisiz mi yoksa dokunma sadece gereksiz mi (zaten otomatik oluyor)" ayrımı net değil — ama otomatik yıkımın var olduğu kesin.

### SORU 1: Hâlâ yakalanamadı
Bu turda da boş bir kule karesine dokunma anı görüntülenemedi — oturum ortasına denk geldik ve tüm kuleler zaten kuruluydu (2 kule sabit duruyordu, sadece Dalga 3→9 arası izlendi). Bir sonraki turda **oyuna en baştan başlarken** (Dalga 1, hiç kule yokken) yakalamak gerekiyor.

**Kullanıcıya spesifik soru:** Kral, yeni bir kule kurarken (boş bir kareye ilk dokunduğunda) ekranda ne beliriyor — kule tipi seçmeni isteyen bir liste/daire mi çıkıyor, yoksa dokunur dokunmaz direkt bir kule mi dikiliyor (yani önceden alt/yan bardan tipi seçip sonra haritaya mı dokunuyorsun)? Bunu bir dahaki kule kurma anında ekran görüntüsü isteyeceğim ya da senin tarif etmen yeterli.

---

## 3. Kule Seçim Menüsü (radial)

Bir kuleye basınca (s8'de net gözlendi):

| Öğe | Konum | Değer |
|---|---|---|
| **Menzil halkası** | Kule merkezli, yarı saydam **yeşil DOLU daire** | [G] |
| **Yükselt** | Kulenin **üstünde** yeşil chevron, içinde fiyat | **200** [G] |
| **Sat** | Kulenin **altında** altın sikke ikonu, içinde iade | **300** [G] |
| Seçili kule | Beyaz kare çerçeve + nişan işareti | [G] |
| Hedef önceliği | Menüde 3. buton gözlenmedi | [G-yok] |

- Sat iadesi (300) yükseltme maliyetinden (200) yüksek → oyun **yatırılan toplamın belirli bir yüzdesini** iade ediyor, muhtemelen %70–80 [T]

**Bizim oyuna ne anlama geliyor:** Menzil halkasını **dolu yarı saydam yeşil disk** yap (sadece çember çizgisi değil) — mobilde çok daha okunur. Yükselt/sat butonlarını kulenin üst/alt tarafına koy: dikey ekranda sağ/sol dar, üst/alt bol.

---

## 4. Ekonomi (gözlenen altın hareketleri)

| Olay | Ölçüm | Kaynak |
|---|---|---|
| Dalga 22'de 11 düşman ölürken altın | 792 → 917 = **+125** | [G] |
| Öldürme başına ortalama altın | **~11 altın/kill** | [G-hesap] |
| Yükseltme maliyeti (seviye ~2→3) | **200** | [G] |
| Kule satış iadesi | **300** | [G] |
| Dalga 21 içinde tek seferde harcama | 527 → 417 = **110** (bir sarf/kule) | [G] |
| Dalga 22'de art arda yükseltme | 727 → 157 = **570** harcandı | [G] |
| Reklam ödülü | **+100 altın** butonu (sol sütun) | [G] |
| Başlangıç altını | Gözlenmedi (oyuna geç girildi) | — |
| Dalga sonu bonusu | Doğrudan gözlenmedi; altın sürekli kill ile arttığı için **yok ya da küçük** | [T] |

**Bizim oyuna ne anlama geliyor:** Geç dalgalarda oyuncu ~500–900 altını bankada tutup tek seferde 2-3 yükseltmeye yatırıyor. Yani **yükseltme fiyatı ≈ 15-20 kill değerinde** olmalı. Bizim ekonomide "kill başına ~10 altın, yükseltme 200" oranını taban al.

---

## 5. Dalga Yapısı

| Dalga | Toplam düşman | Kaynak |
|---|---|---|
| 21 | **34** | [G] |
| 22 | **40** | [G] |
| 23 | **38** | [G] |
| Bölüm uzunluğu | **25 dalga** | [G] |

- Düşman sayısı dalga başına ~35-40'ta **plato yapıyor**, sonsuz artmıyor → zorluk **HP/zırh** ile artıyor [T]
- Sayaç "KALAN / TOPLAM" formatında geriye sayıyor [G]
- Boss: bu 3 dalgada belirgin bir boss gözlenmedi; 25/25 muhtemelen boss dalgası [T]

### Düşman çeşitleri (gözlenen)

| Tip | Görünüm | Not |
|---|---|---|
| Mavi kamyon | uzun, dikdörtgen, taretsiz | hızlı/zayıf [T] |
| Mavi tank | küçük taretli | standart [G] |
| Kırmızı tank | daha büyük, kırmızı gövde | dayanıklı [G] |
| Haki/hardal tank | büyük gövde, uzun namlu | ağır, geç dalgalarda [G] |
| Sarı/altın tank | hasar alırken parlıyor | elit varyant [T] |

- Düşmanların üstünde **ince kırmızı HP barı** var (hasar alınca görünüyor) [G]
- Düşmanlar **palet izi (tire track) decal** bırakıyor — hoş dokunuş [G]

**Bizim oyuna ne anlama geliyor:** 25 dalgalık kapalı bölüm yapısı (sonsuz değil) mobil oturum uzunluğu için ideal. Dalga başına 30-40 düşman tavanı koy, sonrasını HP çarpanıyla ölçekle.

---

## 6. Sarf Malzemeleri (consumables)

Sağ üst şeritte 5 ikon, her birinde mavi rozet sayacı:

| # | İkon | Ad (çıkarım) | Gözlenen sayaç |
|---|---|---|---|
| 1 | Sarı-siyah çizgili bariyer | **Bariyer** (yolu tıkar) | 6 → 5 → 4 → 2 [G] |
| 2 | Gri kasırga/duman | **Duman/sis** veya yavaşlatma | 0 (tükenmiş) [G] |
| 3 | Mavi bomba | **Hava saldırısı** | 2 (sabit kaldı) [G] |
| 4 | Gri kubbe | **Mayın** | sayaç gösterilmiyor [G] |
| 5 | Mor girdap | **Girdap (vortex)** | sayaç gösterilmiyor; sahada 2 kez aktif [G] |

### Kullanım şekli (s13'te net gözlendi)
Boş bir **yol karesine** basınca o karenin etrafında **5 ikonlu radial (halka) menü** açılıyor; ortada cyan hedef kare çerçevesi. Yani sarf malzemeleri **haritada hücreye yerleştiriliyor**, sürüklenmiyor.

- Girdap etkisi: mor kara delik, yolun üstünde 10+ saniye kalıyor, düşmanları emiyor/durduruyor [G]

**Bizim oyuna ne anlama geliyor:** "Hücreye bas → radial menü" deseni dikey ekran için **mükemmel**: alt bar yerine parmağın olduğu yerde menü açılıyor. Bunu birebir mekanik olarak benimse. Sarf sayaçlarını üst/alt şeritte küçük rozetle göster.

---

## 7. Yıkılabilir Örtü (ağaç / kaya / kütük)

- Üç örtü tipi: **kar kaplı çam**, **kaya yığını**, **kesilmiş kütük** [G]
- Ağaç ve kayaların üstünde **siyah/kırmızı HP barı** var; kütüklerde yok [G]
- Zamanla nesneler **yok oluyor**: dalga 21'de 20+ ağaç/kaya varken dalga 23'te sahanın büyük kısmı temizlenmiş [G]
- Yani düşman tankları ilerlerken örtüyü **ateşleyerek yıkıyor** (ya da AoE kule ateşi yıkıyor) [T]
- Yıkılan yer **inşa edilebilir boş kareye** dönüşüyor gibi görünüyor [T]

**Bizim oyuna ne anlama geliyor:** Yıkılabilir örtü haritayı zamanla açan güzel bir dinamik: erken dalgalarda inşa alanı dar, geç dalgalarda genişliyor. Basit bir `HP + OnDeath -> hücreyi buildable yap` mekaniğiyle eklenebilir; Frontline'da düşük maliyetli, yüksek hissiyatlı bir özellik.

---

## 8. Diğer Kayda Değer Detaylar

| Gözlem | Kaynak |
|---|---|
| **X3 oyun hızı** butonu (muhtemelen 1x/2x/3x döngüsü) | [G] |
| Kamera serbest: oyuncu haritayı kaydırıyor ve zoom yapıyor | [G] |
| Üs (base) sol üstte, siyah bunker + sarı yön oku ile işaretli | [G] |
| Kırmızı büyük ok = "düşman buradan geliyor / dikkat" işareti | [G] |
| Hasar sayıları küçük gri rakam olarak yükseliyor ("15") | [G] |
| Sol sütunda iki butonda kırmızı bildirim noktası (ücretsiz ödül hazır) | [G] |
| Reklam butonu "1" sayaçlı -> sınırlı ücretsiz ödül | [G] |

---

## 9. Dikey Ekrana Uyarlama Önerisi (özet)

```
+--------------------------+
| C:9  G:917  DALGA 22/25  |  <- üst şerit
|   KALAN DUSMAN: 19/40    |
+--------------------------+
|                          |
|      OYUN ALANI          |  <- ~%70 yükseklik
|  (hücreye bas -> radial) |
|                          |
+--------------------------+
| [BAR5][BOMB2][MAYIN][GRD]|  <- sarf şeridi
| [||][ayar][X3]      [>]  |  <- kontroller + dalga başlat
+--------------------------+
```

- Referansın **sol dikey sütunu** -> bizde **alt yatay iki sıra**
- Referansın **sağ üst sarf şeridi** -> bizde **alt şerit** (parmak erişimi)
- Kule menüsü: yükselt üstte / sat altta düzenini koru, radial menü çapını dikeyde biraz küçült
