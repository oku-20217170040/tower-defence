# Birlik Türleri — Plan 8

**Tarih:** 21 Ağustos 2026
**Durum:** Görev 1, 2, 3, 6 bitti (altyapı, izci, asker, kraliçe). Görev 4 (kör) ölçüm aracı yetersizliğinden ertelendi, Görev 5 (kuluçka) ölçümle elendi. Sırada Görev 7 — runtime doğrulaması.
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

### Görev 3 — Asker — **TAMAMLANDI (21 Ağustos)**

**Seçilen:** pay **%20** (3. dalgadan itibaren), pekiştirme **x6**, can **x3**, hız **x0,6**.

Ortalama entropi askeri ölçemedi: pekiştirme x1→x8 ortalamayı 0,568'den 0,512'ye taşıyor ve bu, askerin **canından** gelen etkiden ayırt edilemiyor. Doğru metrik: askerin **üsse vardığı sütunun sonraki dalgadaki pay artışı** ("çivileme / lift").

| Pekiştirme | Çivileme (lift) | Sızıntı |
|---|---|---|
| x1 | **−0,009** (çivileme yok) | 11,8 |
| x3 | +0,014 | 12,4 |
| **x6** | **+0,041** | 12,5 |
| x20 | +0,070 (kazancı azalıyor) | 13,6 |

**Bulgu 1 — pekiştirme bir zorluk düğmesi değil, karakter düğmesi.** x1'den x20'ye sızıntı yalnızca 11,8 → 13,6. Zorluk askerin **canından** geliyor ve orada eşik yine keskin (x2 → 4,0 · x3 → 13,2 · x4 → 22,6). İkisini ayırmak şarttı; ayırmasaydık askeri "güçlü piyade" diye ekler ve Plan 8'in kuralını çiğnerdik.

**Bulgu 2 — korkulan doyma tuzağı çıkmadı.** Pekiştirme x20'de bile ordu durdurulamaz hale gelmiyor (sızıntı 13,6). Işın'daki gibi bir çözülme yok.

**Bulgu 3 — asker ile izci birbirinin tersi.** İzci dağıtır, asker toplar. Dalga başına entropi: izci yok 0,428 · izci %25 0,575 · izci %25 + asker %20 0,525. Ordu yine de geç dalgalarda dağılıyor (erken 0,467 → geç 0,678).

**Bulgu 4 — asker ışını güçlendirdi ve bu tasarım niyetiydi.** Asker orduyu koridora çivileyince ışın onun panzehiri oluyor: ışınsız 13,3 sızıntı, ışınlı 3,9. Plandaki "ışın kalabalık koridoru cezalandırır" ifadesinin ta kendisi.

> **Işının atış aralığı Plan 8 bitince yeniden ölçülmeli.** Aralık 1 sn, ordunun **saf piyade** olduğu bir bileşimde seçildi. Şu an aralık 3 sn karışımı tabana yaklaştırıyor (10,8 / 13,3) ama **şimdi ayarlamak yanlış**: Görev 4-6 bunu yine kaydıracak ve üst üste iki ayar hangi değişikliğin ne yaptığını ölçülemez kılar (bu planın kendi uyarısı). Bu yüzden `BeamTowerTests` artık **sayı değil ilişki** iddia ediyor.

**Uygulandı:** `UnitProfile.For(Soldier)`, `UnitRoster.SoldierShare/SoldierFirstWave`, `RunSimulator.WaveColumnUsage` + `WaveSoldierArrivals` (çivileme metriği), spawner'da askerin görsel ayrımı (büyük, koyu kırmızı). `SoldierTests` (5 test).

> **Doğrulanmamış kalan:** askerin oyunda büyük/koyu/yavaş göründüğü ve geçtiği koridorun sonraki dalgada tercih edildiği — **oynayarak doğrulanmalı.**

### Görev 4 — Kör birlik — **ERTELENDİ (ölçüm aracı yetersiz)**

Dört varyant, %10'dan **%100'e** kadar denendi. Hiçbiri ölçülebilir bir şey üretmedi (yem alan oyuncuya karşı sızıntı):

| Varyant | Sızıntı |
|---|---|
| kör yok | 6,3 |
| ize + yeme kör | 6,3 |
| yalnız yeme kör | 6,3 |
| yalnız ize kör | 6,3 |
| yalanı silen kör (yeni mekanik denendi) | 6,2 |
| kör %100 | 6,4 |

**Sebep kör birlikte değil, ölçüm modelinde.** Simülatörün oyuncu modeli kuleleri her koridora eşit dağıtıyor; dolayısıyla ordunun hangi koridoru seçtiği hayatta kalmayı etkilemiyor. Tuzak koridoru yerleşimi de denendi — kör birlik olmadan da %10 kazanıyor (ölçülmüş tasarım gerçeği #3 hâlâ geçerli).

Ayrıca yemin değeri **koridor seçiminden değil, ordunun toplanmasından** geliyor: yem sürüyü topluyor, toplanan sürü bataryanın alan hasarını besliyor. Kör birlikler o toplanmayı bozmaya yetmiyor.

**Açılması için gereken:** simülatörün oyuncusu kuleleri ordunun gerçekten gittiği yere kurmalı (uyarlamalı yerleştirme). Bu, ışının son ayarı ve yem stratejisinin ölçümü için de gerekli.

**Bu görev sırasında bulunan hata:** `RunSimulator` yemi **hiç uygulamıyordu** — bu tarihe kadarki bütün denge ölçümlerinde yem kulesi etkisizdi. Düzeltildi; yem ek kule olarak sızıntıyı 13,3'ten 6,3'e indiriyor.

### Görev 5 — Kuluçka — **EKLENMEDİ (kanalı zaten bedava)**

Metrik eklendi (`WaveDeathColumns`): en çok ölünen sütunun sonraki dalgadaki pay değişimi. Negatif = ordu tuzaktan kaçınıyor.

| Kurulum | Kaçınma | Öldürme | Sızıntı |
|---|---|---|---|
| **kuluçka yok** | **−0,1115** | 212,5 | 12,5 |
| sönümleme x1 | −0,1176 | 273,8 | 19,8 |
| sönümleme x10 | −0,1184 | 273,7 | 19,8 |

**Ordu, kuluçka olmadan da tuzaktan kaçınıyor** (−0,111). Sebep: taban `WeakenOnDeath = 2,5` zaten her ölen birliğin izini söndürüyor. Kuluçkanın `WeakenMultiplier`'ı x1'den x10'a çıkarken kaçınmayı yalnızca −0,1176'dan −0,1184'e taşıyor — yani **iddia ettiği kanal zaten oyunun taban davranışı.**

"Yavrular yeni rota arasın" varyantı da denendi (dağıtıcı bir kanal olur umuduyla): entropi 0,556 → 0,558. Hiçbir şey.

Geriye kalan tek etkisi bölünme: öldürme 212 → 273, sızıntı 12,5 → 19,8. Bu bir **zorluk/ekonomi çarpanı**, öğrenme kanalı değil. Plan 8'in pazarlıksız kuralına göre eklenmez.

> Altyapı kodda duruyor (`SplitCount`, `SplitlingsRepath`) ve test ediliyor. İleride zorluk eğrisi sıkılaştırılmak istenirse kuluçka **bilinçli olarak "sadece zorluk" türü** diye eklenebilir — ama o zaman öyle adlandırılmalı.

### Görev 6 — Kraliçe — **TAMAMLANDI (21 Ağustos)**

**Seçilen:** pay **%5** (5. dalgadan itibaren), can **x8**, hız **x0,7**, **keşif izini siler** (tehdidi değil).

**Bu planın kendi tanımı yanlıştı.** Plan "kraliçe tehdit alanını temizler — ordunun korkusunu siler" diyordu. Ölçüm bunun hiçbir şey yapmadığını gösterdi:

| Kraliçe (pay %8) | Sızıntı | Entropi |
|---|---|---|
| tehdit **siler** | 26,4 | 0,507 |
| tehdit **silmez** | 26,4 | 0,507 |
| **hafıza (keşif izi) siler** | **25,2** | **0,552** |

**Tehdit alanı ordunun hafızası değil** — her dalga başında kulelerden yeniden kurulan, oyuncunun kule yerleşiminin statik bir fonksiyonu. Silmenin etkisi olamaz. Ordunun gerçekten öğrendiği şey **keşif izi**. Kraliçe onu silince kanal ortaya çıktı.

**Kanal candan bağımsız:** zayıf kraliçede (can x2) bile entropi 0,520 → 0,535. Askerdeki ayrımın aynısı — kraliçe "büyük canlı birlik" değil, orduya unutturan birlik.

Pay: %3'te etki zar zor görünüyor (+0,026), %8'te run %65 kazanmaya düşüyor. **%5** seçildi (+0,032, kazanma %95).

**Uygulandı:** `UnitProfile.ClearsRecon`, `UnitRoster.QueenShare/QueenFirstWave`, `UnitAgent` hafıza silme, spawner'da kraliçenin görsel ayrımı (en büyük, mor). `QueenTests` (4 test).

> **Doğrulanmamış kalan:** kraliçe üsse vardığında keşif izi katmanının gözle görülür biçimde silinmesi — **oynayarak doğrulanmalı.** Bu, oyunun en dramatik anı olmalı.

### Görev 7 — Runtime ve görsel ayrım

- `UnitAgent` profil alır; `UnitSpawner` dalga bileşimini kurar.
- Türler **bakışta ayırt edilebilmeli** (boyut/renk). Ayırt edilemeyen tür oyuncu için yok demektir.
- **Kullanıcı doğrulaması şart.** Bu projede ölçülebilir her şey EditMode testinde, zamanlamalı/görsel her şey oyuncuda (Işın'da da böyle yapıldı).

## Bu planın bilinen riski

Her yeni tür **zorluğu artırır** ve bugünkü denge zaten gevşek: 21 Ağustos run'ı 15/15 kazanıldı, üs 24-26/30. Yani türler eklendikçe oyun kendiliğinden sıkılaşacak. Bu iyi haber, ama **denge sayıları bu plan bitmeden yeniden ayarlanmamalı** — yoksa iki kez üst üste ayar yapılır ve hangi değişikliğin ne yaptığı ölçülemez.

Ekonominin gevşekliği (bedava draft kulesi kesilince her strateji çöküyor — Işın ölçümünde ortaya çıktı) ayrı bir iştir ve bu plandan sonra ele alınacak.
