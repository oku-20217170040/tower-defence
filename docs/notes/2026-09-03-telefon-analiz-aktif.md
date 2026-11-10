# Referans Oyun — Aktif Oynama Turu (Ajan Bizzat Oynadı)

- Tarih: 2026-09-03
- Yöntem: Bu turda ajan (ben) `adb shell input tap/swipe` ve `adb exec-out screencap` ile oyunu bizzat oynadı — pasif gözlem değil. Önceki iki tur ("2026-09-02") pasif kalmıştı; bu tur onu tamamlıyor.
- Cihaz: T8IZFMQ4KFZHGY9H, ekran 2712x1220 (yatay, screencap çıktısı bu boyutta; `adb input tap` da bu koordinat sistemini kabul ediyor, ek dönüşüm gerekmedi).
- Test edilen sahne: "Stage 1" (Aşama 1) ve "Stage 66" — ikisi de aynı çöl haritası, aynı Dalga 1/20, 100/100 düşman, 10 can, 200 başlangıç altını.
- Etiketler: **[G] = ekranda gözlendi**, **[T] = tahmin/çıkarım**, **[UYARI] = metodolojik çekince**

---

## 1. SORU 1'İN KESİN CEVABI: Boş kareye dokununca ne oluyor

**Cevap: Liste/menü çıkıyor, direkt kule dikilmiyor.**

Boş bir kareye dokunulduğunda, o karenin merkezinde **artı (+) şeklinde bir çapraz menü** beliriyor [G]:

| Konum | İkon | Kule tipi (çıkarım) | Fiyat |
|---|---|---|---|
| Üst | Mermi/şarjör ikonu | Hızlı-ateş / hafif taret | **100** [G] |
| Sol | Kırmızı hedef/scope ikonu | Keskin nişancı / tekli-hedef yüksek hasar | **300** [G] |
| Sağ | Tek mermi ikonu | Standart top (cannon) | **200** [G] |
| Sol-alt | Saat/kronometre ikonu | Yavaşlatma | **250** [G] |
| Sağ-alt | Radar/hoparlör ikonu | Destek/aura | **200** [G] |

- Tıklanan karenin kendisi **camgöbeği (cyan) çerçeveli boş kare** olarak ortada kalıyor, 5 seçenek onun etrafında beliriyor [G]
- Bir seçeneğe dokununca menü kapanıyor, altın düşüyor (200 seçilirse 200→0 gibi) ve **o karede gerçek bir kule model** beliriyor (küçük top/taret gövdesi) [G] — ekran görüntüsüyle doğrulandı (bkz. build anı: gri top şekli belirdi, altın 200→0 düştü).
- Menü boş bir yere veya dışarı dokunarak da **iptal edilebiliyor** (gözlemlendi: ilk denemede menü dışına yanlışlıkla dokunma sonucu para harcanmadan menü kapandı).

**Bizim oyuna ne anlama geliyor:** Bu, önceki turun tahmin ettiği "hücreye bas → radial menü" desenini kule inşasına da genişletiyor — hem kule hem sarf malzemesi aynı UX kalıbını (bas → çevresinde seçenek küresi/artı aç) kullanıyor. Frontline'da `TowerPlacer` için bunu birebir öneriyorum: boş hücreye dokun → 3-5 kule tipini gösteren mini-menü aç → seç → inşa et. Tek tip kule + "hangi tip" sorusu olmadan direkt dikme yerine bu, mobilde "yanlışlıkla yanlış kule dikme" hatasını sıfırlıyor ve aynı zamanda kule çeşitliliğini her yerleştirmede hatırlatıyor.

---

## 2. Kule Seçim (upgrade/sat) Menüsü — teyit edildi, önceki notlarla birebir örtüşüyor

Dikili bir kuleye dokunulunca [G]:
- **Yarı saydam DOLU yeşil menzil dairesi** kulenin etrafında beliriyor (önceki turun tahminini doğruluyor)
- **Üstte** yeşil chevron + yükseltme fiyatı (bu run'da **200**)
- **Altta** sarı sikke + satış iadesi (bu run'da **150**)
- Kule beyaz köşeli çerçeve ile seçili gösteriliyor

Bu, önceki turda gözlenen 200/300 yükselt-sat çiftinden farklı bir kule tipinde 200/150 çıktı — yani **yükselt/sat fiyatları kule tipine ve seviyesine göre değişiyor**, sabit oran değil [G-yeni bulgu].

---

## 3. Ekonomi ve Zorluk — [UYARI] önemli metodolojik bulgu

Bu hesapta (banka bakiyesi 7728 altın, Aşama 66'ya kadar 2-3 yıldızla tamamlanmış, "AŞAMA TAMAMLAMA ÖDÜLÜ" tekno-ağaç sayaçları 6/3, 3/3, 3/3, 3/3, 3/3 dolu) **hem Stage 1 hem Stage 66'da aynı zorluk gözlendi**: Dalga 1'de 100 düşman, 10 can, 200 başlangıç altını.

Beş ayrı deneme yapıldı (adb ile bizzat oynanarak):
- Kule dikmeden: ~10 düşman sızınca (90/100) anında **YENİLGİ**.
- 1 kule (200 altın, tüm bütçe) dikilince: 15 düşman sızana kadar dayandı (85/100), sonra **YENİLGİ**.
- 2 ucuz kule (2x100) tek şeride dikilince: haritanın **ikinci şeridi tamamen savunmasız** kaldı, oradan hızla can gitti (10→2→0), **YENİLGİ**. Bu sırada 92→30 düşman arası öldürülüp ~186 altın toplanabildi — yani kuleler çalışıyor, sorun kapsama alanı.
- Tek kule + oyun duraklatılıp (pause) para harcanması: **duraklatma sırasında dalga ilerlemiyor, para harcanabiliyor** — bu şekilde güvenli kule dikimi mümkün [G, yeni bulgu].
- Harita **çift şeritli**: sol şerit üsse yakın, sağ şerit ayrı bir yoldan geliyor — tek şeridi kapatmak yetmiyor, her iki şeridin de en az bir kuleyle kapatılması gerekiyor [G, yeni bulgu — önceki turlarda gözlenmemiş].

**Yorum:** Bu hesap yüksek ilerlemiş (66 aşama, 7728 banka altını) olmasına rağmen Dalga 1/Aşama 1'de bile 10 can + 100 düşman + 200 başlangıç altını ile üst üste kaybediyor. Bunun olası nedenleri:
1. Oyun, "Aşama 1"i bile hesabın genel güç seviyesine göre otomatik ölçeklendiriyor olabilir (kalıcı yükseltme/"tech" sistemi olmadan tekrar oynanamaz hale geliyor) [T],
2. Ya da bu seviye tasarımı gerçekten bu kadar zor ve gerçek yeni oyuncular ilk denemede kaybedip "YENİDEN BAŞLAT" ile öğreniyor (souls-like "dene-öğren-tekrar dene" mobil TD kalıbı) [T],
3. Ya da adb tur-başı gecikmesi (ekran görüntüsü al → oku → dokun, ~2-4 sn) düşmanların gerçek zamanlı hızına yetişmemi engelliyor — insan oyuncu parmakla çok daha hızlı art arda kule dikebilir [G-kesin, bu benim kısıtım].

Bu üçü ayrıştırılamadı — **kullanıcıya soru** olarak aşağıda bırakıyorum.

**Bizim oyuna ne anlama geliyor:** Çift şeritli erken dalga tasarımı ilginç bir zorluk eğrisi örneği: oyuncuyu "tüm girişleri kapat" konusunda dalga 1'den itibaren eğitiyor. Frontline'da eğer çoklu giriş noktası olacaksa, dalga 1'de bunu açıkça öğretecek şekilde (ör. ilk 10 saniye düşman gelmeden "iki giriş var" ipucu) tasarlamak, bizim referans oyunda gördüğümüz "sessizce kaybet" tuzağından daha iyi bir UX olur.

---

## 4. Yenilgi Ekranı (yeni gözlem)

Can 0'a inince beliren "YENILGI" penceresi [G]:
- **ÇIK** — aşama seçimine döner
- **YENİDEN BAŞLAT** — aynı aşamayı sıfırdan başlatır (kuleler silinir, altın/can resetlenir)
- **CANLANDIR (3000 altın)** — banka altınıyla o anki durumdan devam etme hakkı; "10 canlanma hakkın kaldı" notu var → **günlük/sınırlı revive hakkı sistemi** [G, yeni bulgu]

**Bizim oyuna ne anlama geliyor:** "Canlandır" mekaniği pay-to-continue tarzı bir yumuşatma; ücretsiz oyuncular için sınırlı sayıda (günlük 10?) hak veriliyor. Bizim oyunda parasal döngü olmayacağı için bunun yerine "YENİDEN BAŞLAT" + belki checkpoint/dalga-bazlı otomatik kayıt yeterli.

---

## 5. Hâlâ belirsiz kalan sorular (kullanıcıya)

1. **Zorluk eşiği:** Az önce açıkladığım gibi, senin hesabında (66 aşama tamamlanmış, yüksek banka altını) Aşama 1'i baştan oynadığımda 10 can + 100 düşman ile üst üste kaybettim — hatta 2 kule dikip aktif oynadığımda bile (sağ şeridi ihmal ettiğim için). **Sen bu oyunu normalde Aşama 1-5 civarında oynarken bu kadar sert mi buluyordun, yoksa senin hesabın/cihazın şu an "ölçeklenmiş" bir zorlukta mı?** Yani gerçek yeni oyuncu deneyimi bu kadar cezalandırıcı mı, yoksa benim adb turlarımın yavaşlığı mı (ekran görüntüsü alıp okuyup dokunma döngüsü ~2-4 sn, gerçek parmak çok daha hızlı) beni geride bıraktı?
2. **Çift şerit:** Bu haritada gerçekten iki ayrı düşman girişi/şerit var mıydı yoksa ben yanlış mı yorumladım (sağdaki "kalan düşman" sayacının yanındaki hareket, ikinci bir spawn noktasından mı geliyordu, yoksa aynı şeridin haritanın sağ tarafında dönüp gelen kısmı mıydı)? Bunu doğrulayabilir misin — sen oynarken haritanın tamamını görebiliyorsun, ben ekran görüntülerinden kestiriyorum.
3. **Sarf malzemesi yerleştirme:** Bu turda zaman/can yetersizliğinden yol karesine dokunup sarf malzemesi (bariyer/duman/vs.) menüsünü tekrar test edemedim — önceki turda ("2026-09-02") bir kez gözlenmişti (5 ikonlu radial). Onaylamak ister misin, yoksa mevcut not yeterli mi?

---

## 6. Bizim Oyuna (Assets/Frontline/) Doğrudan Uygulanabilir Öneriler — Özet

1. **Boş hücreye dokun → kule tipi seçim menüsü aç** (artı/çapraz düzen, 3-5 seçenek, fiyat her ikonun altında) — hem kule çeşitliliğini öğretir hem yanlış-tip-dikme hatasını önler. `TowerPlacer.cs`'e bu akışı ekle.
2. **Kule seçiliyken menzili dolu-yarı-saydam disk olarak göster**, yükselt/sat butonlarını kulenin üstüne/altına yerleştir — bu zaten önceki notta vardı, bu turda ikinci bir kule tipinde de aynı düzenin (farklı fiyatlarla: 200/150) korunduğu teyit edildi, yani **tutarlı bir UI kalıbı**, tip bağımsız.
3. **Çoklu giriş noktası varsa dalga 1'den itibaren buna göre tasarla** — referans oyunda bunun sessizce (ipucu vermeden) oyuncuyu cezalandırdığını gördüm; bizim oyunda bunu daha iyi öğretmek (görsel ok/ipucu, ya da dalga 1'i tek şeritle başlatıp 2. şeridi dalga 3-4'te açmak gibi kademeli tanıtım) daha iyi bir ilk-izlenim sağlar.

---

## 7. Metodoloji Notu

Ekran görüntüleri yalnızca analiz amacıyla kullanıldı, hiçbiri projeye kopyalanmadı veya commit edilmedi. Sadece `adb shell input tap/swipe` ve `adb exec-out screencap` kullanıldı; hiçbir dosya/APK indirilmedi veya cihazdan çekilmedi.
