# Durum ve Devir Notu

**Tarih:** 19 Ağustos 2026
**Branch:** `tasarim/koloni-reboot` (master'a henüz merge edilmedi)
**Test durumu:** 89 EditMode testi, 89 geçiyor
**Sıradaki iş:** Plan 4, Görev 3 — dalga sonu draft sistemi

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

## Ölçülmüş sabitler — TAHMİNLE DEĞİŞTİRME

Bu sayıların hepsi tarama ile seçildi. Her biri bir testle korunuyor. Değiştirmek isteyen önce ölçmeli.

| Sabit | Değer | Neden bu değer |
|---|---|---|
| Keşif oranı | 0,35 | 0 iken öğrenme hiç olmuyor; 0,5 iken koloni asla karar veremiyor |
| Çekim ağırlığı | 3 | 6 iken doymuş iz bir duvar; 2 iken sürü ilk dalgada pes ediyor |
| Tehdit ağırlığı | 3 | — |
| Böcek başına iz | 2 (0,5 yürürken + 1,5 varışta) | — |
| Sessiz kule DPS | 4 | 3'te hiç öldürmüyor, 4'te hepsini öldürüyor — eşik keskin |
| Gürültücü kule DPS | 10 | Önünden geçen kalmadığı için gerçek DPS'i çok daha düşük |
| Böcek canı | 10 × (1 + 0,15 × (dalga−1)) | Asıl zorluk düğmesi bu |
| İlk dalga böcek | 8 | 10 iken oyuncu toparlanamadan 4. dalgada kaybediyordu |
| Yuva canı | 30 | Zayıf düğme: 50 yapmak kayıp dalgasını 10'dan 11'e taşıdı |
| Başlangıç besini | 40 | 60 sıkıcı kolay, 30 bıçak sırtı |
| Kule fiyatları | Gürültücü 12 · Yem 16 · Sessiz 20, aynı türde +10 | Gürültücü ucuz çünkü tek başına işe yaramıyor |

**Hedef eğri:** dalga 5/8/10/15'te 3/5/6/9 kule → run kazanılır ama yuva canının yarısı gider.

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
| Plan 4 — Ekonomi ve draft | Görev 1-2 ve 5'in bir kısmı bitti. **Sırada Görev 3: draft** |
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
