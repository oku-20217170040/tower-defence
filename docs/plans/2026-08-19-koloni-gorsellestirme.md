# Koloni Görselleştirme ve Unity Entegrasyonu — Uygulama Planı (Plan 2)

> Adımlar `- [ ]` kutucuk sözdizimi ile takip edilir.

**Hedef:** Plan 1'de kurulan çekirdeği sahneye bağlamak; feromon izlerini ekranda görünür kılmak; oyuncunun kule koyup sürünün rotayı değiştirdiğini izleyebildiği oynanabilir bir kesit üretmek.

**Neden bu sıra:** Elimizde çalışan ve test edilmiş bir matematik var ama kimse onu göremiyor. Mekaniğin eğlenceli olup olmadığı ancak görüldüğünde anlaşılır. Asset üretimi buna bağlı: neye benzeyeceğini bilmeden model yapmak boşa iştir.

**Mimari:** `Colony.Core` (saf C#, Plan 1) dokunulmadan kalır. Üstüne `Colony.Runtime` assembly'si gelir — MonoBehaviour'lar burada yaşar ve çekirdeği yalnızca *kullanır*. Bu ayrım korunmalıdır: çekirdeğe Unity tipi sızarsa test edilebilirliği kaybederiz.

**Tech Stack:** Unity 6000.3.21f1, URP, C#, Unity Test Framework.

## Global Constraints

- `Colony.Core` değişmez. Yeni Unity bağımlılığı ona girmez.
- Dikey tutuş: 9:16 hedef en-boy oranı, kamera ızgarayı dar eksende sığdırır.
- Feromon görselleştirmesi tek `Texture2D` üzerinden yapılır, hücre başına GameObject üretilmez.
- Geçici görseller: renkli kareler ve daireler. Blender asset'leri Plan 5'in işidir.
- Tanımlayıcılar İngilizce, yorumlar Türkçe (aksansız — proje genelinde encoding sorunu var).
- Branch: `tasarim/koloni-reboot`
- Her görev ayrı commit alır.

## Doğrulama Yöntemi

Plan 1'de her şey birim testiyle kanıtlanabiliyordu. Burada iki tür doğrulama var ve hangisinin kullanılacağı her görevde açıkça yazılıdır:

- **Birim testi (EditMode):** saf hesap içeren parçalar — koordinat dönüşümü, renk eşlemesi, doku doldurma.
- **Görsel doğrulama:** sahne çalıştırılır, `ScreenCapture.CaptureScreenshot` ile kare alınır ve gözle kontrol edilir. Ekran görüntüsü olmadan "çalışıyor" denmez.

---

### Görev 1: Colony.Runtime assembly ve GridSpace

Izgara koordinatı ile dünya koordinatı arasındaki dönüşüm. Saf hesap olduğu için birim testiyle doğrulanır.

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/Colony.Runtime.asmdef` (references: `Colony.Core`)
- Oluştur: `Assets/Colony/Runtime/View/GridSpace.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/GridSpaceTests.cs`

**Arayüzler:**
- Kullanır: `ColonyGrid`
- Üretir: `GridSpace` — `CellSize`, `Origin`, `CellToWorld(int)`, `WorldToCell(Vector2, out int)`, `RequiredOrthographicSize(float aspect)`

**Tasarım notu:** Izgara dünya merkezinde ortalanır. `y = 0` altta (yuva), `y = Height-1` üstte (giriş) — spec 3.6. Kamera boyutu dar eksene göre hesaplanır ki 3 koridor da ekrana sığsın.

- [ ] **Adım 1:** `Colony.Runtime.asmdef` oluştur, `references: ["Colony.Core"]`, `noEngineReferences: false`.
- [ ] **Adım 2:** `GridSpaceTests.cs` yaz — kapsanacak davranışlar: merkez hücrenin dünya konumu, `CellToWorld` ve `WorldToCell` gidiş-dönüşü, ızgara dışı dünya noktasının `false` dönmesi, `RequiredOrthographicSize` dar eksende taşma yapmaması.
- [ ] **Adım 3:** Testi koştur, başarısızlığı gör (derleme hatası).
- [ ] **Adım 4:** `GridSpace` yaz.
- [ ] **Adım 5:** Testleri koştur, geçtiğini gör.
- [ ] **Adım 6:** Commit — `feat: Colony.Runtime assembly ve GridSpace`

---

### Görev 2: Feromon renk eşlemesi ve doku katmanı

Feromon değerini ekranda görünür renge çeviren katman. Renk eşlemesi saf hesaptır ve test edilir; dokunun ekrana çizilmesi görsel doğrulamaya kalır.

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/View/PheromoneColorRamp.cs`
- Oluştur: `Assets/Colony/Runtime/View/PheromoneOverlay.cs` (MonoBehaviour)
- Oluştur: `Assets/Colony/Tests/EditMode/PheromoneColorRampTests.cs`

**Arayüzler:**
- Üretir: `PheromoneColorRamp` — `Evaluate(float normalized)` → `Color`
- Üretir: `PheromoneOverlay` — `Bind(ColonyGrid, PheromoneMap, LureField, GridSpace)`, `Refresh()`

**Tasarım notu:** İz yoğunluğu arttıkça soluk sarıdan parlak turuncuya gider ve alfa yükselir. Sıfır feromon tamamen saydamdır — boş koridor temiz görünmelidir, yoksa oyuncu neyin öğrenildiğini ayırt edemez. Yem katkısı ayrı bir tonda (soğuk yeşil) gösterilir ki oyuncu **gerçek iz ile kendi yalanını ayırt edebilsin.** Bu ayrım spec 9.1'in gereğidir.

- [ ] **Adım 1:** `PheromoneColorRampTests.cs` yaz — sıfırda alfa sıfır, tavanda alfa en yüksek, ara değerde monoton artış, girdi [0,1] dışına taşarsa kırpma.
- [ ] **Adım 2:** Testi koştur, başarısızlığı gör.
- [ ] **Adım 3:** `PheromoneColorRamp` yaz, testleri geçir.
- [ ] **Adım 4:** `PheromoneOverlay` yaz — hücre başına bir texel tutan `Texture2D`, `FilterMode.Bilinear` (izler yumuşak görünsün), tek `SpriteRenderer` üzerinde çizilir.
- [ ] **Adım 5:** Commit — `feat: feromon renk eslemesi ve doku katmani`

---

### Görev 3: ColonyWorld — çekirdeği sahneye bağla

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/ColonyWorld.cs` (MonoBehaviour)
- Oluştur: `Assets/Colony/Runtime/View/GridTileRenderer.cs`
- Oluştur: `Assets/Scenes/Colony_Sandbox.unity`

**Arayüzler:**
- Üretir: `ColonyWorld` — `Grid`, `Pheromone`, `Lure`, `Threat`, `CostModel`, `Pathfinder`, `Space`, `NestCell`, `SpawnCells`, `Regenerate(int seed)`

**Tasarım notu:** `MapGenerator` ile harita üretir, `MapValidator` ile doğrular; **geçmezse yeni seed ile yeniden üretir** (spec 9.4). En fazla 50 deneme, sonra hata verir — sessizce bozuk harita ile devam etmek en kötü sonuçtur.

- [ ] **Adım 1:** `ColonyWorld` yaz — Awake'de harita üret + doğrula + yeniden dene döngüsü.
- [ ] **Adım 2:** `GridTileRenderer` yaz — hücre türüne göre renkli kareler (koridor koyu, kurulum şeridi orta, engel açık). Tek mesh ya da tek texture; hücre başına GameObject yok.
- [ ] **Adım 3:** `Colony_Sandbox` sahnesini kur: kamera (ortografik, dikey), `ColonyWorld`, `GridTileRenderer`, `PheromoneOverlay`.
- [ ] **Adım 4: Görsel doğrulama.** Play moduna gir, ekran görüntüsü al. Beklenen: dikey ızgara, 3 belirgin koridor, aralarında kurulum şeritleri, kadraja tam sığmış.
- [ ] **Adım 5:** Commit — `feat: ColonyWorld ve izgara cizimi`

---

### Görev 4: BugAgent — böcekler ve iz bırakma

Mekaniğin canlandığı yer. Böcek yolu izler, geçtiği hücreye feromon bırakır; yuvaya ulaşırsa iz güçlenir, ölürse zayıflar.

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/Agents/BugAgent.cs`
- Oluştur: `Assets/Colony/Runtime/Agents/BugSpawner.cs`

**Arayüzler:**
- Üretir: `BugAgent` — `Initialize(ColonyWorld, int spawnCell)`, `Kill()`, `ReachedNest` event
- Üretir: `BugSpawner` — `SpawnWave(int count, float interval)`

**Tasarım notu:** Böcek yolu **spawn anında bir kez** hesaplar. Her karede yeniden hesaplamak hem pahalıdır hem de mekaniği bozar: koloni "kararını verip" yola çıkmalı ki oyuncunun yanıltması anlamlı olsun. Rota yalnızca dalga başında yeniden değerlendirilir.

İz bırakma miktarları geçici sabitlerdir; denge Plan 3'ün işidir.

- [ ] **Adım 1:** `BugAgent` yaz — yol takibi, hücre değişiminde `Deposit`, yuvaya varınca `DepositTrail` ile pekiştirme.
- [ ] **Adım 2:** `Kill()` → `WeakenTrail` ile o ana kadarki izi zayıflat.
- [ ] **Adım 3:** `BugSpawner` yaz — dalga halinde spawn, `PheromoneOverlay.Refresh()` tetikleme.
- [ ] **Adım 4: Görsel doğrulama.** Sahneyi çalıştır, 2-3 dalga izle, ekran görüntüsü al. **Beklenen ve kritik olan:** başlangıçta böcekler dağınık ilerler; birkaç dalga sonra tek bir koridorda parlayan bir iz oluşur ve sürü onu takip eder. Bu görülmezse öğrenme çalışmıyordur.
- [ ] **Adım 5:** Commit — `feat: BugAgent ve iz birakma`

---

### Görev 5: Kule yerleştirme ve sessiz/gürültücü ayrımı

Oyuncunun mekaniğe dokunduğu yer. Üç kule tipi — biri gürültücü, biri sessiz, biri yem.

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/Towers/TowerKind.cs` (enum: `Noisy`, `Silent`, `Lure`)
- Oluştur: `Assets/Colony/Runtime/Towers/ColonyTower.cs`
- Oluştur: `Assets/Colony/Runtime/Towers/TowerPlacer.cs`
- Oluştur: `Assets/Colony/Runtime/View/ThreatRingView.cs`

**Tasarım notu — spec 9.1'in doğrudan uygulaması:** Gürültücü kule `ThreatField.AddSource` çağırır **ve** sürekli genişleyen bir titreşim halkası çizer; halkanın yarıçapı tehdit yarıçapının ta kendisidir, dekoratif değildir. Sessiz kule ne `AddSource` çağırır ne halka çizer — soluk ve gölgeli durur. Yem kulesi `LureField.Apply` çağırır ve yeşil bir nabız atar.

Oyuncu kule elde tutarken **hayalet iz ön izlemesi** gösterilir: o kule konursa rotanın nasıl değişeceği soluk bir çizgiyle çizilir. Bu, mekaniği anlatmadan öğreten tek en güçlü araçtır.

- [ ] **Adım 1:** `TowerKind` ve `ColonyTower` yaz — yalnızca `Noisy` olan `AddSource` çağırsın.
- [ ] **Adım 2:** `TowerPlacer` yaz — dokunma/tık ile `BuildStrip` hücresine yerleştirme, geçersiz hücrede reddetme.
- [ ] **Adım 3:** `ThreatRingView` yaz — gürültücü kulede genişleyen halka.
- [ ] **Adım 4:** Hayalet iz ön izlemesi — kule elde tutulurken geçici `ThreatField`/`LureField` kopyası üzerinde yol hesapla ve çiz.
- [ ] **Adım 5: Görsel doğrulama.** Bir koridora gürültücü kule koy, birkaç dalga izle. **Beklenen:** sürü o koridoru terk eder. Sonra komşu koridora yem koy ve sürünün oraya çekildiğini gör. Ekran görüntüsü al.
- [ ] **Adım 6:** Commit — `feat: kule yerlestirme ve sessiz/gurultucu ayrimi`

---

### Görev 6: Oynanabilirlik kontrolü

Kod yazma görevi değil, karar görevi.

- [ ] **Adım 1:** Sandbox'ta 10 dakika oyna: yem + sessiz kule kombinasyonunu kur, sürüyü tuzağa çek.
- [ ] **Adım 2:** Şu üç soruyu cevapla ve `docs/notes/` altına yaz:
  1. Sessiz ile gürültücü kulenin farkı bakınca anlaşılıyor mu?
  2. Sürüyü kasıtlı olarak yanlış koridora çekmek tatmin edici mi?
  3. Feromon izi okunabilir mi, yoksa görsel gürültü mü?
- [ ] **Adım 3:** Cevaplara göre karar: mekanik doğrulandıysa Plan 3'e (oynanış katmanı) geç; doğrulanmadıysa spec bölüm 3'e dön ve tasarımı revize et.

**Bu görev planın asıl amacıdır.** Önceki beş görev bu soruyu sorabilmek için yapılıyor. Cevap "hayır" ise, içerik üretmeden önce döneriz — spec bölüm 8'in başarı kriteri budur.

---

## Bu Planın Kapsamadıkları

- Kulelerin gerçekten ateş etmesi, hasar, düşman canı, ölüm — Plan 3
- Dalga akışı, draft sistemi, ekonomi — Plan 3
- Meta ilerleme, kayıt — Plan 4
- Blender asset'leri, ses, animasyon — Plan 5
- Genetik katmanın run'lar arası evrimi — Plan 3

Bu planda böcekler ölmez; yalnızca yürür ve iz bırakır. Ölüm `Kill()` ile elle tetiklenir ki izin zayıflaması test edilebilsin. Amaç savaş değil, **rota öğrenmesinin görünür olması.**

## Eski Kod

`Assets/Scripts/` hâlâ silinmez. Plan 2 bittiğinde yeni sahne eskisinden bağımsız çalışır hale gelir; kaldırma kararı Plan 3'ün başında verilir.
