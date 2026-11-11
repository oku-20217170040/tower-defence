using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Kule satin alan oyuncu modeli. RunSimulator yalnizca savasi bilir;
    /// ekonomi, draft ve "hangi kuleyi nereye" karari buradadir.
    ///
    /// Neden gerekli: sabit kule listesiyle yapilan olcum gercek run'la
    /// ortusmuyordu (sim 1,5 sizinti / gercek 16). Fark, oyuncunun kuleleri
    /// ZAMANLA kurmasindan geliyor - ilk dalgalarda iki uc kulesi var ve
    /// sizintilarin neredeyse tamami orada olusuyor.
    ///
    /// Kule karisimi gercek bir run'dan alindi (20 Agu 2026, 15 dalga
    /// kazanilmis run): 13 sessiz, 7 batarya, 4 mayin tarlasi. Yani yaklasik
    /// yarisi sessiz, dortte biri batarya, geri kalani mayin tarlasi.
    /// </summary>
    public sealed class EconomyPlanner : RunSimulator.IRunPlanner
    {
        public struct Costs
        {
            public int StartingBalance;
            public int FoodPerKill;
            public int FoodPerWave;
            public int BatteryCost;
            public int AmbushCost;
            public int DecoyCost;
            public int MinefieldCost;
            public int BeamCost;
            public int CostStep;

            /// <summary>EconomyRunner varsayilanlari ile ayni.</summary>
            public static Costs Default()
            {
                // 3 Eylul 2026 (gece 4. tur): EconomyRunner ile ayni x5 yuvarlama
                // olcegiyle guncellendi (tam sayi olcek, denge ORANI degismedi).
                // Bkz. EconomyRunner.cs ust notu.
                return new Costs
                {
                    StartingBalance = 450,
                    FoodPerKill = 5,
                    FoodPerWave = 30,
                    // 11 Kasim 2026 fiyat taramasi (20 seed; usta / askerli usta
                    // / ortalama oyuncu sizintisi, MG/Topcu/Zaman):
                    //   200/100/130 (eski)   8,8 / 10,1 / 23,2
                    //   100/200/130 (takas) 12,7 / 16,7 / 12,1  usta bandin disi
                    //   125/150/175 (degerle orantili) 9,2 / 12,5 / 10,5
                    //        -> usta ile ortalama arasindaki fark kapaniyor
                    //   175/100/150 (SECILDI) 7,8 / 10,3 / 17,4
                    // Secilen set usta oyunu ayni tutuyor, ortalama oyuncuyu
                    // rahatlatiyor (us 7 -> 13) ve alim sirasinin onemini
                    // koruyor (fark 9,6 us cani; esik 8).
                    // Keskin Nisanci (200) ve Radar (125) bu iki politikada
                    // alinmadigi icin taramayla degil olculen KATKILARINA gore
                    // kondu: nisanci en uzun menzil + en guclu sv3, radar erken
                    // oyunda en zayif katki.
                    BatteryCost = 100,   // Topcu
                    AmbushCost = 175,    // Makineli Tufek
                    DecoyCost = 125,     // Radar
                    MinefieldCost = 150, // Zaman Bozulmasi
                    BeamCost = 200,      // Keskin Nisanci
                    CostStep = 25
                };
            }
        }

        /// <summary>Kule turlerinin hangi sirayla alinacagi (dongusel).</summary>
        public struct Policy
        {
            public TowerKind[] BuyOrder;

            public float AmbushDps;
            public float BatteryDps;
            public float TowerRadius;
            public float MinefieldSlowFactor;
            /// <summary>Isinin bir atista verdigi hasar.</summary>
            public float BeamShotDamage;

            /// <summary>Isinin atislari arasindaki sure (saniye). Asil denge dugmesi.</summary>
            public float BeamShotInterval;

            /// <summary>Isin hattinin yarim kalinligi (hucre). Hat incedir.</summary>
            public float BeamWidth;

            /// <summary>
            /// Isinin menzili (hucre). 40 = haritayi bastan basa gecer.
            ///
            /// Neden sinirsiz: gorsel olarak isin sahneyi delip ekran
            /// disinda bitiyor. Menzili 8'de birakmak GORSELI YALANCI
            /// yapardi - oyuncu haritanin obur ucunu vurdugunu sanirdi.
            /// Olculdu ve bedeli kabul edilebilir: karisimda sizinti
            /// 15,7 -> 11,9, ama tek basina dikildiginde HALA ise
            /// yaramiyor (30,0 · kazanma %0). Yani isin uzman kalmaya
            /// devam ediyor.
            /// </summary>
            public float BeamRange;

            /// <summary>Isinin tehdit gucu. 0 ise ordu isini algilayamaz.</summary>
            public float BeamThreatStrength;
            public float BatteryThreatStrength;

            /// <summary>
            /// Batarya kule alan hasari verir (topcu karari, 20 Agu 2026).
            /// TowerPlacer varsayilanlari ile ayni: Splash, 2,5 dps.
            /// </summary>
            public RunSimulator.Targeting BatteryTargeting;

            /// <summary>Topcu patlama yaricapi (hucre); Targeting.Blast icin.</summary>
            public float BatteryBlastRadius;

            /// <summary>Radar (TowerKind.Decoy): etki yaricapi ve menzil eki.</summary>
            public float RadarRadius;
            public float RadarRangeBonus;

            /// <summary>Draft her dalga bedava kule veriyor mu?</summary>
            public bool DraftGivesFreeTower;

            /// <summary>
            /// Kac dalgada bir bedava kule verilir. 1 = her dalga.
            /// Olcum bunun ekonominin ASIL dugmesi oldugunu gosterdi: gelir
            /// ve fiyat artisi degistirilince sonuc neredeyse hic degismiyor,
            /// bedava kule kesilince ise her kombinasyon cokuyor.
            /// </summary>
            public int FreeTowerEveryNWaves;

            /// <summary>Draft bedava kule vermedigi dalgalarda kazanilan besin.</summary>
            public int DraftFoodReward;

            /// <summary>
            /// Gercek run'in karisimi: sessiz agirlikli, arada batarya ve
            /// mayin tarlasi. Sira dongusel uygulanir.
            /// </summary>
            public static Policy Default()
            {
                return new Policy
                {
                    BuyOrder = new[]
                    {
                        TowerKind.Ambush, TowerKind.Ambush, TowerKind.Battery,
                        TowerKind.Ambush, TowerKind.Minefield, TowerKind.Ambush,
                        TowerKind.Battery
                    },
                    // 11 Kasim 2026: sayilar TowerStats'in SEVIYE 1 degerleriyle
                    // hizali (Makineli Tufek = Ambush, Topcu = Battery, ...).
                    AmbushDps = 4f,
                    BatteryDps = TowerStats.CannonDps,
                    TowerRadius = 4f,

                    // DIKKAT - bilincli sapma. Zaman Bozulmasi sv1 hizi 0,6'ya
                    // indirir (carpan 1,67); buradaki 2 ise SV3'un degeridir.
                    // Simulator yukseltmeyi modellemiyor; ortalama oyuncunun
                    // alani yukselttigi varsayiliyor. Sv1 degeriyle olcum
                    // (20 seed, ortalama oyuncu): hiz 0,75 -> kazanma %25,
                    // 0,65 -> %65, 0,6 -> %85, 0,5 -> %100. Yani sv1 alani
                    // tek basina ortalama oyuncuyu tasimiyor.
                    MinefieldSlowFactor = 2f,
                    BeamShotDamage = 3f,
                    BeamShotInterval = 2f,
                    BeamWidth = 0.6f,
                    BeamRange = TowerStats.SniperRange,
                    BatteryThreatStrength = 4f,
                    BatteryTargeting = RunSimulator.Targeting.Blast,
                    BatteryBlastRadius = TowerStats.CannonBlastRadius,
                    RadarRadius = 3f,
                    RadarRangeBonus = 0.2f,
                    DraftGivesFreeTower = true,
                    FreeTowerEveryNWaves = 1,
                    DraftFoodReward = 0
                };
            }

            /// <summary>
            /// USTA oyuncu politikasi (21 Agustos 2026 run'i ile kalibre).
            ///
            /// Default() bir ORTALAMA oyuncuyu modeller: pahali pusudan
            /// baslar, ilk dalgalara iki kuleyle girer. 21 Agustos run'inda
            /// kullanici oyle oynamadi - ucuz batarya ile pahali pusuyu
            /// DONUSUMLU aliyor, mayin tarlasini geciktiriyor. Ayni haritada
            /// ayni ayarlarla olculdu (40 seed):
            ///
            ///   Default()          -> sizinti 22,1 · us 7,9/30
            ///   Skilled()          -> sizinti  5,7 · us 24,3/30
            ///   sadece batarya     -> sizinti 13,9
            ///   sadece pusu        -> sizinti 30   (kazanma %0)
            ///
            /// Yani simulator ile gercek run arasindaki 22 / 4 sizinti farki
            /// simulatorun HATASI degil, modellenen oyuncunun zayifligiydi.
            /// Ikisi birlikte tutulur: Default() alt band, Skilled() ust band.
            /// </summary>
            public static Policy Skilled()
            {
                Policy policy = Default();
                policy.BuyOrder = new[]
                {
                    TowerKind.Battery, TowerKind.Ambush, TowerKind.Battery,
                    TowerKind.Ambush, TowerKind.Minefield
                };
                return policy;
            }
        }

        private readonly Costs costs;
        private readonly Policy policy;
        private readonly RunEconomy economy;
        private readonly Func<int, int, int[]> placement;
        private readonly Dictionary<TowerKind, int> counts = new Dictionary<TowerKind, int>();

        private int buyIndex;
        private int freeTowers;

        /// <summary>Run sonunda kac kule kuruldu.</summary>
        public int TowersBuilt { get; private set; }

        /// <summary>Run sonunda elde kalan besin. Yuksekse gec oyun ekonomisi gevsek demektir.</summary>
        public int LeftoverFood { get { return economy.Balance; } }

        /// <summary>Dalga dalga kule sayisi (1 tabanli; 0. eleman kullanilmaz).</summary>
        public int[] TowersAtWave { get; private set; }

        /// <param name="placement">
        /// (kuleIndeksi, tur) -> {x, y}. Yerlestirme haritaya bagli oldugu icin
        /// disaridan verilir; simulatorun harita bilgisi yoktur.
        /// </param>
        public EconomyPlanner(Costs costs, Policy policy, Func<int, int, int[]> placement, int totalWaves)
        {
            this.costs = costs;
            this.policy = policy;
            this.placement = placement ?? throw new ArgumentNullException(nameof(placement));

            economy = new RunEconomy(costs.StartingBalance);
            TowersAtWave = new int[totalWaves + 1];
        }

        public void OnWaveStart(int wave, List<RunSimulator.TowerSpec> towers)
        {
            // Bedava kule once kullanilir.
            while (freeTowers > 0)
            {
                freeTowers--;
                Build(towers);
            }

            while (true)
            {
                TowerKind kind = NextKind();
                int cost = CostFor(kind);
                if (!economy.TrySpend(cost)) break;

                Build(towers);
            }

            if (wave < TowersAtWave.Length) TowersAtWave[wave] = towers.Count;
        }

        public void OnUnitKilled()
        {
            economy.Earn(costs.FoodPerKill);
        }

        public void OnWaveEnd(int wave)
        {
            economy.Earn(costs.FoodPerWave);

            // Draft odulu: ya bedava kule ya besin.
            int every = policy.FreeTowerEveryNWaves > 0 ? policy.FreeTowerEveryNWaves : 1;

            if (policy.DraftGivesFreeTower && wave % every == 0) freeTowers++;
            else if (policy.DraftFoodReward > 0) economy.Earn(policy.DraftFoodReward);
        }

        private TowerKind NextKind()
        {
            return policy.BuyOrder[buyIndex % policy.BuyOrder.Length];
        }

        private void Build(List<RunSimulator.TowerSpec> towers)
        {
            TowerKind kind = NextKind();
            buyIndex++;

            int count;
            counts.TryGetValue(kind, out count);
            counts[kind] = count + 1;

            int[] xy = placement(towers.Count, (int)kind);
            towers.Add(SpecFor(kind, xy[0], xy[1]));
            TowersBuilt++;
        }

        private RunSimulator.TowerSpec SpecFor(TowerKind kind, int x, int y)
        {
            var spec = new RunSimulator.TowerSpec
            {
                X = x,
                Y = y,
                Kind = kind,
                Radius = policy.TowerRadius,
                Targeting = RunSimulator.Targeting.Single,
                SlowFactor = 1f
            };

            if (kind == TowerKind.Ambush) spec.Dps = policy.AmbushDps;
            else if (kind == TowerKind.Battery)
            {
                spec.Dps = policy.BatteryDps;
                spec.ThreatStrength = policy.BatteryThreatStrength;
                spec.Targeting = policy.BatteryTargeting;
                spec.BlastRadius = policy.BatteryBlastRadius;
            }
            else if (kind == TowerKind.Decoy)
            {
                // Radar: ates etmez, yem basmaz; cevresindeki silahli
                // kulelerin menzilini artirir.
                spec.Radius = policy.RadarRadius > 0f ? policy.RadarRadius : policy.TowerRadius;
                spec.RangeBonus = policy.RadarRangeBonus;
            }
            else if (kind == TowerKind.Minefield) spec.SlowFactor = policy.MinefieldSlowFactor;
            else if (kind == TowerKind.Beam)
            {
                spec.Dps = 1f;   // sifir olursa Fire() kuleyi atlar
                spec.ShotDamage = policy.BeamShotDamage;
                spec.ShotInterval = policy.BeamShotInterval;
                spec.Width = policy.BeamWidth;
                spec.Radius = policy.BeamRange > 0f ? policy.BeamRange : policy.TowerRadius;
                spec.ThreatStrength = policy.BeamThreatStrength;
                spec.Targeting = RunSimulator.Targeting.Beam;
            }

            return spec;
        }

        private int CostFor(TowerKind kind)
        {
            int count;
            counts.TryGetValue(kind, out count);

            int baseCost;
            if (kind == TowerKind.Battery) baseCost = costs.BatteryCost;
            else if (kind == TowerKind.Decoy) baseCost = costs.DecoyCost;
            else if (kind == TowerKind.Minefield) baseCost = costs.MinefieldCost;
            else if (kind == TowerKind.Beam) baseCost = costs.BeamCost;
            else baseCost = costs.AmbushCost;

            return baseCost + costs.CostStep * count;
        }
    }
}
