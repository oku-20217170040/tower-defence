# Kule Sınıfları ve Böcek Türleri — Plan 7 (taslak)

**Tarih:** 20 Ağustos 2026
**Durum:** Karar alındı, uygulama Plan 4 Görev 4 (ekonomi eğrisi) bittikten sonra başlar.

## Karar

Klasik TD roster'ı (okçu/topçu/buz/lazer, piyade/hızlı/tank/uçan/bölünen/boss) **olduğu gibi alınmadı**. Sebep: o roster'ın ekseni hasar tipi ve can-hız matrisidir; bu oyunun ekseni **koloninin görüp göremediğidir** (spec 3.1-3.2). İki eksen üst üste binerse ölçülmüş üç tasarım gerçeği (spec 3.4 tek araç yetmiyor, 3.5 korunmayan koridor bulunur) ezilir.

Roster, mevcut eksene uyarlanarak alındı.

## Kuleler

| Sınıf | Durum | Rol |
|---|---|---|
| Sessiz | Var (4 dps) | Tek hedef, sürekli, tehdit yazmaz. Asıl katil. |
| Gürültücü | Var (10 dps) | **Alan hasarı ölçüldü, eklenmeyi bekliyor.** Aynı 4 dps ile tek hedefli kule 8 böcekten 2'sini, alan hasarlı 8'ini öldürüyor. Yüksek tehdit yazımı; yönlendirme aracı. |
| Yem | Var | Pazarlıksız. Spec 3.4'ün "aha anı" ona bağlı. |
| **Reçine** | **Yapıldı** (20 Ağu) | Yavaşlatır (x2), tehdit yazmaz, fiyat 16. Böcek sessiz kulenin menzilinde uzun kalır. Tek başına değersiz. |
| **Işın** | Yeni | Delip geçen hat hasarı. Kalabalık koridoru cezalandırır; koloninin tek koridora yığılmasıyla doğrudan çatışır. |

**Saat (clock) kulesi — eklenmedi (20 Ağustos kararı).** Kısa süre düşünüldü, sonra çıkarıldı: Reçine zaten yavaşlatmayı sağlıyor. Reçine yerel ve kalıcı (menzilindeki böcek yavaş ilerler, bir *yerleştirme* kararıdır), Saat global ve anlık olacaktı (bir *zamanlama* kararı). Farklı eksende olsalar da oyuncunun kafasında ikisi aynı yere düşer ve roster'ı kalabalıklaştırır. Anlık müdahale aracı ilerde gerekirse Plan 5'te (meta ilerleme) yeniden değerlendirilir.

**Yükseltme:** Draft zaten kalıcı hasar/menzil pasifi veriyor. Kule başına ayrı yükseltme ekonomiye üçüncü para deliği açar; Görev 4 eğrisi kilitlenmeden eklenmeyecek. Eklenirse en fazla 2 seviye.

## Böcekler

Bir böcek türü feromon/öğrenme ile etkileşmiyorsa yalnızca bir can rakamıdır. Her tür bir öğrenme kanalına bağlandı.

| Tür | Davranış |
|---|---|
| Piyade | Mevcut böcek. İz bırakır. |
| **İzci** (hızlı) | Az can, yüksek hız, iz bırakır. Koloni onun sayesinde öğrenir; oyuncu onu öldürerek öğrenmeyi yavaşlatabilir. |
| **Asker** (tank) | Yavaş, çok canlı, **izi çok güçlü basar**. Doymuş iz duvardır — geçtiği koridoru koloniye çiviler. |
| **Kör böcek** (uçan yerine) | Yerde yürür ama feromona ve yeme **hiç bakmaz**; en kısa yolu seçer. Karar: gerçek uçan düşman eklenmedi, çünkü yalan söyleme kanalını tümüyle kapatırdı. |
| **Kuluçka** (bölünen) | Ölünce ikiye ayrılır ve ölüm yerine güçlü ölüm-izi bırakır. Oyuncunun kendi tuzağı koloniye "buradan gitme" diye işaretlenir. |
| **Kraliçe** (boss) | Tehdit alanını temizler — koloninin korkusunu siler, öğrenmeyi resetler. |

## Reçine ölçümü — 20 Ağustos, TAMAMLANDI

**Seçilen değerler:** yavaşlatma çarpanı **x2** (yarım hız), tehdit **yazmaz**, fiyat 16.

Ölçüm sabit kule bütçesiyle yapıldı (ekonomi devre dışı, kuleler baştan kurulu), 20 seed. 6 kule bütçesi, 3 sessiz + 3 reçine:

| Yavaşlatma | Kazanma | Ort. sızıntı | Sonuç |
|---|---|---|---|
| x1,5 | %20 | 28,8 | Reçine ölü mekanik — aldığı sessiz kulenin yerini dolduramıyor |
| **x2** | **%95** | **17,5** | **Seçilen.** Saf sessizle (%100 / 12,7) başa baş → gerçek seçim |
| x3 | %100 | 0,0 | Baskın strateji; sızıntıyı sıfırlıyor, oyunu çözüyor |
| x2 + tehdit yazar | %5 | 29,8 | Koloni kaçıyor, yavaşlatma hiçbir böceğe değmiyor |

**Bulgu 1 — eşik yine keskin.** x1,5 ölü, x3 oyunu çözüyor, arada tek çalışan değer var. Bu projede üçüncü kez oluyor (sessiz kule DPS 3/4, keşif oranı 0/0,35/0,5).

**Bulgu 2 — reçine tehdit yazmamalı, ve bu ölçüldü.** Yazınca kazanma %95'ten %5'e düşüyor: koloni reçineden kaçıyor, yavaşlatma hiç kimseye değmiyor. `SlowField`'ın `ThreatField`'dan ayrı bir sınıf olmasının gerekçesi budur — algılanmayan bir etki, algılanan bir etkiden yapısal olarak farklıdır.

**Bulgu 3 — önceki taramanın "1/3 çöküyor" gizemi çözüldü.** Sorun reçinenin savaş değeri değilmiş: sabit bütçede 6 sessiz + 3 reçine %100 kazanıyor, ekonomiyle birlikte %0'dı. Sebep fiyat artışının (`costStep`) tür başına ayrı sayılması — karışım yapan oyuncu her iki türde de düşük sayıda kaldığı için ekonomi ona beklenmedik biçimde davranıyor. **Açık iş:** reçine artık oyunda olduğuna göre ekonomi eğrisi (Plan 4 Görev 4) karışımlı oyuncu politikasıyla yeniden ölçülmeli.

**Uygulandı:** `Core/Fields/SlowField.cs`, `TowerKind.Resin`, `ColonyWorld.RebuildSlow()`, `BugAgent` hız bölümü, 4 tuşu, fiyat 16, draft seçeneklerine dahil. `ColonyResinTests` (5 test) ve `SlowFieldTests` (7 test) kararları kilitliyor.

## Sıra

1. Plan 4 Görev 4 — ekonomi eğrisi ölçülüp kilitlenir (**önce bu**)
2. Plan 7 Görev 1 — ~~Reçine~~ **bitti**. Sırada: Gürültücü kuleye alan hasarı, sonra Işın
3. **Alan hasarı (topçu) ölçüldü:** `RunSimulator` ile aynı DPS'te tek hedefli 2, alan hasarlı 8 öldürme. Sürüye karşı fark devasa — ama önce temel denge sayıları eşzamanlı modelle yeniden ölçülmeli, yoksa topçu kırık bir zemine oturur.
4. **Işın için uyarı:** mevcut simülasyon böcekleri *sıralı* işliyor, aynı anda kaç böceğin hizada olduğunu modellemiyor. Delici hat hasarının değeri tam olarak buna bağlı; Işın ölçülmeden önce simülasyonun eşzamanlı hale getirilmesi gerekiyor. Bu, reçineden daha büyük bir iş.
3. Plan 8 — böcek türleri, her biri kendi öğrenme testiyle
