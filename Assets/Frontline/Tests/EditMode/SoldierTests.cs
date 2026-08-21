using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Plan 8 Gorev 3: asker.
    ///
    /// Askerin iddiasi "gectigi koridoru orduya civiler". Bu iddia
    /// ORTALAMA ENTROPIYLE OLCULEMEZ - pekistirme x1'den x8'e cikarken
    /// ortalama entropi yalnizca 0,568'den 0,512'ye iniyor ve bu, askerin
    /// canindan gelen etkiden ayirt edilemiyor.
    ///
    /// Dogru metrik: askerin USYA VARDIGI sutunun SONRAKI dalgadaki pay
    /// ARTISI ("civileme / lift"). Olculen:
    ///
    ///   pekistirme x1  -> lift -0,009   (civileme YOK)
    ///   pekistirme x3  -> lift +0,014
    ///   pekistirme x6  -> lift +0,041   (secilen)
    ///   pekistirme x20 -> lift +0,070   (kazanci azaliyor)
    ///
    /// Onemli olan sey: pekistirme bir ZORLUK dugmesi degil. x1'den x20'ye
    /// sizinti 11,8'den yalnizca 13,6'ya cikiyor. Zorluk askerin CANINDAN
    /// geliyor ve orada esik yine keskin (x2 -> 4,0 · x3 -> 13,2 · x4 ->
    /// 22,6). Ikisini ayirmak sart: ayirmasaydik askeri "guclu piyade"
    /// diye ekler ve Plan 8'in kuralini cignerdik.
    /// </summary>
    public class SoldierTests
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

        private static RunSimulator.Settings Composition(float reinforce, float hp)
        {
            var settings = RunSimulator.Settings.Default();

            UnitProfile soldier = UnitProfile.For(UnitType.Soldier);
            soldier.ReinforceMultiplier = reinforce;
            soldier.HpMultiplier = hp;

            settings.Composition = new[]
            {
                new UnitMix
                {
                    Profile = UnitProfile.For(UnitType.Scout),
                    Share = UnitRoster.ScoutShare,
                    FirstWave = 1
                },
                new UnitMix
                {
                    Profile = soldier,
                    Share = UnitRoster.SoldierShare,
                    FirstWave = UnitRoster.SoldierFirstWave
                }
            };

            return settings;
        }

        private struct Result
        {
            public float Leaked;

            /// <summary>Askerin vardigi sutunun sonraki dalgadaki pay artisi.</summary>
            public float Lift;
        }

        private Result Measure(float reinforce, float hp, int seedCount)
        {
            var settings = Composition(reinforce, hp);

            float leaked = 0f;
            double liftSum = 0d;
            int liftCount = 0;

            for (int s = 0; s < seedCount; s++)
            {
                var planner = new EconomyPlanner(
                    EconomyPlanner.Costs.Default(),
                    EconomyPlanner.Policy.Skilled(),
                    Place,
                    settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                var result = sim.Run(built, settings, seed: 1000 + s * 37, planner: planner);
                leaked += result.TotalLeaked;

                for (int wave = 1; wave < settings.TotalWaves; wave++)
                {
                    int[] arrivals = result.WaveSoldierArrivals[wave];

                    int busiest = -1, busiestCount = 0, arrivalTotal = 0;
                    for (int c = 0; c < arrivals.Length; c++)
                    {
                        arrivalTotal += arrivals[c];
                        if (arrivals[c] > busiestCount) { busiestCount = arrivals[c]; busiest = c; }
                    }

                    if (busiest < 0 || arrivalTotal == 0) continue;

                    int[] now = result.WaveColumnUsage[wave];
                    int[] next = result.WaveColumnUsage[wave + 1];

                    int nowTotal = 0, nextTotal = 0;
                    for (int c = 0; c < now.Length; c++) { nowTotal += now[c]; nextTotal += next[c]; }
                    if (nowTotal == 0 || nextTotal == 0) continue;

                    liftSum += (double)next[busiest] / nextTotal - (double)now[busiest] / nowTotal;
                    liftCount++;
                }
            }

            return new Result
            {
                Leaked = leaked / seedCount,
                Lift = liftCount > 0 ? (float)(liftSum / liftCount) : 0f
            };
        }

        private static float ChosenReinforce
        {
            get { return UnitProfile.For(UnitType.Soldier).ReinforceMultiplier; }
        }

        private static float ChosenHp
        {
            get { return UnitProfile.For(UnitType.Soldier).HpMultiplier; }
        }

        [Test]
        public void Soldier_NailsTheArmyToTheCorridorItReached()
        {
            var chosen = Measure(ChosenReinforce, ChosenHp, seedCount: 30);

            Assert.Greater(chosen.Lift, 0.02f,
                "Asker artik gectigi koridoru orduya civilemiyor - o zaman " +
                "yalnizca guclu bir piyadedir ve Plan 8'in kuralina gore " +
                "eklenmemeliydi. lift=" + chosen.Lift);
        }

        [Test]
        public void WithoutReinforcement_ThereIsNoNailing()
        {
            // Pekistirme x1 iken lift NEGATIF cikiyor (-0,009): ordu askerin
            // gectigi koridora yonelmiyor. Civilemenin pekistirmeden geldigi
            // boyle kanitlanir - askerin canindan degil.
            var noReinforce = Measure(1f, ChosenHp, seedCount: 30);
            var chosen = Measure(ChosenReinforce, ChosenHp, seedCount: 30);

            Assert.Greater(chosen.Lift, noReinforce.Lift + 0.02f,
                "Civileme artik pekistirmeden gelmiyor; baska bir sey " +
                "olcuyoruz demektir. x1=" + noReinforce.Lift +
                " secilen=" + chosen.Lift);
        }

        [Test]
        public void Reinforcement_IsACharacterKnob_NotADifficultyKnob()
        {
            // x1 -> 11,8 sizinti, x20 -> 13,6. Pekistirme zorlugu neredeyse
            // hic degistirmiyor; degistirseydi asker "iz civileyen birlik"
            // degil, gizli bir zorluk carpani olurdu.
            var weak = Measure(1f, ChosenHp, seedCount: 20);
            var strong = Measure(20f, ChosenHp, seedCount: 20);

            Assert.That(strong.Leaked, Is.EqualTo(weak.Leaked).Within(5f),
                "Pekistirme artik zorlugu degistiriyor - asker gizli bir " +
                "zorluk carpanina donusmus olabilir. x1=" + weak.Leaked +
                " x20=" + strong.Leaked);
        }

        [Test]
        public void SoldierHp_IsASharpThreshold()
        {
            // Bu projenin imzasi: esikler keskin. x2 -> 4,0 sizinti,
            // x4 -> 22,6. x3 secildi. Test, canin sessizce yukseltilmesini
            // yakalar.
            var soft = Measure(ChosenReinforce, 2f, seedCount: 20);
            var hard = Measure(ChosenReinforce, 4f, seedCount: 20);

            Assert.Greater(hard.Leaked, soft.Leaked + 8f,
                "Asker cani artik keskin bir esik degil; denge yeniden " +
                "olculmeli. x2=" + soft.Leaked + " x4=" + hard.Leaked);
        }

        [Test]
        public void Soldier_MakesTheRunHarder_ButNotUnwinnable()
        {
            // Asker eklenince oyun gercekten zorlasiyor: askersiz 3,3
            // sizinti, askerli 13,3. Bu iyi haber - oyun gevsekti. Ama
            // %40 pay run'i %50 kazanmaya dusuruyordu; %20 secildi.
            var withSoldiers = Measure(ChosenReinforce, ChosenHp, seedCount: 20);

            Assert.That(withSoldiers.Leaked, Is.InRange(6f, 22f),
                "Asker payi/gucu bandin disina cikti: sizinti=" + withSoldiers.Leaked);
        }
    }
}
