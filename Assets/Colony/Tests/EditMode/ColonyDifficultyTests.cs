using System.Collections.Generic;
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    /// <summary>
    /// Zorluk egrisini kilitler. Tam bir run'i (15 dalga) bassiz simule eder.
    ///
    /// Neden bu testler var: denge sayilari elle degistirildiginde sessizce
    /// bozulur. Bu testler "yetersiz savunma kaybeder, yeterli savunma kazanir"
    /// ifadesini kalici hale getirir. Ikisinden biri bozulursa run ya
    /// imkansizdir ya da anlamsizdir.
    ///
    /// Olcum sonucu: kule sayisi baskin degisken. Yuva canini 30'dan 50'ye
    /// cikarmak kayip dalgasini yalnizca 10'dan 11'e tasidi; oysa koridor
    /// basina kuleyi 1'den 2'ye cikarmak kaybi tam zafere cevirdi.
    /// </summary>
    public class ColonyDifficultyTests
    {
        // RunController varsayilanlari ile ayni.
        private const float BaseBugHp = 10f;
        private const float HpGrowthPerWave = 0.15f;
        private const int BaseBugsPerWave = 10;
        private const int TotalWaves = 15;
        private const int NestHp = 30;

        // TowerPlacer / ColonyTower varsayilanlari ile ayni.
        private const float SilentDps = 4f;
        private const float TowerRadius = 4f;

        // BugAgent: hucre basina gecen sure = 1 / cellsPerSecond
        private const float TimePerCell = 0.4f;

        private ColonyGrid grid;
        private int nestCell;
        private List<int> spawnCells;
        private List<int[]> corridors;

        [SetUp]
        public void SetUp()
        {
            var template = new MapTemplate
            {
                Width = 11,
                Height = 20,
                CorridorCount = 3,
                CorridorWidth = 2
            };
            grid = new MapGenerator().Generate(template, seed: 3);

            nestCell = grid.Index(grid.Width / 2, 0);

            spawnCells = new List<int>();
            int topRow = grid.Height - 1;
            for (int x = 0; x < grid.Width; x++)
            {
                int cell = grid.Index(x, topRow);
                if (grid.IsWalkable(cell)) spawnCells.Add(cell);
            }

            corridors = FindCorridors();
            Assert.AreEqual(3, corridors.Count);
        }

        private List<int[]> FindCorridors()
        {
            var groups = new List<int[]>();
            var current = new List<int>();
            int midRow = grid.Height / 2;

            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.IsWalkable(grid.Index(x, midRow))) current.Add(x);
                else if (current.Count > 0)
                {
                    groups.Add(current.ToArray());
                    current = new List<int>();
                }
            }
            if (current.Count > 0) groups.Add(current.ToArray());
            return groups;
        }

        private struct RunResult
        {
            public bool Survived;
            public int LostAtWave;
            public int TotalKilled;
            public int TotalLeaked;
        }

        /// <summary>
        /// Her koridora esit sayida sessiz kule dagitip tam bir run simule eder.
        /// </summary>
        private RunResult SimulateRun(int silentTowersPerCorridor)
        {
            var pheromone = new PheromoneMap(grid, 100f, 0.15f);
            var lure = new LureField(grid, 25f, 0.5f);
            var threat = new ThreatField(grid);
            var model = new RouteCostModel(pheromone, lure, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 3f,
                ExplorationNoise = 0.35f
            };
            var finder = new ColonyPathfinder(grid, model);
            var random = new System.Random(999);
            var path = new List<int>();

            var towers = new List<int[]>();
            int midRow = grid.Height / 2;
            for (int c = 0; c < corridors.Count; c++)
            {
                for (int i = 0; i < silentTowersPerCorridor; i++)
                {
                    int y = silentTowersPerCorridor == 1
                        ? midRow
                        : 3 + (int)((grid.Height - 7) * ((float)i / (silentTowersPerCorridor - 1)));
                    towers.Add(new int[] { corridors[c][0], y });
                }
            }

            var result = new RunResult { LostAtWave = -1 };
            int nestHp = NestHp;

            for (int wave = 1; wave <= TotalWaves && nestHp > 0; wave++)
            {
                float bugHpForWave = BaseBugHp * (1f + HpGrowthPerWave * (wave - 1));
                int count = BaseBugsPerWave + (wave - 1);

                for (int b = 0; b < count; b++)
                {
                    model.BeginQuery(random.Next(), grid);
                    int spawn = spawnCells[random.Next(spawnCells.Count)];
                    if (!finder.FindPath(spawn, nestCell, path)) continue;

                    var visited = new List<int>();
                    float hp = bugHpForWave;
                    bool died = false;

                    for (int i = 0; i < path.Count; i++)
                    {
                        int cell = path[i];
                        visited.Add(cell);
                        pheromone.Deposit(cell, 0.5f);

                        int cx = grid.X(cell);
                        int cy = grid.Y(cell);
                        for (int t = 0; t < towers.Count; t++)
                        {
                            float dx = cx - towers[t][0];
                            float dy = cy - towers[t][1];
                            if (dx * dx + dy * dy <= TowerRadius * TowerRadius)
                                hp -= SilentDps * TimePerCell;
                        }

                        if (hp <= 0f) { died = true; break; }
                    }

                    if (died)
                    {
                        result.TotalKilled++;
                        pheromone.WeakenTrail(visited, 6f);
                    }
                    else
                    {
                        result.TotalLeaked++;
                        pheromone.DepositTrail(path, 1.5f);
                        nestHp--;
                    }
                }

                pheromone.Decay();

                if (nestHp <= 0 && result.LostAtWave < 0)
                    result.LostAtWave = wave;
            }

            result.Survived = nestHp > 0;
            return result;
        }

        [Test]
        public void ThinDefense_LosesTheRun()
        {
            // Koridor basina tek kule yetmez. Yetseydi oyuncunun genisletmek
            // icin hicbir sebebi kalmazdi ve run karar uretmezdi.
            RunResult result = SimulateRun(silentTowersPerCorridor: 1);

            Assert.IsFalse(result.Survived,
                "Zayif savunma run'i tamamlamamali. Oldurulen=" + result.TotalKilled);
            Assert.GreaterOrEqual(result.LostAtWave, 6,
                "Kayip cok erken; oyuncunun tepki verecek zamani olmuyor. Dalga=" + result.LostAtWave);
        }

        [Test]
        public void AdequateDefense_CompletesTheRun()
        {
            // Koridor basina iki kule yeterli. Yetmeseydi run kazanilamaz olurdu.
            RunResult result = SimulateRun(silentTowersPerCorridor: 2);

            Assert.IsTrue(result.Survived,
                "Yeterli savunma run'i tamamlamali. Dalga " + result.LostAtWave + "'de kaybetti.");
            Assert.Greater(result.TotalKilled, 200,
                "Beklenen olum sayisina ulasilmadi: " + result.TotalKilled);
        }

        [Test]
        public void UncoveredCorridor_IsExploitedByTheColony()
        {
            // Butun kuleleri tek koridora yigmak ise yaramaz: koloni korumasiz
            // koridoru bulur. Bu, uc koridorlu tasarimin gercek maliyetidir -
            // oyuncu hepsini dusunmek zorunda.
            var pheromone = new PheromoneMap(grid, 100f, 0.15f);
            var lure = new LureField(grid, 25f, 0.5f);
            var threat = new ThreatField(grid);
            var model = new RouteCostModel(pheromone, lure, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 3f,
                ExplorationNoise = 0.35f
            };
            var finder = new ColonyPathfinder(grid, model);
            var random = new System.Random(999);
            var path = new List<int>();

            int midRow = grid.Height / 2;
            int guardedCorridor = 0;
            int guardedColumn = corridors[guardedCorridor][0];

            int guardedCount = 0;
            int unguardedCount = 0;

            for (int wave = 0; wave < 8; wave++)
            {
                for (int b = 0; b < 12; b++)
                {
                    model.BeginQuery(random.Next(), grid);
                    int spawn = spawnCells[random.Next(spawnCells.Count)];
                    if (!finder.FindPath(spawn, nestCell, path)) continue;

                    bool usedGuarded = path.Contains(grid.Index(guardedColumn, midRow));

                    // Korunan koridordan gecen olur ve izini zayiflatir.
                    if (usedGuarded)
                    {
                        guardedCount++;
                        pheromone.WeakenTrail(path, 6f);
                    }
                    else
                    {
                        unguardedCount++;
                        pheromone.DepositTrail(path, 2f);
                    }
                }
                pheromone.Decay();
            }

            Assert.Greater(unguardedCount, guardedCount,
                "Koloni korumasiz koridoru tercih etmeliydi. Korunan=" + guardedCount +
                " korumasiz=" + unguardedCount);
        }
    }
}
