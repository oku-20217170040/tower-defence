using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Plan 8 Gorev 6: kralice.
    ///
    /// TASARIM BELGESI YANILIYORDU. Belge "kralice tehdit alanini
    /// temizler - ordunun korkusunu siler, ogrenmeyi resetler" diyordu.
    /// Olcum bunun hicbir sey yapmadigini gosterdi:
    ///
    ///   pay %8, tehdit SILER  -> sizinti 26,4 · entropi 0,507
    ///   pay %8, tehdit silmez -> sizinti 26,4 · entropi 0,507
    ///
    /// Sebep: TEHDIT ALANI ORDUNUN HAFIZASI DEGILDIR. Her dalga basinda
    /// kulelerden yeniden kurulur; yani oyuncunun kule yerlesiminin statik
    /// bir fonksiyonudur. Ordunun gercekten OGRENDIGI sey kesif izidir.
    ///
    /// Kralice kesif izini silince kanal ortaya cikti:
    ///
    ///   pay %8, HAFIZA siler -> sizinti 25,2 · entropi 0,552
    ///
    /// Ve kanal candan BAGIMSIZ: zayif kralicede (can x2) bile entropi
    /// 0,520'den 0,535'e cikiyor. Askerdeki ayrimin aynisi - kralice
    /// "buyuk canli birlik" degil, ordunun ogrendigini unutturan birlik.
    /// </summary>
    public class QueenTests
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
            public int WinPercent;
            public float Leaked;
            public float Entropy;
        }

        /// <summary>
        /// Izci + asker sabit; kralice profili disaridan verilir ki
        /// kanalin kendisi yalitilabilsin.
        /// </summary>
        private Averages Measure(UnitProfile? queen, float share, int seedCount)
        {
            var settings = RunSimulator.Settings.Default();

            var mixes = new List<UnitMix>
            {
                new UnitMix
                {
                    Profile = UnitProfile.For(UnitType.Scout),
                    Share = UnitRoster.ScoutShare,
                    FirstWave = 1
                },
                new UnitMix
                {
                    Profile = UnitProfile.For(UnitType.Soldier),
                    Share = UnitRoster.SoldierShare,
                    FirstWave = UnitRoster.SoldierFirstWave
                }
            };

            if (queen.HasValue && share > 0f)
            {
                mixes.Add(new UnitMix
                {
                    Profile = queen.Value,
                    Share = share,
                    FirstWave = UnitRoster.QueenFirstWave
                });
            }

            settings.Composition = mixes.ToArray();

            int wins = 0;
            float leaked = 0f, entropy = 0f;

            for (int s = 0; s < seedCount; s++)
            {
                var planner = new EconomyPlanner(
                    EconomyPlanner.Costs.Default(),
                    EconomyPlanner.Policy.Skilled(),
                    Place,
                    settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                var result = sim.Run(built, settings, seed: 1000 + s * 37, planner: planner);

                if (result.Survived) wins++;
                leaked += result.TotalLeaked;
                entropy += result.RouteEntropy;
            }

            return new Averages
            {
                WinPercent = 100 * wins / seedCount,
                Leaked = leaked / seedCount,
                Entropy = entropy / seedCount
            };
        }

        [Test]
        public void Queen_MakesTheArmyForget()
        {
            // Hafizasi silinen ordu ogrendigi yolu kaybeder ve yeniden
            // kesfe cikar: entropi yukselir. Olculen (pay %8): 0,507 -> 0,552.
            UnitProfile forgetful = UnitProfile.For(UnitType.Queen);

            UnitProfile inert = forgetful;
            inert.ClearsRecon = false;

            var withMemoryWipe = Measure(forgetful, 0.08f, seedCount: 20);
            var withoutWipe = Measure(inert, 0.08f, seedCount: 20);

            Assert.Greater(withMemoryWipe.Entropy, withoutWipe.Entropy + 0.02f,
                "Kralice artik orduya unutturmuyor - o zaman yalnizca buyuk " +
                "canli bir birliktir ve Plan 8'in kuralina gore eklenmemeliydi. " +
                "silen=" + withMemoryWipe.Entropy + " silmeyen=" + withoutWipe.Entropy);
        }

        [Test]
        public void ClearingThreat_DoesNothing_BecauseThreatIsNotMemory()
        {
            // Kayitli gercek. Tehdit alani her dalga kulelerden yeniden
            // kuruldugu icin silmenin etkisi yok. Bu test, birinin
            // "kralice tehdidi silsin" diye geri donmesini engeller.
            UnitProfile clearsThreat = UnitProfile.For(UnitType.Queen);
            clearsThreat.ClearsRecon = false;
            clearsThreat.ClearsThreat = true;

            UnitProfile clearsNothing = clearsThreat;
            clearsNothing.ClearsThreat = false;

            var withClear = Measure(clearsThreat, 0.08f, seedCount: 20);
            var withoutClear = Measure(clearsNothing, 0.08f, seedCount: 20);

            Assert.That(withClear.Leaked, Is.EqualTo(withoutClear.Leaked).Within(1f),
                "Tehdit silmenin artik bir etkisi var - tehdit alani " +
                "modelde degismis olabilir, kralicenin kanali yeniden " +
                "olculmeli. silen=" + withClear.Leaked +
                " silmeyen=" + withoutClear.Leaked);
        }

        [Test]
        public void TheForgettingChannel_IsIndependentOfHp()
        {
            // Askerdeki ayrimin aynisi: kanal ile zorluk ayri dugmeler.
            // Zayif kralicede (can x2) bile hafiza silme entropiyi
            // 0,520'den 0,535'e tasiyor.
            UnitProfile weak = UnitProfile.For(UnitType.Queen);
            weak.HpMultiplier = 2f;

            UnitProfile weakInert = weak;
            weakInert.ClearsRecon = false;

            var wipes = Measure(weak, 0.08f, seedCount: 20);
            var inert = Measure(weakInert, 0.08f, seedCount: 20);

            Assert.Greater(wipes.Entropy, inert.Entropy + 0.005f,
                "Unutturma kanali artik candan bagimsiz degil; kralice " +
                "gizli bir zorluk carpanina donusmus olabilir. silen=" +
                wipes.Entropy + " silmeyen=" + inert.Entropy);
        }

        [Test]
        public void QueenShare_IsRareEnoughToStayWinnable()
        {
            // %8'te run %65 kazanmaya dusuyordu; %5 secildi (%95).
            var chosen = Measure(UnitProfile.For(UnitType.Queen),
                                 UnitRoster.QueenShare, seedCount: 20);

            Assert.GreaterOrEqual(chosen.WinPercent, 80,
                "Kralice payi run'i kaybettiriyor: %" + chosen.WinPercent +
                " (sizinti " + chosen.Leaked + ")");
        }
    }
}
