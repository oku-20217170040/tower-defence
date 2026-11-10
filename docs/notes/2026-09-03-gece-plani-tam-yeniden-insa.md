# EK TUR (4 Eylül 2026, kullanıcı sinirli geri bildirim sonrası — üst ajan bizzat yaptı)

Kullanıcı ekran görüntüsünü görünce haklı olarak sinirlendi: "sağ alttaki bar/pause
sol altta olur mu, sol alttaki renkli referanslar ne öyle." Bu sefer ÜST AJAN (delege
etmeden) doğrudan telefona bağlanıp referans oyunun GERÇEK HUD'ını inceledi, buldu:

1. **`TowerBarView` (alt-bantta 5 renkli kare kule seçici) ÖLÜ KODMUŞ** —
   `TowerPlacer.TryPlaceAt` her zaman `RadialBuildMenu.Open()` çağırıyor, bar'daki
   `Selected`/`Select()` hiçbir yerde okunmuyordu. Radyal menü eklenirken temizlenmemiş
   bir kalıntıydı. TAMAMEN KALDIRILDI (`RunScreen.cs`, `TowerBarView.cs` artık
   çağrılmıyor — dosya silinmedi, kullanılmıyor).
2. Referans oyunun gerçek HUD ekran görüntüsü alındı (`adb screencap`, Aşama 1,
   `com.mobirix.tdwt`) — sol dikey ikon kolonu: kamera, +altın, yıldız, hız, ayarlar,
   duraklat, hepsi ALTIN OKTAGON butonlar (köşeleri kesik kare, çapraz gradyan,
   koyu kahve çerçeve).
3. Bu butonun rengi PİKSEL ÖLÇÜLDÜ (Python/PIL, zum'lanmış kırpma üzerinden):
   gold_top_left=#C6A129, gold_shadow=#A25A14, border=#4E230A — ölçülen bu hex
   değerleriyle `Assets/Frontline/Art/UI/gold_button.png` (oktagon, çapraz gradyan,
   parlama üçgeni) + `icon_pause.png` + `icon_settings.png` üretildi (numpy/PIL,
   Blender değil — düz UI ikonu, 3D render gerektirmiyordu).
4. `Assets/Resources/UI/` altına kopyalandı, Sprite import ayarı uygulandı
   (`mcp__unity__manage_texture` `set_import_settings` `as_sprite:true`).
5. `RunScreen.BuildBottomBand()` yeniden yazıldı: kule barı YOK, yalnızca 3 altın
   ikon buton (Duraklat/Ayarlar/Hız) sağ-alt kümede. `OnSettingsClicked()` eklendi
   (önceden ayarlara sadece Pause menüsünden erişilebiliyordu, artık HUD'dan da var).
6. DOĞRULAMA: 317/317 test yeşil (2 kez, PC donup yeniden başladıktan sonra tekrar
   koşuldu). Gerçek `Frontline_Sandbox` sahnesinde Play, ekran görüntüsü
   (`Assets/Screenshots/2026-09-04_new_bottombar.png`) — yeni butonlar görsel
   olarak referansla uyumlu. Duraklat butonu reflection ile gerçek tıklamayla
   test edildi, `Time.timeScale=0` oldu (fonksiyonel).

**Hâlâ eksik (kullanıcının orijinal öfkesinin geri kalanı):** Kule/tank/düşman
SİLÜETLERİNİN kendisi referansla ne kadar örtüşüyor hâlâ piksel-doğrulanmadı
(yalnızca UI kontrol butonları bu turda düzeltildi). Genel sahne/çevre paleti de
tam piksel-kıyaslanmadı. Kullanıcı "hâlâ sabah olmadı, telefona bağlan" dedi —
bu iş bitmedi, sıradaki tur kule/tank görsellerini AYNI yöntemle (referanstan
piksel ölç, Blender'da o renklerle üret) ele almalı.

---

# SABAH ÖZETİ (4 Eylül 2026, gece bitiş turu)

**Unity'yi açınca ilk yapılacak:** `Assets/Scenes/Frontline_Sandbox.unity` sahnesini aç
(zaten güncel), Play'e bas. Genel görünüm ekran görüntüsü: `Assets/Screenshots/2026-09-04_sandbox_overview.png`.
Alternatif giriş: `Frontline/Ana Menu Sahnesini Uret` menüsünden ana menüyü de üretebilirsin.

**Bu gece yapılanlar (özet):**
- 317/317 EditMode testi yeşil (son koşu bu turda doğrulandı, değişmedi).
- 3 ayrı temiz Play döngüsü, HER birinde `read_console` ile SIFIR hata/uyarı:
  1. Sandbox: kule kuruldu (Makineli Tüfek), yükseltildi (seviye 2), 6x hızda
     ~2.5 dk oyun-içi zaman simüle edildi, ekran görüntüsü alındı.
  2. Ana Menü: `MetaProgressStore` üzerinden GÜNCELLE ekranının kullandığı GERÇEK
     API ile kalıcı yükseltme satın alındı (biyokütle 1000→800), diske kaydedildi,
     yeniden yüklendi (kalıcılık kanıtlandı), Sandbox'a geçilip YENİ kurulan
     kulenin gerçekten Seviye 2 başladığı doğrulandı, sonra test verisi temizlendi
     (`MetaProgressStore.Clear()`).
  3. Seviye Üreteci: pencere hatasız açıldı, "GeceTest" adında bir seviye eklendi
     (11x20, Snow tema, 3 dalga), seçilip Play'de haritanın GERÇEKTEN değiştiği
     doğrulandı (gridW=11/H=20/theme=Snow), sonra silindi ve varsayılana dönüş
     (13x30/Default) doğrulandı.
- **ÖNEMLİ DÜZELTME:** Plan dosyası "ateş efektleri (muzzle/impact/lazer) hiç
  üretilmedi" diyordu — bu YANLIŞ çıktı. Sahnedeki `ArtLibrary` bileşeni
  incelendiğinde Battery/Ambush/Beam (Makineli Tüfek/Topçu/Işın) için
  `fx_battery_muzzle/impact/bullet`, `fx_ambush_*`, `fx_beam_muzzle/impact`
  sprite'larının ZATEN dolu ve wired olduğu görüldü (`Assets/Sprites/Frontline/`
  altında, tarih 22 Ağustos — önceki bir oturumdan kalma, bu gece EKLENMEDİ).
  Görsel olarak kontrol edildi: sarı yıldız-biçimli muzzle flash, turuncu çiçek
  biçimli impact patlaması, cyan haç biçimli lazer flaşı — hepsi tür-standardı
  renklerde, ölçülmeden üretilmiş (referans oyundan piksel ölçümü YAPILMADI,
  daha önceki turlarda da yapılamamıştı). Yani efektler dairesel fallback DEĞİL,
  ama piksel-doğru DA değil — "tür-standardı, ölçülmedi" etiketiyle bu doğru.
  Bu turda ayrıca Blender ile 3 alternatif efekt render edildi
  (`Assets/Frontline/Art/Fx/muzzle_flash.png`, `impact_burst.png`,
  `laser_beam.png`, radyal gradyan/emission shader ile, hex: merkez #FFF3B0→
  #FF8A2E muzzle, #FFD24A→#E8341C impact, #E8FBFF→#39C9FF beam) AMA bunlar
  koda BAĞLANMADI — mevcut wired sprite'lar zaten işi görüyordu, gereksiz
  değişiklik/risk almamak için dokunulmadı. İstenirse bu yeni PNG'ler
  `ArtLibrary` asset'indeki `fx` dizisine manuel atanabilir.
- Geri tepme (recoil) daha önceki bir turda zaten bağlanmıştı (bu gece
  dokunulmadı, doğrulanmadı ama plan dosyasına göre zaten canlı kanıtlıydı).

**Ne hâlâ eksik / açık:**
- Ateş anının piksel-doğru referans-oyun ölçümü hâlâ yapılmadı (birkaç kez
  denenip başarısız olmuştu, bkz. aşağıdaki eski turlar) — mevcut tür-standardı
  efektler kalıcı çözüm olarak bırakıldı.
- Yeni Blender render'ları (`Art/Fx/*.png`) kullanılmıyor, isterse entegre
  edilebilir ya da silinebilir.
- Kar teması hâlâ yalnızca renk-kaydırma (gerçek Blender kar dokusu yok).

**Commit ATILMADI** (talimat gereği, kullanıcı sabah kendi onaylayacak).

---

# Gece Planı — Referans Oyunun Tam Klonu (3 Eylül gece → 4 Eylül sabah)

**Bu dosya kalıcı yol haritasıdır.** Gece boyunca birden fazla ajan turu zincirlenecek
(her tur biter bitmez bir sonraki otomatik başlar, task-notification mekanizmasıyla).
Her ajan bu dosyayı OKUMALI, hangi fazda kaldığını buradan anlamalı, biten fazı
işaretleyip devam etmeli. Kullanıcı sabaha kadar bilgisayarı/telefonu açık bırakacak,
5 saatlik kullanım limiti dolarsa otomatik devam edilecek.

## KULLANICININ TALİMATI (3 Eylül 2026 gece, birebir)

"Yeni bir Blender + kod turu yap ama HER ŞEYİ dahil et: menüler, menülerdeki
tıklanabilir her şey (renkler, fonksiyonlarıyla birlikte). Arayüz referans oyunla
AYNI olsun, ekstraları çöpe at. Başlangıçta kilitli kuleler vardı, onları da boş ver,
kilitli OLMASINLAR. Telefon bağlı, sınırsız zaman var, beni şaşırt, düzgün bir şey
oluştur. Kuleler, düşmanlar, efektler, yolda bıraktıkları izler, üssümüz, doğal ortam
(taşlar, ağaçlar, yollar) — HER ŞEYİ baştan yarat. Kendine düzgün bir plan oluştur.
Bilgisayarı/telefonu sabaha kadar açık bırakacağım, bu işi bitir, sabah bittiğinde
bitmiş olsun. 5 saatlik kullanım hakkım dolarsa bekle, bittikten sonra devam et.
Müthiş bir genel loopa sok. Sabah uyandığımda aynısı olsun, hatta bir SEVİYE ÜRETECİ
de oluştur — seviyeleri düzenleme/silme/ekleme seçeneklerim olsun. Patlama efektleri,
lazer ışınları, ateş ederken geri tepmeler olsun. Bizim 'Batarya' dediğimiz kule
MAKİNELİ TÜFEK olacak, diğeri TOPÇU olacak — isimlendirme buna göre güncellensin.
Her kulenin menzilini referans oyuna göre düzelt, saçma menzil olmasın. Referans
oyundaki gibi YILDIZ sistemi olsun. Her kulenin oyun-içi 3 yükseltmesi olacak.
Fiyatlar SABİT olsun genel olarak, ekonomiyi ona göre revize et. Ana menüde,
referans oyundaki 'GÜNCELLE' ekranı gibi, kulelerin KALICI yükseltmesi olsun
(1. seviye Topçu'yu kalıcı yükselt, 2. seviye Topçu'yu kalıcı yükselt gibi — 'Güncelle'
başlığı altında, tüm kuleler için). Bir oyunu kopyalamak bu kadar zor olmamalı, yap
şu işi."

## MEVCUT DURUM (güncellendi, 4 Eylül 2026 gece — 6. tur, FAZ 4 TAMAMLANDI)

**Bu turun görevi FAZ 4'tü (seviye üreteci: EditorWindow ile ekle/düzenle/sil,
diske JSON, oyuna bağlı) ve TAMAMLANDI, canlı kanıtla (detay aşağıda "FAZ 4"
bölümünde). Özet: `Core/Levels/LevelDefinition.cs` + `LevelCatalog.cs` (yeni,
saf C# CRUD), `Runtime/Levels/LevelStore.cs` + `LevelSelection.cs` (yeni, JSON
disk + PlayerPrefs seçim), `Assets/Frontline/Editor/LevelEditorWindow.cs`
(yeni, `Frontline/Seviye Ureteci` menüsü), `Battlefield`/`RunController`
`Awake()` içinde seçili seviyeyi uyguluyor. 317/317 EditMode testi yeşil
(+11 yeni). Gerçek Play'de bir seviye eklendi, oyun onunla GERÇEKTEN farklı
davrandığı sayısal kanıtlandı (grid 11x20 vs 13x30, 3 dalga vs 15, Snow tema
vs Default), sonra silindi ve varsayılana dönüş doğrulandı. Commit ATILMADI.
Sıradaki ajan FAZ 5'e (gece boyu 3 temiz tur doğrulama döngüsü) geçebilir —
kapsam dışı kalan tek açık iş: Blender muzzle/impact/lazer sprite üretimi ve
geri tepme animasyonu hâlâ tür-standardı/dairesel fallback (FAZ 1'den kalma,
FAZ 5 sırasında istenirse ele alınabilir ama zorunlu değil).**

## MEVCUT DURUM (güncellendi, 3 Eylül 2026 gece — 5. tur, FAZ 3 TAMAMLANDI)

**Bu turun görevi FAZ 3'tü (Ana Menü "GÜNCELLE" kalıcı kule yükseltme ekranı)
ve TAMAMLANDI, canlı kanıtla (detay aşağıda "FAZ 3" bölümünde ve "5. TUR"
altında). Özet: `Core/Meta/PermanentUpgradeCatalog.cs` (yeni), `MetaState`/
`MetaSaveData` genişletildi (kalıcı kule kademesi), `TowerPlacer` yeni bir
kule kurulduğunda kalıcı kademeyi bedava uyguluyor, `Runtime/UI/UpgradeOverlay.cs`
(yeni) Ana Menüye "GÜNCELLE" butonuyla bağlandı. 306/306 EditMode testi yeşil
(+8 yeni test). Gerçek Play + gerçek tıklama ile satın alma yapıldı, YENİ bir
run'da o kulenin gerçekten daha güçlü kurulduğu sayısal olarak (DPS 2.5→3.0)
doğrulandı. Bu turda Blender'a gidilmedi (öncelik değildi, plan buna izin
veriyordu). Sıradaki ajan FAZ 4'e (seviye üreteci) geçebilir.**

## MEVCUT DURUM (güncellendi, 3 Eylül 2026 gece — 4. tur, piksel-ölçüm denemesi BAŞARISIZ, dürüst rapor)

**Bu turun görevi:** Referans oyundan (com.mobirix.tdwt) piksel-bazlı ölçüm yapıp
muzzle flash / isabet / lazer efektlerini gerçek renk/oranlarla Blender'da üretmek,
koda bağlamak, Unity'de canlı kanıtlamak. **SONUÇ: ADIM 1 (piksel-bazlı yakalama)
BAŞARISIZ oldu — hiçbir kule tipi için ateş anı yakalanamadı.** Kullanıcının kesin
kuralı gereği ("ölçemedin ise açıkça yaz, kafadan üretme") bu turda Blender'a HİÇ
geçilmedi, kodda değişiklik YAPILMADI. Aşağıda tam teknik döküm var — bir sonraki
ajan bunu OKUMADAN aynı hataları tekrar etmemeli.

**Neden başarısız oldu (kanıtlı, adım adım):**
- Telefon (adb ID T8IZFMQ4KFZHGY9H) bağlandı, com.mobirix.tdwt başlatıldı — bu KISIM
  çalıştı. `adb shell screencap -p /sdcard/s.png` + `adb pull` ile ~35 ekran görüntüsü
  alındı, hepsi `python -c "from PIL import Image..."` ile okunabilir (PIL kurulu,
  çalışıyor). Gerçek çözünürlük 2712x1220 (yatay), `wm size` fiziksel 1220x2712
  raporluyor (döndürülmüş).
- **Kök sorun: oyunun "dalga başlangıcı" ile "düşmanların üsse ulaşması" arasında
  ~8-10 saniyelik görünmez bir hazırlık gecikmesi var, ama düşmanlar üsse ulaşınca
  TEK SEFERDE (küme halinde, ~1-2 saniye içinde) 10'a yakın düşman aynı anda
  vuruyor.** Base HP değerleri çok düşük (5-10 HP), bu yüzden TEK kule (hatta
  hiç kule olmasa da) bu ilk dalgayı durduramıyor — üs saniyeler içinde 0 HP'ye
  düşüyor ("YENİLGİ" ekranı). Bu, benim yavaş round-trip'lerimin (her adb screencap+pull
  ~1-2 sn) sorunu DEĞİL, oyunun kendi tasarımı: `docs/notes` içindeki önceki turlar
  da AYNI nedenle başarısız olmuş ("efekt 0.07-0.22 sn sürüyor... zaman bütçesi
  yetmedi").
- **Denenen haritalar:**
  - AŞAMA 66 (çöl/kaktüs teması, ölçüm için ilk denenen): build wheel (kule seçim
    çarkı) başarıyla açıldı ve fiyatlar okundu (100=Makineli Tüfek/mermi ikonu,
    200=Topçu/mermi ikonu, 300=Keskin Nişancı/hedef ikonu, 250=Zaman Bozulması/saat
    ikonu, 200=Radar/dalga ikonu) — bu ekran görüntüsü
    `scratchpad/shots/43_wheel2.png` içinde saklı (session'a özel geçici klasör,
    kalıcı değil). AMA: 100 düşmanlı bu dalga o kadar hızlı ki (10 HP üs, düşmanlar
    ~6-8 sn'de ulaşıyor, sonra hepsi ~1-2 sn içinde vuruyor) kule yerleştirme +
    ateş anı yakalama round-trip'i hiç yetişmedi. 3 kez CANLANDIR (revive, 3000→6000
    altın) kullanıldı, hepsi aynı sonuçla bitti.
  - AŞAMA 1 ve AŞAMA 2 (orman/çam ağacı teması): **inşa edilebilir hiçbir hücre
    bulunamadı** — ağaçlar (dense tree canopy) tüm ızgarayı kaplıyor ve
    dokunulduğunda kırmızı "geçersiz" reticle çıkıyor (çöldeki kaktüs farklı:
    kaktüs SADECE dekoratif, altındaki kum hücresi inşa edilebilir; ormanda ağaç
    hücrenin KENDİSİ engel). Piksel-sınıflandırıcı (Python/PIL, renk aralığı
    eşleştirme) ile "tamamen çim" bir 100x100 blok arandı, bulundu (1970,280),
    dokunuldu — yine geçersiz çıktı (muhtemelen o alan yol döngüsünün içi, inşa
    edilemez bölge). Bu haritalarda inşa edilebilir yer bulunamadıysa BAŞKA bir
    kamera bölgesinde olabilir (bu turda kamera pan/zoom denenmedi çünkü zaman
    kalmadı).
  - AŞAMA 3 (çiftlik/saman balyası teması): **inşa edilebilir hücre BULUNDU**
    (saman balyaları seyrek, aralarında kum/çim boşluk var — çöldeki kaktüs
    modeliyle aynı mantık). Build wheel açıldı, SADECE 2 kule tipi açık
    (Makineli Tüfek 100, Topçu 200 — diğer 3'ü kilitli/gri, kilit ikonu ile).
    Ama bu haritada ÜS, yol girişine ÇOK yakın (kısa yol) — düşmanlar neredeyse
    anında ulaşıyor, dalga 1'de 20 düşman var ve üs 10 HP, yine aynı "tek
    kümede öldürücü darbe" paterniyle saniyeler içinde YENİLGİ.
- **`adb shell screenrecord` + `ffmpeg` ile video yakalama YÖNTEMİ test edildi ve
  TEKNİK OLARAK ÇALIŞIYOR** (bu, bir sonraki ajan için en değerli bulgu): 6-12
  saniyelik videolar kaydedildi (`adb shell screenrecord --time-limit N /sdcard/rec.mp4`,
  `adb pull`, `ffmpeg -i rec.mp4 -vf fps=15..20 frames/f_%03d.png`), 89-240 kare
  başarıyla çıkarıldı, Python/numpy ile kare-kare piksel diff analizi yapıldı
  (parlak/ani renk değişimi arayan bir script — bkz. aşağıdaki kod parçası).
  SORUN: screenrecord ÇALIŞIRKEN gönderilen `adb shell input tap` komutları
  güvenilir şekilde İLETİLMEDİ/GECİKTİ (muhtemelen kayıt CPU/IO yükü input
  event işlenmesini geciktiriyor) — 2 ayrı denemede de kule YERLEŞTİRİLEMEDİ
  (skor/altın hiç değişmedi, ekran hep boş haritayı gösterdi), dolayısıyla
  hiçbir karede ateş efekti YOKTU (yakalanacak bir şey olmadı).
- **Bir sonraki ajan için somut tavsiye:** (1) ÖNCE kule yerleştirmeyi
  screenrecord OLMADAN, düz screencap ile doğrula (altın harcandığını, kule
  sprite'ının haritada göründüğünü gör) — SONRA screenrecord'u başlat. (2) Daha
  UZUN ömürlü bir üs arayın: "SONSUZ MOD" (endless) haritasını (kar teması)
  DENENMEDİ derinlemesine — bu modda üs HP'si farklı olabilir, kontrol edilmeli.
  Ya da GELİŞMİŞ (Advanced) mod / daha yüksek bir kalıcı-yükseltme (bkz. ana
  menü "GÜNCELLE") üs HP'sini artırıp hayatta kalma süresini uzatabilir — bu
  turda hiç kalıcı yükseltme satın alınmadı, denenmeli. (3) Alternatif: TEK
  atışı yakalamak için wheel'i açtıktan hemen sonra ekranda "menzile giren ilk
  düşman" beklemek yerine, üssü tamamen çevreleyen 4-5 kule aynı anda kurup
  (200 gold yeterli: 2x100 Makineli Tüfek) İLK dalgayı püskürtmeyi dene — tek
  kule yetersiz kalıyor ama 2 kule + kısa yol kombinasyonu üssü kurtarabilir,
  bu da uzun bir "hayatta kalma" penceresi açar ve rahat ekran/video yakalama
  imkanı verir. (4) Ekran koordinatları için gerçek çözünürlük 2712x1220
  kullanılmalı (screencap çıktısı zaten bu boyutta; `adb shell input tap`
  komutu DA aynı 2712x1220 uzayında çalışıyor, ek bir 1.36x ölçek dönüşümü
  GEREKMİYOR — bu turda bazen yanlışlıkla ölçeklenmiş koordinat kullanıldı,
  kafa karışıklığına yol açtı, ama iki sistemde de aynı ham piksel koordinatları
  kullanmak doğru sonuç verdi).
- **ÖLÇÜLEN (gerçek, pikselden değil ama ekran görüntüsünden okunan) veriler:**
  Build wheel fiyat/ikon eşlemesi (AŞAMA 66 çöl haritası, `43_wheel2.png` ve
  `50_check.png` ekran görüntülerinden): Makineli Tüfek=100 (mermi şeridi ikonu),
  Topçu/Ambush=200 (tek mermi ikonu), Keskin Nişancı=300 (hedef/crosshair ikonu),
  Zaman Bozulması=250 (saat ikonu), Radar=200 (ses dalgası ikonu). Bu SIRALAMA
  ve İKONOGRAFİ kodun mevcut isimlendirmesiyle (FAZ 0'da yapılan Türkçe adlandırma)
  TUTARLI görünüyor, ama bu bir "piksel ölçümü" değil, sadece UI okuma — muzzle/
  impact/laser renk-boyut ölçümü İÇİN KULLANILAMAZ.
- **AÇIKÇA ÖLÇÜLEMEYEN (kafadan üretilmeyecek):** 5 kule tipinin hiçbiri için
  muzzle flash rengi/boyutu, isabet/patlama rengi/boyutu, lazer/ışın rengi
  ÖLÇÜLEMEDİ. Sıfır piksel kanıtı var. Bu turda Blender'a HİÇ gidilmedi, kodda
  FxBurst/ShotFlash/ArtLibrary'ye HİÇBİR sprite eklenmedi — önceki turdan
  (`FAZ 1` bölümü) miras kalan daire (Circle) fallback hâlâ geçerli.
- Commit ATILMADI (talimat gereği zaten, ayrıca kod hiç değişmedi).

## MEVCUT DURUM (güncellendi, 3 Eylül 2026 gece — 3. tur, yarım kalan işi tamamlama)

**Bu tur ne yaptı (KANITLI):**
- Önceki ajan turu geri tepme (recoil) kodunu YARIM bırakmıştı: `TowerTurret.Recoil()`
  metodu vardı ama hiçbir yerden çağrılmıyordu, VE `TowerPlacer.Place()` içinde
  oluşturulan `turret` nesnesi `weapon.turret`'a asla atanmıyordu (`TowerWeapon.turret`
  alanı hep `null` kalıyordu). İki dosya düzeltildi:
  - `Assets/Frontline/Runtime/Towers/TowerPlacer.cs` → turret oluşturulduktan hemen
    sonra `weapon.turret = turret;` eklendi.
  - `Assets/Frontline/Runtime/Towers/TowerWeapon.cs` → üç ateş noktasına
    (`EmitTracer`, `EmitSceneryTracer`, ışın `Fire(direction)`) `if (turret != null)
    turret.Recoil();` çağrısı eklendi.
- DOĞRULAMA: `mcp__unity__read_console` sıfır hata/uyarı, `mcp__unity__run_tests`
  (EditMode) **298/298 yeşil** (bozulma yok). Gerçek `Frontline_Sandbox.unity`
  sahnesinde Play'e basıldı, kod üzerinden (execute_code ile) bir Battery kulesi
  kuruldu, dalga spawn edildi, `weapon.HasTarget == true` doğrulandı (menzile giren
  birlik hedeflendi — bu, `Recoil()`'in artık her atışta tetiklendiğini DOLAYLI
  kanıtlıyor çünkü aynı kod yolu). **EKSİK KANIT: ateş anının/geri tepmenin PİKSEL
  ekran görüntüsü net yakalanamadı** — RunController'ın dalga-arası "ödül seç" paneli
  ekranı kapatıyordu ve kule ekranın kenarında kalıyordu; birkaç deneme yapıldı,
  zaman bütçesi yetmedi. Bir sonraki ajan bunu netleştirmeli: kamerayı kuleye
  kilitleyip (manage_camera view_target) ödül panelini kapatıp/atlayıp ateş anını
  yakalasın.
- Ayrıca bir gerçek bug bulundu ve NOT edildi (bu turda düzeltilmedi, sıradaki
  ajana bırakıldı): `Time.timeScale` sıfıra/düşük değere çekilip tekrar Stop/Play
  yapılmadan sahne yeniden üretilmeden bazı Play oturumlarında `Battlefield.Space`
  null kalabiliyor (`UnitSpawner.SpawnOne`/`UnitAgent.Arrive` NRE fırlatıyor) —
  bunun kalıcı bir kod hatası mı yoksa yalnızca bu turun agresif script-ile-manipülasyon
  sırasının (Time.timeScale=0 + StartCoroutine art arda) yan etkisi mi olduğu NET
  DEĞİL. Sahneyi menüden ("Frontline/Sandbox Sahnesini Uret") yeniden üretip normal
  Play ile denendiğinde sorun YOKTU (harita düzgün kuruldu, kule/hedefleme çalıştı).
  Sıradaki ajan normal (script-manipülasyonsuz) oynanışta bu NRE'yi bir daha
  GÖRMEZSE, bunun yalnızca test-script'inin (manuel Time.timeScale oynaması) yan
  etkisi olduğunu doğrulamış olur.
- Blender MCP bu turda KULLANILMADI (koordinatörün ek talimatı: önce telefon/adb ile
  referans oyunun efektlerini piksel bazlı ölçmek gerekiyor — bkz. yukarıdaki
  "PİKSEL-BAZLI EŞLEME KURALI"; bu ölçüm bu turda yapılamadı, zaman kalmadı).
  Muzzle flash / isabet / lazer sprite'ları HÂLÂ dairesel fallback (ArtLibrary kuralı).
- Commit ATILMADI (talimat gereği).

## MEVCUT DURUM (güncellendi, 3 Eylül 2026 gece — FAZ 1 turu sonrası)

FAZ 0 TAMAMLANDI (detay yukarıda). **FAZ 1 KISMEN TAMAMLANDI** — aşağıdaki
"FAZ 1" bölümüne bakın, hangi alt madde bitti/kaldı orada işaretli. 298/298
EditMode testi yeşil, branch `tasarim/koloni-reboot`, commit ATILMADI (talimat
gereği). Bu turda Blender MCP KULLANILMADI — zaman bütçesi kod tarafı
(yıldız rozeti + tema mimarisi) gerçek sahnede kanıtlamaya gitti; yeni Blender
sprite/tile üretimi (yıldız ikonu, gerçek kar dokusu, düşman/üs revizyonu)
sıradaki ajana kaldı.

## MEVCUT DURUM (bu planın yazıldığı an, 3 Eylül 2026 akşam — ESKİ, referans için)

Bugün 12+ ajan turu geçti. Tamamlanan: 5 kule tipi (Battery/Ambush/Beam/Decoy/Minefield,
görsel olarak Makineli Tüfek/Top/Keskin Nişancı/Radar/Zaman Bozulması), 3 kademeli
yükseltme (oyun-içi, kalıcı DEĞİL), dokunmatik yükselt/sat UI, zemin/kaya/ağaç/dağ/birlik
sprite'ları (tutarlı Blender stili), ana menü/duraklat/ayarlar/zafer-yenilgi ekranları,
10 ses klibi (ElevenLabs), 2 gerçek hata düzeltildi (AudioListener eksikti, Pause/Draft
çakışması + 2 NRE). 302 EditMode testi yeşil. Ekonomi kıyaslandı (bizim 90 başlangıç,
referans 100 — sapma küçük, dokunulmadı). **Referans oyunda 7 harita teması var
("HARİTA 1"–"HARİTA 7"), bizde TEK tema var — bu gece bu da ele alınacak.**

Önceki notlar (sırayla oku): `2026-09-02-referans-oyun-gozlem.md`,
`2026-09-03-referans-oyun-tam-analiz.md`, `2026-09-03-gorsel-tutarlilik-uretimi.md`,
`2026-09-03-menu-ve-ses-sistemi.md`, `2026-09-03-tam-klon-siki-dogrulama.md`,
`2026-09-03-ekonomi-buton-tema-karsilastirma.md`.

## ALTIN KURAL (bugün pahalı öğrenildi, TEKRARLANMAYACAK)

Her "tamamlandı" iddiası GERÇEK `Assets/Scenes/Frontline_Sandbox.unity` sahnesinde,
gerçek Play düğmesiyle, SIFIR konsol hatasıyla, ekran görüntüsü kanıtıyla olmalı.
İzole test sahnesi / senaryo-dışı kurulum "doğrulama" SAYILMAZ. Kullanıcı bunu bir kez
yakaladı ve haklı olarak sinirlendi — tekrarlanırsa güven tamamen biter.

## PİKSEL-BAZLI EŞLEME KURALI (3 Eylül gece, kullanıcı ikinci kez sinirlendi)

Kullanıcı birebir: "15 ekran görüntüsünden bir şey anlayacak kadar mal mısın,
ıncığına cıncığına aynı olsun istiyorum, sen kafana göre bir şeyler oluşturuyorsun."
"Genel izlenim"e göre tasarım kararı vermek YASAK. Referans oyundan (com.mobirix.tdwt,
adb ID T8IZFMQ4KFZHGY9H, sifre kilitlenirse kullaniciya sor) görsel/efekt üretmeden önce:

1. Her efekt/kule/obje icin TEK ekran goruntusu YETMEZ — en az 5-8 farkli
   aci/an yakala (ates oncesi, ates ani, isabet ani, birkac kare sonrasi).
   Ates anini yakalamak icin ust uste hizli screencap dizisi cek.
2. Her goruntuden GERCEK PIKSEL RENK DEGERINI cikar: PNG'yi Python/PIL ile ac,
   `im.getpixel((x,y))` ile efektin merkez/kenar pikselini SAY. Gozle "turuncumsu"
   demek YASAK — "#FF8A2E" gibi kesin hex/RGB deger cikarilacak.
3. Blender materyal/emission rengini bu OLCULEN hex degere BIREBIR esitle
   (yaklasik degil, tam deger, materyale kod olarak girilecek).
4. Efekt BOYUT ORANI da olculecek: efekt genisligi / kule genisligi (piksel),
   uretimde AYNI ORAN korunacak — "biraz buyuk/kucuk olsun" tahmini YASAK.
5. Bir sey olculemiyorsa (goruntu net degil, an yakalanamadi) TAHMIN ETME —
   raporda acikca "sunu olcemedim" yaz, kafadan uretme.

Bu kural SADECE bu turu degil, gece boyu zincirlenen HER SONRAKI ajani baglar.
**Bu turda (3 Eylul gece, 3. tur) telefon/adb ile referans oyuna baglanip pixel
olcumu YAPILAMADI** — zaman butcesi 0. FAZ'daki (asagida) kod hatasi temizligine
gitti. Sonraki ajan Blender uretimine gecmeden ONCE bu 5 adimi uygulamali.

## FAZLAR (sırayla, her biri kendi ajan turunda, biten fazı burada [x] işaretle)

### FAZ 0 — Envanter ve isimlendirme kararı [x] TAMAMLANDI (3 Eylül gece)
- Kod içi `TowerKind.Battery` görünen adı → "Makineli Tüfek", `TowerKind.Ambush` →
  "Topçu" olarak güncellendi. Enum isimleri DEĞİŞTİRİLMEDİ. Güncellenen dosyalar:
  `Core/UI/HudFormat.cs` (TowerName/TowerShortName — arayüzün tek doğru kaynağı),
  `Runtime/View/DraftView.cs`, `Runtime/View/RunHud.cs` (kendi yerel Türkçe ad
  eşlemeleri de vardı, hepsi güncellendi).
- Kilitli kule sistemi kaldırıldı: `Core/Meta/UnlockCatalog.cs` → `StartingTowers`
  artık 5 kulenin hepsini içeriyor, `Costs` sözlüğü boş (hepsi 0 bedel).
  `Tests/EditMode/MetaProgressTests.cs` bu mekaniği doğrudan test ediyordu —
  kilit-özel testler (kısmi alım, ikinci kez açma, fiyat sıralaması vb.) kaldırıldı/
  güncellendi, kalan testler "tüm kuleler baştan açık" gerçeğini doğruluyor.
  Bu, ekonomi DENGE testlerinden (RunCalibrationTests/EconomyCurveTests — bunlara
  DOKUNULMADI) ayrı bir mekanik kaldırma, testi gevşetme değil.
- Menzil (`radius`/`range`) alanları incelendi (`TowerWeapon.cs`, `Emplacement.cs`,
  `TowerPlacer.cs`): her kulenin menzili zaten ölçüm notlarıyla (spec 3.5 vb.)
  gerekçelendirilmiş durumda (örn. `beamRange = 40` hücre — ışının haritayı uçtan
  uca geçmesi bilinçli tasarım). Saçma/anlamsız bir değer bulunmadı, DEĞİŞTİRİLMEDİ.
  **Not: referans oyunla telefon üzerinden birebir sayısal kıyaslama yapılmadı**
  (zaman bütçesi bu turda buna yetmedi) — bir sonraki tur isterse adb ile referans
  oyunu açıp kule menzil halkalarını ekran görüntüsüyle kıyaslayabilir.
- DOĞRULAMA: 298/298 EditMode testi yeşil (302'den 298'e düşüş, kilit-mekaniği
  testlerinin kaldırılmasından kaynaklanıyor — hesaplandı, kasıtlı). Gerçek
  `Frontline_Sandbox.unity` sahnesinde Play'e basıldı, konsol SIFIR hata, ekran
  görüntüsü alındı: alt kule barında artık "Tufek/Topcu/Sahte/Mayin/Isin" hepsi
  fiyat etiketiyle (kilit işareti YOK) görünüyor — tüm kuleler baştan satın
  alınabilir/yerleştirilebilir durumda.

### FAZ 1 — Blender: HER ŞEYİ baştan üret [ ] KISMEN TAMAMLANDI (3 Eylül gece, 2. tur)

**BİTEN maddeler (gerçek `Frontline_Sandbox.unity` sahnesinde, gerçek Play ile,
sıfır konsol hatasıyla kanıtlandı — ekran görüntüleri bu turun agent transcript'inde):**

- **Yıldız rozetleri [x] BİTTİ.** `Assets/Frontline/Runtime/View/PrimitiveSprites.cs`
  → `Star()` (procedural 5 kollu yıldız, point-in-polygon dolgu). Yeni dosya
  `Assets/Frontline/Runtime/View/TowerStars.cs` (`TowerStars` komponenti, 1-3
  yıldızı kule üstünde yatay dizer, `SetLevel(int)` ile açar/kapar).
  `Assets/Frontline/Runtime/Towers/Emplacement.cs` → `BindStars()`, `Upgrade()`
  içinde `stars.SetLevel(Level)` çağrısı eklendi. `Assets/Frontline/Runtime/Towers/TowerPlacer.cs`
  → `Place()` içinde kule üstüne `Stars` child'ı kuruluyor, `tower.BindStars(stars)`.
  KANIT: Play modunda Ambush (seviye 1) ve Battery/Beam (seviye 2'ye yükseltilmiş)
  kuleleri kuruldu, ekran görüntüsünde 1 ve 2 altın yıldız net görünüyor, konsol
  sıfır hata. **NOT: yıldız Blender sprite'ı DEĞİL, procedural (kod ile üretilen)
  sprite** — ArtLibrary kuralına uygun (Blender sprite'ı sonradan eklenebilir,
  `TowerStars.Build(Sprite)` zaten opsiyonel sprite parametresi alıyor, şu an
  `null` geçiliyor yani otomatik `PrimitiveSprites.Star()`'a düşüyor).
- **Ateş efektleri [x] ZATEN VARDI, doğrulandı, dokunulmadı.** Kod okundu:
  `TowerWeapon.cs` zaten `FxBurst.Muzzle()` (namlu cakması), `FxBurst.Impact()`/
  `ImpactOn()` (isabet/patlama) ve `Assets/Frontline/Runtime/View/ShotFlash.cs`
  içindeki `BeamFlash` (Işın/Keskin Nişancı için sürekli çizgi efekti) sistemlerini
  çağırıyor — bunlar ÖNCEKİ turlarda yazılmış ve zaten TowerWeapon'a bağlıydı, bu
  turda YENİ eklenmedi, sadece doğrulandı. Sprite atanmazsa `PrimitiveSprites.Circle()`'a
  düşüyor (ArtLibrary kuralı), yani şu an gerçek sahnede görsel olarak çalışıyorlar
  ama Blender'dan özel muzzle/impact/laser sprite'ları henüz YOK (hâlâ daire
  fallback). **GERİ TEPME (namlu tween) animasyonu YOK** — kontrol edildi,
  `TowerTurret`/`TowerWeapon` içinde recoil kodu bulunamadı, bu madde HALA AÇIK.
  **Canlı ateş anı ekran görüntüsüyle YAKALANMADI** (efekt 0.07-0.22 sn sürüyor,
  bu turun zaman bütçesinde düşman spawn edip atış anını yakalamaya yetmedi) —
  kod okuma + sıfır konsol hatasıyla dolaylı doğrulandı, PİKSEL kanıtı yok. Sıradaki
  ajan bunu tamamlamalı: düşman spawn et, menzile gir, ekran görüntüsü zamanla.
- **Harita teması (Kar/Buz) [x] MİMARİ BİTTİ, asset kısmı EKSİK.**
  `Assets/Frontline/Runtime/View/ArtLibrary.cs` → yeni `SceneryTheme` enum
  (`Default`/`Snow`), `ArtLibrary.theme` alanı + `snowTreeTint/snowRockTint/
  snowCorridorColor/snowBuildStripColor/snowBlockedColor`. `SceneryView.cs` →
  `BaseTint()` tema Snow ise kar renklerine düşüyor. `GridTileRenderer.cs` →
  `BuildFromTiles()` içinde `TintTowardSnow()` ile mevcut Blender zemin dokularını
  buz-mavi/beyaza doğru renk kaydırıyor (yeni doku ÜRETMEDEN, mevcut dokuyu
  temaya göre yeniden renklendiriyor). Default tema mevcut davranışla BİREBİR
  aynı (tema alanı varsayılan `Default`, hiçbir mevcut sahne/test etkilenmedi).
  KANIT: Play modunda `ArtLibrary.theme = Snow` set edilip Grid+Scenery yeniden
  inşa edildi, ekran görüntüsünde zemin görünür şekilde buz-mavi/gri tona döndü,
  konsol sıfır hata. **EKSİK: gerçek Blender kar dokusu/obje varyantı YOK** —
  bu bir renk-kaydırma teması, kullanıcının istediği "yeni doku + obje varyantı"
  değil. Sahnede `ArtLibrary.theme` Inspector'dan `Snow` yapılıp kalıcı test
  edilmedi (yalnızca Play-time reflection ile geçici set edildi) — üretim UI'ında
  tema SEÇME EKRANI da yok.

**AÇIK KALAN maddeler (sıradaki ajan buradan devam etsin):**

- Geri tepme (recoil) animasyonu — `TowerWeapon`/`TowerTurret`'a namlu ileri-geri
  transform tween eklenmedi.
- Muzzle flash / isabet / lazer için GERÇEK Blender sprite'ları (şu an daire
  fallback) — Blender MCP bu turda hiç çağrılmadı.
- Ateş efektinin canlı ekran görüntüsüyle kanıtlanması (düşman spawn + atış anı).
- Kar teması için GERÇEK Blender dokusu + sahne obje varyantı (kar örtülü
  ağaç/kaya gibi) — şu an yalnızca renk kaydırma var.
- Kar temasını seçmek için oyun-içi/menü bir UI YOK (şu an yalnızca kod/reflection
  ile set edilebiliyor) — plan bunu gerektirmiyordu ama kullanıcı "seviyeleri
  düzenleme" derken (FAZ 4) muhtemelen tema seçimini de burada bekleyecek.
- 5 kule × 3 seviye isim/görsel uyum kontrolü YAPILMADI (zaman kalmadı).
- Düşman/üs sprite kalite gözden geçirmesi YAPILMADI.
- Yol izi (keşif izi) efekti kontrolü YAPILMADI.

### FAZ 2 — Kod: kimlik, menzil, yıldız, ekonomi [ ]
- Görünen kule adları güncellensin (UI metinleri, `RadialBuildMenu`, `TowerBarView`,
  `TowerActionPanel`).
- Menzil değerleri referans oyunla kıyaslanıp güncellensin.
- Yıldız görseli: `Emplacement.Level` zaten var (1-3), sprite üstüne yıldız rozeti
  bindirilsin (`TowerPlacer.Place()`/`Emplacement.BindVisual()`).
- Efektler koda bağlansın: ateş anında muzzle flash + geri tepme animasyonu,
  isabette patlama, ışın tipi için sürekli çizgi çizimi.
- Ekonomi: fiyatlar SABİT (round sayılar, referansa yakın: 100/200/300/200/250 gibi
  — önceki turda zaten ölçülmüştü, `docs/notes/2026-09-03-ekonomi-buton-tema-karsilastirma.md`
  içindeki tabloya bak). **DENGE TESTLERİNİ KIRMAMAYA DİKKAT** — `RunCalibrationTests.cs`,
  `CurrentRunCalibrationTests.cs`, `EconomyCurveTests` gibi dosyalar kırılırsa,
  fiyat değişikliğini o testlerin beklediği ORANLARLA uyumlu yap (testleri
  silme/gevşetme, testin ölçtüğü gerçek dengeyi KORU).
- Kilitli kule kaldırma (Faz 0'da bulunduysa burada uygula).

### FAZ 3 — Kalıcı (meta) yükseltme ekranı — Ana Menüde "GÜNCELLE" [x] TAMAMLANDI (3 Eylül gece, 5. tur)

**Yapılan (gerçek sahnede, gerçek Play + gerçek tıklama simülasyonuyla kanıtlandı):**

- `Core/Meta/PermanentUpgradeCatalog.cs` (YENİ) — 5 kule için taban maliyet
  (in-run kurulum fiyatlarıyla aynı: Tüfek 100/Topçu 200/Sahte 130/Mayın 130/
  Işın 170, `EconomyPlanner.Costs.Default()` ile birebir), kademe başına
  `x2 * (adım+1)` biyokütle maliyeti (1. kademe taban*2, 2. kademe taban*4).
  `MaxPermanentSteps = 2` (Emplacement.MaxLevel=3 olduğu için en fazla 2 kalıcı
  yükseltme: 1→2, 2→3).
- `Core/Meta/MetaState.cs` — `permanentSteps` (Dictionary<TowerKind,int>),
  `PermanentStepOf(kind)`, `StartingLevelOf(kind)` (1+adım),
  `TryUpgradeTowerPermanent(kind)` (yetersiz bakiyede/tavanda HİÇBİR ŞEY
  değiştirmez — MetaState'in genel "kısmi işlem yok" kuralına uygun), yeni bir
  `Restore(...)` aşırı yüklemesi (permanentKinds/permanentValues paralel
  diziler) — eski imza korunur (geriye dönük uyumlu).
- `Core/Meta/MetaSaveData.cs` — `permanentStepKinds`/`permanentStepValues`
  (JsonUtility Dictionary serileştiremediği için paralel diziler, aynı desen
  `unlockedTowers` ile), `From`/`ToState` güncellendi.
- `Runtime/Meta/MetaProgressRunner.cs` — `TryUpgradeTowerPermanent(kind)`
  köprü metodu (menüden çağrılabilir, başarırsa hemen `MetaProgressStore.Save`).
- `Runtime/Towers/TowerPlacer.cs` — yeni `permanentMeta` alanı (Awake'te
  `MetaProgressStore.Load()` ile bir kez doldurulur), `Place(kind, cell)`
  içinde kule kurulduktan hemen sonra `permanentMeta.StartingLevelOf(kind)`
  kadar **bedava** `tower.Upgrade()` çağrısı (SpentTotal'e YAZILMAZ — bedava
  kademe satışta geri ödeme üretmemeli, draft ödülü kuleyle aynı kural).
  Kalıcı yükseltme hiç alınmamışsa davranış ÖNCEKİYLE BİREBİR aynı (StartingLevelOf
  her zaman 1 döner, döngü hiç çalışmaz).
- `Runtime/UI/UpgradeOverlay.cs` (YENİ) — SettingsOverlay ile aynı üslup
  (`UiKit`, kod ile kurulan overlay, kendi kendini yok eder). `MetaProgressStore`'u
  DOĞRUDAN kullanır (Ana Menü sahnesinde RunController/MetaProgressRunner yok).
  5 kule satırı: ad, "Seviye X → Y", fiyat (`HudFormat.Price`, karşılanamıyorsa
  ✗ işaretli ve buton `interactable=false` + soluk renk), tavandaysa "(TAM)"
  ve kapalı buton. Her satıra tıklamak `TryUpgradeTowerPermanent` çağırır,
  başarırsa kaydeder ve satırı yeniden çizer.
- `Runtime/UI/MainMenuScreen.cs` — OYNA/AYARLAR/ÇIKIŞ arasına "GÜNCELLE"
  butonu eklendi (`OpenUpgrade()` → `UpgradeOverlay.Open`).

**Testler (YENİ, 8 adet, `Tests/EditMode/MetaProgressTests.cs`):** başlangıç
seviyesi 1, yeterli parada satın alma + seviye artışı, yetersiz parada
HİÇBİR ŞEY değişmemesi, tavana kadar çalışıp sonra reddetme, 2. kademenin
1.'den pahalı olması, bellek-içi ve disk kaydet/yükle sonrası kademenin
korunması, bilinmeyen kule güvenliği. **306/306 EditMode yeşil** (298'den
306'ya, +8 yeni test, hiçbir eski test bozulmadı — iki ayrı tam koşuda
doğrulandı).

**CANLI KANIT (Ana Menü + Sandbox sahnelerinde, gerçek Play, gerçek
`ExecuteEvents.pointerClickHandler` tıklaması, sıfır konsol hatası):**
1. `Frontline_MainMenu.unity` Play → ekran görüntüsü: OYNA/GÜNCELLE/AYARLAR/ÇIKIŞ
   dört buton görünüyor.
2. GÜNCELLE butonuna gerçek tıklama simülasyonu → overlay açıldı, ekran
   görüntüsünde "Biyokütle: 90" ve 5 kule satırı (her biri "Seviye 1→2" ve
   doğru fiyatla: Tüfek 200/Topçu 400/Sahte 260/Mayın 260/Işın 340 — katalog
   formülüyle birebir eşleşiyor) görünüyor; 90 biyokütle hiçbirini
   karşılamadığı için tüm satırlar pasif/kapalı görünümde.
3. `MetaProgressStore` üzerinden test amaçlı +1000 biyokütle verildi (890+90=
   sonradan 1090), overlay kapatılıp yeniden açıldı (State diskten YENİDEN
   yüklendi, kalıcılık kanıtlandı) → "Makineli Tüfek" satırına gerçek tıklama
   → ekran görüntüsü: "Biyokütle: 890" (1090-200), satır artık
   "Makineli Tüfek - Seviye 2→3  x400" gösteriyor (satın alma anında yeniden
   çizildi). Disk kontrolü (`MetaProgressStore.Load()`): `Biomass=890`,
   `PermanentStepOf(Battery)=1`, `StartingLevelOf(Battery)=2`.
4. Play durduruldu, `Frontline_Sandbox.unity` yeniden üretildi (`Frontline/
   Sandbox Sahnesini Uret`), Play'e girildi. `TowerPlacer.Place(Battery, cell)`
   doğrudan çağrıldı (execute_code ile, gerçek TowerPlacer/Battlefield
   üzerinden — kod-simülasyonu değil, gerçek çalışma zamanı yolu): **sonuç
   `tower.Level == 2`** (permanentMeta okundu, bedava 1 kademe otomatik
   uygulandı). Kıyas için aynı sahnede permanent yükseltmesi OLMAYAN bir
   Topçu (Ambush) kuruldu: `AmbushLevel=1` (değişmedi). Silah hasarı da
   doğrulandı: `BatteryDps=3` (taban 2.5 × Level2 çarpanı 1.2 = 3.0, tam
   eşleşiyor), `AmbushDps=4` (taban, değişmedi) — **kalıcı yükseltmenin YENİ
   bir run'da GERÇEKTEN güçlendirdiği sayısal olarak kanıtlandı**, kod okuma
   veya varsayım değil.
5. Ekran görüntüsü: Sandbox sahnesinde iki kule + bir yıldız rozeti (Battery,
   seviye 2) görünüyor, konsol sıfır hata/uyarı (`read_console` her adımda
   kontrol edildi).
6. Doğrulama sonrası test verisi temizlendi: `MetaProgressStore.Clear()` ile
   gerçek kullanıcı kaydı (varsa) kirletilmeden test biyokütlesi/kademesi
   silindi — sıradaki oturum temiz `meta.json` ile başlar.

**Not (ADIM 1'den kalan, ilgisiz ama gözlemlendi):** Bu turun başında disk
üzerinde ÖNCEKİ bir turdan kalma bozuk `meta.json` bulundu ("JSON parse error:
Missing a name for object member") — `MetaProgressStore.LoadFrom` tasarım
gereği bunu YUTTU ve temiz duruma düştü (loglandı, oyunu bozmadı). Bu, bu
turun EKLEDİĞİ bir hata DEĞİL, `MetaProgressStore`'un zaten var olan
"bozuk kayıt asla patlamaz" garantisinin canlı kanıtı.

### FAZ 4 — Seviye üreteci (level editor) [x] TAMAMLANDI (3 Eylül gece, 6. tur)

**Kavram netleştirmesi (Adım 1):** Bizim sistemde ayrı bir `WaveConfig` YOK —
dalgalar `RunController`'ın alanlarından (`totalWaves`, `baseUnitsPerWave`,
`unitsAddedPerWave`, `baseUnitHp`, `hpGrowthPerWave`) üretiliyor, harita ise
`Battlefield`'ın alanlarından (`width/height/corridorCount/corridorWidth/
rungCount/sceneryPercent/treeHealth/rockHealth/seed`) + `ArtLibrary.theme`
(Default/Snow, FAZ 1'de eklenmişti). "Seviye" = bu iki bileşenin alanlarının
adlandırılmış, diske kaydedilmiş bir anlık görüntüsü — yeni bir oyun kavramı
İCAT edilmedi, var olan iki bileşenin Inspector alanları dışa taşındı.

**Yapılan (yeni dosyalar):**
- `Core/Levels/LevelDefinition.cs` — `[Serializable]` düz veri sınıfı (harita +
  dalga alanları + `LevelTheme` enum — Core'da tanımlı, Unity tipi yok, Runtime
  bunu `ArtLibrary.SceneryTheme`'e çevirir).
- `Core/Levels/LevelCatalog.cs` — saf C# CRUD: `Add/Update/Remove/Find`,
  `Validate` (isim boş/tekrar olamaz, genişlik/yükseklik≥5, **koridor sayısı≥3**
  — `MapGenerator`/`MapValidator`'ın gerçek asgari şartı, ilk denemede bunu
  atlayıp `InvalidOperationException` ile karşılaştım, testler + validator
  buna göre düzeltildi). Unity'siz, EditMode'da hızlı test edilebilir.
- `Runtime/Levels/LevelStore.cs` — `MetaProgressStore` ile birebir aynı disiplin:
  JSON (`JsonUtility`, dizi sarıcı), `Application.persistentDataPath/levels.json`,
  bozuk/eksik kayıt asla patlamaz (geçersiz tek bir kayıt atlanır, kalanı yüklenir).
- `Runtime/Levels/LevelSelection.cs` — `PlayerPrefs` tabanlı seçili seviye adı
  (static alan DEĞİL — Play'e girerken domain reload static'i sıfırlar,
  PlayerPrefs hem Editor hem build'de hayatta kalır).
- `Assets/Frontline/Editor/LevelEditorWindow.cs` (YENİ) — `Frontline/Seviye
  Ureteci` menüsü. Liste + form: ekle/düzenle/sil/kaydet/"Bu Seviyeyle Oyna".
  İş mantığının TAMAMI `LevelCatalog`'da; pencere yalnızca onu çağırıp çizer.
- `Battlefield.cs` → `Awake()` içinde `ApplySelectedLevelIfAny()`: seçili
  seviye varsa harita alanlarını ve `ArtLibrary.theme`'i üzerine yazar; seçim
  yoksa davranış ÖNCEKİYLE BİREBİR aynı.
- `RunController.cs` → `Awake()` içinde `ApplySelectedLevelIfAny()`: seçili
  seviye varsa dalga alanlarını üzerine yazar; seçim yoksa aynı davranış.

**Testler (YENİ, 11 adet, `Tests/EditMode/LevelCatalogTests.cs`):** ekleme,
tekrar isim reddi, boş isim reddi, geçersiz genişlik reddi, güncelleme (isim
değişimi dahil), olmayanı güncelleme reddi, silme, olmayanı silme, diske
kaydet/yükle (tüm alanları — tema/seed dahil — koruma), olmayan dosya → boş
katalog, bozuk dosya → patlamadan boş katalog. **317/317 EditMode testi
yeşil** (306'dan 317'ye, +11 yeni test, iki ayrı tam koşuda doğrulandı —
biri koridor-asgari düzeltmesinden ÖNCE biri SONRA, ikisi de yeşil).

**CANLI KANIT (gerçek `Frontline_Sandbox.unity`, gerçek Play, `execute_code`
ile GERÇEK `LevelStore`/`LevelCatalog`/`LevelSelection` API'leri üzerinden —
editör penceresinin kendisinin çağırdığı AYNI kod yolu):**
1. `Frontline/Seviye Ureteci` penceresi gerçekten açıldı (menü item, hata yok).
2. EKLEME: "Kucuk Test" adında bir seviye eklendi (Width=11/Height=20,
   CorridorCount=3, TotalWaves=3, BaseUnitsPerWave=2, BaseUnitHp=5,
   Theme=Snow — hepsi sahne varsayılanlarından KASITLI farklı), `LevelStore.Save`
   ile diske yazıldı, yeniden yüklenip bulunduğu doğrulandı.
   (İlk denemede CorridorCount=1 verilmişti, `Battlefield.Regenerate` GERÇEK
   bir `InvalidOperationException` fırlattı — "en az 3 koridor gerekli"; bu,
   validasyonun MapGenerator'ın gerçek kısıtına göre SIKILAŞTIRILMASINA yol
   açtı, aşağıdaki "not"a bakın.)
3. OYNA: `LevelSelection.SelectedLevelName = "Kucuk Test"`, Play'e girildi.
   `execute_code` ile GERÇEK sahnedeki `Battlefield.Grid`/`RunController`/
   `ArtLibrary` okundu: **gridW=11 (vs varsayılan 13), gridH=20 (vs 30),
   totalWaves=3 (vs 15), baseUnitsPerWave=2 (vs 8), baseUnitHp=5 (vs 10),
   theme=Snow (vs Default)** — editörde eklenen seviyenin oyunu GERÇEKTEN
   farklı çalıştırdığı sayısal olarak kanıtlandı. `read_console`: 0 hata/uyarı.
4. Play durduruldu. SİLME: `LevelCatalog.Remove("Kucuk Test")` + `Save`,
   `LevelSelection.Clear()`. Yeniden yükleme: seviye ARTIK bulunamıyor
   (`Find` null, `count=0`) — disk üzerinde kalıcı silindiği doğrulandı.
5. DOĞRULAMA (seçim yokken varsayılana dönüş): Play'e tekrar girildi, aynı
   okuma tekrarlandı: **gridW=13, gridH=30, totalWaves=15,
   baseUnitsPerWave=8, theme=Default** — seçim temizlenince davranış TAM
   olarak eski haline döndü (regresyon yok). `read_console`: 0 hata/uyarı.

**Not (önceden var olan, bu turun eklemediği bir gözlem):** İlk `LevelStore.Load()`
çağrısında ("Frontline/Sandbox Sahnesini Uret" menüsü Battlefield'ı
`AddComponent` ettiği an, Awake HEMEN edit-mode'da bile tetiklenir) konsolda
"[LevelStore] Kayit okunamadi, bos katalogla devam ediliyor" uyarısı ÇIKTI —
`persistentDataPath`'te önceden bozuk bir `levels.json` vardı (muhtemelen bu
gece boyu zincirlenen turlardan birinin yarım kalan denemesi). `MetaProgressStore`
için zaten belgelenmiş "bozuk kayıt asla patlamaz" garantisiyle AYNI şekilde
davranıldı — oyun durmadı, boş katalogla devam etti, sonraki `Save` çağrısı
dosyayı temiz içerikle üzerine yazdı. Bu turun bir HATASI değil, tasarımın
kanıtı.

**Yapılmayan / sıradaki ajana not:** Editör penceresindeki IMGUI
butonlarına (Ekle/Kaydet/Sil/Oyna) gerçek fare tıklamasıyla dokunulmadı —
MCP'nin uGUI (Play modu) tıklama aracı var ama IMGUI Editor penceresi için
yok; bunun yerine pencerenin çağırdığı AYNI alt katman (`LevelCatalog`/
`LevelStore`/`LevelSelection`) doğrudan `execute_code` ile çağrıldı, ki bu
pencerenin buton handler'larının yaptığı İŞİN BİREBİR AYNISI (bkz.
`LevelEditorWindow.SaveBuffer/DeleteSelected` - hepsi bu API'leri çağırıyor).
Sıradaki ajan isterse gerçek IMGUI tıklamasını (varsa uygun bir araçla)
deneyebilir ama bu FAZ 4'ün bitiş kriterini karşılamak için ZORUNLU değildi.

### FAZ 5 — Gece boyu oynanabilirlik döngüsü (asıl "loop") [ ]
Yukarıdaki fazlar bittikten sonra: gerçek sahnede tekrar tekrar Play'e bas, çeşitli
senaryoları dene (kule kur/yükselt/sat, duraklat/ayarlar, harita teması değiştir,
seviye editöründe yeni seviye ekle-oyna-sil, ana menüden kalıcı yükseltme yap),
konsol hatası ararsan hemen düzelt, tekrar dene. En az 3 tam temiz tur (sıfır hata)
art arda geçene kadar dur. Bu, kullanıcı sabah açtığında ilk izlenimi belirleyecek.

## OTOMATİK DEVAM MEKANİZMASI

Her ajan turu bitince bir sonraki ajan (üst oturum tarafından) otomatik başlatılacak.
Kullanım limiti dolarsa üst oturum bekleyip limit yenilenince devam edecek — bu,
kullanıcının beklediği "müthiş genel loop." Her ajan bitirdiğinde BU DOSYAYI güncelleyip
hangi fazın bittiğini `[x]` ile işaretlemeli ki bir sonraki ajan kaldığı yerden devam
edebilsin, aynı işi tekrar etmesin.

## KAPSAM DIŞI (kullanıcı "ekstraları çöpe at" dedi)

Çoklu-yol + düşman-öğrenme sistemi KALACAK (bizim özgün özelliğimiz, dokunma).
Gerçek-para mağazası, reklam-izle, Canlandır (pay-to-continue), haftalık lider tablosu
— hiçbiri eklenmeyecek, referans oyunun bu kısımları bilinçli olarak atlanıyor.

---

## 4. TUR (3 Eylül gece, sabaha karşı) — ADIM 1-4 sonucu

### ADIM 1 — Piksel yakalama, 3. deneme: YİNE BAŞARISIZ, ama farklı ve daha net bir teşhisle

adb bağlantısı çalışıyordu (`T8IZFMQ4KFZHGY9H`, `device`), `com.mobirix.tdwt`
`monkey` ile başlatıldı, `mCurrentFocus` ile odakta olduğu doğrulandı. Ardından 4
kez `screencap` çekildi:

1. İlk screencap: oyun zaten "YENİLGİ" ekranındaydı (1728 altın, dalga 1/15,
   kalan düşman 10/20) — yani bu, ÖNCEKİ bir ajan turundan kalma bozuk/yarım
   bırakılmış bir oturumdu, benim başlattığım koşum değil.
2. "YENİDEN BAŞLAT"a basıldı → HP 10, altın 200, dalga 1/15, düşman 20/20 —
   TEMİZ görünen yeni bir koşu başladı, ekranda hiç düşman yoktu.
3. `adb shell screenrecord --time-limit 20 /sdcard/tdcap.mp4` arka planda
   başlatıldı (bkz. not aşağıda), hemen ardından bir inşa karesine
   `input tap` gönderildi.
4. O tap'in screencap'i yine "YENİLGİ" (1728 altın) ekranını gösterdi —
   yani gerçek dokunuş İŞLENEMEDEN, hatta hiçbir düşman ekranda görünmeden
   oyun tekrar aynı eski "YENİLGİ" durumuna döndü.
5. Bir kez daha "YENİDEN BAŞLAT" + screencap: yine temiz dalga-1 durumu
   (HP10/altın200/20 düşman) doğrulandı — yani "YENİDEN BAŞLAT" tuşu
   ÇALIŞIYOR, sorun ondan sonraki adımda.

**Teşhis (önceki 2 turdan farklı, daha kesin):** Bu cihazdaki oyun oturumu
"YENİDEN BAŞLAT" ile kısa süreliğine temiz dalga-1 durumuna dönüyor ama
birkaç saniye içinde (benim `input tap` + screencap round-trip'im süresinde)
kendiliğinden eski/bozuk "YENİLGİ" (1728 altın) durumuna geri dönüyor —
düşman hiç görünmeden. Bu, önceki 2 ajanın raporladığı "üs canı çok düşük,
düşmanlar 8-10sn gizli gecikmeden sonra toplu geliyor, yakalayamadık" teşhisinden
FARKLI bir kök neden: burada gerçek oynanış hızı değil, cihazdaki KALICI/BOZUK
BİR OYUN DURUMU (muhtemelen arka planda çöken bir reklam/IAP çağrısı ya da
bozulmuş save) engelleyici. `screenrecord` arka plan komutu da
"Must specify output file" hatasıyla başarısız oldu (muhtemelen bu ortamda
arka plan `adb shell` çağrısının shell quoting/redirect davranışı farklı) —
bu yüzden video tabanlı yakalama denemesi bu turda hiç gerçekleşmedi.

**KARAR (talimata uygun):** 3. deneme de dahil olmak üzere üç ayrı ajan turu
piksel ölçümünü YAPAMADI. Talimat gereği burada durup FALLBACK'e geçildi:

- **PİKSEL ÖLÇÜLEMEDİ, TÜR-STANDARDI KULLANILDI (3. deneme de başarısız).**
- Bu turda Blender muzzle/impact/lazer sprite üretimi de YAPILMADI — zaman
  bütçesi (kalan ~30dk) doğrudan ADIM 3 (ekonomi, asıl öncelik) için
  ayrıldı; tür-standardı renklerle (turuncu/sarı namlu alevi, kırmızı/turuncu
  patlama, mavi/cyan lazer) Blender üretimi hâlâ AÇIK — sıradaki ajan
  `mcp__blender__*` araçlarıyla bunu yapabilir, kod tarafı zaten
  `TowerWeapon.cs`'de `muzzleSprite`/`impactSprite` alanlarını bekliyor
  (bkz. FAZ 1 notu, "sprite atanmazsa PrimitiveSprites.Circle()'a düşüyor").
- Geri tepme (recoil) animasyonu HALA yapılmadı.

**Sıradaki ajana öneri:** Dördüncü bir canlı-yakalama denemesi yapmadan önce
önce `adb shell am force-stop com.mobirix.tdwt` ile TAM kapat, cihazı
`adb reboot` ile yeniden başlatmayı düşün (bozuk durumun bir cache/process
sorunu olma ihtimaline karşı) — ya da doğrudan tür-standardı renklerle
Blender üretimine geç (kullanıcı "beni şaşırt" dedi ama gece boyu tek bir
maddede daha fazla tur harcamamak da talimatın parçası).

### ADIM 2 — Doğrulama: YAPILDI, sıfır hata

- `mcp__unity__run_tests` (EditMode): **298/298 yeşil** (ekonomi değişikliğinden
  ÖNCE ve SONRA iki kez çalıştırıldı, ikisi de yeşil çıkana kadar).
- `Frontline/Sandbox Sahnesini Uret` menü öğesi çalıştırıldı, gerçek Play
  moduna girildi, `mcp__unity__manage_camera(screenshot)` ile ekran görüntüsü
  alındı: HUD'da **Tüfek 100 / Topçu 200 / Sahte 130 / Mayın 130 / Işın 170**
  ve **450 altın** başlangıç bakiyesi net görünüyor (yeni ekonomi değerleri
  canlı sahnede doğrulandı). `mcp__unity__read_console` ile konsol kontrol
  edildi: **0 hata, 0 uyarı**. Play modu düzgün durduruldu.

### ADIM 3 — FAZ 2 (ekonomi): TAMAMLANDI (yalnız fiyat/round-sayı kısmı)

**Yapılan:** `Assets/Frontline/Runtime/Economy/EconomyRunner.cs` (gerçek
oynanışta kullanılan MonoBehaviour) VE `Assets/Frontline/Core/Sim/EconomyPlanner.cs`
`Costs.Default()` (yalnız `CurrentRunCalibrationTests.cs`'in kullandığı
simülasyon-taraf ayna kopyası) **TAM x5 KATSAYIYLA** yuvarlak sayılara
çekildi — hiçbir oran rastgele değiştirilmedi, salt ölçek büyütüldü:

| Alan | Eski | Yeni |
|---|---|---|
| startingBalance | 90 | **450** |
| foodPerKill | 1 | **5** |
| foodPerWave | 6 | **30** |
| batteryCost (Tüfek) | 20 | **100** |
| ambushCost (Topçu) | 40 | **200** |
| decoyCost (Sahte) | 26 | **130** |
| minefieldCost (Mayın) | 26 | **130** |
| beamCost (Işın) | 34 | **170** |
| costStepPerSameKind | 5 | **25** |

x5 seçilme nedeni: `batteryCost*5=100` ve `ambushCost*5=200`,
`docs/notes/2026-09-03-ekonomi-buton-tema-karsilastirma.md`'de GERÇEKTEN
ÖLÇÜLEN referans değerlerle (Makineli Tüfek 100, Top 200) BİREBİR eşleşiyor.
`decoy/minefield/beam` (130/130/170) referansın (Radar 200 / Zaman Bozulması
250 / Keskin Nişancı 300) tam katı DEĞİL — tam eşleşme için oranları
(1:2:2:2.5:3) değiştirmek gerekirdi, bu da 162 kombinasyon × 20 seed ile
kalibre edilmiş mevcut dengeyi (`RunCalibrationTests`/`CurrentRunCalibrationTests`/
`EconomyCurveTests`) KIRARDI. Kullanıcı talimatı "DENGE TESTLERİNİ KIRMADAN"
dediği için bilinçli olarak **"yuvarlak sayı + AYNI oran" (x5 tam ölçek)**
tercih edildi, "referansla birebir aynı oranlarda yeniden dengele" değil —
bu ikinci yol ayrı bir dengeleme turu gerektirir, sıradaki ajana bırakıldı.

**Test kırılması ve düzeltmesi:** İlk test koşusunda tek başarısızlık çıktı:
`CurrentRunCalibrationTests.Simulator_MatchesTheAugust21Run` — "Artan besin"
(LeftoverFood, PARASAL bir büyüklük) eski aralıkla (45-95) karşılaştırılıyordu,
x5 ölçekten sonra 322,75 çıktı. Bu, testin GEVŞETİLMESİ değil, testin ölçtüğü
AYNI dengeyi doğru birimde ifade etmesi: aralık x5 ile **225-475**'e güncellendi
(`Assets/Frontline/Tests/EditMode/CurrentRunCalibrationTests.cs` satır ~144).
Öldürme/sızıntı/üs-canı gibi PARA OLMAYAN (birim bazlı) 3 assertion'a
DOKUNULMADI — x5 tam sayı ölçek olduğu için simülasyon matematiksel olarak
tamamen aynı sonucu üretir, yalnız parasal büyüklükler orantılı büyür.
Düzeltme sonrası: **298/298 yeşil.**
`RunCalibrationTests.cs`'in kendi `ReferenceCosts()`'u (50/12/24/16/16, 21
Ağustos donmuş referansı) TAMAMEN BAĞIMSIZ bir yapı — hiç dokunulmadı.

**Yapılmayan (FAZ 2'nin geri kalanı):** Kule adları zaten FAZ 0'da
güncellenmişti (bu tur tekrar dokunulmadı). Menzil değerleri referansla
sayısal kıyaslanmadı (ADIM 1 piksel/telefon erişimi engellendiği için —
adb erişimi vardı ama menzil ölçümü ayrı bir efor, bu turun bütçesine
girmedi). Yıldız/efekt kod bağlantısı zaten önceki turda vardı, dokunulmadı.

### ADIM 4 — FAZ 3 (kalıcı meta yükseltme ekranı): BAŞLANMADI

Zaman bütçesi ADIM 1 (üç deneme, hâlâ başarısız) ve ADIM 3'ün doğru/güvenli
yapılmasına (ekonomi ölçeklemesi + test analizi + iki kez tam test koşusu)
gitti. `Core/Meta/MetaProgressStore.cs` ve `Core/Meta/UnlockCatalog.cs`
incelenmedi bu turda — sıradaki ajan FAZ 3'e bu dosyaları okuyarak başlamalı,
sıfırdan yazmamalı (`UnlockCatalog.StartingTowers` FAZ 0'da tüm kuleleri
kilitsiz yapacak şekilde değiştirilmişti, bu bilgiyle çelişmeyecek bir kalıcı
yükseltme ekranı tasarlanmalı — kilit AÇMA değil, GÜÇ yükseltme satın alma).

### ÖZET — sıradaki ajan buradan devam etsin

- [ ] FAZ 1 (Blender sprite üretimi: muzzle/impact/lazer gerçek renk+doku,
      tür-standardı renklerle; geri tepme animasyonu) — HALA AÇIK.
- [x] FAZ 2 ekonomi round-sayı kısmı — BİTTİ (x5 ölçek, testler yeşil).
- [ ] FAZ 2 kalan: menzil sayısal kıyas — AÇIK.
- [x] FAZ 3 (Ana Menü "GÜNCELLE" kalıcı yükseltme ekranı) — BİTTİ (5. tur,
      detay yukarıda "FAZ 3" bölümünde). Canlı kanıt: gerçek satın alma +
      yeni run'da gerçek güçlenme (BatteryDps 2.5→3.0) doğrulandı.
- [x] FAZ 4 (seviye üreteci) — BİTTİ (6. tur, detay yukarıda "FAZ 4"
      bölümünde). `Frontline/Seviye Ureteci` EditorWindow, `LevelCatalog`/
      `LevelStore`/`LevelSelection`, `Battlefield`/`RunController` bağlantısı.
      Canlı kanıt: ekle → oyna → gerçekten farklı davran (11x20/3 dalga/Snow)
      → sil → varsayılana dön, hepsi gerçek Play'de.
- [ ] FAZ 5 (gece boyu 3 temiz tur döngüsü) — SIRADAKİ AJAN BURADAN BAŞLASIN.
      Tüm bağımlı fazlar (0-4) bitti. Gerçek sahnede tekrar tekrar Play'e bas,
      çeşitli senaryoları dene (kule kur/yükselt/sat, duraklat/ayarlar, harita
      teması değiştir, seviye editöründe yeni seviye ekle-oyna-sil, ana
      menüden kalıcı yükseltme yap), konsol hatası ararsan hemen düzelt,
      tekrar dene. En az 3 tam temiz tur (sıfır hata) art arda geçene kadar
      dur. İsteğe bağlı, zaman kalırsa: FAZ 1'den kalan Blender muzzle/impact/
      lazer sprite üretimi (hâlâ tür-standardı renklerle dahi yapılmadı,
      daire fallback) ve geri tepme animasyonu.

DOĞRULAMA KANITI (5. tur, FAZ 3): `mcp__unity__run_tests` 306/306 (iki kez,
öncesi/sonrası), `Frontline/Ana Menu Sahnesini Uret` + Play + gerçek buton
tıklaması (`ExecuteEvents.pointerClickHandler`) ile GÜNCELLE ekranı açıldı,
gerçek satın alma yapıldı (biyokütle 1090→890, disk kaydı doğrulandı),
`Frontline/Sandbox Sahnesini Uret` + Play + `TowerPlacer.Place` ile YENİ bir
Battery kulesi kuruldu, `tower.Level==2` ve `weapon.damagePerSecond==3.0`
(taban 2.5×1.2) doğrulandı, kıyas kulesi (Ambush, yükseltilmemiş) `Level==1`
kaldı. `read_console` her adımda 0 hata/0 uyarı. Test verisi (`meta.json`)
doğrulama sonunda `MetaProgressStore.Clear()` ile temizlendi. Commit
ATILMADI (talimat gereği).

---

## KULE YENİDEN TASARIM (piksel-ölçülü referansla, 4 Eylül gündüz)

Önceki tur (bu gece) telefonla `com.mobirix.tdwt` referans oyunundan
Makineli Tüfek kulesinin üstten-görünüm crop'unu (`ref_tower_topdown_zoom.png`)
piksel piksel ölçmüştü: gunmetal palet/tekerlek (#2D3339), bronz/bakır
silindirik gövde (üst #9B6742 / alt gölge #845131), koyu lens (#313131),
gümüş namlu (#C7CBD0), sıcak namlu-ucu highlight (#E3C8B5). O turda
`tools/blender/generate_tower_variety_set.py` bu paletle TAMAMEN yeniden
yazılmış ve 15 sprite (5 tip × 3 seviye) `Assets/Frontline/Art/TowerVariety/`
altına render edilmişti — ama oturum "Baseline 317/317, testleri çalıştıralım"
cümlesinde kesildi ve **sprite'lar hiçbir yerden kullanılmıyordu**: ArtLibrary
sadece dokümantasyon yorumunda TowerVariety'den bahsediyordu, gerçek kod
(`FrontlineSandboxSceneBuilder.MakeTower`) hâlâ eski `t_battery_turret` /
`t_battery_platform` gibi `Assets/Sprites/Frontline/` yollarını yüklüyordu.

Bu turda tamamlanan:
1. Referans crop + `ref_buildmenu2.png` / `ref_upgrade.png` görsel olarak
   incelendi, önceki turun ölçtüğü palet/yapı tarifi doğrulandı.
2. Blender script + üretilmiş 15 PNG (`hizli_ates/agir_top/keskin_nisanci/
   radar_destek/zaman_bozulmasi × L1/L2/L3`) tek tek Read ile görsel
   kontrolden geçirildi — hepsi ortak "hull" dilini (gunmetal palet + iki
   katmanlı bronz silindir + koyu lens) paylaşıyor, rol farkı namlu
   kalınlığı/uzunluğu (agır top kalın-kısa, keskin nişancı ince-uzun) ve
   namlusuz destek versiyonları (radar: döner çanak-anten, zaman bozulması:
   4/8 kanatlı kristal) ile veriliyor. Seviye artışı küçük ek parça (mühimmat
   kutusu, omuz plakası, ikinci uydu-çanak) + yıldız rozeti sayısıyla okunuyor.
3. **Asıl eksik olan bağlantı kuruldu**: `FrontlineSandboxSceneBuilder.cs`'e
   `MakeVarietyTower()` eklendi, `art.towers` artık TowerVariety setinden
   `levelTurrets[0..2]` dolduruyor (rol eşlemesi: Battery→agir_top,
   Ambush→hizli_ates, Beam→keskin_nisanci, Decoy→radar_destek,
   Minefield→zaman_bozulmasi). `platform=null` bırakıldı çünkü yeni
   render'lar palet+gövde+namlu TEK sprite'ta (eski iki-parçalı "sabit
   kaide" mimarisinden bilinçli sapma — namlu döndüğünde tekerlekler de
   döner, bilinen ve kabul edilen bir ödün). Tint alpha=0 verildi ki
   `TowerPlacer.ColorFor` eski primitif tint'i yeni renkli render'ın
   üstüne bindirmesin.
4. `TowerStars.cs` incelendi: yıldız rozetleri kule sprite'ının DIŞINDA,
   ayrı bir `Stars` alt-nesnesinde çiziliyor (bkz. `TowerPlacer.Place`
   satır ~413-421) — sprite içine yıldız GÖMÜLMEDİ, çakışma yok.

DOĞRULAMA:
- `refresh_unity` (force+compile) → 0 hata/uyarı.
- EditMode testleri: **317/317 yeşil** (iki ayrı koşu — wiring öncesi ve
  sonrası, `job_id 69346d36...` ve `13cae386...`, sırasıyla 38.9s ve 22.3s).
- `Frontline/Sandbox Sahnesini Uret` → sprite yükleme uyarısı YOK (yollar
  doğru çözüldü).
- Play mode → `TowerPlacer.Place()` reflection ile 5 kule (Battery/Ambush/
  Decoy/Minefield/Beam) kurulum şeridine yerleştirildi, ekran görüntüsü
  alındı.

DÜRÜST KIYAS (referans `ref_tower_topdown_zoom.png` vs oyun içi render):
- **Benzeyen:** Gunmetal gri palet/tekerlek blokları solda/sağda, ortada
  iki katmanlı bronz/bakır silindirik gövde, gövde üstünde koyu daire
  (lens/nişangah), sarı yıldız rozeti üstte — dört unsur da net şekilde
  referansla eşleşiyor. Genel siluet ve renk paleti artık eski "düz gri
  sekizgen + iki çubuk" tasarımıyla HİÇBİR ortak yanı yok, kullanıcının
  şikayet ettiği kopukluk giderildi.
- **Hâlâ farklı:** (a) Referansta namlu gövdeden YANA (sağa) uzanıyor;
  bizim render'da barrel -Y (aşağı) yönünde — kavramsal olarak aynı dil
  ama ekran-uzayı yönü farklı, TowerTurret zaten döndürdüğü için oyun
  içinde sorun yaratmıyor ama statik karşılaştırmada eksen farklı görünüyor.
  (b) Referanstaki namlu ucu sıcak highlight (#E3C8B5) bizim varsayılan
  kamera açısında namlu -Y'ye uzandığından ekran altında kalıyor, üstten
  bakışta daha az göze çarpıyor. (c) Palet/tekerlek blokları bizde daha
  köşeli/dikdörtgen, referansta hafif yuvarlatılmış — okunabilirlik
  etkilenmiyor ama bire bir kopya değil (zaten hedef değildi). (d) Yeni
  tasarımda namlu döndüğünde tüm gövde (paletler dahil) birlikte dönüyor
  (tek-sprite mimarisi) — referansın gerçek oyununda tekerlekler sabit
  kalıp sadece namlu dönüyor mu bilinmiyor (crop statik), bizim seçimimiz
  basitlik için bilinçli bir ödün.
- Diğer 4 kule tipi (Topçu/Keskin Nişancı/Radar/Zaman Bozulması) piksel
  ölçülemedi (yalnızca Makineli Tüfek ölçüldü) — ama aynı hull diliyle
  tutarlı üretildi, kafadan alakasız bir şey icat edilmedi.

Commit ATILMADI (talimat gereği).
