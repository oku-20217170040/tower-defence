using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Colony.Core;

namespace Colony.Tests
{
    /// <summary>
    /// Ekonomi egrisini kilitler. Tam bir run'i bassiz simule eder; oyuncu
    /// modeli her dalga parasi yettigi kadar sessiz kule alip koridorlara
    /// dagitir - yani ULASILABILIR UST SINIRI olcer.
    ///
    /// Secim 162 kombinasyon x 20 seed taramasiyla yapildi. Kazanan ayar
    /// EconomyRunner varsayilanlaridir. Taramanin iki bulgusu:
    ///
    /// 1. Ayni turde +10 fiyat artisi run'i olduruyordu: kazanma orani %10,
    ///    8. dalgada ortalama 3,5 kule. +5'te %85'e ciktı.
    /// 2. Dalga sonu geliri bir taban gelirdir; oldurmekten bagimsiz oldugu
    ///    icin tuzagi calismayan oyuncunun kaybetme sarmalini kiriyor.
    ///    foodPerWave 5'e dusuruldugunde kazanma %60'a iniyor ve kayip
    ///    5. dalga kadar one geliyor.
    ///
    /// Bu testler bozulursa ya denge sayilari elle degistirilmistir ya da
    /// yol bulma/feromon davranisi kaymistir. Ikisi de sessizce olmamali.
    /// </summary>
    public class ColonyEconomyTests
    {
        private const float BaseBugHp = 10f;
        private const float HpGrowthPerWave = 0.15f;
        private const int BaseBugsPerWave = 8;
        private const float BugsAddedPerWave = 1f;
        private const int TotalWaves = 15;
        private const int NestHp = 30;

        private const float SilentDps = 4f;
        private const float TowerRadius = 4f;
        private const float TimePerCell = 0.4f;

        private ColonyGrid grid;
        private int nestCell;
        private List<int> spawnCells;
        private List<int[]> corridors;

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

            corridors = new List<int[]>();
            var current = new List<int>();
            int midRow = grid.Height / 2;
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.IsWalkable(grid.Index(x, midRow))) current.Add(x);
                else if (current.Count > 0) { corridors.Add(current.ToArray()); current = new List<int>(); }
            }
            if (current.Count > 0) corridors.Add(current.ToArray());
        }

        public struct EconomyParams
        {
            public int StartingBalance;
            public int FoodPerKill;
            public int FoodPerWave;
            public int SilentCost;
            public int CostStep;
            public bool FreeTowerEveryWave;
        }

        public struct ScanResult
        {
            public int[] TowersAtWave;
            public bool Survived;
            public int LostAtWave;
            public int TotalKilled;
            public int TotalLeaked;
        }

        /// <summary>
        /// Oyuncu modeli: her dalga oncesi parasi yettigi kadar SESSIZ kule
        /// alir ve koridorlara sirayla dagitir. Sessiz kule secildi cunku
        /// olculen en guclu strateji odur; bu, ulasilabilir kule sayisinin
        /// UST SINIRINI verir.
        /// </summary>
        private ScanResult Simulate(EconomyParams p, int seed)
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
            var random = new System.Random(seed);
            var path = new List<int>();

            var towers = new List<int[]>();
            int[] yOffsets = { 0, -5, 5, -8, 8, -2, 2, -7, 7, 4 };
            int midRow = grid.Height / 2;

            var economy = new RunEconomy(p.StartingBalance);
            var result = new ScanResult
            {
                LostAtWave = -1,
                TowersAtWave = new int[TotalWaves + 1]
            };
            int nestHp = NestHp;

            for (int wave = 1; wave <= TotalWaves && nestHp > 0; wave++)
            {
                // Draft odulu: bedava kule.
                if (p.FreeTowerEveryWave && wave > 1) AddTower(towers, yOffsets, midRow);

                // Parasi yettigi surece kule al.
                while (true)
                {
                    int cost = p.SilentCost + p.CostStep * towers.Count;
                    if (!economy.TrySpend(cost)) break;
                    AddTower(towers, yOffsets, midRow);
                }

                result.TowersAtWave[wave] = towers.Count;

                float bugHp = BaseBugHp * (1f + HpGrowthPerWave * (wave - 1));
                int count = Mathf.RoundToInt(BaseBugsPerWave + BugsAddedPerWave * (wave - 1));
                int killedThisWave = 0;

                for (int b = 0; b < count; b++)
                {
                    model.BeginQuery(random.Next(), grid);
                    int spawn = spawnCells[random.Next(spawnCells.Count)];
                    if (!finder.FindPath(spawn, nestCell, path)) continue;

                    var visited = new List<int>();
                    float hp = bugHp;
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
                        killedThisWave++;
                        pheromone.WeakenTrail(visited, 6f);
                    }
                    else
                    {
                        result.TotalLeaked++;
                        pheromone.DepositTrail(path, 1.5f);
                        nestHp--;
                        if (nestHp <= 0) break;
                    }
                }

                // Gelir: oldurulen basina + dalga sonu. Sizan bocek kazandirmaz.
                economy.Earn(killedThisWave * p.FoodPerKill);
                if (nestHp > 0) economy.Earn(p.FoodPerWave);

                pheromone.Decay();

                if (nestHp <= 0 && result.LostAtWave < 0) result.LostAtWave = wave;
            }

            result.Survived = nestHp > 0;
            return result;
        }

        private void AddTower(List<int[]> towers, int[] yOffsets, int midRow)
        {
            int index = towers.Count;
            int corridor = index % corridors.Count;
            int slot = index / corridors.Count;
            if (slot >= yOffsets.Length) slot = yOffsets.Length - 1;

            int y = Mathf.Clamp(midRow + yOffsets[slot], 2, grid.Height - 3);
            towers.Add(new int[] { corridors[corridor][0], y });
        }

        /// <summary>EconomyRunner varsayilanlari ile ayni. Ikisi birlikte degisir.</summary>
        private static EconomyParams Chosen()
        {
            return new EconomyParams
            {
                StartingBalance = 50,
                FoodPerKill = 2,
                FoodPerWave = 10,
                SilentCost = 24,
                CostStep = 5
            };
        }

        private struct Aggregate
        {
            public int WinPercent;
            public float TowersAtWave8;
            public float TowersAtWave10;
            public float AverageLeaked;
            public int EarliestLoss;
        }

        private Aggregate Aggregated(EconomyParams p, int seedCount)
        {
            var agg = new Aggregate { EarliestLoss = 99 };
            int wins = 0;
            float w8 = 0f, w10 = 0f, leaked = 0f;

            for (int s = 0; s < seedCount; s++)
            {
                var r = Simulate(p, seed: 1000 + s * 37);
                if (r.Survived) wins++;
                else if (r.LostAtWave < agg.EarliestLoss) agg.EarliestLoss = r.LostAtWave;

                w8 += r.TowersAtWave[8];
                w10 += r.TowersAtWave[10];
                leaked += r.TotalLeaked;
            }

            agg.WinPercent = 100 * wins / seedCount;
            agg.TowersAtWave8 = w8 / seedCount;
            agg.TowersAtWave10 = w10 / seedCount;
            agg.AverageLeaked = leaked / seedCount;
            return agg;
        }

        [Test]
        public void ChosenCurve_IsWinnableButNotGuaranteed()
        {
            // Optimal oynayan oyuncu cogu run'i kazanmali - ama hepsini degil.
            // %100 olsaydi ekonominin karar uretmedigi anlamina gelirdi.
            var agg = Aggregated(Chosen(), seedCount: 20);

            Assert.GreaterOrEqual(agg.WinPercent, 70,
                "Optimal oyun bile cogunlukla kaybediyor: %" + agg.WinPercent);
            Assert.LessOrEqual(agg.WinPercent, 95,
                "Ekonomi hicbir zaman zorlamiyor: %" + agg.WinPercent);
        }

        [Test]
        public void ChosenCurve_ReachesTargetTowerCount()
        {
            // Spec 3.5 hedefi: oyuncu 8-10. dalgada 4-5 etkili kuleye ulasmali.
            var agg = Aggregated(Chosen(), seedCount: 20);

            Assert.That(agg.TowersAtWave8, Is.InRange(4.5f, 6.5f),
                "8. dalgadaki kule sayisi hedef bandin disinda: " + agg.TowersAtWave8);
            Assert.That(agg.TowersAtWave10, Is.InRange(5f, 7.5f),
                "10. dalgadaki kule sayisi hedef bandin disinda: " + agg.TowersAtWave10);
        }

        [Test]
        public void ChosenCurve_CostsMostOfTheNest()
        {
            // Kazanilan run bile ucuza gelmemeli. Yuva cani 30; sizinti bunun
            // yarisinin altina duserse run gerilimini kaybeder.
            var agg = Aggregated(Chosen(), seedCount: 20);

            Assert.That(agg.AverageLeaked, Is.InRange(15f, 28f),
                "Ortalama sizinti bandin disinda: " + agg.AverageLeaked);
        }

        [Test]
        public void ChosenCurve_LeavesTimeToReact()
        {
            // En erken kayip cok erken olmamali; oyuncunun toparlanma sansi
            // olmadan biten run ogretmiyor.
            var agg = Aggregated(Chosen(), seedCount: 20);

            Assert.GreaterOrEqual(agg.EarliestLoss, 6,
                "Run cok erken kaybediliyor: dalga " + agg.EarliestLoss);
        }

        [Test]
        public void SteepCostStep_KillsTheRun()
        {
            // Ayni turde +10 artis olcumde run'i olduruyordu. Bu test o
            // bulguyu kalici kilar: birisi adimi geri yukseltirse gorulur.
            var steep = Chosen();
            steep.CostStep = 10;

            var agg = Aggregated(steep, seedCount: 20);

            Assert.Less(agg.WinPercent, 50,
                "Sert fiyat artisi artik run'i oldurmuyor; ekonomi yeniden " +
                "olculmeli. Kazanma=%" + agg.WinPercent);
        }

        [Test]
        public void WaveIncome_IsTheFloorThatBreaksTheLosingSpiral()
        {
            // Sizan bocek gelir kazandirmaz; tuzagi calismayan oyuncu tek
            // gelir kaynagi olarak dalga sonu gelirine kalir. O taban
            // dusurulunce run cokuyor - taban gelirin gorevi budur.
            var thin = Chosen();
            thin.FoodPerWave = 0;

            var agg = Aggregated(thin, seedCount: 20);

            Assert.Less(agg.WinPercent, 60,
                "Taban gelir olmadan da run kolayca kazaniliyor; taban gelirin " +
                "gerekcesi olcume dayanmiyor. Kazanma=%" + agg.WinPercent);
        }
    }
}
