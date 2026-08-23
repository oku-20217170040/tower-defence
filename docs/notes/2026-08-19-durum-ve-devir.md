# Durum ve Devir Notu

**Tarih:** 19 Ağustos 2026 · **son güncelleme 22 Ağustos 2026**
**Branch:** `tasarim/koloni-reboot` (master'a henüz merge edilmedi)
**Test durumu:** 180 EditMode testi, 180 geçiyor (23 Ağu 2026)
**Sıradaki iş:** görsel tarafta **kraliçenin hafıza silme efekti** (oyunun en dramatik anı, şu an hiç görünmüyor) ve **üs görseli**. Denge tarafında üç açık iş: uyarlamalı yerleştirme (Plan 8 Görev 4'ü açar), ekonomi sıkılaştırması, kör birlik. **Plan 6 (görsel) neredeyse bitti, Plan 7 bitti, Plan 8'in ölçülebilir kısmı bitti.** Bkz. `docs/plans/2026-08-20-kule-ve-birim-cesitliligi.md`. Plan 4 tamamen bitti; tek açık kalan Görev 5 Adım 2 (ekonomi arayüzünü kullanıcıya oynatma).

---

## ✔ Çözüldü: 21 Ağustos'un "kalibrasyon bozuldu" alarmı (21 Ağustos 2026)

**Alarm:** kullanıcı run'ı 15/15 kazandı, üs 26/30 (yalnızca **4 sızıntı**), 221 öldürme, 70 besin artmış. Simülatör aynı ayarlarla **21,8 sızıntı** öngörüyordu.

**Sonuç: simülatör doğru, kırık olan modellenen OYUNCU idi.** `EconomyPlanner.Policy.Default()` pahalı pusudan (40) başlıyor ve ilk dalgalara iki kuleyle giriyor; kullanıcı ise ucuz bataryayla pusuyu dönüşümlü alıp mayın tarlasını geciktiriyor. Yalnızca alım sırası değiştirildi, tek bir denge sayısına dokunulmadı:

| Alım sırası (bugünkü ayarlar, 20-40 seed) | Kazanma | Öldürme | Sızıntı | Üs |
|---|---|---|---|---|
| `Default()` — pusu ağırlıklı A,A,B,A,M,A,B | %100 | 203,6 | 21,4 | 8,6 |
| **`Skilled()` — B,A,B,A,M** | %100 | **219,5** | **5,6** | **24,5** |
| yalnızca batarya | %100 | 211 | 13,9 | 16 |
| yalnızca pusu | **%0** | 18 | 30 | 0 |
| ucuz önce B,B,A,M,A,B,A | %30-40 | 89 | 28,7 | 1,3 |
| **Gerçek run (21 Ağu)** | kazandı | **221** | **4** | **26** |

`Skilled()` gerçek run'ı %1 hatayla yeniden üretiyor. Ölçüm `Assets/Frontline/Core/Sim/*` kaynakları başsız bir .NET 8 konsol projesine derlenerek yapıldı — Unity açmadan tarama yapmanın en hızlı yolu, tekrar gerekirse aynı yöntem kullanılabilir.

**Koda geçen:**
- `EconomyPlanner.Policy.Skilled()` — usta oyuncu bandı. `Default()` dokunulmadan bırakıldı, çünkü 20 Ağustos kalibrasyonu ona bağlı. İkisi birlikte bir **bant** tanımlıyor: alt = ortalama oyuncu, üst = usta oyuncu.
- `Tests/EditMode/CurrentRunCalibrationTests.cs` — 21 Ağustos run'ı ikinci referans olarak sabitlendi (3 test). `RunCalibrationTests` eski ayarlara bağlı kalmaya devam ediyor.

**Ders (spec'e taşınmalı):** simülatör ile gerçek arasındaki sapmanın ilk şüphelisi **oyuncu modelidir**, savaş modeli değil.

**Açık kalan denge sorusu:** usta oyun artık üssün 30 canının yalnızca 4'ünü veriyor. Erken oyun kullanıcı geri bildirimiyle açıldı (başlangıç besini 50 → 90) ve zorluk bu kadar düştü. Bir düğme sıkılaştırılacaksa ölçüm hazır; ama bu bir karardır, hata değil.

---

## Oyun nedir

**Ordu öğrenir, sen yalan söylersin.** Dikey tutuşlu Android roguelike tower defense. Birlik sürüleri keşif izi bırakarak yol öğrenir; oyuncu bu hafızayı okuyabilir ve kirletebilir.

Tam tasarım: `docs/specs/2026-08-19-cekirdek-oyun-tasarimi.md`

---

## Mimari

```
Assets/Frontline/
  Core/      Frontline.Core       — saf C#, noEngineReferences, MonoBehaviour YOK
  Runtime/   Frontline.Runtime    — MonoBehaviour'lar, Core'u kullanır
  Editor/    Frontline.Editor     — sahne üretici menü
  Tests/     Frontline.Tests.EditMode — 13 test dosyası
```

**Bu ayrım pazarlıksız.** `Frontline.Core`'a Unity tipi sızarsa denge ölçümlerini başsız koşamayız; bu projede en çok işe yarayan şey oydu.

Sahne kod ile üretilir: Unity menüsünde **Frontline → Sandbox Sahnesini Uret**. Sahne YAML'ini elle düzenleme.

Eski `Assets/Scripts/` hâlâ duruyor, silinmedi.

---

## Ölçüm modeli düzeltildi + gerçek run kalibrasyonu (20 Ağustos 2026)

**Kırık olan:** bütün eski denge simülasyonları birlikleri **sırayla** işliyordu — bir birlik tüm yolu yürüyor, sonra diğeri. O modelde tek hedefli bir kule, aynı anda geçen sekiz birliğin **hepsine** tam DPS veriyor; gerçekte bir seferde tek birliğe ateş eder (`TowerWeapon.FindNearestTarget`).

| 8 birlik, 3 pusu mevzisi, 4 dps | Ölen | Sızan |
|---|---|---|
| Sıralı (eski model) | 8 | 0 |
| Eşzamanlı (`RunSimulator`) | 2 | 6 |

**Kırık OLMAYAN: oyunun dengesi.** İlk çıkarımım "eşzamanlı modelde 12 kule bile kaybediyor, demek oyun kazanılamaz" idi — **yanlıştı.** Sebep: simülasyonda kule sayısını ekonomiden bağımsız düşük tutmuştum. Gerçek oyuncu draft ödülleri ve öldürme geliriyle çok daha fazla kule kuruyor.

**Gerçek run (kullanıcı, 20 Ağu, ekran görüntüsüyle):** 15/15 dalga, KAZANDI, üs 14/30, 209 öldürme, 16 sızıntı, **24 kule** (13 pusu + 7 batarya + 4 mayın tarlası), üstüne 116 besin artmış.

**Kalibrasyon:** aynı 24 kule kurulumu `RunSimulator`'da 223,5 öldürme veriyor (gerçek 209, %7 fark) — simülatör doğrulanmış sayılır. Sızıntıda fark var (sim 1,5 / gerçek 16) çünkü sim kuleleri baştan kurulu kabul ediyor, oyuncu ise dalga dalga kuruyor. **Yani sızıntının neredeyse tamamı erken dalgalarda, kuleler azken oluşuyor.**

**Kalibrasyon tamamlandı (`EconomyPlanner` + `RunCalibrationTests`).** Simülatöre ekonomi, draft ve kulelerin zamanla kurulması eklendi. 10 seed ortalaması, gerçek run'ın yanında:

| | Gerçek run | Simülatör | Fark |
|---|---|---|---|
| Öldürme | 209 | 206,7 | %1 |
| Sızıntı | 16 | 18,3 | %14 |
| Kule | 24 | 27,2 | %13 |
| Artan besin | 116 | 107,7 | %7 |

**Simülatör artık güvenilir bir ölçüm aracıdır.** Denge sayıları bununla yeniden ölçülebilir.

**Açık kalan iş: geç oyun ekonomisi gevşek.** Hedef "8-10. dalgada 4-5 kule" idi; gerçekte run 24 kule ve 116 artan besinle bitiyor. Üs canının yarısı yine de gidiyor — yani zorluk erken dalgalardan geliyor, geç oyun rahat.

Aşağıdaki tablodaki savaş sayıları hâlâ sıralı modelle seçilmiştir; eski üç denge test dosyasının başına da uyarı konuldu. Testler geçiyor ama ölçtükleri şey gerçek oyun değil.

## Harita büyüdü: 11x20 → 11x30 (23 Ağustos 2026)

Harita artık ekrana sığmıyor, **içinde geziliyor** (`Runtime/View/MapCamera.cs`): tek parmakla sürükle-kaydır, iki parmakla / tekerlekle yakınlaştır, kamera harita sınırlarının dışına çıkmaz.

**Boyut tahminle seçilmedi.** Simülatörle 12 seed, usta oyuncu politikasıyla ölçüldü:

| Harita | Kazanma | Öldürme | Sızıntı | Üs |
|---|---|---|---|---|
| 11x20 (eski) | %100 | 221,0 | 4,0 | 26,0 |
| **11x30 (yeni)** | %100 | 219,8 | 5,3 | 24,8 |
| 13x26 | %100 | 218,3 | 6,8 | 23,3 |
| **15x26** | **%0** | **5,7** | **30,0** | **0,0** |
| 15x20 | %25 | 72,3 | 29,5 | 0,5 |

**Ölçümün asıl bulgusu: yükseklik bedava, genişlik pahalı.** Haritayı uzatmak dengeye neredeyse dokunmuyor (üs 26 → 24,8), ama 13'ten geniş yapmak run'ı tamamen çökertiyor — 15 genişlikte öldürme 221'den 5,7'ye düşüyor. Sebebi araştırılmadı; **haritayı genişletmek isteyen önce ölçsün.** Dikey tutuşlu bir oyunda zaten doğru eksen yükseklik: genişlik ekranı belirler, yükseklik gezilir.

**Kaydırma ile kule kurma ayrımı:** ikisi de tek parmak. Basıldığı yerden 12 pikselden fazla uzaklaşan hareket kaydırmadır ve `TowerPlacer` o dokunuşu yok sayar; yerleştirme artık parmak **kalkınca** oluyor. Eşik olmadan haritayı her kaydırdığında istemediğin yere kule kuruluyor.

## Yol çeşitliliği ve yıkılabilir örtü (23 Ağustos 2026)

Kullanıcı referansı: klasik mobil TD ekran görüntüleri (kıvrımlı yollar, taş/ağaç dolu araziler, çukurlara kurulan kuleler).

**1. Yol ortasında çatallanma (`MapTemplate.RungCount`, varsayılan 4).** Koridorlar artık yalnızca üst ve alt banttan değil, yol ortasındaki **yatay geçitlerle** de bağlanıyor. Geçitsiz haritada birlik girişe adım attığı anda yolunu seçmiş olur — yani hiç seçmez; oyuncunun tehdit ve yem ile yaptığı yönlendirme hiçbir şeyi değiştiremez. `MapValidator` artık en az 4 **geçiş hücresi** istiyor ve geçitsiz haritayı reddediyor.

Ölçü tanımı önemli: "üçten fazla yürünebilir komşusu olan hücre" **kullanılamaz** — iki hücre genişliğindeki bir koridorun her hücresinin zaten üç komşusu vardır ve geçitsiz harita da geçer görünür. Doğru ölçü: hiçbir dikey koridora ait olmayan yürünebilir hücreler.

**2. Genişlik 11 → 13.** Bu ölçülmüş üst sınırdır, tahmin değil: 13x26'da run %100 kazanıyor (öldürme 218,3 / sızıntı 6,8 / üs 23,3), **15 genişlikte aynı run tamamen çöküyor** (öldürme 5,7 / sızıntı 30 / kazanma %0). 13'ün üzerine çıkmak isteyen önce ölçsün.

**3. Yıkılabilir örtü (`CellKind.Scenery` + `Core/MapGen/SceneryMap.cs`).** Kaya ve ağaçların **canı var**: kaya 30, ağaç 12. Başta geçilemez ve üzerine kule kurulamaz; kule ateşi üzerlerinden geçerken hasar yiyorlar (`TowerWeapon.ChipSceneryOnLine` — kule örtüye nişan almaz, örtü yalnızca hattın üzerinde olduğu için atışı yer). Can bitince hücre **çukura** dönüyor (`BuildStrip`) ve **oraya mevzi kurulabiliyor**.

Tasarım sonucu: **mevzi sayısı sabit değil, kazanılan bir şey.** Oyuncu ateş gücünü bir dalga boyunca örtü açmaya harcayıp yeni mevzi kazanabilir ya da doğrudan sürüye harcayabilir — bu yeni bir denge kararı.

`Battlefield.DamageScenery`, `SceneryCleared` olayı ve `Runtime/View/SceneryView.cs` (canı azalan örtü kararır ve küçülür, yıkılınca yerine çukur çizilir) bu zincirin geri kalanı. `TowerPlacer` hiç değişmedi: zaten `BuildStrip` sorguladığı için çukur kendiliğinden kurulabilir hücre oldu.

**Durum:** 194 EditMode testi geçiyor (önceki 180 + 14 yeni). Sahne `Frontline → Sandbox Sahnesini Uret` ile yeniden üretildi, play modda doğrulandı: `koridor=3 catal=8 ortu=55`.

**Örtü görselleri Blender ile üretildi** (`Assets/Sprites/Frontline/scenery_tree|rock|crater.png`, 512x512 RGBA). Cycles, ortografik kamera, 32° eğim, saydam zemin. İki ders kayda değer:

- Blender 4'ün varsayılan **AgX** renk dönüşümü sprite'ları soldurup griye çeviriyor. `view_transform = 'Standard'` şart.
- **Kontur kalınlığı çözünürlükle değil, oyundaki piksel boyutuyla ölçeklenir.** 512'de 5 piksellik Freestyle konturu, ~40 piksel görünen sprite'ta yarım piksele düşüp kayboluyor. 14'e çıkarıldı; referans görsellerdeki koyu dış hat ancak öyle okunuyor.

Sahne üreticisi sprite'ları `AssetDatabase` ile yüklüyor, bulunamazsa uyarı verip daireye düşüyor — eksik asset oyunu durdurmamalı. Tint'ler beyaz: sprite kendi rengini taşıyor, hasar karartması yine çalışıyor.

**Doğrulandı (play modda, 23 Ağu):** 59 örtünün 30'u vuruldu → 30'u yıkıldı → 30'u `BuildStrip` oldu → 30 çukur nesnesi çizildi. Zincirin tamamı çalışıyor.

### Görsel giydirme tamamlandı (23 Ağustos 2026)

Blender'dan 15 asset üretildi, hepsi `Assets/Sprites/Frontline/`:

| Ne | Dosya | Not |
|---|---|---|
| Zemin döşemeleri | `tile_corridor`, `tile_buildstrip`, `tile_blocked` | 64x64, ışıksız (Emission), hücre kenarı hafif koyu |
| Örtü | `scenery_tree`, `scenery_rock`, `scenery_crater` | 512x512 |
| Birlikler | `unit_infantry/scout/soldier/blind/brood/queen` | 512x512 |
| Kuleler | `tower_battery/ambush/decoy/minefield/beam` | 512x512, **namlu +Y'ye bakar** |
| Üs | `home_base` | Beton siper, iki lamba kulesi, sarı tehlike şeridi |

Öğrenilen dört şey — tekrar asset üretecek olan okusun:

1. **Blender 4'ün AgX renk dönüşümü** sprite'ları soldurur. `view_transform = 'Standard'` şart.
2. **`default_value` LINEAR alır.** Renk seçicideki sRGB sayıyı doğrudan yazarsan render solar; döşemelerde bu yüzden iki tur döndük.
3. **Kontur kalınlığı oyundaki piksel boyutuyla ölçeklenir**, çözünürlükle değil. 512'de 5 piksellik Freestyle konturu, ~40 piksel görünen sprite'ta kaybolur. 14 kullanıldı.
4. **Namlu yönü `TowerTurret`'e bağlı:** açı `Atan2(y,x) + 90` ile hesaplanıyor, yani sprite'ın "ileri"si +Y. Yanlış eksene modellenirse bütün kuleler hedefe yan bakar.

**Döşeme boyutu 64 seçildi, tercih değil kısıt:** `GridTileRenderer` hücre başına döşemeyi kopyalayıp TEK atlas üretiyor. 13x30 ızgarada 128'lik döşeme 1664x3840 = ~25 MB RGBA verir; Android hedefinde kabul edilemez. 64'te 832x1920 = 6,1 MB (oyunda ölçüldü).

**`ReconColorRamp.MaxAlpha` 0,7 → 0,42.** Bu bir ölçüm değil, görsel karar: harita giydirilince 0,7'lik tavan bütün zemini beyaza boyayıp altındaki sanatı siliyordu. İz artık zeminin üzerine sürülen bir renk, onun yerine geçen bir boya değil. İzin okunabilirliği bozulursa geri alınacak düğme budur.

**Sahnede `ArtLibrary` hiç yoktu** — bu yüzden her birlik ve kule beyaz daireye düşüyordu. Sahne üretici artık kuruyor; tint'lerin alpha'sı 0, yani sprite kendi renkleriyle çiziliyor.

**Hâlâ açık:** atış görselleri (`FxArt` — mermi, namlu çakması, çarpma) atanmadı; hepsi daireye düşüyor.

**Açık kalan:** yeni harita ve örtü mekaniği **simülatörle ölçülmedi.** Çatallanma sürünün koridor entropisini artırır, örtü açmak ateş gücü harcatır; ikisi de dengeye dokunur. Bir sonraki iş `RunSimulator`'a bunları eklemek olmalı — o yapılmadan denge sayıları tahmine düşer.

## Ölçülmüş sabitler — TAHMİNLE DEĞİŞTİRME

Bu sayıların hepsi tarama ile seçildi. Her biri bir testle korunuyor. Değiştirmek isteyen önce ölçmeli.

| Sabit | Değer | Neden bu değer |
|---|---|---|
| Keşif oranı (taban) | **0,12** | 21 Ağu'da 0,35'ten düşürüldü: keşif izciye yüklendi. 0,35'te izci gereksiz kalıyordu (entropi 0,750→0,789), 0,12'de taşıyıcı oluyor (0,428→0,575). Eski 0,35 ölçümü bütün birlikler aynı sapmayı taşırken yapılmıştı |
| Çekim ağırlığı | 3 | 6 iken doymuş iz bir duvar; 2 iken sürü ilk dalgada pes ediyor |
| Tehdit ağırlığı | 3 | — |
| Birlik başına iz | 2 (0,5 yürürken + 1,5 varışta) | — |
| Mayın tarlası yavaşlatma | x2 | x1,5 ölü mekanik (%20 kazanma), x3 oyunu çözüyor (sızıntı 0). Tehdit yazarsa %95 → %5 |
| Batarya kule | **alan hasarı, 2,5 dps** | Alan hasarı ~4 kat değer katıyor; 2,5 splash = 10 tek hedefli. 4 dps oyunu kolaylaştırıyor, 1,5 ölü |
| Pusu mevzisi DPS | 4 | 3'te hiç öldürmüyor, 4'te hepsini öldürüyor — eşik keskin |
| Batarya kule DPS | 10 | Önünden geçen kalmadığı için gerçek DPS'i çok daha düşük |
| Birlik canı | 10 × (1 + **0,10** × (dalga−1)) | 0,15'te ekonominin hiçbir düğmesi çalışmıyordu: yetersiz kule → az öldürme → az gelir → çöküş sarmalı. 0,10'da eşik yumuşadı |
| İlk dalga birlik | 8 | 10 iken oyuncu toparlanamadan 4. dalgada kaybediyordu |
| Dalga temposu | ilk dalga öncesi 8 sn, ilk 4 dalga arası 6 sn, sonra 2 sn | Erken oyunda düşünme süresi zorluktan değerli; oyuncu ilk kulelerini kurmadan boğulmamalı |
| Harita | **11 x 30**, 3 koridor | Yükseklik dengeye dokunmuyor (üs 26,0 → 24,8), genişlik çökertiyor: 15x26'da öldürme 5,7 / sızıntı 30. 23 Ağu ölçümü |
| Üs canı | 30 | Zayıf düğme: 50 yapmak kayıp dalgasını 10'dan 11'e taşıdı |
| Başlangıç besini | **90** | 50'de ilk dalgada YALNIZCA 1 kule kurulabiliyordu (3 koridor varken) — oyuncu geri bildirimi doğrulandı. Dalga geliri 6, öldürme geliri 1 ile birlikte: erken oyun açılıyor, toplam zorluk aynı kalıyor |
| Işın | **3 hasar / 2 sn, nişan alan delici hat, menzil 40** | Nişan aldığı için ıskalamıyor. hasar 20/aralık 1 → tek başına oyunu çözüyor; 3/2 sn'de karışımda değerli (20,7 → 11,9) ama tek başına işe yaramıyor (30,0) |
| Işın hat kalınlığı | 0,6 hücre (yarı) | Kalınlaşırsa koridoru baştan sona kaplar ve spec 3.5'i siler |
| Kule ölçeği | 1,0 (hücre boyutu) | 0,8'de kuleler küçük duruyordu (oyuncu geri bildirimi) |
| Kule fiyatları | Batarya **20** · Yem/Mayın tarlası **26** · **Işın 34** · Pusu **40**, aynı türde +5 | 20 Ağu taraması: +10 artış run'ı öldürüyordu (kazanma %10 → +5'te %85) |

**Ölçülen eğri (20 Ağu):** optimal oyuncu run'ların %85'ini kazanıyor, 8. dalgada ortalama 5,6 kule, üs canının 30'da 22'si gidiyor. Eski 3/5/6/9 hedefi ölçümde hiç kazanmıyordu — geçersiz.

> **Testlerin kör noktası (20 Ağu 2026'da acı bir şekilde öğrenildi):** Denge testleri saf modeli simüle eder, **sahnedeki zinciri değil.** Üsya varan birliğin can eksiltmesi hiç kodlanmamıştı; 104 test yeşilken oyunda üs canı düşmüyordu. Simülasyon "sızıntı 22/30" derken gerçek oyunda sıfırdı. Runtime zinciri (spawn → yürüme → varış → hasar → run sonu) yalnızca oynayarak doğrulanır. Yeni bir runtime davranışı eklerken buna dikkat.

---

## Ölçümle bulunan üç tasarım gerçeği

Bunlar tasarımda yoktu; ancak ölçünce ortaya çıktılar ve spec'e işlendiler.

**1. Keşif olmadan öğrenme yok (spec 4.1).** A* belirlenimci, koridorlar eşit maliyette — her birlik aynı koridoru seçiyordu. Her birliğe kendine özgü yön tercihi verilerek çözüldü. Sapma **sütun bazında** üretilir; hücre bazında üretilirse yol boyunca ortalamaya gidip söner.

**2. Tek araç yetmiyor (spec 3.4).** Batarya kule tek başına sürüyü sarsıyor ama ordu geri dönüyor. Yem tek başına hiç etki etmiyor. Yalnızca ikisi birlikte sürüyü taşıyor. Oyunun aha anı bu.

**3. Korunmayan koridor her zaman bulunur (spec 3.5).** Bütün kuleleri tek koridora yığmak işe yaramıyor; ordu orada ölmeye başlayınca izi terk edip korumasız koridordan akıyor. Kuleler üç koridora dağıtılmalı.

---

## Çalışma yöntemi — bu projede işe yarayan disiplin

1. **Denge sayıları tahminle seçilmez.** Önce `execute_code` ile tarama yapılır, kazanan değer koda geçer, sonra o davranışı koruyan test yazılır. Bu yöntem şimdiye kadar üç kez tasarım hatası yakaladı.
2. **TDD:** test yaz → Unity konsolunda başarısızlığı gör → implementasyon → testler yeşil → commit.
3. **Görsel doğrulama kullanıcıya aittir.** Unity play modu pencere odağı gerektiriyor; MCP üzerinden çalışırken oyun ilerlemiyor (`frame=1`de donuyor). Ölçülebilir her şey EditMode testine, görsel olan her şey kullanıcıya.
4. Alternatif görsel doğrulama: dokuyu başsız üretip PNG kaydetmek işe yarıyor (`overlay_preview.png` böyle üretildi).

---

## Görsel üretim başladı — 22 Ağustos

Sanat yönü: **sade askeri silüet, düşük poli** (kullanıcı kararı). Üreteçler repoda ve tekrarlanabilir: `tools/blender/units.py`, `towers.py`, `tiles.py` — Blender'da `exec` edilir.

**En önemli ders — silüet, anatomi değil.** İlk iki birlik denemesi anatomik askerdi (omuz + kask + tüfek) ve 30 pikselde blob oluyordu. Gövde **yön belirten bir kamaya** çevrilince okunabilirlik geldi. Telefonda bir birlik ~30 piksel; o ölçekte okunan şey dış hattır.

**Tür ayrımı üç kanaldan birden:** silüet + vurgu rengi + ölçek. Üçü aynı anda çalışır, renk körlüğünde bile tür ayırt edilir.

**Görsel dil kuralı modele taşındı (spec 9.1): yükseklik bir süs değil, bilgi.** Batarya yüksek taretli ve gürültülü (tehdit yazar), pusu alçak ve gömülü, mayın tarlası tamamen yassı (dikey yapı yok). Ordunun görebildiği kule ile göremediği kule bakışta ayrılıyor.

**Bağlama noktası `ArtLibrary`:** sprite atanmamış her tür otomatik eski daireye düşer. Asset'ler parça parça gelebilir, oyun hiçbir anda bozulmaz. Görsel katman oyunun çalışması için zorunlu değil.

Ayrıntı: `docs/plans/2026-08-21-gorsel-uretim.md`

---

## Plan durumu

| Plan | Durum |
|---|---|
| Plan 1 — Ordu çekirdeği | **Bitti.** Keşif izi, yem, tehdit, rota modeli, harita üretimi |
| Plan 2 — Görselleştirme | **Bitti.** Mekanik oyuncu tarafından doğrulandı |
| Plan 3 — Savaş | **Bitti.** Can, hasar, üs, run akışı, arayüz, zorluk eğrisi |
| Plan 4 — Ekonomi ve draft | **Bitti.** Ekonomi, draft, eğri ölçümü. Açık: Görev 5 Adım 2 (kullanıcı testi) |
| Plan 7 — Kule sınıfları (Mayın tarlası, Batarya alan hasarı, Işın) | **Bitti** (21 Ağu). Saat kulesi ölçümle elendi |
| Plan 8 — Birlik türleri (izci, asker, kör, kuluçka, kraliçe) | Karar alındı, plan yazılmadı |
| Plan 5 — Meta ilerleme, nemesis | Yazılmadı |
| Plan 6 — Blender görselleri | **Neredeyse bitti** (22 Ağu). 4 birlik + 5 kule + 3 döşeme + 8 atış efekti + üs üretildi ve bağlandı; dönme, mermi, namlu çakması, çarpma patlaması, elektrik ışını ve üs vurulması çalışıyor. Kalan: kraliçe efekti, ses |

---

## Sıradaki iş: Plan 4 Görev 3 — dalga sonu draft

Plan: `docs/plans/2026-08-19-ordu-ekonomi.md`

Kritik tasarım noktası (spec 2.2): sunulan 3 seçenek rastgele **değil**, ordunun o anki davranışına göre ağırlıklandırılır. Sol koridor ezberlenmişse oraya uygun seçeneklerin çıkma olasılığı artar. Bu, adaptif sistemi oynanışa bağlayan ikinci kanal ve draft'ı sıradan bir kart seçiminden ayıran şey.

Dosyalar: `Core/Draft/DraftOption.cs`, `Core/Draft/DraftGenerator.cs`, `Runtime/View/DraftView.cs`, `Tests/EditMode/DraftGeneratorTests.cs`

---

## Devredilen ve bekleyen işler

- **Hayalet iz ön izlemesi** (Plan 2 Görev 5): kule elde tutulurken rotanın nasıl değişeceğini göstermek. Yapılmadı, mekanik onsuz da anlaşıldı. Okunabilirlik sorun çıkarırsa öne alınmalı.
- **Yem tazelemenin maliyeti:** Sahte hedef her dalga bedava tazeleniyor. Spec 3.3 "yem sürekli beslenmeli" diyor ama besleme otomatikse ortada karar kalmıyor. Ekonomiyle birlikte karara bağlanmalı.
- **Birliklerin üst üste binmesi:** kozmetik, gerçek asset'lerle çözülecek.
- **Eski `Assets/Scripts/` silinmesi:** yeni sistem eskisinin yerini tamamen aldığında.

---

## Ortam

- Unity 6000.3.21f1, sahne `Assets/Scenes/Frontline_Sandbox.unity`
- Unity MCP ve Blender MCP kullanıcı seviyesinde tanımlı, ikisi de bağlanabiliyor
- Blender bağlantısı: 3D görünümde **N** → **BlenderMCP** sekmesi → **Connect to Claude** (port 9876)
- Unity MCP portu 8080 — Brick Havoc projesiyle paylaşılıyor, aynı anda tek Unity açık tutulmalı
- Oyun kontrolleri: **1** batarya, **2** pusu, **3** yem, **4** mayın tarlası, **5** ışın; sol tık kurulum şeridine yerleştirir
- **Play Mode açıkken kod derlemeyin.** Domain reload, serialize edilmeyen alanları (`EconomyRunner.Economy` gibi) null bırakıyor ve konsol sahte `NullReferenceException` + "referenced script missing" hatalarıyla doluyor. Bir kez yarım saat kaybettirdi; hatalar gerçek değildi. Play'den çıkıp konsolu temizleyince sıfır hata.
