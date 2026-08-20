# Durum ve Devir Notu

**Tarih:** 19 Ağustos 2026
**Branch:** `tasarim/koloni-reboot` (master'a henüz merge edilmedi)
**Test durumu:** 129 EditMode testi, 129 geçiyor (20 Ağu 2026)
**Sıradaki iş:** Plan 7 — kule sınıfları (Reçine, Işın, Saat) ve Plan 8 — böcek türleri. Bkz. `docs/plans/2026-08-20-kule-ve-bocek-cesitliligi.md`. Plan 4 tamamen bitti; tek açık kalan Görev 5 Adım 2 (ekonomi arayüzünü kullanıcıya oynatma).

---

## Oyun nedir

**Koloni öğrenir, sen yalan söylersin.** Dikey tutuşlu Android roguelike tower defense. Böcek sürüleri feromon izi bırakarak yol öğrenir; oyuncu bu hafızayı okuyabilir ve kirletebilir.

Tam tasarım: `docs/specs/2026-08-19-cekirdek-oyun-tasarimi.md`

---

## Mimari

```
Assets/Colony/
  Core/      Colony.Core       — saf C#, noEngineReferences, MonoBehaviour YOK
  Runtime/   Colony.Runtime    — MonoBehaviour'lar, Core'u kullanır
  Editor/    Colony.Editor     — sahne üretici menü
  Tests/     Colony.Tests.EditMode — 13 test dosyası
```

**Bu ayrım pazarlıksız.** `Colony.Core`'a Unity tipi sızarsa denge ölçümlerini başsız koşamayız; bu projede en çok işe yarayan şey oydu.

Sahne kod ile üretilir: Unity menüsünde **Colony → Sandbox Sahnesini Uret**. Sahne YAML'ini elle düzenleme.

Eski `Assets/Scripts/` hâlâ duruyor, silinmedi.

---

## Ölçüm modeli düzeltildi + gerçek run kalibrasyonu (20 Ağustos 2026)

**Kırık olan:** bütün eski denge simülasyonları böcekleri **sırayla** işliyordu — bir böcek tüm yolu yürüyor, sonra diğeri. O modelde tek hedefli bir kule, aynı anda geçen sekiz böceğin **hepsine** tam DPS veriyor; gerçekte bir seferde tek böceğe ateş eder (`TowerWeapon.FindNearestTarget`).

| 8 böcek, 3 sessiz kule, 4 dps | Ölen | Sızan |
|---|---|---|
| Sıralı (eski model) | 8 | 0 |
| Eşzamanlı (`RunSimulator`) | 2 | 6 |

**Kırık OLMAYAN: oyunun dengesi.** İlk çıkarımım "eşzamanlı modelde 12 kule bile kaybediyor, demek oyun kazanılamaz" idi — **yanlıştı.** Sebep: simülasyonda kule sayısını ekonomiden bağımsız düşük tutmuştum. Gerçek oyuncu draft ödülleri ve öldürme geliriyle çok daha fazla kule kuruyor.

**Gerçek run (kullanıcı, 20 Ağu, ekran görüntüsüyle):** 15/15 dalga, KAZANDI, yuva 14/30, 209 öldürme, 16 sızıntı, **24 kule** (13 sessiz + 7 gürültücü + 4 reçine), üstüne 116 besin artmış.

**Kalibrasyon:** aynı 24 kule kurulumu `RunSimulator`'da 223,5 öldürme veriyor (gerçek 209, %7 fark) — simülatör doğrulanmış sayılır. Sızıntıda fark var (sim 1,5 / gerçek 16) çünkü sim kuleleri baştan kurulu kabul ediyor, oyuncu ise dalga dalga kuruyor. **Yani sızıntının neredeyse tamamı erken dalgalarda, kuleler azken oluşuyor.**

**Buradan çıkan iki iş:**
1. `RunSimulator`'a ekonomi + draft eklenip kulelerin zamanla kurulması modellenmeli. O zaman sim gerçek sızıntıyı (16) üretmeli; ürettiğinde denge sayıları güvenle yeniden ölçülebilir.
2. **Geç oyun ekonomisi gevşek.** Hedef "8-10. dalgada 4-5 kule" idi; gerçekte run 24 kule ve 116 artan besinle bitiyor. Yuva canının yarısı yine de gidiyor — yani zorluk erken dalgalardan geliyor, geç oyun rahat.

Aşağıdaki tablodaki savaş sayıları hâlâ sıralı modelle seçilmiştir; eski üç denge test dosyasının başına da uyarı konuldu. Testler geçiyor ama ölçtükleri şey gerçek oyun değil.

## Ölçülmüş sabitler — TAHMİNLE DEĞİŞTİRME

Bu sayıların hepsi tarama ile seçildi. Her biri bir testle korunuyor. Değiştirmek isteyen önce ölçmeli.

| Sabit | Değer | Neden bu değer |
|---|---|---|
| Keşif oranı | 0,35 | 0 iken öğrenme hiç olmuyor; 0,5 iken koloni asla karar veremiyor |
| Çekim ağırlığı | 3 | 6 iken doymuş iz bir duvar; 2 iken sürü ilk dalgada pes ediyor |
| Tehdit ağırlığı | 3 | — |
| Böcek başına iz | 2 (0,5 yürürken + 1,5 varışta) | — |
| Reçine yavaşlatma | x2 | x1,5 ölü mekanik (%20 kazanma), x3 oyunu çözüyor (sızıntı 0). Tehdit yazarsa %95 → %5 |
| Sessiz kule DPS | 4 | 3'te hiç öldürmüyor, 4'te hepsini öldürüyor — eşik keskin |
| Gürültücü kule DPS | 10 | Önünden geçen kalmadığı için gerçek DPS'i çok daha düşük |
| Böcek canı | 10 × (1 + 0,15 × (dalga−1)) | Asıl zorluk düğmesi bu |
| İlk dalga böcek | 8 | 10 iken oyuncu toparlanamadan 4. dalgada kaybediyordu |
| Yuva canı | 30 | Zayıf düğme: 50 yapmak kayıp dalgasını 10'dan 11'e taşıdı |
| Başlangıç besini | 50 | 20 Ağu taraması; 40'ta oyuncu ilk dalgalarda iki kule bile kuramıyor |
| Kule fiyatları | Gürültücü 12 · Yem 16 · Sessiz 24, aynı türde **+5** | 20 Ağu taraması: +10 artış run'ı öldürüyordu (kazanma %10 → +5'te %85) |

**Ölçülen eğri (20 Ağu):** optimal oyuncu run'ların %85'ini kazanıyor, 8. dalgada ortalama 5,6 kule, yuva canının 30'da 22'si gidiyor. Eski 3/5/6/9 hedefi ölçümde hiç kazanmıyordu — geçersiz.

> **Testlerin kör noktası (20 Ağu 2026'da acı bir şekilde öğrenildi):** Denge testleri saf modeli simüle eder, **sahnedeki zinciri değil.** Yuvaya varan böceğin can eksiltmesi hiç kodlanmamıştı; 104 test yeşilken oyunda yuva canı düşmüyordu. Simülasyon "sızıntı 22/30" derken gerçek oyunda sıfırdı. Runtime zinciri (spawn → yürüme → varış → hasar → run sonu) yalnızca oynayarak doğrulanır. Yeni bir runtime davranışı eklerken buna dikkat.

---

## Ölçümle bulunan üç tasarım gerçeği

Bunlar tasarımda yoktu; ancak ölçünce ortaya çıktılar ve spec'e işlendiler.

**1. Keşif olmadan öğrenme yok (spec 4.1).** A* belirlenimci, koridorlar eşit maliyette — her böcek aynı koridoru seçiyordu. Her böceğe kendine özgü yön tercihi verilerek çözüldü. Sapma **sütun bazında** üretilir; hücre bazında üretilirse yol boyunca ortalamaya gidip söner.

**2. Tek araç yetmiyor (spec 3.4).** Gürültücü kule tek başına sürüyü sarsıyor ama koloni geri dönüyor. Yem tek başına hiç etki etmiyor. Yalnızca ikisi birlikte sürüyü taşıyor. Oyunun aha anı bu.

**3. Korunmayan koridor her zaman bulunur (spec 3.5).** Bütün kuleleri tek koridora yığmak işe yaramıyor; koloni orada ölmeye başlayınca izi terk edip korumasız koridordan akıyor. Kuleler üç koridora dağıtılmalı.

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
| Plan 1 — Koloni çekirdeği | **Bitti.** Feromon, yem, tehdit, rota modeli, harita üretimi |
| Plan 2 — Görselleştirme | **Bitti.** Mekanik oyuncu tarafından doğrulandı |
| Plan 3 — Savaş | **Bitti.** Can, hasar, yuva, run akışı, arayüz, zorluk eğrisi |
| Plan 4 — Ekonomi ve draft | **Bitti.** Ekonomi, draft, eğri ölçümü. Açık: Görev 5 Adım 2 (kullanıcı testi) |
| Plan 7 — Kule sınıfları (Reçine, Işın, Saat) | Taslak yazıldı, karar alındı |
| Plan 8 — Böcek türleri (izci, asker, kör, kuluçka, kraliçe) | Karar alındı, plan yazılmadı |
| Plan 5 — Meta ilerleme, nemesis | Yazılmadı |
| Plan 6 — Blender asset'leri, ses | Yazılmadı |

---

## Sıradaki iş: Plan 4 Görev 3 — dalga sonu draft

Plan: `docs/plans/2026-08-19-koloni-ekonomi.md`

Kritik tasarım noktası (spec 2.2): sunulan 3 seçenek rastgele **değil**, kolonin o anki davranışına göre ağırlıklandırılır. Sol koridor ezberlenmişse oraya uygun seçeneklerin çıkma olasılığı artar. Bu, adaptif sistemi oynanışa bağlayan ikinci kanal ve draft'ı sıradan bir kart seçiminden ayıran şey.

Dosyalar: `Core/Draft/DraftOption.cs`, `Core/Draft/DraftGenerator.cs`, `Runtime/View/DraftView.cs`, `Tests/EditMode/DraftGeneratorTests.cs`

---

## Devredilen ve bekleyen işler

- **Hayalet iz ön izlemesi** (Plan 2 Görev 5): kule elde tutulurken rotanın nasıl değişeceğini göstermek. Yapılmadı, mekanik onsuz da anlaşıldı. Okunabilirlik sorun çıkarırsa öne alınmalı.
- **Yem tazelemenin maliyeti:** Yem kulesi her dalga bedava tazeleniyor. Spec 3.3 "yem sürekli beslenmeli" diyor ama besleme otomatikse ortada karar kalmıyor. Ekonomiyle birlikte karara bağlanmalı.
- **Böceklerin üst üste binmesi:** kozmetik, gerçek asset'lerle çözülecek.
- **Eski `Assets/Scripts/` silinmesi:** yeni sistem eskisinin yerini tamamen aldığında.

---

## Ortam

- Unity 6000.3.21f1, sahne `Assets/Scenes/Colony_Sandbox.unity`
- Unity MCP ve Blender MCP kullanıcı seviyesinde tanımlı, ikisi de bağlanabiliyor
- Blender bağlantısı: 3D görünümde **N** → **BlenderMCP** sekmesi → **Connect to Claude** (port 9876)
- Unity MCP portu 8080 — Brick Havoc projesiyle paylaşılıyor, aynı anda tek Unity açık tutulmalı
- Oyun kontrolleri: **1** gürültücü, **2** sessiz, **3** yem, sol tık kurulum şeridine yerleştirir
