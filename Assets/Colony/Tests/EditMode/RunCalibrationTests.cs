using System.Collections.Generic;
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    /// <summary>
    /// Simulatoru GERCEK bir run'a karsi kalibre tutar.
    ///
    /// Referans run (kullanici, 20 Agustos 2026, ekran goruntusuyle kayitli):
    ///   15/15 dalga, KAZANDI, yuva 14/30
    ///   209 oldurme, 16 sizinti
    ///   24 kule (13 sessiz + 7 gurultucu + 4 recine)
    ///   uzerine 116 besin artmis
    ///
    /// Kule sayilari ekrandaki fiyatlardan geri hesaplandi: gurultucu 47 =
    /// 12 + 5x7, recine 27 = (16 + 5x4) x 0,75 (draft indirimi), sessiz
    /// 66 = (24 + 5x13) x 0,75.
    ///
    /// NOT: referans run BUGUNKU ayarlarla oynanmadi. O sirada gurultucu
    /// kule tek hedefli 10 dps idi, bocek cani her dalga %15 buyuyordu ve
    /// kuleler ucuzdu (sessiz 24). Kalibrasyon bu kosullari acikca kurar -
    /// yoksa oyun her dengelendiginde referans kirilir ve olcum aracinin
    /// dogrulugu hakkinda hicbir sey soylemez.
    ///
    /// Bu testler oyunun dengesini degil, OLCUM ARACININ gercekle
    /// ortusmesini korur. Sapma buyurse ya simulator ya da oyun degismistir;
    /// hangisi oldugu arastirilmadan denge sayilari degistirilmemeli.
    /// </summary>
    public class RunCalibrationTests
    {
        private ColonyGrid grid;
        private int nestCell;
        private List<int> spawnCells;
        private List<int> corridorColumns;
        private RunSimulator sim;

        private static readonly int[] YSlots = { 10, 5, 15, 7, 13, 3, 17, 9, 11 };

        [SetUp]
        public void SetUp()
        {
            var template = new MapTemplate { Width = 11, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            grid = new MapGenerator().Generate(template, seed: 3);
            nestCell = grid.Index(grid.Width / 2, 0);

            spawnCells = new List<int>();
            int topRow = grid.Height - 1;
            for (int x = 0; x < grid.Width; x++)
            {
                int cell = grid.Index(x, topRow);
                if (grid.IsWalkable(cell)) spawnCells.Add(cell);
            }

            corridorColumns = new List<int>();
            int midRow = grid.Height / 2;
            bool inRun = false;
            for (int x = 0; x < grid.Width; x++)
            {
                bool walkable = grid.IsWalkable(grid.Index(x, midRow));
                if (walkable && !inRun) { corridorColumns.Add(x); inRun = true; }
                else if (!walkable) inRun = false;
            }

            sim = new RunSimulator(grid, spawnCells, nestCell);
        }

        /// <summary>Kuleleri koridorlara sirayla dagitir; recine bir onceki kulenin yanina.</summary>
        private int[] Place(int index, int kind)
        {
            int column = corridorColumns[index % corridorColumns.Count];
            int y = YSlots[(index / corridorColumns.Count) % YSlots.Length];

            if (kind == (int)TowerKind.Resin) y += 1;

            if (y < 2) y = 2;
            if (y > grid.Height - 3) y = grid.Height - 3;

            return new[] { column, y };
        }

        private struct Averages
        {
            public int WinPercent;
            public float Killed;
            public float Leaked;
            public float Towers;
            public float LeftoverFood;
            public float TowersAtWave8;
        }

        /// <summary>
        /// Referans run'i yeniden uretir. Gurultucu kule O RUN'DA tek hedefli
        /// 10 dps idi; kalibrasyon kendi kosullariyla olculmeli, bugunku
        /// varsayilanlarla degil. Alan hasari karari ayri testlerde olculuyor.
        /// </summary>
        private Averages Measure(int seedCount)
        {
            return MeasureWithNoisy(RunSimulator.Targeting.Single, 10f, seedCount);
        }

        [Test]
        public void Simulator_MatchesTheRecordedRun()
        {
            var avg = Measure(seedCount: 10);

            // Gercek: 209 oldurme, 16 sizinti, 24 kule, 116 artan besin.
            Assert.That(avg.Killed, Is.InRange(170f, 250f), "Oldurme sayisi gercek run'dan sapti: " + avg.Killed);
            Assert.That(avg.Leaked, Is.InRange(10f, 26f), "Sizinti gercek run'dan sapti: " + avg.Leaked);
            Assert.That(avg.Towers, Is.InRange(19f, 33f), "Kule sayisi gercek run'dan sapti: " + avg.Towers);
            Assert.That(avg.LeftoverFood, Is.InRange(70f, 150f), "Artan besin gercek run'dan sapti: " + avg.LeftoverFood);
        }

        [Test]
        public void RecordedRun_IsWon()
        {
            var avg = Measure(seedCount: 10);

            Assert.GreaterOrEqual(avg.WinPercent, 80,
                "Referans kurulum artik kazanmiyor: %" + avg.WinPercent);
        }

        [Test]
        public void MostLeaksHappenWhileTowersAreStillFew()
        {
            // Sabit kule listesiyle ayni kurulum 1,5 sizinti veriyordu; ekonomiyle
            // 18. Aradaki fark tamamen erken dalgalardan geliyor - oyunun zorlugu
            // orada. Bu test o gercegin kaybolmamasini sagliyor.
            var settings = ReferenceSettings();

            var planner = new EconomyPlanner(
                ReferenceCosts(),
                EconomyPlanner.Policy.Default(),
                Place,
                settings.TotalWaves);

            var built = new List<RunSimulator.TowerSpec>();
            var withEconomy = sim.Run(built, settings, seed: 1000, planner: planner);

            // Ayni son kule listesi, bastan kurulu.
            var fixedTowers = new List<RunSimulator.TowerSpec>(built);
            var fromStart = sim.Run(fixedTowers, settings, seed: 1000);

            Assert.Greater(withEconomy.TotalLeaked, fromStart.TotalLeaked * 2,
                "Kuleleri zamanla kurmak artik ceza getirmiyor; erken dalga baskisi " +
                "kaybolmus olabilir. zamanla=" + withEconomy.TotalLeaked +
                " bastan=" + fromStart.TotalLeaked);
        }

        [Test]
        public void LateEconomy_IsLooserThanTheSpecTarget()
        {
            // Spec hedefi: 8-10. dalgada 4-5 etkili kule. Olcum bunun cok
            // asildigini soyluyor (gercek run da oyle bitti: 24 kule + 116
            // besin). Bu test bir HATA degil, kayitli bir gercek: ekonomi
            // duzeltilirse bu test de guncellenmeli.
            var avg = Measure(seedCount: 10);

            Assert.Greater(avg.TowersAtWave8, 8f,
                "8. dalgadaki kule sayisi dusmus - ekonomi degistiyse bu test " +
                "ve spec 3.5b birlikte guncellenmeli. Olculen: " + avg.TowersAtWave8);
        }

        [Test]
        public void NoisySplash_MatchesSingleTargetAtAQuarterOfTheDps()
        {
            // Topcu karari: gurultucu kule alan hasari verir. Olcumde alan
            // hasari yaklasik 4 kat deger katiyor - 2,5 alan hasari, 10 tek
            // hedefli hasarla ayni zorluk bandini uretiyor:
            //   Single 10  -> sizinti 18,3
            //   Splash 2,5 -> sizinti 20,6
            //   Splash 4   -> sizinti 12,2  (oyunu kolaylastirir)
            //   Splash 1,5 -> sizinti 29,3  (kazanma %20)
            var splash = MeasureWithNoisy(RunSimulator.Targeting.Splash, 2.5f, seedCount: 10);
            var single = MeasureWithNoisy(RunSimulator.Targeting.Single, 10f, seedCount: 10);

            Assert.That(splash.Leaked, Is.EqualTo(single.Leaked).Within(6f),
                "Alan hasarli 2,5 dps artik tek hedefli 10 dps'e denk degil: " +
                "alan=" + splash.Leaked + " tek=" + single.Leaked);
        }

        [Test]
        public void NoisySplash_TooStrongWouldTrivializeTheRun()
        {
            // 4 dps alan hasari sizintiyi 12'ye indiriyordu; secilmemesinin
            // sebebi bu. Test, dps'in sessizce yukseltilmesini yakalar.
            var strong = MeasureWithNoisy(RunSimulator.Targeting.Splash, 4f, seedCount: 10);
            var chosen = MeasureWithNoisy(RunSimulator.Targeting.Splash, 2.5f, seedCount: 10);

            Assert.Less(strong.Leaked, chosen.Leaked,
                "Alan hasarini yukseltmek artik oyunu kolaylastirmiyor; secim " +
                "gerekcesi olcume dayanmiyor.");
        }

        /// <summary>Referans run'in oynandigi kosullar. Bugunku dengeden bagimsizdir.</summary>
        private static RunSimulator.Settings ReferenceSettings()
        {
            var settings = RunSimulator.Settings.Default();
            settings.HpGrowthPerWave = 0.15f;
            return settings;
        }

        /// <summary>
        /// Referans run'in ekonomisi. Bugunku degerlerden BAGIMSIZ olmali;
        /// Costs.Default()'tan tek bir alan sizarsa kalibrasyon sessizce
        /// yanlis seyi olcer (bir kez yasandi: gelirler guncellenince
        /// sizinti 18'den 6'ya dustu).
        /// </summary>
        private static EconomyPlanner.Costs ReferenceCosts()
        {
            return new EconomyPlanner.Costs
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

        private Averages MeasureWithNoisy(RunSimulator.Targeting targeting, float dps, int seedCount)
        {
            var settings = ReferenceSettings();
            int wins = 0;
            float killed = 0f, leaked = 0f, towers = 0f, food = 0f, w8 = 0f;

            for (int s = 0; s < seedCount; s++)
            {
                var policy = EconomyPlanner.Policy.Default();
                policy.NoisyTargeting = targeting;
                policy.NoisyDps = dps;

                var planner = new EconomyPlanner(
                    ReferenceCosts(), policy, Place, settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                var result = sim.Run(built, settings, seed: 1000 + s * 37, planner: planner);

                if (result.Survived) wins++;
                killed += result.TotalKilled;
                leaked += result.TotalLeaked;
                towers += planner.TowersBuilt;
                food += planner.LeftoverFood;
                w8 += planner.TowersAtWave[8];
            }

            return new Averages
            {
                WinPercent = 100 * wins / seedCount,
                Killed = killed / seedCount,
                Leaked = leaked / seedCount,
                Towers = towers / seedCount,
                LeftoverFood = food / seedCount,
                TowersAtWave8 = w8 / seedCount
            };
        }
    }
}
