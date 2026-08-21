# Durum ve Devir Notu

**Tarih:** 19 Ağustos 2026 · **son güncelleme 21 Ağustos 2026**
**Branch:** `tasarim/koloni-reboot` (master'a henüz merge edilmedi)
**Test durumu:** 139 EditMode testi, 139 geçiyor (20 Ağu 2026)
**Sıradaki iş:** (1) yeni dengenin gerçek run'da doğrulanması — aşağıya bak, (2) Plan 8 — birlik türleri (tank, asker, keşif). Plan 7'den yalnızca Işın kulesi kaldı. Bkz. `docs/plans/2026-08-20-kule-ve-birim-cesitliligi.md`. Plan 4 tamamen bitti; tek açık kalan Görev 5 Adım 2 (ekonomi arayüzünü kullanıcıya oynatma).

---

## ⚠ Yeni oturum için ilk iş: denge ölçümü gerçekle uyuşmuyor

**21 Ağustos, kullanıcı run'ı (ekran görüntüsüyle):** 15/15 dalga KAZANDI, **üs canı 26/30 — yani yalnızca 4 sızıntı**, 221 öldürme, 70 besin artmış.

Simülasyon aynı ayarlarla **21,8 sızıntı** öngörüyordu. Gerçek 4. Bu, kalibrasyonun bozulduğu anlamına gelir; sebebi araştırılmadan yeni denge sayısı seçilmemeli.

Muhtemel sebepler (ölçülmedi):
1. Yeni tempo (8 sn hazırlık + ilk 4 dalga 6 sn ara) oyuncuya simülasyonun modellemediği bir avantaj veriyor olabilir — sim dalga arası beklemeyi hiç modellemiyor.
2. Batarya kulesinin alan hasarı gerçek oyunda simülasyondakinden etkili olabilir (gerçek oyunda kuleler sürekli ateş ediyor, sim tick tabanlı).
3. Oyuncu kule yerleşimini simülasyondaki sabit şablondan iyi yapıyor olabilir.

`RunCalibrationTests` hâlâ **eski** referans run'a (20 Ağustos, 16 sızıntı) bağlı ve geçiyor — yani araç eski koşullarda doğru. Yeni denge için **yeni bir referans run** alınmalı: kullanıcıdan bir run'ın kule dağılımı + sonucu istenip `RunCalibrationTests`'e ikinci bir referans olarak eklenmeli.

## Oyun nedir

**Ordu öğrenir, sen yalan söylersin.** Dikey tutuşlu Android roguelike tower defense. Birlik sürüleri keşif izi bırakarak yol öğrenir; oyuncu bu hafızayı okuyabilir ve kirletebilir.

Tam tasarım: `docs/specs/2026-08-19-cekirdek-oyun-tasarimi.md`

---

## Mimari

```
Assets/Frontline/
  Core/      Frontline.Core       — saf C#, noEngineReferences, MonoBehaviour YOK
  Runtime/   Frontline.Runtime    — MonoBehaviour'lar, Core'u kullanır
  Editor/    Frontline.Editor     — sahne üretici menü
  Tests/     Frontline.Tests.EditMode — 13 test dosyası
```

**Bu ayrım pazarlıksız.** `Frontline.Core`'a Unity tipi sızarsa denge ölçümlerini başsız koşamayız; bu projede en çok işe yarayan şey oydu.

Sahne kod ile üretilir: Unity menüsünde **Frontline → Sandbox Sahnesini Uret**. Sahne YAML'ini elle düzenleme.

Eski `Assets/Scripts/` hâlâ duruyor, silinmedi.

---

## Ölçüm modeli düzeltildi + gerçek run kalibrasyonu (20 Ağustos 2026)

**Kırık olan:** bütün eski denge simülasyonları birlikleri **sırayla** işliyordu — bir birlik tüm yolu yürüyor, sonra diğeri. O modelde tek hedefli bir kule, aynı anda geçen sekiz birliğin **hepsine** tam DPS veriyor; gerçekte bir seferde tek birliğe ateş eder (`TowerWeapon.FindNearestTarget`).

| 8 birlik, 3 pusu mevzisi, 4 dps | Ölen | Sızan |
|---|---|---|
| Sıralı (eski model) | 8 | 0 |
| Eşzamanlı (`RunSimulator`) | 2 | 6 |

**Kırık OLMAYAN: oyunun dengesi.** İlk çıkarımım "eşzamanlı modelde 12 kule bile kaybediyor, demek oyun kazanılamaz" idi — **yanlıştı.** Sebep: simülasyonda kule sayısını ekonomiden bağımsız düşük tutmuştum. Gerçek oyuncu draft ödülleri ve öldürme geliriyle çok daha fazla kule kuruyor.

**Gerçek run (kullanıcı, 20 Ağu, ekran görüntüsüyle):** 15/15 dalga, KAZANDI, üs 14/30, 209 öldürme, 16 sızıntı, **24 kule** (13 pusu + 7 batarya + 4 mayın tarlası), üstüne 116 besin artmış.

**Kalibrasyon:** aynı 24 kule kurulumu `RunSimulator`'da 223,5 öldürme veriyor (gerçek 209, %7 fark) — simülatör doğrulanmış sayılır. Sızıntıda fark var (sim 1,5 / gerçek 16) çünkü sim kuleleri baştan kurulu kabul ediyor, oyuncu ise dalga dalga kuruyor. **Yani sızıntının neredeyse tamamı erken dalgalarda, kuleler azken oluşuyor.**

**Kalibrasyon tamamlandı (`EconomyPlanner` + `RunCalibrationTests`).** Simülatöre ekonomi, draft ve kulelerin zamanla kurulması eklendi. 10 seed ortalaması, gerçek run'ın yanında:

| | Gerçek run | Simülatör | Fark |
|---|---|---|---|
| Öldürme | 209 | 206,7 | %1 |
| Sızıntı | 16 | 18,3 | %14 |
| Kule | 24 | 27,2 | %13 |
| Artan besin | 116 | 107,7 | %7 |

**Simülatör artık güvenilir bir ölçüm aracıdır.** Denge sayıları bununla yeniden ölçülebilir.

**Açık kalan iş: geç oyun ekonomisi gevşek.** Hedef "8-10. dalgada 4-5 kule" idi; gerçekte run 24 kule ve 116 artan besinle bitiyor. Üs canının yarısı yine de gidiyor — yani zorluk erken dalgalardan geliyor, geç oyun rahat.

Aşağıdaki tablodaki savaş sayıları hâlâ sıralı modelle seçilmiştir; eski üç denge test dosyasının başına da uyarı konuldu. Testler geçiyor ama ölçtükleri şey gerçek oyun değil.

## Ölçülmüş sabitler — TAHMİNLE DEĞİŞTİRME

Bu sayıların hepsi tarama ile seçildi. Her biri bir testle korunuyor. Değiştirmek isteyen önce ölçmeli.

| Sabit | Değer | Neden bu değer |
|---|---|---|
| Keşif oranı | 0,35 | 0 iken öğrenme hiç olmuyor; 0,5 iken ordu asla karar veremiyor |
| Çekim ağırlığı | 3 | 6 iken doymuş iz bir duvar; 2 iken sürü ilk dalgada pes ediyor |
| Tehdit ağırlığı | 3 | — |
| Birlik başına iz | 2 (0,5 yürürken + 1,5 varışta) | — |
| Mayın tarlası yavaşlatma | x2 | x1,5 ölü mekanik (%20 kazanma), x3 oyunu çözüyor (sızıntı 0). Tehdit yazarsa %95 → %5 |
| Batarya kule | **alan hasarı, 2,5 dps** | Alan hasarı ~4 kat değer katıyor; 2,5 splash = 10 tek hedefli. 4 dps oyunu kolaylaştırıyor, 1,5 ölü |
| Pusu mevzisi DPS | 4 | 3'te hiç öldürmüyor, 4'te hepsini öldürüyor — eşik keskin |
| Batarya kule DPS | 10 | Önünden geçen kalmadığı için gerçek DPS'i çok daha düşük |
| Birlik canı | 10 × (1 + **0,10** × (dalga−1)) | 0,15'te ekonominin hiçbir düğmesi çalışmıyordu: yetersiz kule → az öldürme → az gelir → çöküş sarmalı. 0,10'da eşik yumuşadı |
| İlk dalga birlik | 8 | 10 iken oyuncu toparlanamadan 4. dalgada kaybediyordu |
| Dalga temposu | ilk dalga öncesi 8 sn, ilk 4 dalga arası 6 sn, sonra 2 sn | Erken oyunda düşünme süresi zorluktan değerli; oyuncu ilk kulelerini kurmadan boğulmamalı |
| Üs canı | 30 | Zayıf düğme: 50 yapmak kayıp dalgasını 10'dan 11'e taşıdı |
| Başlangıç besini | **90** | 50'de ilk dalgada YALNIZCA 1 kule kurulabiliyordu (3 koridor varken) — oyuncu geri bildirimi doğrulandı. Dalga geliri 6, öldürme geliri 1 ile birlikte: erken oyun açılıyor, toplam zorluk aynı kalıyor |
| Kule fiyatları | Batarya **20** · Yem/Mayın tarlası **26** · Pusu **40**, aynı türde +5 | 20 Ağu taraması: +10 artış run'ı öldürüyordu (kazanma %10 → +5'te %85) |

**Ölçülen eğri (20 Ağu):** optimal oyuncu run'ların %85'ini kazanıyor, 8. dalgada ortalama 5,6 kule, üs canının 30'da 22'si gidiyor. Eski 3/5/6/9 hedefi ölçümde hiç kazanmıyordu — geçersiz.

> **Testlerin kör noktası (20 Ağu 2026'da acı bir şekilde öğrenildi):** Denge testleri saf modeli simüle eder, **sahnedeki zinciri değil.** Üsya varan birliğin can eksiltmesi hiç kodlanmamıştı; 104 test yeşilken oyunda üs canı düşmüyordu. Simülasyon "sızıntı 22/30" derken gerçek oyunda sıfırdı. Runtime zinciri (spawn → yürüme → varış → hasar → run sonu) yalnızca oynayarak doğrulanır. Yeni bir runtime davranışı eklerken buna dikkat.

---

## Ölçümle bulunan üç tasarım gerçeği

Bunlar tasarımda yoktu; ancak ölçünce ortaya çıktılar ve spec'e işlendiler.

**1. Keşif olmadan öğrenme yok (spec 4.1).** A* belirlenimci, koridorlar eşit maliyette — her birlik aynı koridoru seçiyordu. Her birliğe kendine özgü yön tercihi verilerek çözüldü. Sapma **sütun bazında** üretilir; hücre bazında üretilirse yol boyunca ortalamaya gidip söner.

**2. Tek araç yetmiyor (spec 3.4).** Batarya kule tek başına sürüyü sarsıyor ama ordu geri dönüyor. Yem tek başına hiç etki etmiyor. Yalnızca ikisi birlikte sürüyü taşıyor. Oyunun aha anı bu.

**3. Korunmayan koridor her zaman bulunur (spec 3.5).** Bütün kuleleri tek koridora yığmak işe yaramıyor; ordu orada ölmeye başlayınca izi terk edip korumasız koridordan akıyor. Kuleler üç koridora dağıtılmalı.

---

## Çalışma yöntemi — bu projede işe yarayan disiplin

1. **Denge sayıları tahminle seçilmez.** Önce `execute_code` ile tarama yapılır, kazanan değer koda geçer, sonra o davranışı koruyan test yazılır. Bu yöntem şimdiye kadar üç kez tasarım hatası yakaladı.
2. **TDD:** test yaz → Unity konsolunda başarısızlığı gör → implementasyon → testler yeşil → commit.
3. **Görsel doğrulama kullanıcıya aittir.** Unity play modu pencere odağı gerektiriyor; MCP üzerinden çalışırken oyun ilerlemiyor (`frame=1`de donuyor). Ölçülebilir her şey EditMode testine, görsel olan her şey kullanıcıya.
4. Alternatif görsel doğrulama: dokuyu başsız üretip PNG kaydetmek işe yarıyor (`overlay_preview.png` böyle üretildi).

---

## Plan durumu

| Plan | Durum |
|---|---|
| Plan 1 — Ordu çekirdeği | **Bitti.** Keşif izi, yem, tehdit, rota modeli, harita üretimi |
| Plan 2 — Görselleştirme | **Bitti.** Mekanik oyuncu tarafından doğrulandı |
| Plan 3 — Savaş | **Bitti.** Can, hasar, üs, run akışı, arayüz, zorluk eğrisi |
| Plan 4 — Ekonomi ve draft | **Bitti.** Ekonomi, draft, eğri ölçümü. Açık: Görev 5 Adım 2 (kullanıcı testi) |
| Plan 7 — Kule sınıfları (Mayın tarlası, Işın, Saat) | Taslak yazıldı, karar alındı |
| Plan 8 — Birlik türleri (izci, asker, kör, kuluçka, kraliçe) | Karar alındı, plan yazılmadı |
| Plan 5 — Meta ilerleme, nemesis | Yazılmadı |
| Plan 6 — Blender asset'leri, ses | Yazılmadı |

---

## Sıradaki iş: Plan 4 Görev 3 — dalga sonu draft

Plan: `docs/plans/2026-08-19-ordu-ekonomi.md`

Kritik tasarım noktası (spec 2.2): sunulan 3 seçenek rastgele **değil**, ordunun o anki davranışına göre ağırlıklandırılır. Sol koridor ezberlenmişse oraya uygun seçeneklerin çıkma olasılığı artar. Bu, adaptif sistemi oynanışa bağlayan ikinci kanal ve draft'ı sıradan bir kart seçiminden ayıran şey.

Dosyalar: `Core/Draft/DraftOption.cs`, `Core/Draft/DraftGenerator.cs`, `Runtime/View/DraftView.cs`, `Tests/EditMode/DraftGeneratorTests.cs`

---

## Devredilen ve bekleyen işler

- **Hayalet iz ön izlemesi** (Plan 2 Görev 5): kule elde tutulurken rotanın nasıl değişeceğini göstermek. Yapılmadı, mekanik onsuz da anlaşıldı. Okunabilirlik sorun çıkarırsa öne alınmalı.
- **Yem tazelemenin maliyeti:** Sahte hedef her dalga bedava tazeleniyor. Spec 3.3 "yem sürekli beslenmeli" diyor ama besleme otomatikse ortada karar kalmıyor. Ekonomiyle birlikte karara bağlanmalı.
- **Birliklerin üst üste binmesi:** kozmetik, gerçek asset'lerle çözülecek.
- **Eski `Assets/Scripts/` silinmesi:** yeni sistem eskisinin yerini tamamen aldığında.

---

## Ortam

- Unity 6000.3.21f1, sahne `Assets/Scenes/Frontline_Sandbox.unity`
- Unity MCP ve Blender MCP kullanıcı seviyesinde tanımlı, ikisi de bağlanabiliyor
- Blender bağlantısı: 3D görünümde **N** → **BlenderMCP** sekmesi → **Connect to Claude** (port 9876)
- Unity MCP portu 8080 — Brick Havoc projesiyle paylaşılıyor, aynı anda tek Unity açık tutulmalı
- Oyun kontrolleri: **1** batarya, **2** pusu, **3** yem, sol tık kurulum şeridine yerleştirir
