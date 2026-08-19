# Çekirdek Oyun Tasarımı — Koloni Reboot

**Tarih:** 19 Ağustos 2026
**Durum:** Onaylandı
**Kapsam:** Bu belge yalnızca çekirdek oyun tasarımını kapsar. Teknik mimari, sanat yönü, ses tasarımı ve içerik üretimi ayrı belgelerde ele alınacaktır.

---

## 1. Konumlandırma

**Tek cümle:** Koloni öğrenir, sen yalan söylersin.

Böcek sürüleri yuvaya doğru ilerler ve geçtikleri yola feromon izi bırakır. İşe yarayan rotalar güçlenir, ölümle biten rotalar sönümlenir. Koloni zamanla haritayı ezberler. Oyuncu bu hafızayı okuyabilir ve kirletebilir.

**Platform:** Android, yatay tutuş (gerekçe: bölüm 3.6)
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

### 3.6 Ekran yönü

Yatay tutuş seçilmiştir. Mekanik, 3-4 koridorun aynı anda karşılaştırılabilmesini gerektirir; dikey ekranda koridorlar ya çok dar ya da kadraj dışında kalır ve kandırma kararı okunamaz hale gelir.

---

## 4. Yapay Zeka Katmanları

Mevcut üç sistem korunur, ancak her birine ayrı ve oyuncu tarafından okunabilir bir rol verilir.

| Katman | Rolü | Oyuncuya görünümü | Zaman ölçeği |
|---|---|---|---|
| ThreatMap | Böceğin algıladığı anlık tehlike | Yalnızca gürültücü kuleler yazar | Anlık |
| Q-Learning | Feromon izi, rota tercihi | Parlayan, kalınlaşan izler | Run içi |
| Genetic | Koloni evrimi | Dalga kompozisyonu ve dirençler değişir | Run'lar arası |

Genetik katman böylece nemesis işlevi görür: koloni run'lar boyunca oyuncuya karşı evrilir. Oyuncunun favori stratejisi zamanla aşınır.

---

## 5. Dikey Dilim İçeriği

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

Meta ilerleme dikey dilimde iskelet halinde bulunur; tam ağaç sonraki aşamada genişletilir.

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

Yaşamıyorsa mekanik yanlıştır ve içerik eklenmeden önce tasarıma dönülür. Bu kriter, dikey dilimin var oluş sebebidir.

---

## 9. Açık Riskler

- **Okunabilirlik riski:** Gürültücü/sessiz ayrımı oyuncuya net anlatılamazsa mekanik çöker. İlk oynanış testinin ana ölçüm hedefi budur.
- **Yem dengesi:** Yem kulesi çok güçlü olursa oyun tek stratejiye iner; çok zayıf olursa mekanik hiç kullanılmaz.
- **Nemesis cezalandırması:** Genetik katman yeni oyuncuyu bunaltmamalı; ilk run'larda evrim hızı düşük tutulmalıdır.
- **Prosedürel kalite:** Üretilen haritalar koridor ayrımını her zaman net vermelidir; belirsiz topoloji kandırma kararını anlamsızlaştırır.
