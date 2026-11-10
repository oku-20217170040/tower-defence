# Ekonomi / Buton Envanteri / Tema Karşılaştırma — 2026-09-03 (Tur 6)

Bu tur, önceki turların bitiremediği 3 maddeyi (ekonomi kıyası, buton envanteri,
kule/harita "versiyonları") kapatmak için yapıldı. Yöntem: gerçek telefon
(`T8IZFMQ4KFZHGY9H`) üzerinde `com.mobirix.tdwt` (Turret Defense King, v1.2.44)
adb ile canlı oynandı; ekran görüntüleri `docs/notes/ref_*.png` altında.
Kodda hiçbir değişiklik yapılmadı (kullanıcı talimatı: dengeyi kırma), bu yüzden
test/derleme adımı gerekmedi — 302 test durumu önceki turdan (bu oturumda
dokunulmadı) geçerliliğini koruyor.

## 1) Ekonomi Kıyası

### Referans oyun (bu turda GERÇEKTEN ölçülen: Sonsuz Mod, Dalga 1, taze hesap)
- **Başlangıç altını: 100** [G, ekran görüntüsü `ref_05_start.png` — "100" HUD'da net].
- Can: 10, Dalga 1'de toplam düşman **120** (önceki turların 25-dalgalık
  Aşama modundaki 34-40 düşman/dalga çok daha düşük — **Sonsuz Mod dalga 1
  bile çok daha kalabalık**, modlar arası karşılaştırma yapılmamalı) [G].
- Hiç kule kurulmadan (~20 saniye) üs anında düştü, "TEMİZLENEN DALGA: 0"
  ile YENİLGİ ekranı çıktı [G] — yani oyuncunun altını kuleye HEMEN yatırması
  zorunlu, bekleme payı yok.
- Bu turda kule inşa anını (fiyat okuma) yakalayamadık: Sonsuz Mod haritası
  (kar/buz, Harita 1) başlangıçta tamamen kaya/ağaç örtüsüyle kaplı, boş
  inşa karesi yok; örtü ancak kule ateşiyle/düşman geçişiyle zamanla açılıyor
  (bkz. `2026-09-02` notu, madde 7) — kule kurmadan önce örtü yok etmek
  gerekiyor ve bu turun bütçesi buna yetmedi. **Bu nokta [T]/eksik kaldı.**
- Zaten önceki turlarda (`2026-09-03-referans-oyun-tam-analiz.md`) **inşa
  fiyatları tam ve çapraz doğrulanmış** durumdaydı (statik GÜNCELLE ekranı +
  canlı inşa menüsü aynı sonucu verdi): Makineli Tüfek 100, Top 200, Keskin
  Nişancı 300, Zaman Bozulması 250, Radar 200. Bu turda tekrarlanmadı,
  aynen kabul edildi.
- Yükseltme/Sat: önceki turlarda **iki farklı** kule üzerinde okunmuş veri var:
  kule A'da yükselt 200 / sat 300 (sat > yükselt — muhtemelen kule zaten
  birkaç kademe yükseltilmişti, sat toplam yatırımın oranı), kule B'de
  (Makineli Tüfek) yükselt 100 / sat 75 (**sat = yükseltin %75'i**, taban
  değer). Yani referansta **sat oranı sabit bir yüzde değil, kule/duruma
  göre değişiyor gibi görünüyor** — net formül çıkarılamadı.
- Öldürme başı altın: önceki turda ölçülmüş **~11 altın/kill** (Dalga 22,
  792→917 / 11 kill). Dalga sonu bonusu doğrudan gözlenmedi (yok ya da
  küçük).
- **Yeni bulgu (bu tur):** "REKLAM GÜÇLENDİRMESİ" reklam-teşvik popup'ı net
  okundu: **"Nesneyi yok ettiğinde +5 ALTIN"** [G, `ref_07_wave1b.png`] —
  yani normal (reklamsız) örtü-yok-etme ödülü zaten var ve reklam onu +5
  artırıyor; önceki turun "kaya parçalanınca 15 altın" bulgusuyla birlikte
  okunursa örtü-ekonomisi rastgele değil, obje tipine göre değişen küçük bir
  yan-gelir kanalı.
- **Canlandır (revive) maliyeti: 3000 altın**, "10 canlanma hakkın kaldı"
  [G, yeni bulgu, `ref_10_wave.png`] — parasal/idle döngü, kapsam dışı ama
  not düşülüyor.

### Bizim taraf (kod okundu, GERÇEK sayılar)
`Assets/Frontline/Core/Sim/EconomyPlanner.cs` `Costs.Default()`:
- StartingBalance = **90**
- FoodPerKill = **1**, FoodPerWave = **6**
- Kule fiyatları: Batarya 20, Pusu(Ambush) 40, Sahte(Decoy) 26, Mayın 26,
  Işın(Beam) 34 — **CostStep = 5** (aynı türden her ek kulede +5)

`Assets/Frontline/Runtime/Towers/Emplacement.cs`:
- `UpgradeCost = baseCost * 1.5^fromLevel` (seviye başına **%50** artış)
- `SellRefundRatio = 0.6` → **satış = harcananın %60'ı, sabit** (kule
  tipinden/seviyeden bağımsız, referanstaki değişken orandan farklı ama daha
  basit ve öngörülebilir)
- 3 sabit görsel/güç kademesi (referansla birebir örtüşüyor), seviye başına
  **+%20** stat çarpanı.

### Kıyas/hissiyat notu (SAYI EŞİTLEMEDİ, sadece oran karşılaştırması)
- Başlangıç altını aynı büyüklük mertebesinde (100 vs 90) — **uyumlu**.
- Kill-başı gelir bizde çok daha düşük (referans ~11/kill, biz 1/kill) AMA
  bizim kule fiyatları da çok daha düşük (20-40 vs 100-300) — **oranlar
  benzer** (referans: ucuz kule ≈ 10-20 kill; biz: ucuz kule ≈ 20 kill).
  Ölçek farklı, ORAN tutarlı görünüyor — büyük sapma yok, testleri kırma
  riskiyle değiştirmeye gerek YOK.
- Bizim satış oranı (%60 sabit) referansın gözlenen iki örneğinin (%75 ve
  yatırım-üstü) arasında/altında — kabul edilebilir, referans da net bir
  formüle sahip görünmüyor (muhtemelen kademeye göre iç tablo).
- Yükseltme çarpanı bizde %50/seviye — referansın tam ara-maliyet eğrisi
  hiç okunamadı (hesap tüm dalları maksimize etmişti), kıyaslanamıyor.
- **Sonuç: büyük bir sapma YOK, ekonomiye dokunulmadı** (kullanıcı
  talimatına uygun).

## 2) Buton Envanteri

Bu turda referans tarafında **yeni 2 ekran** eklendi (önceki notlarda yoktu):
**Sonsuz Mod (SONSUZ MOD)** ekranı ve onun **YENİLGİ** ekranı. Bunlar dışında
kalan envanter önceki 3 turda (`2026-09-02`, iki adet `2026-09-03-...`)
zaten kapsamlı çıkarılmıştı ve bu turda TEKRAR tıklanmadı (bütçe / tekrar
önleme). Aşağıdaki tablo o notların BİRLEŞTİRİLMİŞ hâli + bu turun 2 yeni
satırı.

| Referans ekran/buton | Konum/işlev | Bizim karşılığı | Durum |
|---|---|---|---|
| Ana menü OYNA/AŞAMA/SONSUZ MOD/RASTGELE MOD/GÜNCELLE/DÜKKAN sekmeleri | Sol dikey menü | `MainMenuScreen.cs`: sadece OYNA/AYARLAR/ÇIKIŞ | **Eksik**: AŞAMA seçim ızgarası, Sonsuz/Rastgele mod, GÜNCELLE (kalıcı meta), DÜKKAN yok — hepsi bilinçli kapsam dışı (idle-progression, önceki turlarda karara bağlandı) |
| HUD: can/altın/dalga (sol üst) | Play içi | `RunScreen.cs` HUD | **Var** |
| HUD: kalan düşman sayacı (üst orta) | Play içi | Var (önceki analizde teyitli) | **Var** |
| HUD: sarf malzemesi şeridi (sağ üst, 5 ikon) | Bariyer/Vorteks/Bomba/Mayın/Tornado | Yok | **Eksik** (kapsam dışı — consumable ekonomisi) |
| Sol dikey sütun: kamera/foto, +100 reklam, yıldız/reklam-yükselt, X2 hız, ayar, duraklat | Play içi | `RunScreen.cs`: sadece hız + duraklat + ayar var; reklam ödülleri yok | **Kısmen eksik** (reklam ödülleri kapsam dışı, hız/duraklat/ayar var) |
| Kule seçim radial: yükselt(üst)/sat(alt) | Kule tıklanınca | `TowerPlacer`/`Emplacement`: yükselt+sat var | **Var** |
| İnşa menüsü (5 kule, artı-radial) | Boş kareye dokun | `RadialBuildMenu.cs` | **Var** (önceki turda doğrulandı) |
| Duraklatma menüsü: YARDIM/DEVAM ET/AYAR/YENİDEN BAŞLAT/ÇIKIŞ | Duraklat ikonu | `PauseOverlay.cs`: DEVAM ET/AYARLAR/YENİDEN BAŞLAT/ÇIKIŞ (YARDIM bilinçli atlandı) | **Var** (1 buton bilinçli eksik) |
| Ayarlar: müzik/efekt aç-kapa | Duraklat/ana menü | `SettingsOverlay.cs`: MÜZİK/EFEKT | **Var** |
| Zafer/Yenilgi ekranı: ÇIK/YENİDEN BAŞLAT/CANLANDIR(3000 altın) | Oyun biter | `RunScreen.cs`: YENİDEN BAŞLAT + ÇIKIŞ var, CANLANDIR YOK | **Kısmen eksik** (CANLANDIR bilinçli atlandı — parasal döngü kapsam dışı) |
| **[YENİ] Sonsuz Mod ekranı**: 7 harita seçici + haftalık lider tablosu (top-8 + oyuncu satırı) | Ana menüden | Yok | **Eksik, kapsam dışı** (leaderboard/multiplayer-ranking, büyük iş, bu turda kodlanmadı) |
| **[YENİ] Sonsuz Mod Yenilgi ekranı**: "TEMİZLENEN DALGA: N" + ÇIK/YENİDEN BAŞLAT/CANLANDIR | Sonsuz mod biter | Zaten normal yenilgi ekranımızla aynı ailede (bkz. üstteki satır) | **Kapsanmış sayılır** |
| AŞAMA (bölüm) ızgarası: 4 sütun, 0-3 yıldız, ilk-temizleme +3000/tekrar +300 ödülü | Ana menü | Yok (bizde tek sürekli sandbox run) | **Eksik, kapsam dışı** (bölüm/idle-progression sistemi) |
| DÜKKAN: gerçek-para paketleri | Ana menü | Yok | **Kapsam dışı, dokunulmayacak** (parasal) |

**Bu turda kodlanan yeni öğe: YOK.** Önceki turda zaten en kritik 2 gerçek
hata (PauseOverlay/DraftView çakışması, 2 NRE) üst ajan tarafından
düzeltilmişti; bu tur onları tekrar test etmedi (kapsam dışı, görev net
şekilde 3 maddeye odaklandı). Eksik kalan öğelerin tamamı ya bilinçli
kapsam-dışı (parasal döngü, idle-progression, leaderboard) ya da önceki
turlarda zaten "sıradaki iş" olarak işaretlenmiş; bu tur önceliklendirme
sonucu YENİ bir eksik bulup acilen kodlanacak kritiklikte bir şey
saptamadı.

## 3) Kule/Harita "Versiyonları" — NETLEŞTİRME

### Kule versiyonları: yükseltme kademesi, KOZMETİK SKIN DEĞİL
Önceki turda (`2026-09-03-referans-oyun-tam-analiz.md`) GÜNCELLE ekranı tek
tek tarandı: her kule tipinin sadece **3 kalıcı güç/görsel seviyesi** var
(1→2→3 yıldız), skin/renk seçimi (aynı seviyede farklı kozmetik varyant)
YOK. Bu tur bunu değiştirecek yeni kanıt bulmadı — **soru kapandı: "kule
versiyonu" = yükseltme kademesi, harita teması değil**. Bizim
`Emplacement.Level` (1-3, `MaxLevel=3`) + `ArtLibrary.levelTurrets[]` sistemi
bunu birebir karşılıyor.

### Harita temaları: BİRDEN FAZLA, kanıt bu turda GÜÇLENDİ
- Önceki notlarda kar/buz haritası (`2026-09-02`, Dalga 21-25) VE
  çöl/kaktüs haritası (`2026-09-03-tam-analiz`, Aşama 1 ve 66) ayrı ayrı
  gözlenmişti.
- **Bu tur yeni kanıt:** Sonsuz Mod menüsü açıkça **"HARİTA 1" — "HARİTA 7"**
  arası 7 seçilebilir harita listeliyor [G, `ref_04_infinite.png`] ve
  seçili olan (Harita 1) kar/buz teması gösteriyor. Yani referans oyunda
  **en az 7 farklı harita şablonu var**, bunların en az ikisi (çöl/kaktüs,
  kar/buz) tamamen farklı zemin/dekor paleti kullanıyor — **tek tema
  DEĞİL, kanıtlanmış çoklu tema sistemi**.
- Bizim tarafta `Assets/Frontline/Core/MapGen/SceneryMap.cs`,
  `Assets/Frontline/Runtime/View/SceneryView.cs`, `ArtLibrary.cs` içinde
  `theme`/`Theme`/`biome` gibi bir kavram **hiç yok** (grep: 0 eşleşme) —
  yani şu an **tek sabit zemin/dekor paleti** var, tema DEĞİŞTİRME
  mekanizması mevcut değil.
- **AÇIK İŞ (bu turda kodlanmadı, kapsam bilinçli büyütülmedi):** Çoklu
  harita teması (en az 2. bir tema: örn. kar/buz) eklemek hem yeni sprite
  seti (Blender üretimi, ayrı bir tur) hem de `SceneryMap`/`ArtLibrary`'ye
  bir tema-seçim parametresi eklemeyi gerektiriyor — küçük bir kod
  iskeleti bile şu an mevcut tek-tema varsayımını (muhtemelen birçok yerde
  sabit sprite referansı) kırabileceği için kullanıcı onayı olmadan
  başlanmadı. **Karar kullanıcıya bırakıldı.**

## Kanıt Dosyaları (bu turda üretildi)
`docs/notes/ref_00_launch.png` … `ref_14_build2.png` (15 ekran görüntüsü,
gerçek adb `screencap`, hiçbir APK/asset çıkarılmadı).

## Bütçe/dürüstlük notu
Kule inşa fiyatını bu turda CANLI yeniden ölçemedik (Sonsuz Mod haritası
baştan tamamen örtülü, örtü temizleme fırsatı bulunamadan üs düştü) — bu
tek nokta [T]/önceki tur verisine dayanıyor, ama önceki tur verisi zaten
statik+canlı çapraz doğrulamalıydı, güvenilir kabul edildi. Diğer tüm
iddialar bu tur veya önceki turlarda [G] etiketli, ekran görüntüsü kanıtlı.
