# Kule Yetenek ve Yükseltme Kararları — 11 Kasım 2026

Serhat ile kule kule konuşularak alınan kararlar. Bu dosya kulelerin
mekaniği ve görseli için tek doğru kaynaktır; eski notlardaki
Batarya/Pusu/Yem/Mayın/Işın tanımlarının yerine geçer.

Sayılar bilinçli olarak yazılmadı (aşağıda açıkça verilenler hariç):
her değer simülasyon taramasıyla bulunacak, tahminle girilmeyecek.

## Genel kurallar

- Her kulenin 3 seviyesi var. Seviye 2 sayısal güçlenme + küçük ek,
  seviye 3 imza yetenek. Seviye 3'ün görseli imza yeteneği göstermeli.
- İsimler mekaniğe göre: ekrandaki ad kulenin gerçekte yaptığı işi söyler.
- Yem ve Mayın Tarlası kaldırılıyor; yerlerine Radar ve Zaman Bozulması
  geliyor (görsel + kod). Yem'e bağlı sahte iz mekaniği ve testleri de
  sökülecek ya da yeniden yazılacak.
- Destek kulelerinin (Radar, Zaman Bozulması) seviye 3 bonusları HAFİF
  olacak: hissedilir ama stratejiyi tek başına değiştirmeyecek kadar.

## Kod eşlemesi

| Yeni kule | Eski TowerKind | Değişiklik |
|---|---|---|
| Makineli Tüfek | Ambush | isim + seviye yetenekleri |
| Topçu | Battery | halka hasarından gerçek mermiye |
| Keskin Nişancı | Beam | sınırlı menzil + seviye yetenekleri + yeni görsel |
| Zaman Bozulması | Minefield | yavaşlatma değerleri + kırılganlık |
| Radar | Decoy | tamamen yeni mekanik |

## 1. Makineli Tüfek

- Temel: tek hedef, çok hızlı ateş.
- Seviye 2: ateş hızı artar.
- Seviye 3: çift namlu, aynı anda iki hedefe ateş.

## 2. Topçu

- Temel: gerçek top mermisi. Yavaş atar, düştüğü yerde patlar, patlama
  yarıçapındaki herkese hasar verir. Gürültülüdür: sürüyü o yoldan kaçırma
  etkisi korunur.
- Seviye 2: patlama yarıçapı büyür, hasar artar.
- Seviye 3: yanan krater. Patlama yerinde birkaç saniye yanan alan kalır,
  içinden geçen hasar alır.

## 3. Keskin Nişancı

- Temel: düz hat boyunca üzerindeki herkesi delen tek atış. Yavaş ateş,
  pahalı, ıskalamaz. Menzili oyundaki EN UZUN menzil ama SINIRLI (tüm
  haritayı kapsamaz).
- Seviye 2: atış aralığı kısalır, hasar artar, menzil büyür.
- Seviye 3: menzil bir kez daha büyür; zırh delme (hattaki en yüksek
  canlı hedefe ek hasar, kraliçe gibi iri düşmanlara cevap); öldürdüğü
  hedeften sonra hemen ikinci atış.
- Görsel: elektrik ışını yerine ince mermi izi ve belirgin geri tepme.
  Kule görseli baştan yapılacak.

## 4. Zaman Bozulması

- Temel: çevresindeki alanda düşman hızı 0,6 katına iner (%40 yavaşlama).
  İlk karar 0,75'ti; ölçümde ortalama oyuncu o değerle run'ların %25'ini
  kazandı, 0,6 ile %85. Serhat 0,6'yı onayladı.
  Ateş etmez. İki kulenin etkisi üst üste binmez, güçlü olan geçerli.
- Seviye 2: alan büyür, yavaşlatma güçlenir.
- Seviye 3: yavaşlatma bir kademe daha güçlenir + hafif kırılganlık
  (alandaki düşman diğer kulelerden biraz daha fazla hasar alır).
- Uçan birlikler ETKİLENMEZ.
- Donduran darbe yok.

## 5. Radar

- Temel: çevresindeki SİLAHLI kulelerin menzilini artırır. Ateş etmez.
  Aynı kuleye iki radarın etkisi üst üste binmez.
- Seviye 2: etki alanı büyür, menzil bonusu artar.
- Seviye 3: menzil bonusu bir kez daha artar + etki alanındaki kulelerin
  ateş hızı hafifçe artar.
- Keskin Nişancı'nın menzilini de artırır.
- Zaman Bozulması'nı ETKİLEMEZ.

## Uygulama durumu ve ölçümler (11 Kasım 2026)

Kod yazıldı: sayıların tek kaynağı `Core/Towers/TowerStats.cs`, silah
`Runtime/Towers/TowerWeapon.cs`, kararları kilitleyen testler
`Tests/EditMode/TowerStatsTests.cs`. 335 test yeşil; mevcut kalibrasyon
testlerinin hiçbiri değiştirilmedi.

Ölçülerek seçilen seviye 1 değerleri (usta oyuncu, 20 seed):

- **Topçu:** patlama yarıçapı 2,5 hücre, saniyelik 2,5 hasar eşdeğeri.
  Patlama toplanmış sürüye çok daha sert vurduğu için iki kurulum birlikte
  tarandı; tam tablo `TowerStats.cs` içinde.
- **Keskin Nişancı:** menzil 9 hücre. Toplanmış orduya karşı sızıntıyı
  19,1'den 15,5'e indiriyor (menzil 40 iken 12,2).

Seviye 2 ve 3 değerleri ÖLÇÜLMEDİ: simülatör yükseltmeyi modellemiyor.
Yön kararlara uygun, sayılar ilk değerdir.

## Serhat'ın karar vermesi gerekenler

1. ~~Zaman Bozulması seviye 1 zayıf~~ ÇÖZÜLDÜ: hız 0,75 → 0,6
   (seviyeler 0,6 / 0,55 / 0,5). Simülatör hâlâ seviye 3 değerini (0,5)
   kullanıyor; yükseltmeyi modellemediği için bilinçli sapma.
2. ~~Fiyatlar~~ ÇÖZÜLDÜ (Serhat fiyat kararını simülasyona bıraktı):

   | Kule | Eski | Yeni |
   |---|---|---|
   | Topçu | 100 | 100 |
   | Radar | 130 | 125 |
   | Zaman Bozulması | 130 | 150 |
   | Makineli Tüfek | 200 | 175 |
   | Keskin Nişancı | 170 | 200 |

   Taranan setler (usta / askerli usta / ortalama oyuncu sızıntısı):
   eski 8,8 / 10,1 / 23,2 · takas 12,7 / 16,7 / 12,1 · değerle orantılı
   9,2 / 12,5 / 10,5 · seçilen 7,8 / 10,3 / 17,4. Değerle orantılı set
   usta ile ortalama oyuncu arasındaki farkı kapatıyordu ("alım sırası
   önemli olmalı" kuralını bozuyordu); seçilen set farkı koruyor ve
   ortalama oyuncuyu rahatlatıyor. Keskin Nişancı ve Radar bu politikalarda
   alınmadığı için ölçülen katkılarına göre kondu, taranmadı.
3. **Radar'ın Keskin Nişancı'ya oranı** yarım uygulandı (aşağıya bak).
4. **Kör birlik** yemi yok sayma/silme özelliği taşıyor; yem kulesi
   kalktığı için bu özellik oyunda artık karşılıksız.

## Açık nokta

- Radar'ın Keskin Nişancı'ya verdiği menzil bonusu diğer kulelerle aynı
  oranda mı, yarı oranda mı olacak: Serhat "artırsın" dedi, oran
  konuşulmadı. Varsayılan: tarama haritanın çoğunu kapsadığını gösterirse
  yarı oran, aksi halde tam oran.

## Etki geri bildirimi (Serhat, 11 Kasım 2026)

Bir kulenin başka bir şeyi etkilediği her durum oyuncuya HİSSETTİRİLMELİ:
görsel, gerekirse dokunsal (titreşim) ve işitsel.

- Zaman Bozulması etkisindeki düşman: üzerine ağırlık çökmüş hissi,
  kırmızı loş bir ışık düşer. Alandan çıkınca kaybolur.
- Radar etkisindeki kule: ara ara yanıp sönen kısa bir parlama ile
  "bu kule radarın etkisinde" hissi.
- Diğer kuleler de aynı ilkeye uyar: kırılganlık alan düşman, yanan
  kraterdeki düşman, zırh delen atış, ikinci atış, çift namlu ateşi
  her biri kendi görsel/işitsel işaretini taşır. Ayrıntıları her kulenin
  kod adımında belirlenir.

## Kamera (11 Kasım 2026)

- Harita sürüklenerek kaydırılır (tek parmak).
- Zoom YALNIZCA İKİ KADEME: geniş (haritanın tam genişliği, 13 hücre) ve
  onun biraz darı (9,5 hücre). Kıstırma ya da fare tekerleği kademeyi
  değiştirir; daha yakına inilmez. Açılış dar kademede.

## Görsel kararlar

- Kuleler Blender'da 3D modellenip tam tepeden render edilir: toon boyama,
  koyu kontur, zemine düşen gölge.
- Kaide ve taret AYRI sprite: kaide sabit durur, yalnızca taret döner.
- Destek kulelerinde namlu yok.
- Tasarımlar bize özgü; referans oyundan yalnızca roller ve çizim tekniği
  alınır, kule tasarımları kopyalanmaz.
- Üretilen her asset kalıcı kütüphaneye kaydedilir: kaynak `.blend` +
  üretim scripti + export + dizin dosyası.
