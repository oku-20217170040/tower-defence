# Birlik Türleri — Plan 8

**Tarih:** 21 Ağustos 2026
**Durum:** Plan yazıldı, uygulama başlıyor.
**Ön koşul:** Plan 7 bitti (pusu, batarya, yem, mayın tarlası, ışın).

## Pazarlıksız kural

> **Bir birlik türü keşif izi/öğrenme ile etkileşmiyorsa, yalnızca bir can rakamıdır.**

Plan 7'de aynı kural kuleler için uygulandı ve iki tasarımı eledi (Saat kulesi, sürekli Işın). Burada da her tür bir **öğrenme kanalına** bağlanır; bağlanamayan tür eklenmez.

Klasik TD roster'ı (hızlı/tank/uçan/bölünen/boss) olduğu gibi alınmadı; bu oyunun ekseni **ordunun görüp göremediğidir** (spec 3.1-3.2), can-hız matrisi değil.

## Türler ve bağlandıkları kanal

| Tür | Öğrenme kanalı | Oyuncuya sorduğu soru |
|---|---|---|
| **Piyade** | Mevcut taban. İz bırakır. | — |
| **İzci** | **Keşif üretir.** Yüksek sapma ile yeni koridor dener; ordu onun sayesinde öğrenir. | Öğrenmeyi yavaşlatmak için onu öncelikli öldürecek misin? |
| **Asker** | **İzi doyurur.** Vardığında rotayı çok güçlü pekiştirir — geçtiği koridoru orduya çiviler. | Geçmesine izin verip koridoru sabitlemek mi, durdurup ordunun aramaya devam etmesini göze almak mı? |
| **Kör birlik** | **Hiçbirine bakmaz.** Keşif izini ve yemi yok sayar, en kısa yolu tutar. | Yem stratejin ona işlemez; ne yapacaksın? |
| **Kuluçka** | **Ölüm izini yazar.** Ölünce ikiye ayrılır ve öldüğü yere güçlü ölüm-izi basar. | Kendi tuzağın orduya "buradan gitme" diye işaretlenir — tuzağını nerede kuracaksın? |
| **Kraliçe** | **Tehdit alanını siler.** Ordunun korkusunu sıfırlar, öğrenmeyi resetler. | Batarya ile kurduğun yönlendirme çöker; yeniden mi kuracaksın? |

**Kör birlik, gerçek uçan düşmanın yerine geçer (20 Ağu kararı).** Uçan düşman yalan söyleme kanalını tümüyle kapatırdı; kör birlik yalnızca *yem* kanalını kapatır, tehdit kanalı çalışmaya devam eder.

## Görevler

Sıra rastgele değil: **ölçüm aracı olmadan tür seçilemez**, o yüzden Görev 1 önce gelir. Her tür için yöntem aynı: önce başsız tarama, kazanan değer koda, sonra o kararı koruyan test (bkz. devir notu "Çalışma yöntemi").

### Görev 1 — `UnitProfile` ve dalga bileşimi (ölçüm altyapısı)

`RunSimulator` şu an tek bir birlik profili biliyor. Türler ölçülemeden seçilemez.

- `Core/Units/UnitProfile.cs`: `HpMultiplier`, `SpeedMultiplier`, `DepositMultiplier`, `ReinforceMultiplier`, `WeakenMultiplier`, `IgnoresRecon`, `IgnoresDecoy`, `ExplorationNoise` (override), `SplitOnDeath`, `ClearsThreat`.
- `RunSimulator.Settings`: dalga başına **bileşim** (`WaveComposition` — hangi türden kaç tane).
- `RunSimulator.Unit`: profilini taşır; `Advance`, `Fire` ve ölüm/varış yolları profili uygular.
- **Varsayılan bileşim yalnızca piyadedir.** Bu şart: `RunCalibrationTests` ve `CurrentRunCalibrationTests` bugünkü referans run'lara bağlı; varsayılan değişirse ikisi de sessizce yanlış şeyi ölçer.

**Bitti sayılır:** mevcut 147 test yeşil kalır ve yeni bir profille koşulan run ölçülebilir.

### Görev 2 — İzci — **TAMAMLANDI (21 Ağustos)**

**Seçilen:** izci payı **%25**, sapma **0,7**, can **x0,5**, hız **x1,6**. Ve birlikte alınan karar: **taban keşif sapması 0,35 → 0,12.**

Ölçüm metriği sızıntı değil, **dalga başına koridor entropisi** (`RunResult.RouteEntropy`). Sızıntı izciyi hiç görmüyor.

Planın öngördüğü risk gerçekleşti: taban 0,35'te bırakılınca izci işe yaramıyor.

| Taban sapma | İzci yok | İzci %25 | İzci %50 |
|---|---|---|---|
| 0,35 (eski) | 0,750 | ~0,77 | 0,789 |
| **0,12 (seçilen)** | **0,428** | **0,575** | 0,694 |

0,35'te izcinin katkısı 0,04 — Plan 8'in pazarlıksız kuralına göre "yalnızca bir can rakamı", yani eklenmemesi gerekirdi. 0,12'de ordu izcisiz saplanıp kalıyor, izciyle keşfe dönüyor: **izci keşfi taşıyan birlik oldu** ve "izciyi öldürerek orduyu kör bırakma" kararı gerçek oldu.

0,35 ölçülmüş bir sabitti, ama o ölçüm **bütün birlikler aynı sapmayı taşırken** yapılmıştı; tür sistemi o varsayımı kaldırdı. İki kalibrasyon dosyası (20 ve 21 Ağustos run'ları) taban sapmayı kendi içinde 0,35'e **sabitler** — o run'lar öyle oynandı.

**Sapma zayıf düğme:** izci sapması 0,5 ile 0,85 arası entropiyi yalnızca 0,552'den 0,584'e taşıyor. Pay ise güçlü: %50'de oyun kolaylaşıyor (sızıntı 4,1 → 2,3), çünkü izci kırılgan.

**Yan bulgu — ordu YERLEŞMİYOR, dağılıyor.** Eski tooltip "ordu dağılarak başlar ve 6-8 dalgada tek koridora yerleşir" diyordu. Ekonomiyle ölçülünce tersi çıktı: erken dalgalar 0,329 → geç dalgalar 0,619. Sebep spec 3.5: kuleler dikildikçe ordu korunmayan koridoru aramaya devam ediyor. `ScoutTests` bunu kayda geçirdi.

**Yan etki — ışın yeniden ölçüldü.** Taban sapma düşünce ışının dengesi kaydı: izcisiz orduda ışın spam'i 12,9 sızıntı veriyor (batarya spam'i 20,9) — yani **baskın** çıkıyor. İzciler orduyu dağıttığı için canlı koşulda 18,7'ye dönüyor (batarya 17,3) ve denge korunuyor. Işın dar bir hatta vurduğu için değeri ordunun ne kadar toplandığına bağlı: **keşif ile ışın doğrudan bağlı.** `BeamTowerTests` artık canlı bileşimi ölçüyor.

**Uygulandı:** `UnitProfile.For(Scout)`, `UnitRoster.Default()` (simülatör ve runtime aynı listeyi kullanır), `UnitAgent.profile`, `UnitSpawner` bileşim seçimi + izcinin görsel ayrımı (küçük, açık mavi). `ScoutTests` (5 test).

> **Doğrulanmamış kalan:** izcinin runtime'da gerçekten hızlı/kırılgan göründüğü ve ordunun izcisiz saplanması — **oynayarak doğrulanmalı.**

### Görev 3 — Asker

Ölçülecek: can çarpanı, hız çarpanı, `ReinforceMultiplier`.

Aranan davranış: askerin geçtiği koridor sonraki 2-3 dalga boyunca ordunun tercihi olmalı. Ölçüm metriği: askerin vardığı koridorun sonraki dalgalardaki payı.

Bilinen eşik riski: iz doyması. Ölçülmüş sabit "çekim ağırlığı 3; 6 iken doymuş iz bir duvar". Asker doğrudan buraya bastığı için **askerin pekiştirmesi 6'ya denk gelen bir doyma üretebilir** — yani asker tek başına oyunu çözebilir. Işın'da olan tam buydu; aynı tuzak burada da var.

### Görev 4 — Kör birlik

Ölçülecek: oranı ve can çarpanı. Davranış ikili (bakar/bakmaz), ayarlanacak sayı yok.

Aranan davranış: yem ağırlıklı oyuncunun sızıntısı artmalı, tehdit (batarya) ağırlıklı oyuncununki artmamalı. Ölçüm: iki politika (yem ağırlıklı / batarya ağırlıklı) kör birlik oranına karşı taranır.

Kabul edilmez sonuç: kör birlik yemi tümüyle işe yaramaz hale getirirse. Yem spec 3.4'ün "aha anı"na bağlı ve pazarlıksız; kör birliğin oranı bunu bozamayacak kadar düşük olmalı.

### Görev 5 — Kuluçka

Ölçülecek: bölünme sayısı (2), yavruların can/hız çarpanı, `WeakenMultiplier`.

Aranan davranış: oyuncunun en çok öldürdüğü koridor, ordunun *kaçındığı* koridor haline gelmeli — yani tuzak kendini bozmalı. Ölçüm metriği: öldürme yoğunluğu ile sonraki dalga koridor payı arasındaki ilişki.

Bu, oyunun tersine çevrilmiş sezgisini (spec 3.2) doğrudan besleyen tür; muhtemelen en değerlisi.

### Görev 6 — Kraliçe

Ölçülecek: hangi dalgalarda çıkacağı, can çarpanı, tehdit silmenin kapsamı (tümü mü, menzil içi mi) ve kalıcılığı.

Aranan davranış: kraliçe dalgasından sonra oyuncunun yönlendirmeyi *yeniden kurması* gerekmeli — ama kurabilmeli. Ölçüm: kraliçe sonrası 2 dalgada sızıntı sıçraması.

Kabul edilmez sonuç: tehdidin tamamen ve kalıcı silinmesi. Ölçülmüş sabit "batarya tehdit gücü 4, doymuş bir izi 5-6 dalgada kırar" — sıfırlama bu yatırımı bir anda çöpe atarsa batarya ölü kule olur.

### Görev 7 — Runtime ve görsel ayrım

- `UnitAgent` profil alır; `UnitSpawner` dalga bileşimini kurar.
- Türler **bakışta ayırt edilebilmeli** (boyut/renk). Ayırt edilemeyen tür oyuncu için yok demektir.
- **Kullanıcı doğrulaması şart.** Bu projede ölçülebilir her şey EditMode testinde, zamanlamalı/görsel her şey oyuncuda (Işın'da da böyle yapıldı).

## Bu planın bilinen riski

Her yeni tür **zorluğu artırır** ve bugünkü denge zaten gevşek: 21 Ağustos run'ı 15/15 kazanıldı, üs 24-26/30. Yani türler eklendikçe oyun kendiliğinden sıkılaşacak. Bu iyi haber, ama **denge sayıları bu plan bitmeden yeniden ayarlanmamalı** — yoksa iki kez üst üste ayar yapılır ve hangi değişikliğin ne yaptığı ölçülemez.

Ekonominin gevşekliği (bedava draft kulesi kesilince her strateji çöküyor — Işın ölçümünde ortaya çıktı) ayrı bir iştir ve bu plandan sonra ele alınacak.
