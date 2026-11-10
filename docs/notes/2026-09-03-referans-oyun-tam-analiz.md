# Referans Oyun — Tam Kule/Düşman/UI Analizi (Tur 3)

## [TUR 4 EK — aynı gün, dördüncü oturum] Doğrulama + yeni ekranlar

Bu turda GÜNCELLE ekranındaki 5 kule kategorisi tek tek açılıp inşa menüsüyle birebir eşleştirildi, İNŞA menüsü ve kule seçim menüsü canlı oyunda tekrar tetiklendi, ayrıca daha önce görülmemiş üç ekran yakalandı: **duraklatma menüsü**, **DÜKKAN (gerçek para mağazası)**, **AŞAMA (bölüm seçim) ızgarası**. Aşağıdakiler [G] yeni gözlem, önceki bölümlerle çelişen hiçbir şey bulunmadı (Tur 3'ün 5-kule envanteri ve inşa fiyatları %100 teyit edildi).

- **[DÜZELTME]** Sol dikey hız butonu **X2**, önceki turların notundaki "X3" değil [G, net okundu].
- **Duraklatma menüsü** (sol alt duraklat ikonuna basınca): "DURDURULDU" başlığı altında **YARDIM / DEVAM ET / AYAR / YENİDEN BAŞLAT / ÇIKIŞ** — 5 buton, dikey liste. Duraklatınca dalga sayacı ve düşman sayısı donuyor (test: Dalga 1, 100/100 sabit kaldı) [G].
- **AŞAMA (bölüm seçim) ekranı**: sağda 4 sütunlu bölüm ızgarası (65-76 görüldü, hesap 66'da), her bölüm kutusunda **0-3 arası yıldız** (performans notu), aktif/seçili bölüm yeşil çerçeve. Solda seçili bölümün küçük harita önizlemesi + o bölümde kullanılabilecek kule/yetenek dallarının mini-ikon sayaçları (ör. "3/3" dolu altın rozet = o dalın kalıcı yükseltmesi tamamlanmış). Alt kısımda "AŞAMA TAMAMLAMA ÖDÜLÜ (İLK KEZ TEMİZLEME) +3000" ve "(YENİDEN DENEME) +300" — yani bir bölümü ilk kez bitirmek çok daha yüksek altın veriyor, tekrar oynayınca küçük sabit ödül var [G, yeni bulgu]. NORMAL/GELİŞMİŞ (advanced) iki zorluk sekmesi var, bu turda GELİŞMİŞ içeriği açılmadı [G-yüzeysel].
- **DÜKKAN**: PAKET/ALTIN/EŞYA/REKLAM alt-sekmeleri var; PAKET sekmesinde 3 gerçek-para paketi (₺81/₺160/₺325 karşılığı 3000+2, 7000+4, 15000+8 birim — muhtemelen altın+mücevher) + "REKLAMSIZ" tek seferlik satın alma + "3 Günlük Katılım Hediye Paketi". **Kapsam dışı** (ekonomi/parasal döngü), sadece varlığı not edildi [G].
- **İnşa menüsü fiyat-kategori eşleşmesi tam doğrulandı**: GÜNCELLE ekranının üst şeridindeki 5 kule ikonu (yeşil mermi demeti / tekli mermi / kırmızı hedef / mavi kronometre / gri radar) **birebir aynı sırayla ve aynı ikonlarla** inşa menüsünün 5 kolunda çıkıyor — üst=Makineli Tüfek(100), sol=Keskin Nişancı(300), sağ=Top(200), sol-alt=Zaman Bozulması(250), sağ-alt=Radar(200). Yani GÜNCELLE ekranı ile inşa menüsü **aynı 5 kule taksonomisini** kullanıyor, iki ayrı sistem değil [G, doğrulandı].
- **GÜNCELLE ekranının 6. sekmesi (mor vorteks ikonu) kule DEĞİL** — bu sekmeye dokununca ekran "AŞAMA" sekmesine geçti (kule yerleştirilemeyen, muhtemelen ayrı bir "Yetenekler/rastgele mod" kısayolu ya da bu hesapta henüz kilitli/boş içerik) [G, Tur 3'ün "Yetenekler kule değil" tespitini destekliyor].
- Kule seçiliyken menzil dairesi + üstte yükselt(yeşil chevron)/altta sat(sarı sikke) düzeni, bu turda **Makineli Tüfek** kulesinde de teyit edildi: yükselt 100, sat 75 — yani sat fiyatı yükseltmeden bağımsız, kule tipine göre ayrı bir taban değer [G, yeni veri noktası].
- Harita: Aşama 1/Stage-1 çöl haritasında da (Aşama 66 ile aynı harita şablonu) **4 giriş oku** (üst-sol, üst-sağ, alt-sol, alt-sağ) net görüldü, merkezde tek bunker; Tur 3'ün "4 ayrı giriş" tespiti bu turda ikinci bir bölümde de doğrulandı [G].

**Metodoloji notu:** Bu tur GÜNCELLE ekranındaki statik tekno-ağaç taramasını canlı-oyun inşa/seçim/duraklatma/yenilgi döngüsüyle çapraz doğruladı — iki kaynak da aynı 5 kule + aynı fiyatları veriyor, envanterin artık kesinleştiğini düşünüyoruz. Gözlem fazı burada gerçekten tükendi: 2 ardışık kaynak (statik menü + canlı oyun) aynı sonucu verdi, yeni kule/menü keşfedilmedi.

---

- Tarih: 2026-09-03
- Bu not önceki ikisinin (`2026-09-02-referans-oyun-gozlem.md`, `2026-09-03-telefon-analiz-aktif.md`) devamı ve **kesinleştirmesidir**. Çelişki varsa bu not geçerli (en güncel, en doğrudan kaynak: oyunun kendi "GÜNCELLE" tekno-ağacı ekranı).
- Yöntem: adb `input tap/swipe` + `exec-out screencap` ile GÜNCELLE (kalıcı yükseltme) ekranı tam taranarak her kule tipinin **resmi adı, açıklaması ve 3 görsel seviyesi** doğrudan okundu. Hiçbir APK/asset indirilmedi.
- Etiketler: **[G]** = ekranda gözlendi, **[T]** = tahmin, **[DÜZELTME]** = önceki turun hatasını düzeltiyor.

---

## [DÜZELTME] Kamera zoom kesin doğrulandı: YOK

Aktif oynanan Aşama 66'da haritanın ortasından yatay swipe (sağa ve sola, iki ayrı denemede) yapıldı. İki ekran görüntüsünde de sahne aynı ölçekte kaldı — üs binası, kaktüsler, yol genişliği piksel bazında **birebir aynı boyutta**. Kamera bu haritada pan bile gerektirmiyor (harita ekrana tam sığıyor), ama daha önemlisi **hiçbir pinch/swipe zoom'u tetiklemedi**. Önceki nottaki "kamera serbest: oyuncu haritayı kaydırıyor ve zoom yapıyor" ifadesi **YANLIŞTI** — zoom yok, sadece (büyük haritalarda) pan var. Bizim `MapCamera.cs` sabit-zoom + pan-only yaklaşımımız referansla birebir örtüşüyor, değiştirmeye gerek yok.

---

## 1. Kule Tipleri — RESMİ ENVANTER (GÜNCELLE ekranından okundu)

Oyunda **tam 5 kule tipi** var (+ 1 ayrı "Yetenekler" sekmesi, kule değil, sarf malzemesi/global yetenek). İnşa menüsünde (boş kareye dokununca çıkan artı-menü) bu 5 tipin fiyatları önceki turda gözlenmişti; bu turda resmi ad+rol+görsel teyit edildi.

| # | Resmi Ad [G] | Rol / Açıklama [G] | İnşa fiyatı [G, önceki tur] | Silüet |
|---|---|---|---|---|
| 1 | **Makineli Tüfek Taret** | "Yüksek ateş hızına sahip, piyadeye karşı etkili taretler" — hızlı, düşük hasarlı, çoklu-hedef DPS | 100 | Çift/tek kısa namlulu, dönen kanat-gibi gövde |
| 2 | **Top Taret** (Cannon) | "Güçlü tek atışlara sahip, tanklar ve zırhlı araçlar gibi yavaş hareket eden düşmanlara karşı etkili" — ağır tekli hasar | 200 | Tek kalın namlu, bronz/gri gövde |
| 3 | **Keskin Nişancı Taret** (Sniper) | "Yıkıcı ancak pahalı. Düz bir çizgideki TÜM düşmanlara ve nesnelere zarar verir. Geniş menzile sahiptir." — piercing/line-damage, en pahalı, en uzun menzil | 300 | İnce namlulu, görünür yay/spring detayı (geri tepme mekanizması) |
| 4 | **Radar Taret** | "Yakındaki taretlerin düşman algılama kabiliyetini artırır, menzillerini artırır" — **destek/aura kulesi**, kendisi ateş etmiyor, %45 menzil artışı gözlendi, birikmez | 200 | Beyaz/gri çanak-anten üstünde dönen radar |
| 5 | **Zaman Bozulması Taret** (Time Distortion) | "Yakındaki düşmanların zamanını yavaşlatır ve hareket hızlarını düşürür" — **destek/AoE debuff kulesi**, %60 yavaşlatma gözlendi, birikmez | 250 | Mavi kar tanesi/turbin şekilli gövde |

**[DÜZELTME]** Önceki turun "6. gri artı/pervane = hava savunma" ve "3. kırmızı yaylı fırlatıcı = havan" tahminleri yanlıştı — bunlar aslında **Zaman Bozulması** (mavi turbin) ve **Keskin Nişancı** (kırmızı, yaylı=spring geri tepme detayı, havan değil) kuleleriydi. Toplam kule sayısı 6 değil **5**.

### Kule rolü matrisi (netleşmiş)
| Rol | Kule |
|---|---|
| Çoklu-hedef DPS (piyade) | Makineli Tüfek |
| Tekli-hedef ağır hasar (zırhlı/yavaş) | Top |
| Piercing/line AoE, uzun menzil, pahalı | Keskin Nişancı |
| Destek — menzil/algı artırma (aura, kendi ateş etmiyor) | Radar |
| Destek — yavaşlatma (aura, kendi ateş etmiyor) | Zaman Bozulması |

---

## 2. Yükseltme Zinciri — HER KULE İÇİN AYNI YAPI

GÜNCELLE ekranında her kule tipi için:
- **3 görsel/güç seviyesi** (satır satır) — soldan: Seviye 1 (temel renk) → Seviye 2 (1-2 yıldız rozetli, ekstra detay) → Seviye 3 (3 yıldız rozetli, en kalabalık/parlak gövde). Bu, sahada dövüşürken görülen "kule üstünde yıldız rozeti" ile birebir örtüşüyor [G, önceki tur teyidi].
- Her seviyenin **4 ayrı stat dalı** var, her biri ayrı ayrı **5/5 seviyeye kadar** yükseltilebiliyor (bu hesapta hepsi MAKSİMUM, o yüzden tam maliyet tablosu okunamadı — [T] eksik veri):
  1. Hasar (turuncu mermi patlaması ikonu)
  2. İsabet/kritik (turuncu hedef ikonu)
  3. Ateş hızı (turuncu — muhtemelen, ikon net değil, "hız" temalı)
  4. Çoklu mermi/salvo (3 beyaz mermi ikonu — muhtemelen AoE/çoklu atış)
- Bu **kalıcı meta-yükseltme sistemi**, oyun-içi (in-run) kule yükselt/sat menüsünden **AYRI** bir katman: GÜNCELLE ekranı hesap geneli kalıcı güç, oyun-içi radial menüdeki "yükselt/sat" ise o tek dalga/koşu için geçici. **[YENİ BULGU]** — önceki iki not bu ayrımı netleştirmemişti, tek bir yükseltme sistemi sanılıyordu.

**Bizim oyuna ne anlama geliyor:** Bizim Frontline'da tek katmanlı in-run yükseltme (Tower.cs seviye sistemi) yeterli — referans oyunun kalıcı meta-tekno-ağacı bizim "çoklu yol + öğrenen düşman" özgünlüğümüzün dışında, **kapsam dışı** (ekonomi/idle-progression sistemi, görevin kapsam dışı maddesiyle örtüşüyor). Sadece görsel fikir alınabilir: **3 kademeli silüet+renk değişimi** (temel → 1-2 yıldız → 3 yıldız, kademeli detay artışı) — bunu üretilen sprite'larda uyguladık.

---

## 3. Yetenekler (Abilities) — Kule Değil, Global Sarf

GÜNCELLE ekranının 6. (son) sekmesi "YETENEKLER" — kule değil, ayrı kategori: "Ordunun özel yeteneği. Düşmanlar üsse yaklaştığında ancak ateş gücü yetersiz olduğunda kullanın." 3 alt-yetenek: saat ikonu (global yavaşlatma?), hedef ikonu (nokta vuruş/strike), patlama ikonu (bomba/hava saldırısı). Bunlar önceki turda gözlenen sağ-üst şeritteki 5 sarf malzemesiyle (bariyer, vorteks, ışın/bomba, mayin, tornado) aynı aile — sahadaki ikonlar (bariyer=6, mor vorteks=2, mavi ışın bomba=1, gri mayin, mor tornado) bu GÜNCELLE listesindeki 3 yetenekten bağımsız üçüncü bir kademe olabilir [T] (envanterden satın alınan tüketilebilir vs. yetenek ağacından açılan pasif — ayrım net değil, kapsam dışı, ekonomiye girmiyoruz).

---

## 4. Kule Seçim Menüsü — teyit, değişiklik yok

Önceki turlarda tanımlanan davranış (dolu yarı-saydam yeşil menzil dairesi, üstte yükselt/altta sat butonu, fiyatlar kule tipine göre değişken 200/150 ya da 200/300) bu turda tekrar doğrulandı, yeni bilgi yok.

---

## 5. İnşa Menüsü (boş kareye dokun) — teyit, değişiklik yok

Artı-şeklinde 5 seçenekli radial menü (önceki tur [G]) artık resmi kule adlarıyla eşleşti: üst=Makineli Tüfek(100), sol=Keskin Nişancı(300), sağ=Top(200), sol-alt=Zaman Bozulması(250), sağ-alt=Radar(200).

---

## 6. Harita/Yol — teyit

Aşama 66'da çöl haritası: merkezde tek bunker-üs, **4 ayrı giriş** (dört köşede kırmızı ok — üst-sol, üst-sağ, alt-sol, alt-sağ), her biri yolu takip edip merkeze ulaşıyor. Yol dokusu kum-rengi, kenarları kahve/toprak çentikli çizgiyle. Kaktüs + kaya yığını + toprak tümseği 3 tip dekoratif/yıkılabilir örtü nesnesi olarak sahada dağılmış duruyor.

---

## 7. Hâlâ Belirsiz Kalanlar

1. Her stat dalının (hasar/isabet/hız/salvo) tam maliyet eğrisi — bu hesap tüm dalları maksimum seviyeye getirmiş, ara maliyetler okunamadı. **Kapsam dışı** zaten (ekonomiye kafa yormuyoruz).
2. Yetenekler sekmesindeki 3 alt-yeteneğin tam işlevi (saat/hedef/patlama ikonlarının açıklama metinleri okunamadı, panel boştu).
3. Sağ-üstteki 5 sarf malzemesi ile Yetenekler sekmesindeki 3 yeteneğin aynı sistem mi ayrı sistem mi olduğu.

Bunlar ekonomi/idle-progression kapsamında olduğu ve görev kapsam dışı bıraktığı için daha fazla tur harcanmadı — **gözlem fazı burada tükenme kriterine ulaştı** (5 kule tipi net görüldü + her birinin en az 2 seviyesi görüldü + inşa/seçim menüsündeki tüm butonlar önceki turlarda tetiklenmişti).

---

## 8. Blender Üretimine Yansıyan Tasarım Kararları

1. **5 kule silüeti**, her biri net farklı rol okunur şekilde: makineli-tüfek (ince çift-namlu, taret dönebilir gövde), top (tek kalın namlu, ağır platform), keskin-nişancı (uzun ince namlu + görünür spring/recoil detay, kırmızı vurgu), radar (çanak-anten, namlu yok, döner disk), zaman-bozulması (kristal/turbin şekilli, namlu yok, mavi vurgu).
2. **3 kademeli görsel yükseltme dili**: seviye 1 = düz renk + sade gövde, seviye 2 = +1 küçük detay (yan plaka/ikinci namlu ucu) + hafif parlaklık, seviye 3 = +yıldız rozeti + en kalabalık gövde + metalik parlak vurgu. Bunu üretilen sprite'larda 3 varyant olarak uyguladık.
3. Destek kuleleri (radar, zaman-bozulması) **namlu YOK** — bu okunabilirlik için önemli bir ayraç, oyuncu saldırı kulesini destek kuleden silüetten anında ayırt edebilmeli.

---

## 9. Üretilen Varlıklar

- Blender script: `tools/blender/generate_tower_variety_set.py` — 5 kule tipi × 3 seviye = 15 üstten-görünüm PNG sprite, tek çalıştırmada üretir.
- Kaynak sahne: `Assets/Frontline/Art/tower-variety-source.blend`
- Çıktı sprite'lar: `Assets/Frontline/Art/TowerVariety/*.png` (üstten görünüm, platform+namlu ayrımı, mevcut Frontline sprite tarzına uygun düz renk + kontur stili)
