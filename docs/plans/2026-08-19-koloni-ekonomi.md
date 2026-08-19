# Ekonomi ve Draft — Uygulama Planı (Plan 4)

> Adımlar `- [ ]` kutucuk sözdizimi ile takip edilir.

**Hedef:** Kule sayısını kısıtlamak. Plan 3'ün ölçümü, kule sayısının oyunun **baskın değişkeni** olduğunu gösterdi — ama şu an kuleler bedava ve sınırsız, dolayısıyla oyunun zorluk ayarı diye bir şey yok.

**Ölçülmüş hedef (spec 3.5):** Oyuncu 8-10. dalga civarında 4-5 etkili kuleye ulaşmalı. Koridor başına 1 kule (toplam 3) dalga 10'da kaybettiriyor; koridor başına 2 kule (toplam 6) sıfır sızıntıyla kazandırıyor. Ekonominin işi oyuncuyu bu iki uç arasında tutmak: her dalga bir şeyler kazandırmalı ama hiçbir zaman rahatlatmamalı.

**Mimari:** Ekonomi saf hesap; `Colony.Core` içinde MonoBehaviour'suz yaşar ve birim testiyle doğrulanır. Draft arayüzü Runtime'da.

## Global Constraints

- `Colony.Core` MonoBehaviour içermez, Unity tipine bağımlı olmaz
- Plan 1-3'te ölçülmüş sabitler korunur: keşif 0,35 · çekim 3 · tehdit ağırlığı 3 · sessiz kule 4 dps · yuva canı 30
- Tanımlayıcılar İngilizce, yorumlar Türkçe (aksansız)
- Branch: `tasarim/koloni-reboot`
- Denge sayıları tahminle değil taramayla seçilir

---

### Görev 1: Ekonomi çekirdeği

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/Economy/RunEconomy.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/RunEconomyTests.cs`

**Arayüzler:**
- Üretir: `RunEconomy` — `Balance`, `TotalEarned`, `TotalSpent`, `Earn(int)`, `CanAfford(int)`, `TrySpend(int)`, `Refund(int)`

**Tasarım notu:** Kaynak birimi **besin**. Böcek öldürmek ve dalga tamamlamak besin kazandırır. Sızan böcek kazandırmaz — oyuncunun tuzağı çalışmazsa hem can hem gelir kaybeder. Bu, kaybetme sarmalı yaratabilir; Görev 4'te ölçülüp gerekirse taban gelir eklenecek.

- [ ] **Adım 1:** `RunEconomyTests` yaz — kazanma, harcama, yetersiz bakiye reddi, negatife düşmeme, toplam sayaçlar.
- [ ] **Adım 2:** Testi koştur, başarısızlığı gör.
- [ ] **Adım 3:** `RunEconomy` yaz, testleri geçir.
- [ ] **Adım 4:** Commit — `ozellik: run ekonomisi cekirdegi`

---

### Görev 2: Kule maliyetleri ve satın alma

**Dosyalar:**
- Oluştur: `Assets/Colony/Runtime/Economy/EconomyRunner.cs`
- Değiştir: `Assets/Colony/Runtime/Towers/TowerPlacer.cs`
- Değiştir: `Assets/Colony/Runtime/Agents/BugSpawner.cs`
- Değiştir: `Assets/Colony/Runtime/RunController.cs`

**Tasarım notu:** Kule fiyatları sınıflarına göre farklı olmalı ve bu fark tasarımı taşımalı. Gürültücü kule ucuz olmalı — çünkü yalnız başına işe yaramıyor (spec 3.4), oyuncunun onu denemesi ucuz olsun. Sessiz kule pahalı olmalı — gerçek katliamı o yapıyor. Yem orta.

Aynı türden her yeni kule biraz daha pahalıya gelmeli; aksi halde oyuncu tek stratejiyi spam'ler.

- [ ] **Adım 1:** `EconomyRunner` yaz — `RunEconomy`'yi sahneye bağlar, ölüm ve dalga sonu gelirlerini dinler.
- [ ] **Adım 2:** `TowerPlacer` satın alma kontrolü yapar; parası yetmezse yerleştirmeyi reddeder ve sebebini söyler.
- [ ] **Adım 3:** Aynı türden kule fiyatı artışı.
- [ ] **Adım 4:** Commit — `ozellik: kule maliyetleri ve satin alma`

---

### Görev 3: Dalga sonu draft

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/Draft/DraftOption.cs`
- Oluştur: `Assets/Colony/Core/Draft/DraftGenerator.cs`
- Oluştur: `Assets/Colony/Runtime/View/DraftView.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/DraftGeneratorTests.cs`

**Tasarım notu — spec 2.2'nin gereği:** Sunulan 3 seçenek rastgele değil, **kolonin o anki davranışına göre** ağırlıklandırılır. Sol koridor ezberlenmişse oraya uygun seçeneklerin çıkma olasılığı artar. Bu, adaptif sistemi oynanışa bağlayan ikinci kanaldır ve draft'ı sıradan bir kart seçiminden ayıran şeydir.

Seçenek türleri: bedava kule, kule indirimi, besin, kalıcı pasif (menzil/hasar artışı).

- [ ] **Adım 1:** `DraftOption` ve `DraftGenerator` — 3 seçenek üretir, koloni durumunu girdi alır.
- [ ] **Adım 2:** `DraftGeneratorTests` — aynı seed aynı seçenekleri verir, seçenekler tekrarlanmaz, koloni durumu dağılımı etkiler.
- [ ] **Adım 3:** `DraftView` — dalga sonunda 3 seçenek gösterir, seçim yapılana kadar bekler.
- [ ] **Adım 4:** Commit — `ozellik: dalga sonu draft`

---

### Görev 4: Ekonomi eğrisinin dengelenmesi

**Bu planın asıl görevi.**

**Ölçülecek soru:** Oyuncu dalga dalga kaç kuleye ulaşıyor ve bu, Plan 3'te ölçülen kazanma/kaybetme bandına oturuyor mu?

- [ ] **Adım 1:** `execute_code` ile tarama: öldürme başına besin × dalga başına besin × kule fiyatı kombinasyonları. Ölçülecek çıktı: dalga dalga alınabilen kule sayısı ve run sonucu.
- [ ] **Adım 2:** Hedef bant — 8-10. dalgada 4-5 etkili kule. Erken ulaşırsa oyun kolaylaşır, geç ulaşırsa oyuncu çaresiz kalır.
- [ ] **Adım 3:** Kaybetme sarmalı kontrolü: tuzağı çalışmayan oyuncu gelir kaybedip büsbütün çaresiz kalıyor mu? Kalıyorsa taban gelir eklenir.
- [ ] **Adım 4:** `ColonyEconomyTests` yaz — seçilen eğriyi kalıcı olarak koru.
- [ ] **Adım 5:** Bulguyu spec'e işle.
- [ ] **Adım 6:** Commit — `denge: ekonomi egrisi`

---

### Görev 5: Ekonomi arayüzü

**Dosyalar:**
- Değiştir: `Assets/Colony/Runtime/View/RunHud.cs`

- [ ] **Adım 1:** Besin bakiyesi, kule fiyatları ve alınamayan kulelerin soluk gösterimi.
- [ ] **Adım 2:** Kullanıcıya oynatıp geri bildirim al.
- [ ] **Adım 3:** Commit — `ozellik: ekonomi arayuzu`

---

## Bu Planın Kapsamadıkları

- **Meta ilerleme, biyokütle, kalıcı yetenek ağacı** — Plan 5. Run içi ekonomi oturmadan run'lar arası ekonomi tasarlanamaz.
- **Genetik katmanın run'lar arası evrimi (nemesis)** — Plan 5.
- **Blender asset'leri, ses, animasyon** — Plan 6.
- **Yem tazelemenin maliyeti** — Plan 3'ten devredilen tasarım sorunu. Yem kulesi şu an her dalga bedava tazeleniyor ve bu, spec 3.3'ün istediği "beslemeye devam etme" kararını ortadan kaldırıyor. Görev 4'te ekonomiyle birlikte ölçülüp karara bağlanacak.
