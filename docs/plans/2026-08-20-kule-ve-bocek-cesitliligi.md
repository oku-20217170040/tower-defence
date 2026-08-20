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
| Gürültücü | Var (10 dps) | **Alan hasarı eklenecek.** Yüksek tehdit yazımı; yönlendirme aracı. |
| Yem | Var | Pazarlıksız. Spec 3.4'ün "aha anı" ona bağlı. |
| **Reçine** | Yeni | Yavaşlatır. Böcek koridorda uzun kalır → sessiz kulelerin gerçek DPS'i katlanır. **Ölçülecek soru:** tehdit yazsın mı? Yazarsa yönlendirici, yazmazsa muhtemelen fazla güçlü. |
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

## Reçine ölçümü — 20 Ağustos, YARIM KALDI

İlk tarama yapıldı (`ResinScanTests`, `[Explicit]`, silinmedi — sonraki tur için duruyor). Sonuç **karar vermeye yetmiyor**; nedenini yazıyorum ki bir sonraki tur baştan başlamasın.

**Ne ölçüldü:** reçine karışım oranı (yok / 1-4 / 1-3 / 1-2) × yavaşlatma çarpanı (x1,5 / x2 / x3) × tehdit yazma × fiyat (16 / 24), 20 seed.

**Güvenilir olan tek bulgu:** reçineyi sessiz kulenin *menziline* koymak şart. İlk modelde reçine bağımsız yerleştiriliyordu ve yavaşlatma çarpanının sonuca **hiç** etkisi olmuyordu — modelin yanlış olduğunun işaretiydi. Eşleştirilmiş yerleşimle etki ortaya çıktı.

**Modelin hâlâ çözemediği iki şey:**

1. **1/4 karışımda yavaşlatma çarpanı sonucu değiştirmiyor** (x1,5 / x2 / x3 → hepsi %95, sızıntı 20,7). Muhtemel açıklama: tuzağın zaten öldürdüğü böceği daha hızlı öldürmek sonucu değiştirmiyor; sızıntının kaynağı korunmasız koridor (spec 3.5) ve yavaşlatma oraya dokunmuyor. Doğruysa bu bir tasarım gerçeği: **yavaşlatma yalnızca hasarın yetersiz kaldığı yerde iş görür.** Ama doğrulanmadı.

2. **1/3 karışım açıklanamaz biçimde çöküyor:** 1/4 → %95 ve 12,6 kule, 1/3 → **%0** ve 5,4 kule, 1/2 → %100 ve 15,0 kule. Ortadaki oranın iki ucundan da kötü olması modelin bozuk olduğuna işaret ediyor; muhtemelen fiyat artışı (costStep) tür başına ayrı sayıldığı için karışım oranı ekonomiyi beklenmedik yönde buruyor.

**Sonraki tur için:** önce (2)'yi çöz — karışım oranının kule bütçesine etkisini ekonomiden ayır (sabit kule bütçesiyle koştur), sonra yavaşlatmanın etkisini ölç. Model düzelmeden reçine sayıları koda geçirilmemeli.

## Sıra

1. Plan 4 Görev 4 — ekonomi eğrisi ölçülüp kilitlenir (**önce bu**)
2. Plan 7 Görev 1 — Reçine (ölçüm modeli düzeltilecek) ve Işın
3. **Işın için uyarı:** mevcut simülasyon böcekleri *sıralı* işliyor, aynı anda kaç böceğin hizada olduğunu modellemiyor. Delici hat hasarının değeri tam olarak buna bağlı; Işın ölçülmeden önce simülasyonun eşzamanlı hale getirilmesi gerekiyor. Bu, reçineden daha büyük bir iş.
3. Plan 8 — böcek türleri, her biri kendi öğrenme testiyle
