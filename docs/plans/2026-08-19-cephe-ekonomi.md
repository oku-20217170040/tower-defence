# Ekonomi ve Draft — Uygulama Planı (Plan 4)

> Adımlar `- [ ]` kutucuk sözdizimi ile takip edilir.

**Hedef:** Kule sayısını kısıtlamak. Plan 3'ün ölçümü, kule sayısının oyunun **baskın değişkeni** olduğunu gösterdi — ama şu an kuleler bedava ve sınırsız, dolayısıyla oyunun zorluk ayarı diye bir şey yok.

**Ölçülmüş hedef (spec 3.5):** Oyuncu 8-10. dalga civarında 4-5 etkili kuleye ulaşmalı. Koridor başına 1 kule (toplam 3) dalga 10'da kaybettiriyor; koridor başına 2 kule (toplam 6) sıfır sızıntıyla kazandırıyor. Ekonominin işi oyuncuyu bu iki uç arasında tutmak: her dalga bir şeyler kazandırmalı ama hiçbir zaman rahatlatmamalı.

**Mimari:** Ekonomi saf hesap; `Frontline.Core` içinde MonoBehaviour'suz yaşar ve birim testiyle doğrulanır. Draft arayüzü Runtime'da.

## Global Constraints

- `Frontline.Core` MonoBehaviour içermez, Unity tipine bağımlı olmaz
- Plan 1-3'te ölçülmüş sabitler korunur: keşif 0,35 · çekim 3 · tehdit ağırlığı 3 · pusu mevzisi 4 dps · üs canı 30
- Tanımlayıcılar İngilizce, yorumlar Türkçe (aksansız)
- Branch: `tasarim/koloni-reboot`
- Denge sayıları tahminle değil taramayla seçilir

---

### Görev 1: Ekonomi çekirdeği

**Dosyalar:**
- Oluştur: `Assets/Frontline/Core/Economy/RunEconomy.cs`
- Oluştur: `Assets/Frontline/Tests/EditMode/RunEconomyTests.cs`

**Arayüzler:**
- Üretir: `RunEconomy` — `Balance`, `TotalEarned`, `TotalSpent`, `Earn(int)`, `CanAfford(int)`, `TrySpend(int)`, `Refund(int)`

**Tasarım notu:** Kaynak birimi **besin**. Birlik öldürmek ve dalga tamamlamak besin kazandırır. Sızan birlik kazandırmaz — oyuncunun tuzağı çalışmazsa hem can hem gelir kaybeder. Bu, kaybetme sarmalı yaratabilir; Görev 4'te ölçülüp gerekirse taban gelir eklenecek.

- [x] **Adım 1:** `RunEconomyTests` yaz — kazanma, harcama, yetersiz bakiye reddi, negatife düşmeme, toplam sayaçlar.
- [x] **Adım 2:** Testi koştur, başarısızlığı gör.
- [x] **Adım 3:** `RunEconomy` yaz, testleri geçir.
- [x] **Adım 4:** Commit — `ozellik: run ekonomisi cekirdegi`

---

### Görev 2: Kule maliyetleri ve satın alma

**Dosyalar:**
- Oluştur: `Assets/Frontline/Runtime/Economy/EconomyRunner.cs`
- Değiştir: `Assets/Frontline/Runtime/Towers/TowerPlacer.cs`
- Değiştir: `Assets/Frontline/Runtime/Agents/UnitSpawner.cs`
- Değiştir: `Assets/Frontline/Runtime/RunController.cs`

**Tasarım notu:** Kule fiyatları sınıflarına göre farklı olmalı ve bu fark tasarımı taşımalı. Batarya kule ucuz olmalı — çünkü yalnız başına işe yaramıyor (spec 3.4), oyuncunun onu denemesi ucuz olsun. Pusu mevzisi pahalı olmalı — gerçek katliamı o yapıyor. Yem orta.

Aynı türden her yeni kule biraz daha pahalıya gelmeli; aksi halde oyuncu tek stratejiyi spam'ler.

- [x] **Adım 1:** `EconomyRunner` yaz — `RunEconomy`'yi sahneye bağlar, ölüm ve dalga sonu gelirlerini dinler.
- [x] **Adım 2:** `TowerPlacer` satın alma kontrolü yapar; parası yetmezse yerleştirmeyi reddeder ve sebebini söyler.
- [x] **Adım 3:** Aynı türden kule fiyatı artışı.
- [x] **Adım 4:** Commit — `ozellik: kule maliyetleri ve satin alma`

---

### Görev 3: Dalga sonu draft

**Dosyalar:**
- Oluştur: `Assets/Frontline/Core/Draft/DraftOption.cs`
- Oluştur: `Assets/Frontline/Core/Draft/DraftGenerator.cs`
- Oluştur: `Assets/Frontline/Runtime/View/DraftView.cs`
- Oluştur: `Assets/Frontline/Tests/EditMode/DraftGeneratorTests.cs`

**Tasarım notu — spec 2.2'nin gereği:** Sunulan 3 seçenek rastgele değil, **ordunun o anki davranışına göre** ağırlıklandırılır. Sol koridor ezberlenmişse oraya uygun seçeneklerin çıkma olasılığı artar. Bu, adaptif sistemi oynanışa bağlayan ikinci kanaldır ve draft'ı sıradan bir kart seçiminden ayıran şeydir.

Seçenek türleri: bedava kule, kule indirimi, besin, kalıcı pasif (menzil/hasar artışı).

- [x] **Adım 1:** `DraftOption` ve `DraftGenerator` — 3 seçenek üretir, ordu durumunu girdi alır.
- [x] **Adım 2:** `DraftGeneratorTests` — aynı seed aynı seçenekleri verir, seçenekler tekrarlanmaz, ordu durumu dağılımı etkiler.
- [x] **Adım 3:** `DraftView` — dalga sonunda 3 seçenek gösterir, seçim yapılana kadar bekler.
- [x] **Adım 4:** Commit — `ozellik: dalga sonu draft`

---

### Görev 4: Ekonomi eğrisinin dengelenmesi

**Bu planın asıl görevi.**

**Ölçülecek soru:** Oyuncu dalga dalga kaç kuleye ulaşıyor ve bu, Plan 3'te ölçülen kazanma/kaybetme bandına oturuyor mu?

- [x] **Adım 1:** `execute_code` ile tarama: öldürme başına besin × dalga başına besin × kule fiyatı kombinasyonları. Ölçülecek çıktı: dalga dalga alınabilen kule sayısı ve run sonucu.
- [x] **Adım 2:** Hedef bant — 8-10. dalgada 4-5 etkili kule. Erken ulaşırsa oyun kolaylaşır, geç ulaşırsa oyuncu çaresiz kalır.
- [x] **Adım 3:** Kaybetme sarmalı kontrolü: tuzağı çalışmayan oyuncu gelir kaybedip büsbütün çaresiz kalıyor mu? Kalıyorsa taban gelir eklenir.
- [x] **Adım 4:** `EconomyCurveTests` yaz — seçilen eğriyi kalıcı olarak koru.
- [x] **Adım 5:** Bulguyu spec'e işle.
- [x] **Adım 6:** Commit — `denge: ekonomi egrisi`

---

### Görev 5: Ekonomi arayüzü

**Dosyalar:**
- Değiştir: `Assets/Frontline/Runtime/View/RunHud.cs`

- [x] **Adım 1:** Besin bakiyesi, kule fiyatları ve alınamayan kulelerin soluk gösterimi.
- [ ] **Adım 2:** Kullanıcıya oynatıp geri bildirim al. — *bekliyor, görsel doğrulama kullanıcıya ait*
- [x] **Adım 3:** Commit — `ozellik: ekonomi arayuzu`

---

## Bu Planın Kapsamadıkları

- **Meta ilerleme, biyokütle, kalıcı yetenek ağacı** — Plan 5. Run içi ekonomi oturmadan run'lar arası ekonomi tasarlanamaz.
- **Genetik katmanın run'lar arası evrimi (nemesis)** — Plan 5.
- **Blender asset'leri, ses, animasyon** — Plan 6.
- **Yem tazelemenin maliyeti** — Plan 3'ten devredilen tasarım sorunu. Sahte hedef şu an her dalga bedava tazeleniyor ve bu, spec 3.3'ün istediği "beslemeye devam etme" kararını ortadan kaldırıyor. Görev 4'te ekonomiyle birlikte ölçülüp karara bağlanacak.


---

## Ek: Ekonomiyi sıkma denemesi — 20 Ağustos 2026 (dört tur ölçüm)

**Talep:** 8-10. dalgada 6-8 kule (şu an 14,5). Kalibre `RunSimulator` ile ölçüldü.

**Sonuç: bu bant mevcut yapıda ulaşılamıyor.** Ekonomi ikili davranıyor — ya kritik kule kütlesine ulaşıp kazanıyorsun (w8 ≈ 14), ya ulaşamayıp çöküyorsun (w8 ≈ 0-3). Arada bant yok. w8 = 10-11 olan kombinasyonlar bile %0 kazanıyor.

### Tur 1 — gelir ve fiyat düğmeleri
Öldürme geliri (1/2/3) × fiyat artışı (5/8/12) × draft. **Gelir ve fiyat neredeyse hiç fark yaratmadı.** Draft bedava kule vermeyi bırakınca her kombinasyon %0'a düştü. Ekonominin gerçek kaynağı para akışı değil, draft ödülü.

### Tur 2 — draft sıklığı
Bedava kule her dalga / 2 dalgada bir / 3 dalgada bir. Yine uçurum: 1/1 → %100 (w8=14); 1/2 + besin ödülü yoksa → %0. Ara değer yok.

### Tur 3 — taban geliri yükselt, öldürme gelirini kıs
Dalga geliri 20-30, öldürme 0-1, pusu fiyat 24-40. 16 kombinasyondan yalnızca biri kazandı (yine w8=14). Sarmal kırılmadı.

### Tur 4 — asıl sebep bulundu: birlik canı büyümesi

| Can büyümesi | Kazanan kombinasyon sayısı | En düşük kazanan w8 |
|---|---|---|
| **%15 (mevcut)** | 4'te 1 | 14,0 |
| %10 | 4'te 3 | 12,1 |
| %6 | 4'te 3 | 12,5 |

Sarmalın kaynağı **`hpGrowthPerWave = 0,15`**. Birlik canı her dalga %15 büyüdüğü için yetersiz kule sayısı hızla öldürme kaybına, o da gelir kaybına dönüşüyor. %10'a inince eşik yumuşuyor ve ekonomi ayarlanabilir hale geliyor — fiyat 40'a çıkarılsa bile run kazanılabiliyor.

**Yine de 6-8 bandına ulaşılamadı; en düşük kazanan değer 12.** Bandı açmak için kule *sayısını* değil kule *gücünü* ölçeklendiren bir sistem (yükseltme) gerekiyor — bu Plan 5'in konusu.

### Uygulanan karar (kullanıcı, 20 Ağu): can büyümesi %10

`hpGrowthPerWave` 0,15 → **0,10**, ve fiyatlar birlikte yükseltildi (pusu 24→40, batarya 12→20, yem/mayın tarlası 16→26). **İkisi birlikte** yapılmalıydı: tek başına can düşürülseydi oyun kolaylaşırdı.

Ölçülen yeni denge (20 seed):

| | Öncesi (%15, ucuz) | Sonrası (%10, pahalı) |
|---|---|---|
| Kazanma | %100 | %100 |
| Sızıntı | 20,6 | 22,1 |
| Kule (dalga 8) | 15,6 | **12,0** |
| Kule (dalga 5) | — | 7,0 |
| Artan besin | 69,5 | 120,6 |

**Kazanılan:** kule sayısı hedefe yaklaştı, zorluk korundu, ve ekonomi artık ayarlanabilir — %15'te hiçbir düğme çalışmıyordu.

**Kalan sorun:** artan besin 120'ye çıktı. Fiyat yükselince aynı parayla daha az kule alınıyor, kalan para birikiyor. Paranın harcanacak ikinci bir yeri yok (yükseltme sistemi Plan 5).

### Hedef bant artık ULAŞILABİLİR — ama bedeli var

Can %10 olunca 6-8 bandı ölçümde göründü. Dalga gelirini 10'dan 5'e indirmek yetiyor:

| dalga geliri | fiyat adımı | kazanma | sızıntı | w8 | w15 |
|---|---|---|---|---|---|
| 10 (mevcut) | 5 | %100 | 22,5 | 12,0 | 24,0 |
| 5 | 5 | %66 | 27,7 | 8,8 | 16,0 |
| 5 | 8 | %58 | 27,8 | **7,3** | 13,5 |
| 5 (öldürme geliri 1) | 5 | %41 | 28,8 | 6,7 | 8,8 |

Hedef bandı tutturmanın bedeli: kazanma %58-66'ya iniyor ve sızıntı üs canının %93'üne çıkıyor — her run bıçak sırtı olur. **Karar bekliyor.**
