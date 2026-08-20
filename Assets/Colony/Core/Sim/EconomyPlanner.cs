using System;
using System.Collections.Generic;

namespace Colony.Core
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
    /// kazanilmis run): 13 sessiz, 7 gurultucu, 4 recine. Yani yaklasik
    /// yarisi sessiz, dortte biri gurultucu, geri kalani recine.
    /// </summary>
    public sealed class EconomyPlanner : RunSimulator.IRunPlanner
    {
        public struct Costs
        {
            public int StartingBalance;
            public int FoodPerKill;
            public int FoodPerWave;
            public int NoisyCost;
            public int SilentCost;
            public int LureCost;
            public int ResinCost;
            public int CostStep;

            /// <summary>EconomyRunner varsayilanlari ile ayni.</summary>
            public static Costs Default()
            {
                return new Costs
                {
                    StartingBalance = 50,
                    FoodPerKill = 2,
                    FoodPerWave = 10,
                    NoisyCost = 12,
                    SilentCost = 24,
                    LureCost = 16,
                    ResinCost = 16,
                    CostStep = 5
                };
            }
        }

        /// <summary>Kule turlerinin hangi sirayla alinacagi (dongusel).</summary>
        public struct Policy
        {
            public TowerKind[] BuyOrder;

            public float SilentDps;
            public float NoisyDps;
            public float TowerRadius;
            public float ResinSlowFactor;
            public float NoisyThreatStrength;

            /// <summary>
            /// Gurultucu kule alan hasari verir (topcu karari, 20 Agu 2026).
            /// TowerPlacer varsayilanlari ile ayni: Splash, 2,5 dps.
            /// </summary>
            public RunSimulator.Targeting NoisyTargeting;

            /// <summary>Draft her dalga bedava kule veriyor mu?</summary>
            public bool DraftGivesFreeTower;

            /// <summary>Draft bedava kule vermedigi dalgalarda kazanilan besin.</summary>
            public int DraftFoodReward;

            /// <summary>
            /// Gercek run'in karisimi: sessiz agirlikli, arada gurultucu ve
            /// recine. Sira dongusel uygulanir.
            /// </summary>
            public static Policy Default()
            {
                return new Policy
                {
                    BuyOrder = new[]
                    {
                        TowerKind.Silent, TowerKind.Silent, TowerKind.Noisy,
                        TowerKind.Silent, TowerKind.Resin, TowerKind.Silent,
                        TowerKind.Noisy
                    },
                    SilentDps = 4f,
                    NoisyDps = 2.5f,
                    TowerRadius = 4f,
                    ResinSlowFactor = 2f,
                    NoisyThreatStrength = 4f,
                    NoisyTargeting = RunSimulator.Targeting.Splash,
                    DraftGivesFreeTower = true,
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

        public void OnBugKilled()
        {
            economy.Earn(costs.FoodPerKill);
        }

        public void OnWaveEnd(int wave)
        {
            economy.Earn(costs.FoodPerWave);

            // Draft odulu: ya bedava kule ya besin.
            if (policy.DraftGivesFreeTower) freeTowers++;
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

            if (kind == TowerKind.Silent) spec.Dps = policy.SilentDps;
            else if (kind == TowerKind.Noisy)
            {
                spec.Dps = policy.NoisyDps;
                spec.ThreatStrength = policy.NoisyThreatStrength;
                spec.Targeting = policy.NoisyTargeting;
            }
            else if (kind == TowerKind.Resin) spec.SlowFactor = policy.ResinSlowFactor;

            return spec;
        }

        private int CostFor(TowerKind kind)
        {
            int count;
            counts.TryGetValue(kind, out count);

            int baseCost;
            if (kind == TowerKind.Noisy) baseCost = costs.NoisyCost;
            else if (kind == TowerKind.Lure) baseCost = costs.LureCost;
            else if (kind == TowerKind.Resin) baseCost = costs.ResinCost;
            else baseCost = costs.SilentCost;

            return baseCost + costs.CostStep * count;
        }
    }
}
