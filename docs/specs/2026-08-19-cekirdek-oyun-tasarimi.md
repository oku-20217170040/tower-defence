# Çekirdek Oyun Tasarımı — Koloni Reboot

**Tarih:** 19 Ağustos 2026
**Durum:** Onaylandı
**Kapsam:** Bu belge yalnızca çekirdek oyun tasarımını kapsar. Teknik mimari, sanat yönü, ses tasarımı ve içerik üretimi ayrı belgelerde ele alınacaktır.

---

## 1. Konumlandırma

**Tek cümle:** Koloni öğrenir, sen yalan söylersin.

Böcek sürüleri yuvaya doğru ilerler ve geçtikleri yola feromon izi bırakır. İşe yarayan rotalar güçlenir, ölümle biten rotalar sönümlenir. Koloni zamanla haritayı ezberler. Oyuncu bu hafızayı okuyabilir ve kirletebilir.

**Platform:** Android, dikey tutuş (gerekçe: bölüm 3.6)
**Amaç:** Ticari mobil ürün
**Tür:** Roguelike tower defense

**Ayırt edici iddia:** Türde öğrenen düşman AI'ı olan başka oyunlar vardır, ancak öğrenmeyi oyuncunun kasıtlı olarak yanıltabildiği bir oyun bilinmemektedir. Ürünün tüm farklılaşması bu tek mekaniğe dayanır.

---

## 2. Temel Döngü

### 2.1 Run yapısı

- Bir run 12-15 dalga, hedef süre 10-12 dakika.
- Harita her run prosedürel üretilir: yuvaya çıkan 3-4 belirgin koridor.
- Her dalga sonrası oyuncuya 3 seçenek sunulur, biri alınır: yeni kule, kule yükseltmesi veya pasif yetenek.
- Run kaybedilirse veya tamamlanırsa oyuncu kalıcı para (biyokütle) kazanır.

### 2.2 Draft sistemi

Sunulan 3 seçenek tamamen rastgele değildir. Kolonin o anki davranışına göre ağırlıklandırılır: sol koridor ezberlenmişse oyuncuya o koridorda işe yarayacak seçeneklerin çıkma olasılığı artar. Bu, adaptif sistemi oynanışa bağlayan ikinci kanaldır.

---

## 3. Kandırma Mekaniği

Oyunun tamamı bu bölüme dayanır.

### 3.1 Kule sınıfları

Her kule iki sınıftan birine aittir:

**Gürültücü kuleler** böceklerin tehdit algısına yazılır (ThreatMap'e `threatCost` ekler). Yüksek hasar verirler, ancak koloni onlardan kaçmayı öğrenir — zamanla önlerinden kimse geçmez.

**Sessiz kuleler** tehdit haritasına hiç yazılmaz. Böcekler onları algılayamaz ve öğrenemez. Karşılığında hasarları düşüktür.

### 3.2 Tasarımın merkezi sonucu

Bu ayrım klasik TD sezgisini tersine çevirir: **güçlü kule, düşmanı öldürmek istediğin yere değil, düşmanı kaçırmak istediğin yere konur.** Gerçek öldürme işini sessiz kuleler yapar. Gürültücü kuleler bir silah değil, bir yönlendirme aracıdır.

Bu, oyuncunun öğrenmesi gereken tek büyük fikirdir ve oyunun "aha" anıdır.

### 3.3 Yem kulesi ve kendi kendini dengeleme

Sahte Feromon Yayıcı bir koridora yapay feromon basarak sürüyü oraya çeker. Ancak böcekler o koridorda ölürse iz doğal olarak zayıflar — yani yemin sürekli tazelenmesi gerekir.

Bu, mekaniğe kendi kendini dengeleyen bir ritim verir: mükemmel bir tuzak koridoru kurmak, onu sürekli beslemeyi gerektirir. Oyuncu "kur ve unut" yapamaz.

### 3.4 Feromon görselleştirmesi

Feromon izi ekranda görünür: parlayan, yoğunlukla kalınlaşan izler. Oyuncu hangi koridorun öğrenilmiş olduğunu bakışta anlar. Bu, mekaniğin arayüzüdür — ayrı bir UI paneline ihtiyaç bırakmaz.

### 3.5 Kurgu ile mekaniğin örtüşmesi

Karınca kolonisi teması keyfi değildir. Gerçek karıncalar feromon izi bırakarak yol öğrenir; bu, `learnedCost` haritasının birebir doğal karşılığıdır. Oyuncu feromon gördüğünde öğrenmeyi anlamak için tutorial'a ihtiyaç duymaz. Kurgu, açıklama yükünü ortadan kaldırır.

### 3.6 Ekran yönü ve harita düzeni

**Dikey tutuş.** Mobilde dikey oynanış artık beklenen standarttır; oyuncular tek elle, telefonu çevirmeden oynamak ister. Yatay tutuş kurulum sürtünmesi yaratır ve oturum başlatma oranını düşürür.

Dikey ekran bu mekaniğe engel değil, avantajdır:

- **Akış yukarıdan aşağı.** Böcekler üst kenardan iner, yuva alt kenardadır. Uzun dikey mesafe, feromon izlerinin birikip görünür hale gelmesi için gereken yolu sağlar.
- **Koridorlar dikey, yan yana.** 3 koridor ekranın dar ekseninde yan yana durur; oyuncu hangisinin parladığını tek bakışta karşılaştırır. Karşılaştırma dar eksende olduğu için göz gezdirme mesafesi kısadır.
- **Kule alanları koridorlar arasında.** Kuleler yolu kapatmadığı için koridorların arasındaki ve kenarlarındaki şeritlere kurulur. Bir şeride konan kule iki koridorun ikisini birden etkileyebilir — bu, dikey düzenin kendiliğinden getirdiği bir karar zenginliğidir.
- **Alt şerit arayüze ayrılır.** Ekranın alt bandı yuva ve draft/kule arayüzüdür; baş parmağın doğal erişim alanıdır.

Örnek grid oranı: 11 sütun x 20 satır (3 koridor, aralarında ve kenarlarda kurulum şeritleri). Kesin sayılar denemeyle ayarlanacaktır.

---

## 4. Yapay Zeka Katmanları

Mevcut üç sistem korunur, ancak her birine ayrı ve oyuncu tarafından okunabilir bir rol verilir.

| Katman | Rolü | Oyuncuya görünümü | Zaman ölçeği |
|---|---|---|---|
| ThreatMap | Böceğin algıladığı anlık tehlike | Yalnızca gürültücü kuleler yazar | Anlık |
| Q-Learning | Feromon izi, rota tercihi | Parlayan, kalınlaşan izler | Run içi |
| Genetic | Koloni evrimi | Dalga kompozisyonu ve dirençler değişir | Run'lar arası |

Genetik katman böylece nemesis işlevi görür: koloni run'lar boyunca oyuncuya karşı evrilir. Oyuncunun favori stratejisi zamanla aşınır.

### 4.1 Keşif — öğrenmenin ön koşulu

*(19 Ağustos 2026'da uygulama sırasında eklendi. Tasarımda atlanmış bir boşluktu ve mekanik onsuz hiç çalışmıyordu.)*

Yalnızca feromon takibi tanımlamak yeterli değildir. Böcekler her zaman en ucuz rotayı seçerse ve koridorlar eşit maliyetteyse — ki bölüm 9.4 bunu bilerek zorunlu kılar — her böcek aynı koridoru seçer. Koloni hiçbir zaman alternatif denemez, dolayısıyla öğrenecek bir şey de olmaz: ilk seçim son seçimdir.

Bu, kandırma mekaniğini de öldürür. Sürü ilk saniyede tek yola kilitlenmişse yem basmanın karşılığı kalmaz.

**Karar:** Her böcek, koridorlara dair kendine özgü küçük bir yön tercihiyle yola çıkar. Bu tercih böcek başına sabittir (yol boyunca değişmez), böylece bazı böcekler sürünün geri kalanından farklı bir koridoru dener. Gerçek karıncaların feromonu olasılıksal takip etmesinin karşılığıdır.

Keşif oranı, kolonin karakterini belirleyen bir ayar hâline gelir ve ileride oynanış kaldıracı olarak kullanılabilir: çok keşfeden koloni kandırılması zor ama tahmin edilebilirliği düşük, az keşfeden koloni kolay tuzağa düşer.

**Ölçülen değerler:** Keşif oranı 0,35 ve böcek başına hücreye 2 birim feromon ile koloni dağılarak başlar, birkaç dalga tereddüt eder ve 6-8 dalgada tek koridora yerleşir. Sürünün dalga bazında baskın koridor payı: 0,42 → 0,75 → 0,58 → 0,58 → 0,75 → 0,92 → 0,92 → 1,00.

---

## 5. İlk Kesit İçeriği (vertical slice)

İlk hedef, az içerik fakat bitmiş kalitede tek bir tam çalışan kesittir.

### 5.1 Kuleler (5)

| Kule | Sınıf | Rolü |
|---|---|---|
| Reçine Tabancası | Gürültücü | Temel tek hedef hasarı |
| Spor Mantarı | Sessiz | Alan hasarı, zamana yayılı |
| Yapışkan Salgı | Sessiz | Yavaşlatma |
| Sahte Feromon Yayıcı | — | Hasarsız; koridora yapay iz basar |
| Yırtıcı Yuvası | Çok gürültücü | Yüksek hasar, korkuluk rolü |

### 5.2 Düşmanlar (4)

| Düşman | Özelliği |
|---|---|
| İşçi | Temel birim, feromon bırakır |
| Asker | Zırhlı, feromona az uyar (daha inatçı rota) |
| İzci | Hızlı; izi en çok güçlendiren birim |
| Yaban Arısı | Uçar, koridor tanımaz |

**İzci notu:** Öldürülemezse koloni o rotayı ezberler. Bu, oyuncuya doğal bir öncelik hedefi verir ve hedef seçimini mekaniğe bağlar.

### 5.3 Harita

Tek harita tipi, prosedürel varyasyon. 3-4 koridor, tek yuva.

---

## 6. Meta İlerleme

Kalıcı para birimi: **biyokütle.**

Açılabilenler:
- Yeni kuleler
- Yeni harita tipleri
- Hafif kalıcı pasifler (güç artışı sınırlı tutulur; beceri tavanı korunmalıdır)
- **İstihbarat seviyesi:** yükseldikçe oyuncu kolonin hafızasını daha net görür

Meta ilerleme ilk kesitde iskelet halinde bulunur; tam ağaç sonraki aşamada genişletilir.

---

## 7. Mevcut Kod Tabanına Etkisi

**Korunur ve yeniden amaçlanır:** Pathfinder ve MinHeap, üç AI katmanı, bullet/text pooling, TowerManager ve EnemyManager kayıt sistemleri, event tabanlı UI altyapısı.

**Kaldırılır:** 10 sabit level sahnesi, `WaveConfig[]` Inspector dalga planı, mevcut tüm görsel ve ses assetleri, `Tower.cs` içindeki seviye-dizisi tabanlı stat yapısı.

**Eklenir:** ScriptableObject tabanlı veri katmanı, prosedürel harita üretimi, draft sistemi, kalıcı kayıt sistemi, feromon görselleştirme katmanı.

Bu değişikliklerin nasıl uygulanacağı teknik mimari belgesinin konusudur.

---

## 8. Başarı Kriteri

Oynanabilir 15 dalgalık tek run.

**Kriter:** Oyuncu "yem + sessiz kule" kombinasyonunu kendi keşfettiğinde bir aha anı yaşıyor mu?

Yaşamıyorsa mekanik yanlıştır ve içerik eklenmeden önce tasarıma dönülür. Bu kriter, ilk kesitin var oluş sebebidir.

---

## 9. Riskler ve Alınan Kararlar

Aşağıdaki dört risk, mekaniğin temelini ilgilendirdiği için uygulamaya geçmeden karara bağlanmıştır.

### 9.1 Okunabilirlik: gürültücü/sessiz ayrımı anlaşılmazsa mekanik çöker

**Karar: ayrım anlatılmaz, gösterilir.**

- **Görsel dil.** Gürültücü kuleler sürekli genişleyen bir titreşim halkası yayar; halkanın yarıçapı tehdit alanının ta kendisidir. Sessiz kulelerin halkası yoktur, soluk ve gölgeli dururlar. Oyuncu iki sınıfı isimlerinden değil, ekrandan ayırt eder.
- **Yerleştirme ön izlemesi.** Kule elde tutulurken, koridorlardaki feromon izinin o kule konursa nasıl değişeceği hayalet iz olarak gösterilir. Oyuncu sonucu koymadan önce görür.
- **Sessiz keşif anı.** İlk run'ın 2. dalgasında oyuncuya garantili bir gürültücü kule verilir. Böcekler gözle görülür biçimde o koridordan kaçar. Tutorial metni yoktur; olay yaşatılır.
- **Ölçüm.** Oyun testinde tek soru sorulur: "Bu kule düşmanı çeker mi, kaçırır mı?" Doğru cevap oranı düşükse görsel dil yeniden çalışılır.

### 9.2 Yem dengesi: çok güçlüyse tek strateji, çok zayıfsa ölü mekanik

**Karar: yem bir kurulum aracıdır, bir çözüm değil.**

- Yem kulesi hiç hasar vermez. Tek başına hiçbir run kazandırmaz.
- Yem sabit miktarda feromon ekler, oransal değil. Böylece zayıf izleri kolayca yönlendirir ama güçlü, oturmuş bir izi tek başına çeviremez. Doğal sonuç: yem erken oyunda güçlü, geç oyunda destek rolündedir.
- Aynı koridora üst üste yem basmak azalan getiri verir.
- Tuzak koridorunda böcekler öldükçe iz zayıflar, yani başarılı tuzak kendini yer ve tazelenmek ister.
- Tüm denge tek bir sayıya bağlanır: `lureStrength`. Tek parametre üzerinden ayarlanabilir olması, dengelemeyi deneme meselesine indirger.

### 9.3 Nemesis: evrim yeni oyuncuyu bunaltmamalı

**Karar: evrim bir ceza değil, oyuncunun kendi alışkanlığının aynası olsun.**

- Genetik katman ilk 3 run boyunca kapalıdır. Oyuncu önce oyunu öğrenir.
- Evrim hızı oyuncunun kazanma oranına bağlanır. Üst üste kaybeden oyuncuda evrim durur, gerekirse geriler. Baskı yalnızca kazanan oyuncuda artar.
- Koloni yalnızca oyuncunun **en çok kullandığı** stratejiye karşı evrilir. Oyuncu çeşitlenirse baskı kendiliğinden azalır. Böylece sistem cezalandırmaz, çeşitliliği ödüllendirir.
- Evrim görünürdür. Run başında "Koloni şunu öğrendi: ..." kartı gösterilir. Oyuncu neyle karşılaşacağını bilir; sürpriz zorluk değil, okunabilir zorluk.

### 9.4 Prosedürel harita kalitesi: belirsiz topoloji kandırmayı anlamsızlaştırır

**Karar: üretim serbest değil, şablon tabanlı.**

- Elle tasarlanmış 5-6 topoloji şablonu bulunur. Prosedürel olan yalnızca koridor uzunluğu, genişliği, engel yerleşimi ve kurulum şeridi varyasyonudur. Rastgelelik topolojiyi değil, dokuyu üretir.
- Üretilen her harita otomatik doğrulamadan geçer:
  - En az 3 ayrık koridor bulunmalıdır (yol bulma ile doğrulanır).
  - En ucuz iki koridor arasındaki başlangıç maliyet farkı belirlenen eşiğin altında olmalıdır. Aksi halde bir koridor baştan galiptir ve seçim yanılsamaya döner.
  - Her koridorun yanında en az bir kurulum şeridi bulunmalıdır.
- Doğrulamadan geçemeyen harita atılır ve yeniden üretilir. Bu kurallar editör zamanı test olarak yazılır; teknik mimari belgesinde ele alınacaktır.

---

## 10. Çalışma Yöntemi

Bu belgeden sonraki kararlar uygulama sırasında alınacaktır. Sanat yönü, ses ve içerik için ayrı ve daha hafif belgeler yazılacak; her şeyi önceden yazmak yerine çalışan bir ilk kesite doğru ilerlenecektir. Bölüm 9'daki dört karar istisnadır, çünkü mekaniğin temelini belirlerler.
