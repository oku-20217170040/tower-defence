using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Eszamanli simulatorun sozlesmesi.
    ///
    /// Bu testler bir olcum araci dogruluyor, oyunu degil. Arac yanlissa
    /// onunla secilen her denge sayisi yanlis olur - nitekim sirali modelde
    /// oyle oldu (bkz. SequentialModel_OverstatedSingleTargetTowers).
    /// </summary>
    public class RunSimulatorTests
    {
        private BattleGrid grid;
        private int baseCell;
        private List<int> spawnCells;
        private List<int> corridorColumns;
        private int midRow;
        private RunSimulator sim;

        [SetUp]
        public void SetUp()
        {
            var template = new MapTemplate { Width = 11, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            grid = new MapGenerator().Generate(template, seed: 3);
            baseCell = grid.Index(grid.Width / 2, 0);
            midRow = grid.Height / 2;

            spawnCells = new List<int>();
            int topRow = grid.Height - 1;
            for (int x = 0; x < grid.Width; x++)
            {
                int cell = grid.Index(x, topRow);
                if (grid.IsWalkable(cell)) spawnCells.Add(cell);
            }

            corridorColumns = new List<int>();
            bool inRun = false;
            for (int x = 0; x < grid.Width; x++)
            {
                bool walkable = grid.IsWalkable(grid.Index(x, midRow));
                if (walkable && !inRun) { corridorColumns.Add(x); inRun = true; }
                else if (!walkable) inRun = false;
            }

            sim = new RunSimulator(grid, spawnCells, baseCell);
        }

        private List<RunSimulator.TowerSpec> Towers(float dps, RunSimulator.Targeting targeting)
        {
            var towers = new List<RunSimulator.TowerSpec>();
            for (int c = 0; c < corridorColumns.Count; c++)
                towers.Add(new RunSimulator.TowerSpec
                {
                    X = corridorColumns[c],
                    Y = midRow,
                    Kind = TowerKind.Ambush,
                    Dps = dps,
                    Radius = 4f,
                    Targeting = targeting,
                    SlowFactor = 1f
                });

            return towers;
        }

        private static RunSimulator.Settings OneWave(int bugCount)
        {
            var s = RunSimulator.Settings.Default();
            s.TotalWaves = 1;
            s.BaseUnitsPerWave = bugCount;
            s.UnitsAddedPerWave = 0f;
            s.HomeBaseHp = 999;
            return s;
        }

        [Test]
        public void NoTowers_NothingDies()
        {
            var result = sim.Run(new List<RunSimulator.TowerSpec>(), OneWave(8), seed: 1000);

            Assert.AreEqual(0, result.TotalKilled);
            Assert.AreEqual(8, result.TotalLeaked);
        }

        [Test]
        public void OverwhelmingDamage_KillsEveryUnit()
        {
            var result = sim.Run(Towers(1000f, RunSimulator.Targeting.Single), OneWave(8), seed: 1000);

            Assert.AreEqual(8, result.TotalKilled);
            Assert.AreEqual(0, result.TotalLeaked);
        }

        [Test]
        public void SingleUnit_IsKilledByOrdinaryTowers()
        {
            // Tek birlik, 4 dps'lik uc kule: yol boyunca yeterince hasar alir.
            var result = sim.Run(Towers(4f, RunSimulator.Targeting.Single), OneWave(1), seed: 1000);

            Assert.AreEqual(1, result.TotalKilled);
        }

        [Test]
        public void SwarmOverwhelmsSingleTargetTowers()
        {
            // ISIN NOKTASI: ayni kuleler tek birligi olduruyor ama sekiz
            // birligin cogunu kaciriyor - cunku bir kule bir anda yalnizca bir
            // birlige ates eder. Sirali modelde bu HIC gorunmuyordu.
            var result = sim.Run(Towers(4f, RunSimulator.Targeting.Single), OneWave(8), seed: 1000);

            Assert.Less(result.TotalKilled, 8,
                "Sürü artik kuleleri asamiyor; eszamanlilik modeli bozulmus olabilir.");
            Assert.Greater(result.TotalLeaked, 0);
        }

        [Test]
        public void SplashBeatsSingleTargetAgainstASwarm()
        {
            // Topcu (alan hasari) kararinin olcumu: AYNI dps, farkli hedefleme.
            var single = sim.Run(Towers(4f, RunSimulator.Targeting.Single), OneWave(8), seed: 1000);
            var splash = sim.Run(Towers(4f, RunSimulator.Targeting.Splash), OneWave(8), seed: 1000);

            Assert.Greater(splash.TotalKilled, single.TotalKilled,
                "Alan hasari sürüye karsi ustunlugunu kaybetti: alan=" +
                splash.TotalKilled + " tek=" + single.TotalKilled);
        }

        [Test]
        public void SameSeed_GivesSameResult()
        {
            var first = sim.Run(Towers(4f, RunSimulator.Targeting.Single), OneWave(8), seed: 4242);
            var second = sim.Run(Towers(4f, RunSimulator.Targeting.Single), OneWave(8), seed: 4242);

            Assert.AreEqual(first.TotalKilled, second.TotalKilled);
            Assert.AreEqual(first.TotalLeaked, second.TotalLeaked);
        }

        [Test]
        public void Minefield_SlowsUnitsAndRaisesKills()
        {
            // Mayin tarlasi hasar vermez; katkisi yalnizca birligi menzilde tutmaktir.
            var plain = Towers(4f, RunSimulator.Targeting.Single);

            var withMinefield = Towers(4f, RunSimulator.Targeting.Single);
            for (int c = 0; c < corridorColumns.Count; c++)
                withMinefield.Add(new RunSimulator.TowerSpec
                {
                    X = corridorColumns[c],
                    Y = midRow + 1,
                    Kind = TowerKind.Minefield,
                    Dps = 0f,
                    Radius = 4f,
                    Targeting = RunSimulator.Targeting.Single,
                    SlowFactor = 2f
                });

            var without = sim.Run(plain, OneWave(8), seed: 1000);
            var with = sim.Run(withMinefield, OneWave(8), seed: 1000);

            Assert.Greater(with.TotalKilled, without.TotalKilled,
                "Mayin tarlasi artik oldurme sayisini artirmiyor: mayin tarlasili=" +
                with.TotalKilled + " mayin tarlasisiz=" + without.TotalKilled);
        }
    }
}
