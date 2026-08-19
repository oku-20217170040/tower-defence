# Plan 2 — Oynanabilirlik Kararı

**Tarih:** 19 Ağustos 2026
**Karar:** Mekanik doğrulandı. Plan 3'e geçiliyor.

Plan 2'nin altı görevi tek bir soruyu sorabilmek için yapıldı (spec bölüm 8): *oyuncu "yem + kule" kombinasyonunu kendi keşfettiğinde bir aha anı yaşıyor mu?*

## Üç soru, üç cevap

**1. Sessiz ile gürültücü kulenin farkı bakınca anlaşılıyor mu?**
Evet. Titreşim halkası ayrımı taşıyor; halkasızlık "koloni beni göremiyor" mesajını isim okumadan veriyor.

**2. Sürüyü kasıtlı olarak yanlış koridora çekmek tatmin edici mi?**
Evet. Doğrulandı ve Plan 3'e geçme kararı buna dayanıyor.

**3. Feromon izi okunabilir mi?**
İlk halinde değildi. İz koridorun dışına taşıp kurulum şeritlerinin üzerine yayılıyor, birkaç dalgada tavana dayanıp tek düze bir lekeye dönüşüyordu. Hücre başına texel sayısı 4'e çıkarıldı ve alfaya gamma eğrisi eklendi; sonrasında okunabilir.

## Ölçümle bulunan iki tasarım gerçeği

Bu ikisi tasarımda yoktu ve ancak ölçünce ortaya çıktı. İkisi de spec'e işlendi.

**Keşif olmadan öğrenme yok (spec 4.1).** A* belirlenimci, koridorlar eşit maliyette — dolayısıyla her böcek aynı koridoru seçiyor ve koloni hiçbir zaman alternatif denemiyordu. Öğrenecek bir şey yoktu. Her böceğe kendine özgü bir yön tercihi verilerek çözüldü.

**Tek araç yetmiyor (spec 3.4).** Gürültücü kule tek başına sürüyü sarsıyor ama koloni geri dönüyor. Yem tek başına hiç etki etmiyor. Yalnızca ikisi birlikte sürüyü taşıyor. Bu, aha anının doğrudan karşılığı oldu.

## Devredilen işler

- **Hayalet iz ön izlemesi** (Plan 2, Görev 5, Adım 4): kule elde tutulurken rotanın nasıl değişeceğini göstermek. Yapılmadı. Mekanik onsuz da anlaşıldığı için ertelendi; oyuncu testinde okunabilirlik sorun çıkarırsa öne alınmalı.
- **Böceklerin üst üste binmesi:** kozmetik. Gerçek asset'ler ve konum sapmasıyla çözülecek.

## Sıradaki en önemli şey

Plan 2'de böcekler ölmüyor, bu yüzden izler yalnızca güçleniyor. Ölüm devreye girdiğinde döngü kapanıyor: tuzak koridorunda ölen böcekler izi zayıflatacak, yani **başarılı tuzak kendini yiyecek** (spec 3.3). Bu, tasarımda yazılı ama hiç ölçülmemiş tek büyük dinamik. Plan 3'ün merkezi bu olmalı.
