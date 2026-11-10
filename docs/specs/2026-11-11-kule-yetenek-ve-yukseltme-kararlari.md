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

- Temel: çevresindeki alanda düşman hızı 0,75 katına iner (%25 yavaşlama).
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

## Görsel kararlar

- Kuleler Blender'da 3D modellenip tam tepeden render edilir: toon boyama,
  koyu kontur, zemine düşen gölge.
- Kaide ve taret AYRI sprite: kaide sabit durur, yalnızca taret döner.
- Destek kulelerinde namlu yok.
- Tasarımlar bize özgü; referans oyundan yalnızca roller ve çizim tekniği
  alınır, kule tasarımları kopyalanmaz.
- Üretilen her asset kalıcı kütüphaneye kaydedilir: kaynak `.blend` +
  üretim scripti + export + dizin dosyası.
