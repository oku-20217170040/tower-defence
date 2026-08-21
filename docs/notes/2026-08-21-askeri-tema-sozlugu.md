# Askeri Tema Sözlüğü — 21 Ağustos 2026

Proje böcek/koloni temasından **askeri temaya** geçti. Karar: *mekanik hiç değişmedi*, yalnızca isimler ve kurgu değişti. Ölçülmüş 139 testlik denge zemini aynen geçerli.

## Neden

Oyunun çekirdeği "düşman öğrenir, sen yalan söylersin". Bu fikir askeri istihbaratla böcek feromonundan **daha** doğal çalışıyor: düşmanın keşif raporunu kirletiyorsun, sahte hedef gösteriyorsun, gizli mevzileri istihbarattan saklıyorsun.

## Sözlük

| Eski | Yeni | Kurgusal karşılık |
|---|---|---|
| Colony (namespace, klasör, menü) | **Frontline** | Cephe |
| ColonyWorld | Battlefield | Savaş alanı |
| ColonyGrid | BattleGrid | — |
| ColonyPathfinder | RoutePathfinder | — |
| ColonyTower | Emplacement | Mevzi |
| PheromoneMap | **ReconMap** | Ordunun "bu rota güvenli" hafızası |
| LureField | DecoyField | Sahte hedef alanı |
| ThreatField | ThreatField (aynı) | Tehdit — zaten askeri terim |
| SlowField | SlowField (aynı) | — |
| BugAgent / BugHealth / BugSpawner | UnitAgent / UnitHealth / UnitSpawner | Birlik |
| NestHealth | **HomeBaseHealth** | Üs canı (eski `Assets/Scripts/BaseHealth` ile çakışmasın diye "Home") |
| ColonyViewBinder | BattlefieldViewBinder | — |
| PheromoneOverlay / ColorRamp | ReconOverlay / ReconColorRamp | — |

## Kule sınıfları

| Eski | Yeni | Rolü (değişmedi) |
|---|---|---|
| Noisy | **Battery** — Batarya | Alan hasarı (topçu), yüksek tehdit yazar; ordu ondan kaçmayı öğrenir |
| Silent | **Ambush** — Pusu | Tek hedef, tehdit yazmaz; istihbarata düşmez, asıl katil |
| Lure | **Decoy** — Sahte Hedef | Hasar vermez, orduyu koridora çeker |
| Resin | **Minefield** — Mayın Tarlası | Yavaşlatır (x2), tehdit yazmaz |

## Türkçe terimler

böcek → **birlik** · koloni → **ordu** · feromon → **keşif izi** · yuva → **üs** · gürültücü → **batarya** · sessiz kule → **pusu mevzisi** · yem → **sahte hedef** · reçine → **mayın tarlası**

## Dikkat

- `Debug.Log` içindeki "bug." toplu değiştirmede `Deunit.Log`'a dönüştü ve elle düzeltildi. Benzer bir toplu işlemde aynı tuzağa dikkat.
- Branch adı `tasarim/koloni-reboot` olarak **kaldı** — git geçmişi bozulmasın diye değiştirilmedi.
- Sahne: `Assets/Scenes/Frontline_Sandbox.unity`, menü **Frontline → Sandbox Sahnesini Uret**.
- Eski `Assets/Scripts/` (reboot öncesi sistem) hâlâ duruyor ve bu değişiklikten etkilenmedi.
