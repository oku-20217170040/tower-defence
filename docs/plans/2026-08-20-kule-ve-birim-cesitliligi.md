# Kule Sınıfları ve Birlik Türleri — Plan 7 (taslak)

**Tarih:** 20 Ağustos 2026
**Durum:** Karar alındı, uygulama Plan 4 Görev 4 (ekonomi eğrisi) bittikten sonra başlar.

## Karar

Klasik TD roster'ı (okçu/topçu/buz/lazer, piyade/hızlı/tank/uçan/bölünen/boss) **olduğu gibi alınmadı**. Sebep: o roster'ın ekseni hasar tipi ve can-hız matrisidir; bu oyunun ekseni **ordunun görüp göremediğidir** (spec 3.1-3.2). İki eksen üst üste binerse ölçülmüş üç tasarım gerçeği (spec 3.4 tek araç yetmiyor, 3.5 korunmayan koridor bulunur) ezilir.

Roster, mevcut eksene uyarlanarak alındı.

## Kuleler

| Sınıf | Durum | Rol |
|---|---|---|
| Pusu | Var (4 dps) | Tek hedef, sürekli, tehdit yazmaz. Asıl katil. |
| Batarya / topçu | **Yapıldı** (20 Ağu) | **Alan hasarı, 2,5 dps.** Menzildeki tüm birliklere aynı anda vurur. Yüksek tehdit yazımı; yönlendirme aracı. |
| Yem | Var | Pazarlıksız. Spec 3.4'ün "aha anı" ona bağlı. |
| **Mayın tarlası** | **Yapıldı** (20 Ağu) | Yavaşlatır (x2), tehdit yazmaz, fiyat 16. Birlik pusu mevzisinin menzilinde uzun kalır. Tek başına değersiz. |
| **Işın** | **Yapıldı** (21 Ağu) | **Periyodik yatay hat.** 1 sn'de bir ateş eder, o anda kendi SATIRI üzerindeki herkese 20 hasar. Kalabalığı ve yavaşlatılmış birliği cezalandırır. |

**Saat (clock) kulesi — eklenmedi (20 Ağustos kararı).** Kısa süre düşünüldü, sonra çıkarıldı: Mayın tarlası zaten yavaşlatmayı sağlıyor. Mayın tarlası yerel ve kalıcı (menzilindeki birlik yavaş ilerler, bir *yerleştirme* kararıdır), Saat global ve anlık olacaktı (bir *zamanlama* kararı). Farklı eksende olsalar da oyuncunun kafasında ikisi aynı yere düşer ve roster'ı kalabalıklaştırır. Anlık müdahale aracı ilerde gerekirse Plan 5'te (meta ilerleme) yeniden değerlendirilir.

**Yükseltme:** Draft zaten kalıcı hasar/menzil pasifi veriyor. Kule başına ayrı yükseltme ekonomiye üçüncü para deliği açar; Görev 4 eğrisi kilitlenmeden eklenmeyecek. Eklenirse en fazla 2 seviye.

## Birlikler

Bir birlik türü keşif izi/öğrenme ile etkileşmiyorsa yalnızca bir can rakamıdır. Her tür bir öğrenme kanalına bağlandı.

| Tür | Davranış |
|---|---|
| Piyade | Mevcut birlik. İz bırakır. |
| **İzci** (hızlı) | Az can, yüksek hız, iz bırakır. Ordu onun sayesinde öğrenir; oyuncu onu öldürerek öğrenmeyi yavaşlatabilir. |
| **Asker** (tank) | Yavaş, çok canlı, **izi çok güçlü basar**. Doymuş iz duvardır — geçtiği koridoru orduya çiviler. |
| **Kör birlik** (uçan yerine) | Yerde yürür ama keşif izine ve yeme **hiç bakmaz**; en kısa yolu seçer. Karar: gerçek uçan düşman eklenmedi, çünkü yalan söyleme kanalını tümüyle kapatırdı. |
| **Kuluçka** (bölünen) | Ölünce ikiye ayrılır ve ölüm yerine güçlü ölüm-izi bırakır. Oyuncunun kendi tuzağı orduya "buradan gitme" diye işaretlenir. |
| **Kraliçe** (boss) | Tehdit alanını temizler — ordunun korkusunu siler, öğrenmeyi resetler. |

## Mayın tarlası ölçümü — 20 Ağustos, TAMAMLANDI

**Seçilen değerler:** yavaşlatma çarpanı **x2** (yarım hız), tehdit **yazmaz**, fiyat 16.

Ölçüm sabit kule bütçesiyle yapıldı (ekonomi devre dışı, kuleler baştan kurulu), 20 seed. 6 kule bütçesi, 3 pusu + 3 mayın tarlası:

| Yavaşlatma | Kazanma | Ort. sızıntı | Sonuç |
|---|---|---|---|
| x1,5 | %20 | 28,8 | Mayın tarlası ölü mekanik — aldığı pusu mevzisinin yerini dolduramıyor |
| **x2** | **%95** | **17,5** | **Seçilen.** Saf pusule (%100 / 12,7) başa baş → gerçek seçim |
| x3 | %100 | 0,0 | Baskın strateji; sızıntıyı sıfırlıyor, oyunu çözüyor |
| x2 + tehdit yazar | %5 | 29,8 | Ordu kaçıyor, yavaşlatma hiçbir birliğe değmiyor |

**Bulgu 1 — eşik yine keskin.** x1,5 ölü, x3 oyunu çözüyor, arada tek çalışan değer var. Bu projede üçüncü kez oluyor (pusu mevzisi DPS 3/4, keşif oranı 0/0,35/0,5).

**Bulgu 2 — mayın tarlası tehdit yazmamalı, ve bu ölçüldü.** Yazınca kazanma %95'ten %5'e düşüyor: ordu mayın tarlasıden kaçıyor, yavaşlatma hiç kimseye değmiyor. `SlowField`'ın `ThreatField`'dan ayrı bir sınıf olmasının gerekçesi budur — algılanmayan bir etki, algılanan bir etkiden yapısal olarak farklıdır.

**Bulgu 3 — önceki taramanın "1/3 çöküyor" gizemi çözüldü.** Sorun mayın tarlasınin savaş değeri değilmiş: sabit bütçede 6 pusu + 3 mayın tarlası %100 kazanıyor, ekonomiyle birlikte %0'dı. Sebep fiyat artışının (`costStep`) tür başına ayrı sayılması — karışım yapan oyuncu her iki türde de düşük sayıda kaldığı için ekonomi ona beklenmedik biçimde davranıyor. **Açık iş:** mayın tarlası artık oyunda olduğuna göre ekonomi eğrisi (Plan 4 Görev 4) karışımlı oyuncu politikasıyla yeniden ölçülmeli.

**Uygulandı:** `Core/Fields/SlowField.cs`, `TowerKind.Minefield`, `Battlefield.RebuildSlow()`, `UnitAgent` hız bölümü, 4 tuşu, fiyat 16, draft seçeneklerine dahil. `MinefieldTests` (5 test) ve `SlowFieldTests` (7 test) kararları kilitliyor.

## Işın ölçümü — 21 Ağustos, TAMAMLANDI

**Seçilen değerler:** atış başına **20 hasar**, atış aralığı **1 sn**, hat **yatay** ve 1 hücre kalınlığında (bütün koridorları keser), tehdit **yazmaz**, fiyat **34**.

### Işın ilk tasarımıyla oyunu bozdu — ve sebep sayısal değildi

Plandaki tanım "delip geçen **sürekli** hat hasarı" idi. Her ayarda ya ölü ya baskın çıktı: dps 1-40, genişlik 0-4, menzil 6-20, yatay hat, dikey hat, tehdit yazan sürüm — hiçbiri arada bir bant bulmadı. Karışımda bataryadan kötü, tek başına ise oyunu çözüyordu.

Sebebi bulmak için bedava draft kulesi **tamamen kapatılıp** ölçüldü (20 seed):

| Bedava kule yok | Kazanma | Sızıntı | Kule |
|---|---|---|---|
| Skilled (usta karışım) | %0 | 30 | 4,0 |
| batarya spam | %0 | 30 | 4,4 |
| pusu spam | %0 | 30 | 2,0 |
| **sürekli ışın spam** | **%100** | **7,5** | **7,0** |

Her strateji çökerken **7 sürekli ışın oyunu tek başına kazanıyordu.** Sebep: bir koridoru baştan sona kaplayan şey spec 3.5'i ("korunmayan koridor her zaman bulunur") siler — ve o gerçek oyunun omurgası. Tehdit yazdırmak da çözmedi (spam sızıntısı 1,9 → 2,6): her koridorda ışın varken ordunun kaçacak koridoru kalmıyor.

### Çözüm: sürekli değil, örnekleyen silah

Işın periyodik ateş eder ve hattı **1 hücre kalınlığındadır**. Artık bir koridoru kapatmaz; *örnekler*. Hızlı birlik iki atış arasında karşıya geçebilir, yavaşlatılmış ya da sıra olmuş kalabalık tam yer.

| Kurulum (20 seed, bugünkü denge) | Kazanma | Sızıntı | Üs |
|---|---|---|---|
| Taban: B,A,B,**Pusu**,M | %100 | 5,6 | 24,5 |
| **B,A,B,Işın,M** (pusu yerine ışın) | %100 | **5,3** | 24,7 |
| batarya spam | %100 | 14,4 | 15,6 |
| **ışın spam** | %95 | **15,4** | 14,6 |
| ışın spam, aralık 0,5 sn | %100 | 5,5 | 24,5 |

Karışımda pusuyla **başa baş** (5,3 / 5,6) → gerçek bir seçim. Spam'de bataryayla **başa baş** (15,4 / 14,4) → baskın değil.

**Bulgu 1 — denge düğmesi hasar değil, ATIŞ ARALIĞI.** Karışımda hasar 10 ile 45 arası tamamen aynı sonucu veriyor (5,3). Aralık 1 sn → 0,5 sn yapılınca ışın spam'i 15,4'ten 5,5'e düşüyor. Hasar seçimi (20) yalnızca spam davranışından belirlendi.

**Bulgu 2 — mayın tarlası sinerjisi ölçüldü ve gerçek.** Işın + mayın tarlası 17,1 sızıntı, ışın + pusu 21,8. Yavaşlatılan birlik hatta daha uzun kalıyor. Spec 3.4'ün "tek araç yetmiyor" gerçeği ışın için de geçerli.

**Bulgu 3 — fiyat ışın için ZAYIF bir düğme.** 20 ile 50 arası sızıntıyı yalnızca 15,3'ten 15,9'a taşıyor, çünkü kulelerin çoğu draft'tan bedava geliyor. 34 seçildi (mayın tarlası 26 ile pusu 40 arasında). Ekonomi sıkıldığında yeniden ölçülmeli.

**Uygulandı:** `TowerKind.Beam`, `RunSimulator.Targeting.Beam` (periyodik atış sayacı), `EconomyPlanner` ışın alanları, `TowerWeapon.BindBeam/UpdateBeam`, `TowerPlacer` 5 tuşu + renk, `EconomyRunner.beamCost`, draft seçenekleri, HUD. `BeamTowerTests` (5 test) kararları kilitliyor.

**Runtime doğrulaması yapıldı (21 Ağustos, kullanıcı run'ı, ekran görüntüsüyle):** ışın ağırlıklı bir kurulumla 15/15 dalga KAZANILDI, üs 24/30, 219 öldürme, 6 sızıntı. Yani 5 tuşu → yerleştirme → periyodik atış → hasar zinciri gerçekten çalışıyor. Simülatör aynı bandı söylüyordu (ışınlı karışım: 5,3 sızıntı, üs 24,7) — gerçek 6 sızıntı / üs 24. **Bu, ışın için ayrı bir kalibrasyon noktasıdır.**

## Sıra

1. Plan 4 Görev 4 — ekonomi eğrisi ölçülüp kilitlenir (**önce bu**)
2. Plan 7 Görev 1 — ~~Mayın tarlası~~ **bitti**, ~~Batarya alan hasarı~~ **bitti**, ~~Işın~~ **bitti (21 Ağu)**. **Plan 7 tamamlandı.**
3. ~~Topçu~~ **bitti.** Ölçülen DPS eğrisi (10 seed, kalibre simülatör): Splash 1,5 → sızıntı 29,3 (%20 kazanma); **2,5 → 20,6 (seçilen)**; 4 → 12,2; 10 → 10,0 (doygunluk). Tek hedefli 10 dps 18,3 veriyordu — yani alan hasarı ~4 kat değer katıyor ve 2,5 dps eski zorluğu koruyor. Topçu bir güç artışı değil, karakter değişimi.
4. **Sırada: ekonomiyi sık** (kullanıcı kararı, 20 Ağu). Hedef 8-10. dalgada 6-8 kule; şu an 14,5. Kalibre simülatörle taranacak.
5. ~~**Işın için uyarı:** simülasyon eşzamanlı hale getirilmeli.~~ **Yapıldı** (`RunSimulator`, 20 Ağu) — ışın onun üstünde ölçüldü.
3. Plan 8 — birlik türleri, her biri kendi öğrenme testiyle
