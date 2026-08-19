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

### Görev 4: Zorluk eğrisinin dengelenmesi

*(19 Ağustos 2026'da ölçüm sonrası yeniden yazıldı. Özgün hali "ölüm geri beslemesinin dengelenmesi" idi; ölçüm o düğmenin işe yaramadığını gösterdi.)*

**Ölçüm ne gösterdi:** Sabit canlı böceklerde tuzak ya hep ya hiç çalışıyor. Bir böcek 4 yarıçaplı kulenin menzilinde ~3,2 saniye geçiriyor; dps 3'te toplam hasar 9,6 (10 canlı böcek sağ kurtulur), dps 4'te 12,8 (hepsi ölür). Arada gradyan yok.

Daha kötüsü, `weakenOnDeath` değerini 6'dan 15'e çıkarmak sonucu değiştirmedi (82 → 81 ölüm). Yani "ölüm geri beslemesini ayarlama" diye planladığım düğme, yüksek ölüm oranında hiçbir şey yapmıyor.

**Yeni yaklaşım:** Asıl zorluk düğmesi dalga başına can artışı. Sabit bir tuzak zamanla yetersiz kalır ve oyuncu genişletmek, taşımak ya da yükseltmek zorunda kalır. Bu, TD türünün kanıtlanmış baskısıdır ve ölçümün gösterdiği eşik davranışıyla uyumludur.

**Uygulanan değerler:** böcek canı `10 * (1 + 0,15 * (dalga - 1))`, dalga başına böcek `10 + (dalga - 1)`, gürültücü kule 10 dps, sessiz kule 4 dps, yuva canı 20, run 15 dalga.

- [ ] **Adım 1:** `execute_code` ile tam run simülasyonu: 15 dalga, oyuncu tuzağı sabit. Ölçülecek: hangi dalgada tuzak yetersiz kalmaya başlıyor, yuva canı ne zaman bitiyor.
- [ ] **Adım 2:** Hedef eğri: oyuncu hiçbir şey yapmazsa 8-11. dalga civarında kaybetmeli. Daha erken ise oyuncunun tepki verecek zamanı olmaz; daha geç ise ilk yarı anlamsızlaşır.
- [ ] **Adım 3:** `ColonyDifficultyTests` yaz — seçilen eğriyi kalıcı olarak koru.
- [ ] **Adım 4:** Bulguyu spec'e işle (yeni alt bölüm 3.8).
- [ ] **Adım 5:** Commit — `denge: zorluk egrisi`

**Not — Plan 4'e devredilen tasarım sorunu:** Yem kulesi her dalga otomatik ve bedava tazeleniyor. Spec 3.3 "başarılı tuzak kendini yer, yem sürekli beslenmeli" diyordu; ama besleme otomatikse besleme diye bir karar kalmıyor. Bunun gerçek karşılığı ekonomi: yem tazelemenin bir maliyeti olmalı.

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
