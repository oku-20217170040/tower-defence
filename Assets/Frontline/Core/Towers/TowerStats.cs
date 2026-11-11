using System;

namespace Frontline.Core
{
    /// <summary>
    /// Bir kulenin tek bir seviyedeki butun sayilari. Kullanilmayan alan 0
    /// (ya da carpanlarda 1) kalir - her kule yalnizca kendi alanlarini okur.
    /// </summary>
    public struct TowerLevelStats
    {
        /// <summary>Silah menzili (hucre). Destek kulelerinde 0.</summary>
        public float Range;

        /// <summary>Makineli Tufek: hedef basina saniyelik hasar.</summary>
        public float Dps;

        /// <summary>Makineli Tufek: ayni anda kac hedefe ates eder.</summary>
        public int Targets;

        /// <summary>Topcu mermisi / Keskin Nisanci atisi basina hasar.</summary>
        public float ShotDamage;

        /// <summary>Iki atis arasindaki sure (saniye).</summary>
        public float ShotInterval;

        /// <summary>Topcu: patlama yaricapi (hucre).</summary>
        public float BlastRadius;

        /// <summary>Topcu sv3: yanan kraterin saniyelik hasari ve suresi.</summary>
        public float BurnDps;
        public float BurnSeconds;

        /// <summary>
        /// Keskin Nisanci sv3 (zirh delme): hattaki EN YUKSEK canli hedefe
        /// atis hasarinin bu kati kadar EK hasar. 0 = yok.
        /// </summary>
        public float PierceBonus;

        /// <summary>Keskin Nisanci sv3: atis birini oldururse hemen ikinci atis.</summary>
        public bool FollowUpOnKill;

        /// <summary>
        /// Zaman Bozulmasi: gecis SURESI carpani (hizi boler). Hiz carpani
        /// 0,75 ise bu 1/0,75'tir. 1 = etkisiz.
        /// </summary>
        public float SlowFactor;

        /// <summary>
        /// Zaman Bozulmasi sv3 (kirilganlik): alandaki dusmanin aldigi
        /// hasarin carpani. 1 = etkisiz.
        /// </summary>
        public float Fragility;

        /// <summary>Destek kulesinin etki yaricapi (hucre).</summary>
        public float AuraRadius;

        /// <summary>Radar: etkiledigi kulelere menzil eki (0,2 = +%20).</summary>
        public float RangeBonus;

        /// <summary>Radar sv3: etkiledigi kulelere ates hizi eki.</summary>
        public float FireRateBonus;
    }

    /// <summary>
    /// Bes kulenin uc seviyesinin tek dogru kaynagi (11 Kasim 2026 kararlari,
    /// docs/specs/2026-11-11-kule-yetenek-ve-yukseltme-kararlari.md).
    ///
    /// Kod adlari eski, gorunen adlar HudFormat'ta:
    ///   Ambush    = Makineli Tufek     Battery   = Topcu
    ///   Beam      = Keskin Nisanci     Minefield = Zaman Bozulmasi
    ///   Decoy     = Radar
    ///
    /// OLCUM DURUMU - hangi sayi neye dayaniyor:
    ///   * Seviye 1 sayilari basssiz simulasyonla (RunSimulator) hizalidir;
    ///     degistirirsen EconomyPlanner.Policy.Default() da degismeli.
    ///   * Seviye 2-3 sayilari OLCULMEDI: simulator yukseltmeyi modellemiyor.
    ///     Bunlar kararlarin yonunu tasiyan ilk degerlerdir, denge iddiasi
    ///     degildir. Simulator yukseltme almayi ogrenince taranmalidir.
    /// </summary>
    public static class TowerStats
    {
        public const int MaxLevel = 3;

        /// <summary>
        /// Zaman Bozulmasi sv1 hiz carpani. Ilk karar 0,75'ti; olcumde
        /// ortalama oyuncu o degerle run'larin yalnizca %25'ini kazandi
        /// (0,65 -> %65, 0,6 -> %85, 0,5 -> %100). Serhat 11 Kasim 2026'da
        /// 0,6'yi onayladi.
        /// </summary>
        public const float SlowSpeedLevel1 = 0.6f;

        /// <summary>
        /// Radar'in Keskin Nisanci'ya verdigi menzil ekinin orani. Harita 13
        /// hucre genisliginde; tam oranla sv3 radar + sv3 nisanci haritanin
        /// enini bastan basa kapsiyordu, bu yuzden YARIM (spec'teki acik nokta).
        /// </summary>
        public const float RadarShareForSniper = 0.5f;

        public static TowerLevelStats For(TowerKind kind, int level)
        {
            if (level < 1) level = 1;
            if (level > MaxLevel) level = MaxLevel;

            var s = new TowerLevelStats { Targets = 1, SlowFactor = 1f, Fragility = 1f };

            switch (kind)
            {
                case TowerKind.Ambush: // Makineli Tufek
                    s.Range = 4f;
                    s.Dps = level == 1 ? 4f : 5.2f;       // sv2: ates hizi +%30
                    s.Targets = level == 3 ? 2 : 1;       // sv3: cift namlu
                    break;

                case TowerKind.Battery: // Topcu
                    s.Range = 4f;
                    s.ShotInterval = 1.2f;
                    s.ShotDamage = (level == 1 ? CannonDps : CannonDps * 1.3f) * s.ShotInterval;
                    s.BlastRadius = level == 1 ? CannonBlastRadius : CannonBlastRadius + 0.5f;
                    if (level == 3) { s.BurnDps = 1.5f; s.BurnSeconds = 3f; }
                    break;

                case TowerKind.Beam: // Keskin Nisanci
                    s.Range = SniperRange + (level - 1) * 1.5f;
                    s.ShotDamage = level == 1 ? 3f : 3.75f;
                    s.ShotInterval = level == 1 ? 2f : 1.6f;
                    if (level == 3) { s.PierceBonus = 1f; s.FollowUpOnKill = true; }
                    break;

                case TowerKind.Minefield: // Zaman Bozulmasi
                    s.AuraRadius = level == 1 ? 4f : 4.75f;
                    // sv2 ve sv3 OLCULMEDI; sv1'den guclu olmalari karar geregi.
                    float speed = level == 1 ? SlowSpeedLevel1 : (level == 2 ? 0.55f : 0.5f);
                    s.SlowFactor = 1f / speed;
                    s.Fragility = level == 3 ? 1.12f : 1f;
                    break;

                case TowerKind.Decoy: // Radar
                    s.AuraRadius = level == 1 ? 3f : 3.75f;
                    s.RangeBonus = 0.2f + 0.1f * (level - 1);
                    s.FireRateBonus = level == 3 ? 0.1f : 0f;
                    break;
            }

            return s;
        }

        /// <summary>
        /// Topcu'nun surekli-hasar esdegeri ve patlama yaricapi. Simulator
        /// bu ikisini kullanir (Targeting.Blast); calisma zamani ayni hasari
        /// ShotInterval araliklarla mermi olarak verir.
        ///
        /// OLCUM (11 Kasim 2026, usta oyuncu, 20 seed). Iki kurulum birlikte
        /// tutulmali: 21 Agustos run'i (yayilmis suru) ve askerli oynanan
        /// kurulum (toplanmis suru). Patlama toplanmis suruye cok daha sert
        /// vurdugu icin tek kurulumla secilen deger digerini bozuyor:
        ///
        ///   model                  21 Agu sizinti   askerli sizinti
        ///   eski (menzile 2,5/sn)       5,6              13,3
        ///   yaricap 1,5 · 4,0/sn        5,2               3,2   (askerli cok kolay)
        ///   yaricap 2,0 · 3,0/sn        7,8               5,9
        ///   yaricap 2,5 · 3,0/sn        4,7               4,9
        ///   yaricap 2,5 · 2,7/sn        6,6               7,4
        ///   yaricap 2,5 · 2,5/sn        8,8              10,1   (SECILDI)
        ///
        /// Secilen deger iki kurulumda da test bantlarinin icinde ve eski
        /// dengeye en dengeli uzaklikta (+3,2 / -3,2).
        /// </summary>
        public const float CannonDps = 2.5f;
        public const float CannonBlastRadius = 2.5f;

        /// <summary>Keskin Nisanci sv1 menzili (hucre).</summary>
        public const float SniperRange = 9f;

        /// <summary>
        /// Bir radarin bir kuleye verdigi menzil eki: Keskin Nisanci yarim
        /// alir, destek kuleleri (silahsiz) hic almaz.
        /// </summary>
        public static float RangeBonusFor(TowerKind receiver, float radarBonus)
        {
            if (receiver == TowerKind.Decoy || receiver == TowerKind.Minefield) return 0f;
            if (receiver == TowerKind.Beam) return radarBonus * RadarShareForSniper;
            return radarBonus;
        }

        /// <summary>Radarin ates hizi eki yalnizca silahli kulelere gecer.</summary>
        public static float FireRateBonusFor(TowerKind receiver, float radarBonus)
        {
            if (receiver == TowerKind.Decoy || receiver == TowerKind.Minefield) return 0f;
            return radarBonus;
        }

        public static bool IsWeapon(TowerKind kind)
        {
            return kind == TowerKind.Ambush || kind == TowerKind.Battery || kind == TowerKind.Beam;
        }
    }
}
