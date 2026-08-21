# Ordu Görselleştirme ve Unity Entegrasyonu — Uygulama Planı (Plan 2)

> Adımlar `- [ ]` kutucuk sözdizimi ile takip edilir.

**Hedef:** Plan 1'de kurulan çekirdeği sahneye bağlamak; keşif izi izlerini ekranda görünür kılmak; oyuncunun kule koyup sürünün rotayı değiştirdiğini izleyebildiği oynanabilir bir kesit üretmek.

**Neden bu sıra:** Elimizde çalışan ve test edilmiş bir matematik var ama kimse onu göremiyor. Mekaniğin eğlenceli olup olmadığı ancak görüldüğünde anlaşılır. Asset üretimi buna bağlı: neye benzeyeceğini bilmeden model yapmak boşa iştir.

**Mimari:** `Frontline.Core` (saf C#, Plan 1) dokunulmadan kalır. Üstüne `Frontline.Runtime` assembly'si gelir — MonoBehaviour'lar burada yaşar ve çekirdeği yalnızca *kullanır*. Bu ayrım korunmalıdır: çekirdeğe Unity tipi sızarsa test edilebilirliği kaybederiz.

**Tech Stack:** Unity 6000.3.21f1, URP, C#, Unity Test Framework.

## Global Constraints

- `Frontline.Core` değişmez. Yeni Unity bağımlılığı ona girmez.
- Dikey tutuş: 9:16 hedef en-boy oranı, kamera ızgarayı dar eksende sığdırır.
- Keşif izi görselleştirmesi tek `Texture2D` üzerinden yapılır, hücre başına GameObject üretilmez.
- Geçici görseller: renkli kareler ve daireler. Blender asset'leri Plan 5'in işidir.
- Tanımlayıcılar İngilizce, yorumlar Türkçe (aksansız — proje genelinde encoding sorunu var).
- Branch: `tasarim/koloni-reboot`
- Her görev ayrı commit alır.

## Doğrulama Yöntemi

Plan 1'de her şey birim testiyle kanıtlanabiliyordu. Burada iki tür doğrulama var ve hangisinin kullanılacağı her görevde açıkça yazılıdır:

- **Birim testi (EditMode):** saf hesap içeren parçalar — koordinat dönüşümü, renk eşlemesi, doku doldurma.
- **Görsel doğrulama:** sahne çalıştırılır, `ScreenCapture.CaptureScreenshot` ile kare alınır ve gözle kontrol edilir. Ekran görüntüsü olmadan "çalışıyor" denmez.

---

### Görev 1: Frontline.Runtime assembly ve GridSpace

Izgara koordinatı ile dünya koordinatı arasındaki dönüşüm. Saf hesap olduğu için birim testiyle doğrulanır.

**Dosyalar:**
- Oluştur: `Assets/Frontline/Runtime/Frontline.Runtime.asmdef` (references: `Frontline.Core`)
- Oluştur: `Assets/Frontline/Runtime/View/GridSpace.cs`
- Oluştur: `Assets/Frontline/Tests/EditMode/GridSpaceTests.cs`

**Arayüzler:**
- Kullanır: `BattleGrid`
- Üretir: `GridSpace` — `CellSize`, `Origin`, `CellToWorld(int)`, `WorldToCell(Vector2, out int)`, `RequiredOrthographicSize(float aspect)`

**Tasarım notu:** Izgara dünya merkezinde ortalanır. `y = 0` altta (üs), `y = Height-1` üstte (giriş) — spec 3.6. Kamera boyutu dar eksene göre hesaplanır ki 3 koridor da ekrana sığsın.

- [ ] **Adım 1:** `Frontline.Runtime.asmdef` oluştur, `references: ["Frontline.Core"]`, `noEngineReferences: false`.
- [ ] **Adım 2:** `GridSpaceTests.cs` yaz — kapsanacak davranışlar: merkez hücrenin dünya konumu, `CellToWorld` ve `WorldToCell` gidiş-dönüşü, ızgara dışı dünya noktasının `false` dönmesi, `RequiredOrthographicSize` dar eksende taşma yapmaması.
- [ ] **Adım 3:** Testi koştur, başarısızlığı gör (derleme hatası).
- [ ] **Adım 4:** `GridSpace` yaz.
- [ ] **Adım 5:** Testleri koştur, geçtiğini gör.
- [ ] **Adım 6:** Commit — `feat: Frontline.Runtime assembly ve GridSpace`

---

### Görev 2: Keşif izi renk eşlemesi ve doku katmanı

Keşif izi değerini ekranda görünür renge çeviren katman. Renk eşlemesi saf hesaptır ve test edilir; dokunun ekrana çizilmesi görsel doğrulamaya kalır.

**Dosyalar:**
- Oluştur: `Assets/Frontline/Runtime/View/ReconColorRamp.cs`
- Oluştur: `Assets/Frontline/Runtime/View/ReconOverlay.cs` (MonoBehaviour)
- Oluştur: `Assets/Frontline/Tests/EditMode/ReconColorRampTests.cs`

**Arayüzler:**
- Üretir: `ReconColorRamp` — `Evaluate(float normalized)` → `Color`
- Üretir: `ReconOverlay` — `Bind(BattleGrid, ReconMap, DecoyField, GridSpace)`, `Refresh()`

**Tasarım notu:** İz yoğunluğu arttıkça soluk sarıdan parlak turuncuya gider ve alfa yükselir. Sıfır keşif izi tamamen saydamdır — boş koridor temiz görünmelidir, yoksa oyuncu neyin öğrenildiğini ayırt edemez. Yem katkısı ayrı bir tonda (soğuk yeşil) gösterilir ki oyuncu **gerçek iz ile kendi yalanını ayırt edebilsin.** Bu ayrım spec 9.1'in gereğidir.

- [ ] **Adım 1:** `ReconColorRampTests.cs` yaz — sıfırda alfa sıfır, tavanda alfa en yüksek, ara değerde monoton artış, girdi [0,1] dışına taşarsa kırpma.
- [ ] **Adım 2:** Testi koştur, başarısızlığı gör.
- [ ] **Adım 3:** `ReconColorRamp` yaz, testleri geçir.
- [ ] **Adım 4:** `ReconOverlay` yaz — hücre başına bir texel tutan `Texture2D`, `FilterMode.Bilinear` (izler yumuşak görünsün), tek `SpriteRenderer` üzerinde çizilir.
- [ ] **Adım 5:** Commit — `feat: keşif izi renk eslemesi ve doku katmani`

---

### Görev 3: Battlefield — çekirdeği sahneye bağla

**Dosyalar:**
- Oluştur: `Assets/Frontline/Runtime/Battlefield.cs` (MonoBehaviour)
- Oluştur: `Assets/Frontline/Runtime/View/GridTileRenderer.cs`
- Oluştur: `Assets/Scenes/Frontline_Sandbox.unity`

**Arayüzler:**
- Üretir: `Battlefield` — `Grid`, `Pheromone`, `Lure`, `Threat`, `CostModel`, `Pathfinder`, `Space`, `NestCell`, `SpawnCells`, `Regenerate(int seed)`

**Tasarım notu:** `MapGenerator` ile harita üretir, `MapValidator` ile doğrular; **geçmezse yeni seed ile yeniden üretir** (spec 9.4). En fazla 50 deneme, sonra hata verir — sessizce bozuk harita ile devam etmek en kötü sonuçtur.

- [ ] **Adım 1:** `Battlefield` yaz — Awake'de harita üret + doğrula + yeniden dene döngüsü.
- [ ] **Adım 2:** `GridTileRenderer` yaz — hücre türüne göre renkli kareler (koridor koyu, kurulum şeridi orta, engel açık). Tek mesh ya da tek texture; hücre başına GameObject yok.
- [ ] **Adım 3:** `Frontline_Sandbox` sahnesini kur: kamera (ortografik, dikey), `Battlefield`, `GridTileRenderer`, `ReconOverlay`.
- [ ] **Adım 4: Görsel doğrulama.** Play moduna gir, ekran görüntüsü al. Beklenen: dikey ızgara, 3 belirgin koridor, aralarında kurulum şeritleri, kadraja tam sığmış.
- [ ] **Adım 5:** Commit — `feat: Battlefield ve izgara cizimi`

---

### Görev 4: UnitAgent — birlikler ve iz bırakma

Mekaniğin canlandığı yer. Birlik yolu izler, geçtiği hücreye keşif izi bırakır; üsse ulaşırsa iz güçlenir, ölürse zayıflar.

**Dosyalar:**
- Oluştur: `Assets/Frontline/Runtime/Agents/UnitAgent.cs`
- Oluştur: `Assets/Frontline/Runtime/Agents/UnitSpawner.cs`

**Arayüzler:**
- Üretir: `UnitAgent` — `Initialize(Battlefield, int spawnCell)`, `Kill()`, `ReachedNest` event
- Üretir: `UnitSpawner` — `SpawnWave(int count, float interval)`

**Tasarım notu:** Birlik yolu **spawn anında bir kez** hesaplar. Her karede yeniden hesaplamak hem pahalıdır hem de mekaniği bozar: ordu "kararını verip" yola çıkmalı ki oyuncunun yanıltması anlamlı olsun. Rota yalnızca dalga başında yeniden değerlendirilir.

İz bırakma miktarları geçici sabitlerdir; denge Plan 3'ün işidir.

- [ ] **Adım 1:** `UnitAgent` yaz — yol takibi, hücre değişiminde `Deposit`, üsse varınca `DepositTrail` ile pekiştirme.
- [ ] **Adım 2:** `Kill()` → `WeakenTrail` ile o ana kadarki izi zayıflat.
- [ ] **Adım 3:** `UnitSpawner` yaz — dalga halinde spawn, `ReconOverlay.Refresh()` tetikleme.
- [ ] **Adım 4: Görsel doğrulama.** Sahneyi çalıştır, 2-3 dalga izle, ekran görüntüsü al. **Beklenen ve kritik olan:** başlangıçta birlikler dağınık ilerler; birkaç dalga sonra tek bir koridorda parlayan bir iz oluşur ve sürü onu takip eder. Bu görülmezse öğrenme çalışmıyordur.
- [ ] **Adım 5:** Commit — `feat: UnitAgent ve iz birakma`

---

### Görev 5: Kule yerleştirme ve pusu/batarya ayrımı

Oyuncunun mekaniğe dokunduğu yer. Üç kule tipi — biri batarya, biri pusu, biri yem.

**Dosyalar:**
- Oluştur: `Assets/Frontline/Runtime/Towers/TowerKind.cs` (enum: `Noisy`, `Silent`, `Lure`)
- Oluştur: `Assets/Frontline/Runtime/Towers/Emplacement.cs`
- Oluştur: `Assets/Frontline/Runtime/Towers/TowerPlacer.cs`
- Oluştur: `Assets/Frontline/Runtime/View/ThreatRingView.cs`

**Tasarım notu — spec 9.1'in doğrudan uygulaması:** Batarya kule `ThreatField.AddSource` çağırır **ve** sürekli genişleyen bir titreşim halkası çizer; halkanın yarıçapı tehdit yarıçapının ta kendisidir, dekoratif değildir. Pusu mevzisi ne `AddSource` çağırır ne halka çizer — soluk ve gölgeli durur. Sahte hedef `DecoyField.Apply` çağırır ve yeşil bir nabız atar.

Oyuncu kule elde tutarken **hayalet iz ön izlemesi** gösterilir: o kule konursa rotanın nasıl değişeceği soluk bir çizgiyle çizilir. Bu, mekaniği anlatmadan öğreten tek en güçlü araçtır.

- [ ] **Adım 1:** `TowerKind` ve `Emplacement` yaz — yalnızca `Noisy` olan `AddSource` çağırsın.
- [ ] **Adım 2:** `TowerPlacer` yaz — dokunma/tık ile `BuildStrip` hücresine yerleştirme, geçersiz hücrede reddetme.
- [ ] **Adım 3:** `ThreatRingView` yaz — batarya kulede genişleyen halka.
- [ ] **Adım 4:** Hayalet iz ön izlemesi — kule elde tutulurken geçici `ThreatField`/`DecoyField` kopyası üzerinde yol hesapla ve çiz.
- [ ] **Adım 5: Görsel doğrulama.** Bir koridora batarya kule koy, birkaç dalga izle. **Beklenen:** sürü o koridoru terk eder. Sonra komşu koridora yem koy ve sürünün oraya çekildiğini gör. Ekran görüntüsü al.
- [ ] **Adım 6:** Commit — `feat: kule yerlestirme ve pusu/gurultucu ayrimi`

---

### Görev 6: Oynanabilirlik kontrolü

Kod yazma görevi değil, karar görevi.

- [ ] **Adım 1:** Sandbox'ta 10 dakika oyna: yem + pusu mevzisi kombinasyonunu kur, sürüyü tuzağa çek.
- [ ] **Adım 2:** Şu üç soruyu cevapla ve `docs/notes/` altına yaz:
  1. Pusu ile batarya kulenin farkı bakınca anlaşılıyor mu?
  2. Sürüyü kasıtlı olarak yanlış koridora çekmek tatmin edici mi?
  3. Keşif izi izi okunabilir mi, yoksa görsel gürültü mü?
- [ ] **Adım 3:** Cevaplara göre karar: mekanik doğrulandıysa Plan 3'e (oynanış katmanı) geç; doğrulanmadıysa spec bölüm 3'e dön ve tasarımı revize et.

**Bu görev planın asıl amacıdır.** Önceki beş görev bu soruyu sorabilmek için yapılıyor. Cevap "hayır" ise, içerik üretmeden önce döneriz — spec bölüm 8'in başarı kriteri budur.

---

## Bu Planın Kapsamadıkları

- Kulelerin gerçekten ateş etmesi, hasar, düşman canı, ölüm — Plan 3
- Dalga akışı, draft sistemi, ekonomi — Plan 3
- Meta ilerleme, kayıt — Plan 4
- Blender asset'leri, ses, animasyon — Plan 5
- Genetik katmanın run'lar arası evrimi — Plan 3

Bu planda birlikler ölmez; yalnızca yürür ve iz bırakır. Ölüm `Kill()` ile elle tetiklenir ki izin zayıflaması test edilebilsin. Amaç savaş değil, **rota öğrenmesinin görünür olması.**

## Eski Kod

`Assets/Scripts/` hâlâ silinmez. Plan 2 bittiğinde yeni sahne eskisinden bağımsız çalışır hale gelir; kaldırma kararı Plan 3'ün başında verilir.
