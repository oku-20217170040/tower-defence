# Savaş ve Ölüm Geri Beslemesi — Uygulama Planı (Plan 3)

> Adımlar `- [ ]` kutucuk sözdizimi ile takip edilir.

**Hedef:** Döngüyü kapatmak. Böcekler ölecek, ölüm izi zayıflatacak ve oyunun en ilginç dinamiği ilk kez çalışacak: **başarılı tuzak kendini yer.**

**Neden bu sıra:** Plan 2'de böcekler ölmüyor, bu yüzden izler yalnızca güçleniyor — tek yönlü bir sistem. Ölüm devreye girince negatif geri besleme doğuyor ve oyuncunun kurduğu tuzak kendi izini söndürmeye başlıyor. Bu, spec 3.3'te yazılı ama hiç ölçülmemiş tek büyük dinamik. Ekonomi ve draft bundan sonra gelmeli; neyin dengeleneceğini bilmeden ekonomi tasarlanmaz.

**Mimari:** `Colony.Core` saf kalmaya devam eder. Hasar, hedefleme ve can Runtime'da yaşar; çekirdek yalnızca "bu böcek öldü, rotasını zayıflat" bilgisini alır.

## Global Constraints

- `Colony.Core` MonoBehaviour içermez ve Unity tipine bağımlı olmaz
- Ölçülmüş sabitler korunur: keşif 0,35 · çekim 3 · tehdit ağırlığı 3 · böcek başına iz 2
- Gürültücü kule tehdit yazar, sessiz kule **yazmaz** — bu ayrım hiçbir koşulda bozulmaz
- Tanımlayıcılar İngilizce, yorumlar Türkçe (aksansız)
- Branch: `tasarim/koloni-reboot`
- Her görev ayrı commit alır

## Doğrulama Yöntemi

Plan 2'de öğrendiğimiz ders: **denge sayıları tahminle değil taramayla seçilir.** Her denge kararı önce `execute_code` ile taranır, kazanan değer koda geçer, sonra o davranışı koruyan bir test yazılır.

Görsel doğrulama Unity penceresi odak gerektirdiği için kullanıcıya bırakılır; ölçülebilir her şey EditMode testiyle doğrulanır.

---

### Görev 1: Böcek canı ve ölüm

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/Agents/BugHealth.cs`
- Değiştir: `Assets/Colony/Runtime/Agents/BugAgent.cs`
- Değiştir: `Assets/Colony/Runtime/Agents/BugSpawner.cs`

**Arayüzler:**
- Üretir: `BugHealth` — `MaxHp`, `CurrentHp`, `TakeDamage(float)`, `Died` event
- Değişir: `BugAgent.Kill()` zaten var; `BugHealth` onu çağırır

**Tasarım notu:** Ölüm zaten `WeakenTrail` çağırıyor (Plan 2'de yazıldı, hiç tetiklenmedi). Bu görev o tetiği bağlar. `weakenOnDeath` değeri Görev 4'te taranarak seçilecek; şimdilik varsayılan 2,5 kalır.

- [ ] **Adım 1:** `BugHealth` yaz — can, hasar alma, sıfırlanınca `BugAgent.Kill()`.
- [ ] **Adım 2:** `BugSpawner` böcek üretirken `BugHealth` ekler.
- [ ] **Adım 3:** Can barı yok; ölüm anında böcek yok olur. Görsel geri bildirim Görev 3'te.
- [ ] **Adım 4:** Commit — `ozellik: bocek cani ve olum`

---

### Görev 2: Kule ateşi ve hasar

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/Towers/TowerWeapon.cs`
- Değiştir: `Assets/Colony/Runtime/Towers/ColonyTower.cs`
- Değiştir: `Assets/Colony/Runtime/Towers/TowerPlacer.cs`

**Arayüzler:**
- Üretir: `TowerWeapon` — `damagePerSecond`, `range`, `Tick(float deltaTime)`
- Değişir: `ColonyTower` bir `TowerWeapon` taşır

**Tasarım notu — kritik denge kuralı:** Gürültücü kule yüksek hasar, sessiz kule düşük hasar verir. Ama gürültücü kulenin önünden zamanla kimse geçmez; dolayısıyla **teorik DPS'i ile gerçek DPS'i arasında büyük fark oluşur.** Oyuncunun keşfetmesi gereken şey tam olarak budur (spec 3.2). Sayılar bunu bozmayacak şekilde seçilmeli: sessiz kulenin DPS'i düşük olmalı ama yok sayılacak kadar değil.

Mermi yok; hasar sürekli ve anlıktır. Mermi görselleri Plan 5'in işi.

- [ ] **Adım 1:** `TowerWeapon` yaz — menzildeki en yakın böceğe saniyede `damagePerSecond` uygular.
- [ ] **Adım 2:** `ColonyTower` türüne göre silah parametresi alır (gürültücü yüksek, sessiz düşük, yem sıfır).
- [ ] **Adım 3:** Aktif böcek listesi — her karede tüm böcekleri taramak yerine `BugSpawner` canlı listesi tutar.
- [ ] **Adım 4:** Commit — `ozellik: kule atesi ve hasar`

---

### Görev 3: Yuva canı ve run sonu

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/NestHealth.cs`
- Oluştur: `Assets/Colony/Runtime/RunController.cs`
- Değiştir: `Assets/Colony/Runtime/Agents/BugSpawner.cs`

**Arayüzler:**
- Üretir: `NestHealth` — `MaxHp`, `CurrentHp`, `Damage(int)`, `Emptied` event
- Üretir: `RunController` — `TotalWaves`, `CurrentWave`, `State` (Running/Won/Lost), `RunEnded` event

**Tasarım notu:** Run 12-15 dalga (spec 2.1). Yuvaya ulaşan her böcek can eksiltir. Dalgalar artık sonsuz değil; `BugSpawner` `RunController`'dan komut alır.

- [ ] **Adım 1:** `NestHealth` yaz; `BugAgent.Arrive()` yuvaya hasar verir.
- [ ] **Adım 2:** `RunController` yaz — dalga sayacı, kazanma ve kaybetme koşulu.
- [ ] **Adım 3:** `BugSpawner` sonsuz döngüden çıkar, `RunController` tarafından sürülür.
- [ ] **Adım 4:** Commit — `ozellik: yuva cani ve run sonu`

---

### Görev 4: Ölüm geri beslemesinin dengelenmesi

**Bu planın asıl görevi.** Diğer üçü bunu ölçebilmek için yapılıyor.

**Dosyalar:**
- Oluştur: `Assets/Colony/Tests/EditMode/ColonyTrapFeedbackTests.cs`
- Değiştir: ölçüm sonucuna göre `BugAgent.weakenOnDeath` ve kule hasar değerleri

**Ölçülecek soru:** Oyuncu bir tuzak koridoru kurduğunda ne oluyor?

Üç olası sonuç var ve hangisinin gerçekleştiğini tahmin etmiyoruz, ölçüyoruz:

1. **Tuzak kendini yer (isteneni):** Böcekler ölür, iz zayıflar, sürü başka koridora kayar. Oyuncu tuzağı taşımak ya da yemle beslemek zorunda kalır. Sürekli karar üretir.
2. **Tuzak kalıcıdır:** Ölüm izi yeterince zayıflatmaz, sürü akmaya devam eder. Oyuncu bir kez kurar ve unutur — karar üretmez, sıkıcıdır.
3. **Tuzak anında çöker:** İlk ölümlerde iz siliner, sürü dağılır, oyuncu hiçbir zaman kazanç sağlayamaz. Sinir bozucudur.

- [ ] **Adım 1:** `execute_code` ile tarama: `weakenOnDeath` × sessiz kule DPS kombinasyonları. Ölçülecek çıktı: tuzak koridorundaki sürü payının dalga dalga seyri ve toplam öldürülen böcek sayısı.
- [ ] **Adım 2:** Kazanan değerleri koda geçir.
- [ ] **Adım 3:** `ColonyTrapFeedbackTests` yaz — seçilen davranışı kalıcı olarak koru.
- [ ] **Adım 4:** Bulguyu spec'e işle (yeni alt bölüm 3.8).
- [ ] **Adım 5:** Commit — `denge: olum geri beslemesi`

---

### Görev 5: Ekranda geri bildirim

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/View/RunHud.cs`
- Değiştir: `Assets/Colony/Runtime/View/PrimitiveSprites.cs` (gerekirse)

**Tasarım notu:** Oyuncu şu üç şeyi görmeden karar veremez: yuva canı, kaçıncı dalga, ölen böcek sayısı. Ölüm anında kısa bir görsel işaret (böceğin sönmesi) gerekir; yoksa böceğin öldüğü mü yoksa yuvaya mı ulaştığı ayırt edilemez — bu, tuzağın işe yarayıp yaramadığını okumayı imkânsız kılar.

- [ ] **Adım 1:** `RunHud` yaz — yuva canı, dalga sayacı, öldürülen sayısı.
- [ ] **Adım 2:** Ölüm anında böcek sönme efekti (renk + küçülme).
- [ ] **Adım 3:** Kullanıcıya oynatıp geri bildirim al.
- [ ] **Adım 4:** Commit — `ozellik: run arayuzu ve olum geri bildirimi`

---

## Bu Planın Kapsamadıkları

- **Draft sistemi ve ekonomi** — Plan 4. Neyin dengeleneceğini bilmeden ekonomi tasarlanmaz; önce savaş oturmalı.
- **Meta ilerleme, biyokütle, yetenek ağacı** — Plan 4.
- **Genetik katmanın run'lar arası evrimi** — Plan 4.
- **Blender asset'leri, ses, animasyon** — Plan 5.
- **Hayalet iz ön izlemesi** — Plan 2'den devredildi; oyuncu testinde okunabilirlik sorun çıkarırsa öne alınır.

## Eski Kod

`Assets/Scripts/` hâlâ duruyor. Plan 3 bittiğinde yeni sahne eski sistemin yaptığı her şeyi yapıyor olacak; kaldırma kararı Plan 4'ün başında verilir.
