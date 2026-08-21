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
            public int CostStep;

            /// <summary>EconomyRunner varsayilanlari ile ayni.</summary>
            public static Costs Default()
            {
                return new Costs
                {
                    StartingBalance = 90,
                    FoodPerKill = 1,
                    FoodPerWave = 6,
                    BatteryCost = 20,
                    AmbushCost = 40,
                    DecoyCost = 26,
                    MinefieldCost = 26,
                    CostStep = 5
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
            public float BatteryThreatStrength;

            /// <summary>
            /// Batarya kule alan hasari verir (topcu karari, 20 Agu 2026).
            /// TowerPlacer varsayilanlari ile ayni: Splash, 2,5 dps.
            /// </summary>
            public RunSimulator.Targeting BatteryTargeting;

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
                    AmbushDps = 4f,
                    BatteryDps = 2.5f,
                    TowerRadius = 4f,
                    MinefieldSlowFactor = 2f,
                    BatteryThreatStrength = 4f,
                    BatteryTargeting = RunSimulator.Targeting.Splash,
                    DraftGivesFreeTower = true,
                    FreeTowerEveryNWaves = 1,
                    DraftFoodReward = 0
                };
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
            }
            else if (kind == TowerKind.Minefield) spec.SlowFactor = policy.MinefieldSlowFactor;

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
            else baseCost = costs.AmbushCost;

            return baseCost + costs.CostStep * count;
        }
    }
}
