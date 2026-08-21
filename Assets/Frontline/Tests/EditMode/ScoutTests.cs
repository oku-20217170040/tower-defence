using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Plan 8 Gorev 2: izci ve onunla birlikte alinan TABAN SAPMA karari.
    ///
    /// Izcinin degeri "kac kisi oldu" degil, ORDUNUN KAC FARKLI KORIDOR
    /// DENEDIGIDIR. Sizinti sayisi izciyi hic gormuyor; olcum metrigi
    /// dalga basina koridor entropisidir (RunResult.RouteEntropy).
    ///
    /// TABAN SAPMA 0,35 -> 0,12 (21 Agustos 2026 karari). Sebep olculdu:
    ///
    ///   taban 0,35: izcisiz 0,750 -> izcili 0,789   (izci gereksiz)
    ///   taban 0,12: izcisiz 0,428 -> izcili 0,630   (izci TASIYICI)
    ///
    /// 0,35 olculmus bir sabitti ama o olcum BUTUN BIRLIKLER AYNI SAPMAYI
    /// TASIRKEN yapilmisti. Tur sistemi gelince kesif izciye yuklendi:
    /// ordu izcisiz saplanip kalir, izciyi oldurmek orduyu kor birakir.
    /// Plan 8'in pazarliksiz kuralina uyan tek kurulum budur - 0,35'te
    /// izci yalnizca bir can rakami olurdu ve eklenmemesi gerekirdi.
    ///
    /// Not: iki kalibrasyon dosyasi (20 ve 21 Agustos run'lari) taban
    /// sapmayi kendi icinde 0,35'e SABITLER; o run'lar oyle oynandi.
    /// </summary>
    public class ScoutTests
    {
        private BattleGrid grid;
        private int baseCell;
        private List<int> spawnCells;
        private List<int> corridorColumns;
        private RunSimulator sim;

        private static readonly int[] YSlots = { 10, 5, 15, 7, 13, 3, 17, 9, 11 };

        [SetUp]
        public void SetUp()
        {
            var template = new MapTemplate { Width = 11, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            grid = new MapGenerator().Generate(template, seed: 3);
            baseCell = grid.Index(grid.Width / 2, 0);

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

            sim = new RunSimulator(grid, spawnCells, baseCell);
        }

        private int[] Place(int index, int kind)
        {
            int column = corridorColumns[index % corridorColumns.Count];
            int y = YSlots[(index / corridorColumns.Count) % YSlots.Length];

            if (kind == (int)TowerKind.Minefield) y += 1;

            if (y < 2) y = 2;
            if (y > grid.Height - 3) y = grid.Height - 3;

            return new[] { column, y };
        }

        /// <summary>Dalga basina koridor entropisinin ortalamasi.</summary>
        private float MeasureEntropy(float baseNoise, float scoutShare, int seedCount)
        {
            var settings = RunSimulator.Settings.Default();
            settings.ExplorationNoise = baseNoise;

            if (scoutShare > 0f)
            {
                settings.Composition = new[]
                {
                    new UnitMix
                    {
                        Profile = UnitProfile.For(UnitType.Scout),
                        Share = scoutShare,
                        FirstWave = 1
                    }
                };
            }
            else
            {
                settings.Composition = null;
            }

            float total = 0f;

            for (int s = 0; s < seedCount; s++)
            {
                var planner = new EconomyPlanner(
                    EconomyPlanner.Costs.Default(),
                    EconomyPlanner.Policy.Skilled(),
                    Place,
                    settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                total += sim.Run(built, settings, seed: 1000 + s * 37, planner: planner).RouteEntropy;
            }

            return total / seedCount;
        }

        [Test]
        public void WithoutScouts_TheArmyGetsStuck()
        {
            // Taban sapma 0,12'de ordu kendi basina kesif yapamaz.
            // Olculen: 0,428.
            float stuck = MeasureEntropy(RunSimulator.Settings.Default().ExplorationNoise,
                                         scoutShare: 0f, seedCount: 20);

            Assert.Less(stuck, 0.55f,
                "Ordu izcisiz de genis kesif yapiyor - o zaman izci gereksiz " +
                "demektir (Plan 8 kurali). entropi=" + stuck);
        }

        [Test]
        public void Scouts_CarryTheExploration()
        {
            float withoutScouts = MeasureEntropy(
                RunSimulator.Settings.Default().ExplorationNoise, 0f, seedCount: 20);
            float withScouts = MeasureEntropy(
                RunSimulator.Settings.Default().ExplorationNoise,
                UnitRoster.ScoutShare, seedCount: 20);

            Assert.Greater(withScouts, withoutScouts + 0.1f,
                "Izci artik kesfi tasimiyor; izcisiz=" + withoutScouts +
                " izcili=" + withScouts);
        }

        [Test]
        public void AtTheOldBaseNoise_ScoutsWouldBePointless()
        {
            // Kayitli gercek: eski taban 0,35'te izcinin katkisi 0,04'tu.
            // Bu test, tabanin sessizce geri yukseltilmesini yakalar -
            // yukseltilirse izci yeniden gereksiz bir can rakamina doner.
            float withoutScouts = MeasureEntropy(0.35f, 0f, seedCount: 20);
            float withScouts = MeasureEntropy(0.35f, UnitRoster.ScoutShare, seedCount: 20);

            Assert.Less(withScouts - withoutScouts, 0.1f,
                "Eski tabanda izci artik fark yaratiyor - taban sapma karari " +
                "yeniden olculmeli. fark=" + (withScouts - withoutScouts));
        }

        [Test]
        public void ScoutShare_IsNotSoHighThatItSolvesTheRun()
        {
            // %50 izci sizintiyi 4,1'den 2,3'e dusuruyordu: izci kirilgan
            // oldugu icin cok olursa oyun kolaylasir. %25 secildi.
            Assert.LessOrEqual(UnitRoster.ScoutShare, 0.35f,
                "Izci payi olculen bandin uzerine cikti: " + UnitRoster.ScoutShare);
        }

        [Test]
        public void TheArmySpreadsOutOverWaves_ItDoesNotSettle()
        {
            // Bu bir DUZELTMEDIR. Eski tooltip "ordu dagilarak baslar ve 6-8
            // dalgada tek koridora YERLESIR" diyordu. Ekonomiyle birlikte
            // olculunce tam tersi cikti: erken 0,329 -> gec 0,619. Sebep
            // spec 3.5'tir - kuleler dikildikce ordu korunmayan koridoru
            // aramaya devam eder. Yerlesme diye bir sey olmuyor.
            var settings = RunSimulator.Settings.Default();
            settings.Composition = UnitRoster.Default();

            float early = 0f, late = 0f;
            const int Seeds = 20;

            for (int s = 0; s < Seeds; s++)
            {
                var planner = new EconomyPlanner(
                    EconomyPlanner.Costs.Default(),
                    EconomyPlanner.Policy.Skilled(),
                    Place,
                    settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                var result = sim.Run(built, settings, seed: 1000 + s * 37, planner: planner);

                early += (result.WaveEntropy[1] + result.WaveEntropy[2] + result.WaveEntropy[3]) / 3f;
                late += (result.WaveEntropy[13] + result.WaveEntropy[14] + result.WaveEntropy[15]) / 3f;
            }

            early /= Seeds;
            late /= Seeds;

            Assert.Greater(late, early,
                "Ordu artik gec dalgalarda TOPLANIYOR. Bu, spec 3.5'in " +
                "(korunmayan koridor her zaman bulunur) tersidir ve bir " +
                "davranis degisikligi demektir. erken=" + early + " gec=" + late);
        }
    }
}
