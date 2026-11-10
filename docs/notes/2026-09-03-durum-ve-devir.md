# Durum ve Devir Notu — 2 Eylül'den 3 Eylül 2026'ya devam

**Branch:** `tasarim/koloni-reboot`
**Test durumu:** 292 EditMode testi, hepsi geçiyor (son doğrulama 2 Eylül 2026, 23:14).
**Commit durumu:** HİÇBİR ŞEY COMMIT EDİLMEDİ. `git status` 234 satır değişiklik gösteriyor (düzenlemeler + eski `Assets/Scripts`/`Assets/Editor`/kök `.cs` dosyalarının silinmesi). Kullanıcı commit istemedi, sıradaki oturum da istenmeden commit ATMAMALI.

**Bu not neden var:** Sohbet bağlamı doldu, kullanıcı yeni bir oturuma geçiyor. Aşağıdaki her şey o oturumun bilmesi gereken güncel durum.

---

## Unity MCP bağlantısı KOPTU

Bu notun yazıldığı anda `mcp__unity__*` araçları hiçbiri kullanılamıyor durumda ("unity (ConnectionRefused): Unable to connect"). **Sıradaki oturum önce Unity Editor'ün açık olduğunu ve MCP for Unity bridge'inin bağlı olduğunu doğrulamalı** (`mcpforunity://instances` kaynağına bakmak yeterli). Unity kapalıysa kullanıcıdan açmasını istemek gerekir.

## Telefon (adb) bağlantısı

USB, PC şarjı ile telefon arasında SIRAYLA kullanılıyor (kullanıcının donanım kısıtı) — aynı anda ikisi birden bağlı olmayabilir. Kullanıcı en son "telefonu bağladım, bu sefer şarj sorunu yok" dedi ama bu mesajdan hemen sonra bağlam doldu ve devir notuna geçildi; **yeni oturum `adb devices -l` ile bağlantıyı YENİDEN doğrulamalı**, varsayma.

### ÖNEMLİ YÖN DEĞİŞİKLİĞİ (3 Eylül 2026) — telefon analiz ajanı artık PASİF DEĞİL, AKTİF OYNAMALI

Kullanıcı açıkça talimat verdi: **"telefona USB ile bağlanacak olan ajan artık benim oynamamı beklemesin, kendisi oynasın ve gerekli bütün yerlere kendisi baksın, notlarını alsın, merak ettiği şeyleri kendisi denesin."**

Önceki iki telefon-analiz turu (2 Eylül 2026) PASİF gözlemciydi: yalnızca `adb exec-out screencap` ile ekran görüntüsü alıp kullanıcının oynamasını bekliyordu — bu yüzden ikisi de yarım kaldı (biri "ekran 3.5 dakikadır sabit, oyunda değilsin" dedi, diğeri "boş kareye ilk dokunma anını yakalayamadım" dedi). Kullanıcı bu bekleme modelini artık istemiyor.

**Sıradaki telefon-analiz ajanı şöyle çalışmalı:**
- `adb shell input tap X Y`, `adb shell input swipe X1 Y1 X2 Y2` ile OYUNU KENDİSİ OYNASIN — menülerde gezinsin, bir aşama başlatsın, kule kursun, ağaç/kayaya dokunsun, sarf malzemesi kullansın, merak ettiği her mekaniği bizzat tetikleyip sonucunu gözlemlesin.
- Koordinatları önce bir `screencap` ile bulsun (ekranın neresinde ne var), sonra oraya dokunsun, sonra tekrar `screencap` ile sonucu görsün — bu döngüyü kendi başına, kullanıcıdan hamle beklemeden sürdürsün.
- Telif kuralı DEĞİŞMEDİ: asset kopyalama/indirme yok, yalnızca mekanik/sayı/akış gözlemi. Oynaması bu kuralı ihlal etmiyor çünkü hâlâ yalnızca ekran görüntüsü alıp not çıkarıyor, dosya indirmiyor.
- Belirsiz kalan bir şey varsa (örn. bir mekaniğin var olup olmadığı net değilse) hâlâ TAHMİN ETMEMELİ, ama artık "kullanıcı oynamıyor" diye durmak yerine KENDİSİ o senaryoyu oynayarak netleştirmeli.
- Hâlâ açık soru olarak kalanlar (varsa) rapor sonunda kullanıcıya sorulmalı, ama bu artık "kullanıcı oynasın" değil "belirsizliği ben çözemedim, şuna bakar mısın" formatında olmalı.

---

## Bugün (2 Eylül 2026) yapılanlar — hepsi 292 testle doğrulandı

### Kullanıcı geri bildirimiyle düzeltilen/eklenen sistemler

1. **Kamera yakınlaştırma** — `MapCamera` başlangıç görünümü 13→7 hücre genişliğine, en yakın zoom limiti 10→5 hücreye indirildi (`FrontlineSandboxSceneBuilder.cs`). Objeler artık büyük görünüyor, harita tamamı ekranda değil.
2. **Bataryanın sürekli titreşen kırmızı tehdit çemberi kaldırıldı** (`ThreatRingView` kullanımı `TowerPlacer.Place()`'ten söküldü; dosya duruyor ama artık hiçbir yerden çağrılmıyor — istenirse silinebilir).
3. **Hücre başına en fazla bir kule** — bu kontrol HİÇ YOKTU, eklendi (`TowerPlacer.IsOccupied`).
4. **Kule seçimi + menzil halkası** — `TowerSelection.cs` (yeni) + `RangeRingView.cs` (yeni): bir kuleye dokununca SADECE onun menzili görünür (silik, sabit, Blender'ın ürettiği gerçek `fx_range_ring.png` sprite'ıyla), tekrar dokununca ya da başka yere dokununca kaybolur. Aynı anda en fazla bir halka.
5. **Manuel hedef kilitleme (ağaç/kaya)** — kullanıcı "düşmanlara otomatik kilitleme zaten var (referans oyunda da öyle), ben taş/ağaç gibi yıkılabilir objeler için MANUEL istiyorum" dedi. Uygulandı:
   - `TowerWeapon.cs`: `TrySetManualTarget(cell)` / `ClearManualTarget()` / `HasManualTarget`. Manuel hedef aktifken kule **TAMAMEN** o hedefe odaklanır, birliklere ateş etmez (kullanıcının "odak tamamen oraya dönsün" tarifine göre bilinçli tercih).
   - `SceneryFocusController.cs` (yeni): ağaç/kayaya dokununca, o hücreyi menziline alan TÜM kuleleri kilitler. Tekrar aynı hücreye dokununca serbest bırakır. Görsel işaret: Blender'ın ürettiği `fx_focus_marker.png`.
   - Obje patlayınca kule otomatik olarak manuel hedefi bırakır (test edildi, `execute_code` ile gerçek `Update()` döngüsü çalıştırılarak kanıtlandı).
6. **Obje patlatma ekonomisi** — `SceneryReward.cs` (Core, yeni, saf): `coins = maxHp * 0.5`, en az 1. Varsayılan kaya (30 HP) → 15 coin (referans oyunda gözlenen +15 ile birebir örtüşüyor), ağaç (12 HP) → 6 coin. `SceneryRewardRunner.cs` (Runtime, yeni) bunu `Battlefield.SceneryClearedDetailed` event'ine bağlıyor. `SceneryMap.cs`'e additive bir `ClearedDetailed` event'i eklendi (mevcut `Cleared` event'i BOZULMADI, testler etkilenmedi).
7. **Radial kule yerleştirme menüsü** — `RadialBuildMenu.cs` (yeni, `Runtime/UI/`): boş kurulum karesine dokununca 5 kule seçeneği **gerçek çember üzerinde**, her dilimde **gerçek kule sprite'ı** (düz renk kare DEĞİL — kullanıcı bunu özellikle istedi), fiyat etiketi, kilit gösterimi. Merkezde nötr bir suret placeholder'ı var. `TowerPlacer.TryPlaceAt` artık boş hücreye dokununca DOĞRUDAN kurmuyor, bu menüyü açıyor; seçim `RadialBuildMenu.ChooseTower` içinde gerçekleşiyor.

### Bu turda bulunup düzeltilen İKİ gerçek hata (ikisi de kullanıcının gerçek kullanımda yakaladığı)

**Hata 1 — arka plan "duraklatma menüsü" gibi görünüyordu.** Radial menünün backdrop'u (tüm ekranı kaplayan koyu overlay, alpha 0.35) sahneyi karartıp "pause/ana menü" hissi veriyordu. Kullanıcı "oyunun bir parçası gibi açılsın" dedi. **Düzeltme:** backdrop artık tamamen şeffaf (`Color(0,0,0,0)`), yalnızca dokunma yakalayıcı olarak duruyor, sahne hiç kararmıyor.

**Hata 2 — KRİTİK: kule, ilk dokunulan hücreye değil, "seçim anında dilimin ekranda göründüğü yere" kuruluyordu.** Kök neden: **sahnede hiç `EventSystem` yoktu.** Bu iki şeyi bozuyordu: (a) uGUI `Button.onClick` hiçbir zaman EventSystem olmadan tetiklenmez, (b) `UiTouchGuard.IsPointerOverUi()` `EventSystem.current` null olduğu için her zaman `false` dönüyordu. Sonuç: dilime "dokunmak" aslında butonu hiç tetiklemiyor, bunun yerine `TowerPlacer.Update()` bu dokunuşu HAM bir harita dokunuşu sanıp kendi `TryPlaceAt`'ini çalıştırıyor, dokunulan ekran konumundaki (dilimin göründüğü yerdeki) YENİ bir hücre için `radialMenu.Open()` çağırıp menünün hedef hücresini (`targetCell`) SESSİZCE değiştiriyordu.

**Düzeltme (üç parça):**
1. `FrontlineSandboxSceneBuilder.cs`'e `EventSystem` + `StandaloneInputModule` eklendi (eski Input Manager aktif, yeni Input System paketi projede YÜKLÜ DEĞİL — `InputSystemUIInputModule` KULLANILMAMALI, derleme hatası verir).
2. Ek güvenlik katmanı: `RadialBuildMenu`'ye statik `Instance` eklendi (`MapCamera.Instance` deseniyle aynı). `TowerPlacer`, `TowerSelection`, `SceneryFocusController` artık `radialMenu.IsOpen` iken hiçbir dokunma işlemi yapmıyor.
3. **Gerçek bir tıklama olayı** (`UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler` üzerinden, `EventSystem.current.RaycastAll` ile) simüle edilerek kanıtlandı: kule artık **açılan hücreye** kuruluyor, fiyat düşüyor, menü kapanıyor. Bu önemli çünkü **önceki tüm testlerim** (`button.onClick.Invoke()` ile doğrudan çağırma) bu eksikliği gizlemişti — gerçek kullanıcı etkileşimini hiç simüle etmemiştim. Ders: uGUI etkileşimini test ederken `ExecuteEvents` ile gerçek olay simülasyonu şart, `onClick.Invoke()` yeterli değil.

**HENÜZ KULLANICI TARAFINDAN GERÇEK PARMAKLA/FAREYLE DOĞRULANMADI.** Kullanıcı "telefonu tekrar bağladım, şarj sorunu yok" dedi ve tam o anda bağlam doldu. **Sıradaki oturumun ilk işi:** kullanıcıdan Unity Editor'de (ya da build alınmışsa telefonda) radial menüyü gerçekten deneyip iki hatanın da düzeldiğini teyit etmesini istemek.

---

## Önceki turdan (aynı gün, daha erken) tamamlanan büyük sistemler

Bunlar da 292 teste dahil, hepsi çalışıyor:

- **Meta ilerleme** (`Core/Meta/`, `Runtime/Meta/`): biyokütle ekonomisi, kule kilitleri (Batarya+Pusu açık başlar, Sahte/Mayın/Işın kilitli), istihbarat seviyesi, disk kaydı (`MetaProgressStore`, `persistentDataPath/meta.json`).
- **Genetik evrim katmanı** (`Core/Evolution/`): run'lar arası nemesis sistemi, toplam güç bütçesi matematiksel olarak sabit (oyuncu cezalandırılmıyor, çeşitlendiriliyor), tam deterministik (seed'li).
- **Uçan birlik** (`UnitType.Flyer`, `Core/Units/FlightPath.cs`): koridor tanımayan tek birlik türü, üç kısıtla dengelenmiş (seyrek/iz bırakmaz/menzilli kuleye açık). `UnitRoster.Default()`'a KARIŞTIRILMADI, ayrı `WithFlyers()` metodu var — mevcut kalibrasyon testleri bozulmasın diye.
- **Dikey mobil UI** (`Runtime/UI/RunScreen.cs`, `TowerBarView.cs`, `UiKit.cs`, `UiTouchGuard.cs`, `Core/UI/SafeAreaLayout.cs`, `HudFormat.cs`): eski IMGUI `RunHud` yerine geçen gerçek uGUI üretim ekranı. Üç bant (üst: can/dalga/kalan düşman, orta: harita, alt: kule barı + hız/duraklat). `RunHud.cs` hâlâ duruyor ama sahne kurucudan artık çağrılmıyor (debug/sandbox için elle eklenebilir).

## Blender'ın ürettiği asset'ler (aktif klasör: `Assets/Sprites/Frontline/`)

Önemli: proje ZATEN kaliteli bir sprite setine sahipti (44 dosya, turret+platform ayrımı). Bugün Blender ajanı bir kere yanlışlıkla ayrı bir `Assets/Frontline/Art/Generated/` klasörüne gereksiz bir KOPYA set üretmişti — **o silindi**, kullanıcı onayıyla. Doğru üretim aktif klasöre yapıldı:
- `fx_scenery_burst.png` — obje patlama efekti
- `fx_focus_marker.png` — hedef kilitleme işareti (kullanımda, `SceneryFocusController`)
- `fx_range_ring.png` — menzil halkası (kullanımda, `RangeRingView`)
- `scenery_tree_large.png`, `scenery_rock_large.png` — büyük obje varyasyonu, **HENÜZ SAHNEYE BAĞLANMADI** (aşağıya bak).

Kaynak: `Assets/Frontline/Art/fx-source.blend`, üretim scripti `tools/blender/fx_scenery_extra.py` (tekrar çalıştırılabilir).

---

## AÇIK / BEKLEYEN İŞLER

1. **Radial menü düzeltmesinin gerçek parmak/fare testi** — yukarıda anlatıldı, en öncelikli.
2. **Telefon derin analizi tamamlanmadı.** İki arka plan turu çalıştı, ikisinde de adb bağlantısı koptu ya da ekran sabit kaldı. Son bilinen sonuçlar `docs/notes/2026-09-02-referans-oyun-gozlem.md` dosyasında — hedef kilitleme referans oyunda OTOMATİK olduğu netleşti (biz MANUEL yaptık, kullanıcı onayladı), ama "kule kurma anının referans oyunda birebir görünümü" hâlâ net değil. Artık önemi azaldı çünkü kullanıcının kendi tarifine göre zaten uyguladık, ama istenirse ince ayar için devam ettirilebilir.
3. **`scenery_tree_large.png` / `scenery_rock_large.png` sahneye bağlanmadı.** Şu an obje büyüklüğü yalnızca COİN miktarında yansıyor (maxHp'ye bağlı), GÖRSEL olarak değil — tüm ağaçlar/kayalar hâlâ tek boyutta. Bağlamak için: `SceneryMap`'e ya da `MapGenerator`'a bir "büyük/küçük" varyant kavramı eklenmeli (örn. health'e göre ya da rastgele oranla), `SceneryView.cs`'te iki sprite arasından seçim yapılmalı.
4. **`Level_1..10.unity`, `MainMenu.unity`, `Game 9.unity` sahneleri** hâlâ projede duruyor (build'de değiller, zararsız). Kullanıcıya sorulmadı, silinmedi.
5. **`ThreatRingView.cs`** artık hiçbir yerden çağrılmıyor (ölü kod). İstenirse silinebilir.

## Mimari hatırlatmaları (değişmedi)

```
Assets/Frontline/
  Core/      Frontline.Core       — saf C#, noEngineReferences=true, MonoBehaviour YOK
  Runtime/   Frontline.Runtime    — MonoBehaviour'lar, Core'u kullanır
  Editor/    Frontline.Editor     — sahne üretici menü (Frontline → Sandbox Sahnesini Uret)
  Tests/     Frontline.Tests.EditMode
```

Sahne KOD ile üretilir (`FrontlineSandboxSceneBuilder.cs`), YAML elle düzenlenmez. Her değişiklikten sonra: derle → `Frontline/Sandbox Sahnesini Uret` → testleri koş → mümkünse Play mode'da `execute_code` ile ya da ekran görüntüsüyle canlı doğrula.

**Oyun tasarımı referansı:** `docs/specs/2026-08-19-cekirdek-oyun-tasarimi.md`. Rehber ilke değişmedi: "ordu öğrenir, sen yalan söylersin" — batarya kuleler yönlendirme aracı, pusu mevzileri gerçek katliamı yapar, sahte hedef koridora yapay iz basar.
