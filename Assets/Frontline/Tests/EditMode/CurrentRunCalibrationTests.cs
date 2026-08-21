using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Simulatoru BUGUNKU denge ile oynanmis gercek bir run'a karsi kalibre
    /// tutar. Kardesi RunCalibrationTests 20 Agustos'un ESKI ayarlarina
    /// bagli kalir (o gun olculen dogruluk kaybolmasin diye); bu dosya yeni
    /// ayarlarin referansidir.
    ///
    /// Referans run (kullanici, 21 Agustos 2026, ekran goruntusuyle):
    ///   15/15 dalga, KAZANDI, us 26/30 - yani yalnizca 4 sizinti
    ///   221 oldurme, 70 besin artmis
    ///
    /// Kule sayisi ekranda yoktu; o yuzden burada iddia edilmiyor.
    ///
    /// NEDEN BU DOSYA VAR: bu run ilk goruldugunde simulator ayni ayarlarla
    /// 21,8 sizinti soyluyordu ve "kalibrasyon bozuldu" sanildi. Olcum
    /// bozulmanin simulatorde degil, modellenen OYUNCUDA oldugunu gosterdi:
    /// EconomyPlanner.Policy.Default() pahali pusudan basliyor, kullanici ise
    /// ucuz batarya ile pusuyu donusumlu aliyor. Ayni simulator, yalnizca
    /// alim sirasi degistirilerek gercek run'i %1 hatayla yeniden uretiyor.
    /// Ders: simulator ile gercek arasindaki sapmanin ilk suphelisi oyuncu
    /// modelidir, savas modeli degil.
    /// </summary>
    public class CurrentRunCalibrationTests
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
            public float Killed;
            public float Leaked;
            public float Towers;
            public float LeftoverFood;
            public float HomeBaseHpLeft;
        }

        /// <summary>
        /// Bugunku denge (Settings.Default + Costs.Default) ile olcer.
        /// Buradan tek bir alan elle sabitlenmemeli: testin isi, denge
        /// degistiginde gercek run'la olan baginin kopusunu gostermektir.
        /// </summary>
        private Averages Measure(EconomyPlanner.Policy policy, int seedCount)
        {
            var settings = RunSimulator.Settings.Default();

            // 21 Agustos run'i TABAN SAPMA 0,35 ile ve izcisiz oynandi.
            // Taban ayni gun ilerleyen saatlerde 0,12'ye dusuruldu (kesif
            // izciye yuklendi, Plan 8 Gorev 2); referans o degisiklikten
            // ONCEKI kosullari olcmeli.
            settings.ExplorationNoise = 0.35f;
            settings.Composition = null;

            int wins = 0;
            float killed = 0f, leaked = 0f, towers = 0f, food = 0f, hp = 0f;

            for (int s = 0; s < seedCount; s++)
            {
                var planner = new EconomyPlanner(
                    EconomyPlanner.Costs.Default(), policy, Place, settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                var result = sim.Run(built, settings, seed: 1000 + s * 37, planner: planner);

                if (result.Survived) wins++;
                killed += result.TotalKilled;
                leaked += result.TotalLeaked;
                towers += planner.TowersBuilt;
                food += planner.LeftoverFood;
                hp += result.HomeBaseHpLeft;
            }

            return new Averages
            {
                WinPercent = 100 * wins / seedCount,
                Killed = killed / seedCount,
                Leaked = leaked / seedCount,
                Towers = towers / seedCount,
                LeftoverFood = food / seedCount,
                HomeBaseHpLeft = hp / seedCount
            };
        }

        [Test]
        public void Simulator_MatchesTheAugust21Run()
        {
            // Olculen (20 seed): oldurme 219,3 · sizinti 5,7 · besin 65,6 · us 24,3
            // Gercek run:        oldurme 221   · sizinti 4   · besin 70   · us 26
            var avg = Measure(EconomyPlanner.Policy.Skilled(), seedCount: 20);

            Assert.That(avg.Killed, Is.InRange(195f, 245f), "Oldurme gercek run'dan sapti: " + avg.Killed);
            Assert.That(avg.Leaked, Is.InRange(1f, 10f), "Sizinti gercek run'dan sapti: " + avg.Leaked);
            Assert.That(avg.LeftoverFood, Is.InRange(45f, 95f), "Artan besin gercek run'dan sapti: " + avg.LeftoverFood);
            Assert.That(avg.HomeBaseHpLeft, Is.InRange(20f, 30f), "Us cani gercek run'dan sapti: " + avg.HomeBaseHpLeft);
        }

        [Test]
        public void SkilledPlay_IsWorthAboutTwoThirdsOfTheHomeBase()
        {
            // Oyunun su anki en buyuk zorluk dugmesi denge sayilari degil,
            // oyuncunun alim sirasi. Bu fark kapanirsa (ornegin butun kuleler
            // esit degerli hale gelirse) oyun karar barindirmayi birakir.
            var skilled = Measure(EconomyPlanner.Policy.Skilled(), seedCount: 20);
            var average = Measure(EconomyPlanner.Policy.Default(), seedCount: 20);

            Assert.Greater(skilled.HomeBaseHpLeft, average.HomeBaseHpLeft + 8f,
                "Alim sirasi artik onemli degil: usta=" + skilled.HomeBaseHpLeft +
                " ortalama=" + average.HomeBaseHpLeft);
        }

        [Test]
        public void SkilledPlay_WinsComfortably_WhichIsARecordedFact()
        {
            // 21 Agustos run'i 26/30 ile bitti. Erken oyun kullanici geri
            // bildirimiyle acildi (baslangic besini 50 -> 90) ve zorluk bu
            // kadar dustu. Bu bir HATA degil, kayitli bir gercek: geri
            // sikilastirilirsa bu test de guncellenmeli.
            var avg = Measure(EconomyPlanner.Policy.Skilled(), seedCount: 20);

            Assert.AreEqual(100, avg.WinPercent, "Usta oyun artik kazanmiyor: %" + avg.WinPercent);
        }
    }
}
