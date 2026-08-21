using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Plan 8 Gorev 1: dalga bilesimi altyapisi.
    ///
    /// Bu dosya bir DENGE testi degil, OLCUM ARACI testidir. Turlerin
    /// sayilari (can, hiz, pay) Gorev 2-6'da tek tek taranacak; buradaki
    /// tek iddia, simulatorun turleri gercekten calistirabildigi ve
    /// varsayilan davranisin degismedigidir.
    ///
    /// Varsayilanin PIYADE kalmasi neden sart: RunCalibrationTests
    /// (20 Agustos run'i) ve CurrentRunCalibrationTests (21 Agustos run'i)
    /// gercek run'lara bagli. Varsayilan bilesim degisirse ikisi de sessizce
    /// yanlis seyi olcer ve simulatorun dogrulugu hakkinda hicbir sey
    /// soylemez.
    /// </summary>
    public class UnitCompositionTests
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

        private struct Averages
        {
            public float Killed;
            public float Leaked;
        }

        private Averages Measure(UnitMix[] composition, int seedCount)
        {
            var settings = RunSimulator.Settings.Default();
            settings.Composition = composition;

            float killed = 0f, leaked = 0f;

            for (int s = 0; s < seedCount; s++)
            {
                var planner = new EconomyPlanner(
                    EconomyPlanner.Costs.Default(),
                    EconomyPlanner.Policy.Skilled(),
                    Place,
                    settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                var result = sim.Run(built, settings, seed: 1000 + s * 37, planner: planner);

                killed += result.TotalKilled;
                leaked += result.TotalLeaked;
            }

            return new Averages { Killed = killed / seedCount, Leaked = leaked / seedCount };
        }

        private static UnitMix[] Mix(UnitType type, float share, int firstWave)
        {
            return new[]
            {
                new UnitMix { Profile = UnitProfile.For(type), Share = share, FirstWave = firstWave }
            };
        }

        [Test]
        public void NoComposition_BehavesAsAllInfantry()
        {
            var implicitInfantry = Measure(null, seedCount: 20);
            var explicitInfantry = Measure(
                new[] { new UnitMix { Profile = UnitProfile.Infantry(), Share = 1f, FirstWave = 1 } },
                seedCount: 20);

            Assert.That(explicitInfantry.Leaked, Is.EqualTo(implicitInfantry.Leaked).Within(3f),
                "Bilesim verilmeyen run artik saf piyade run'i gibi davranmiyor; " +
                "iki kalibrasyon dosyasi da bu varsayima bagli. bilesimsiz=" +
                implicitInfantry.Leaked + " piyade=" + explicitInfantry.Leaked);
        }

        [Test]
        public void Soldier_ReachesTheSimulation_AndMakesTheRunHarder()
        {
            // Asker izi cok guclu basar: gectigi koridoru orduya civiler.
            // Olculen (%30 pay, 3. dalgadan): sizinti 5,3 -> 15,7.
            var infantry = Measure(null, seedCount: 20);
            var withSoldiers = Measure(Mix(UnitType.Soldier, 0.3f, 3), seedCount: 20);

            Assert.Greater(withSoldiers.Leaked, infantry.Leaked + 3f,
                "Asker artik run'i zorlastirmiyor - profil simulatore " +
                "ulasmiyor olabilir. askerli=" + withSoldiers.Leaked +
                " piyade=" + infantry.Leaked);
        }

        [Test]
        public void Brood_Splits_ProducingMoreUnitsToKill()
        {
            // Kulucka olunce ikiye ayrilir; oldurulen sayisi bu yuzden artar.
            // Olculen: 219,7 -> 322,7.
            var infantry = Measure(null, seedCount: 20);
            var withBrood = Measure(Mix(UnitType.Brood, 0.3f, 3), seedCount: 20);

            Assert.Greater(withBrood.Killed, infantry.Killed + 40f,
                "Kulucka artik bolunmuyor. kuluckali=" + withBrood.Killed +
                " piyade=" + infantry.Killed);
        }

        [Test]
        public void BroodSpawnlings_DoNotSplitAgain()
        {
            // Yavru tekrar bolunseydi tek bir kulucka sinirsiz ordu uretirdi.
            // Ust sinir: her kulucka en fazla kendisi + 2 yavru.
            var infantry = Measure(null, seedCount: 20);
            var withBrood = Measure(Mix(UnitType.Brood, 1f, 1), seedCount: 20);

            Assert.Less(withBrood.Killed, infantry.Killed * 3.5f,
                "Kulucka yavrulari da boluniyor olabilir - oldurulen sayisi " +
                "ust siniri asti. kuluckali=" + withBrood.Killed +
                " piyade=" + infantry.Killed);
        }

        [Test]
        public void Queen_ClearsThreat_AndThatIsFeltImmediately()
        {
            // Kralice usya varinca tehdit alanini siler: oyuncunun batarya
            // ile kurdugu yonlendirme cokuyor. %30 pay ile run kaybediliyor
            // (olculen: sizinti 30, kazanma %0) - yani kralice NADIR olmali.
            // Oran ve kapsam Gorev 6'da olculecek; bu test yalnizca etkinin
            // simulatore ulastigini kanitlar.
            var infantry = Measure(null, seedCount: 20);
            var withQueens = Measure(Mix(UnitType.Queen, 0.3f, 3), seedCount: 20);

            Assert.Greater(withQueens.Leaked, infantry.Leaked + 10f,
                "Kralicenin tehdit silmesi artik hissedilmiyor. kraliceli=" +
                withQueens.Leaked + " piyade=" + infantry.Leaked);
        }
    }
}
