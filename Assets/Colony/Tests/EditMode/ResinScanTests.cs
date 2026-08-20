using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Colony.Core;

namespace Colony.Tests
{
    /// <summary>
    /// GECICI tarama: Recine (yavaslatici) kulesi gercek bir secenek mi?
    ///
    /// Iki soru olculuyor:
    /// 1. Recine karisimi saf sessiz kuleden daha iyi mi? Degilse olu mekanik.
    /// 2. Recine tehdit yazmali mi? Yazarsa koloni ondan kacar ve yavaslatma
    ///    ise yaramaz; yazmazsa muhtemelen fazla guclu.
    /// </summary>
    public class ResinScanTests
    {
        private const float BaseBugHp = 10f;
        private const float HpGrowthPerWave = 0.15f;
        private const int BaseBugsPerWave = 8;
        private const int TotalWaves = 15;
        private const int NestHp = 30;

        private const float SilentDps = 4f;
        private const float TowerRadius = 4f;
        private const float TimePerCell = 0.4f;

        // Secilen ekonomi (EconomyRunner varsayilanlari).
        private const int StartingBalance = 50;
        private const int FoodPerKill = 2;
        private const int FoodPerWave = 10;
        private const int SilentCost = 24;
        private const int CostStep = 5;

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

        private struct Tower
        {
            public int X;
            public int Y;
            public bool IsResin;
        }

        private struct Setup
        {
            /// <summary>Kac kulede bir recine alinir. 0 = hic. 2 = her ikincisi.</summary>
            public int ResinEvery;

            /// <summary>Recinenin gecis suresini kac katina cikardigi.</summary>
            public float SlowFactor;

            /// <summary>Recine tehdit alanina yaziyor mu?</summary>
            public bool ResinWritesThreat;

            public int ResinCost;
        }

        private struct Result
        {
            public bool Survived;
            public int Leaked;
            public int Towers;
            public int ResinCount;
        }

        private Result Simulate(Setup s, int seed)
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

            var towers = new List<Tower>();
            int[] yOffsets = { 0, -5, 5, -8, 8, -2, 2, -7, 7, 4 };
            int midRow = grid.Height / 2;

            var silentPositions = new List<int[]>();
            int resinPaired = 0;
            var economy = new RunEconomy(StartingBalance);
            int nestHp = NestHp;
            var result = new Result();

            for (int wave = 1; wave <= TotalWaves && nestHp > 0; wave++)
            {
                while (true)
                {
                    // Recine ancak eslesecek bir sessiz kule varsa alinir.
                    bool wantResin = s.ResinEvery > 0
                                     && (towers.Count + 1) % s.ResinEvery == 0
                                     && silentPositions.Count > resinPaired;

                    int sameKind = 0;
                    for (int i = 0; i < towers.Count; i++)
                        if (towers[i].IsResin == wantResin) sameKind++;

                    int cost = (wantResin ? s.ResinCost : SilentCost) + CostStep * sameKind;
                    if (!economy.TrySpend(cost)) break;

                    if (wantResin)
                    {
                        // Gercek oyuncu recineyi sessiz kulenin MENZILINE koyar;
                        // yavaslatma ancak orada hasara donusur. Ilk olcumde bu
                        // yapilmamisti ve yavaslatma carpaninin sonuca hic etkisi
                        // olmadi - modelin yanlis oldugunun isaretiydi.
                        var partner = silentPositions[resinPaired];
                        resinPaired++;
                        towers.Add(new Tower { X = partner[0], Y = partner[1] + 1, IsResin = true });
                    }
                    else
                    {
                        int index = towers.Count;
                        int corridor = index % corridors.Count;
                        int slot = Mathf.Min(index / corridors.Count, yOffsets.Length - 1);
                        int y = Mathf.Clamp(midRow + yOffsets[slot], 2, grid.Height - 3);

                        towers.Add(new Tower { X = corridors[corridor][0], Y = y, IsResin = false });
                        silentPositions.Add(new[] { corridors[corridor][0], y });
                    }
                }

                // Tehdit alanini kur. Recine yaziyorsa koloni ondan kacmayi ogrenir.
                threat.Clear();
                if (s.ResinWritesThreat)
                {
                    for (int t = 0; t < towers.Count; t++)
                        if (towers[t].IsResin)
                            threat.AddSource(grid.Index(towers[t].X, towers[t].Y), TowerRadius, 4f);
                }

                float bugHp = BaseBugHp * (1f + HpGrowthPerWave * (wave - 1));
                int count = BaseBugsPerWave + (wave - 1);
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

                        // Once bu hucrede ne kadar kalindigini bul: recine
                        // menzilindeyse gecis suresi uzar.
                        float timeHere = TimePerCell;
                        for (int t = 0; t < towers.Count; t++)
                        {
                            if (!towers[t].IsResin) continue;
                            float rdx = cx - towers[t].X;
                            float rdy = cy - towers[t].Y;
                            if (rdx * rdx + rdy * rdy <= TowerRadius * TowerRadius)
                            {
                                timeHere = TimePerCell * s.SlowFactor;
                                break;
                            }
                        }

                        for (int t = 0; t < towers.Count; t++)
                        {
                            if (towers[t].IsResin) continue;
                            float dx = cx - towers[t].X;
                            float dy = cy - towers[t].Y;
                            if (dx * dx + dy * dy <= TowerRadius * TowerRadius)
                                hp -= SilentDps * timeHere;
                        }

                        if (hp <= 0f) { died = true; break; }
                    }

                    if (died)
                    {
                        killedThisWave++;
                        pheromone.WeakenTrail(visited, 6f);
                    }
                    else
                    {
                        result.Leaked++;
                        pheromone.DepositTrail(path, 1.5f);
                        nestHp--;
                        if (nestHp <= 0) break;
                    }
                }

                economy.Earn(killedThisWave * FoodPerKill);
                if (nestHp > 0) economy.Earn(FoodPerWave);
                pheromone.Decay();
            }

            result.Survived = nestHp > 0;
            result.Towers = towers.Count;
            for (int i = 0; i < towers.Count; i++) if (towers[i].IsResin) result.ResinCount++;
            return result;
        }

        private string Aggregate(Setup s, int seedCount)
        {
            int wins = 0;
            float leaked = 0f, towers = 0f, resin = 0f;

            for (int i = 0; i < seedCount; i++)
            {
                var r = Simulate(s, seed: 1000 + i * 37);
                if (r.Survived) wins++;
                leaked += r.Leaked;
                towers += r.Towers;
                resin += r.ResinCount;
            }

            return (100 * wins / seedCount) + "% | sizan " + (leaked / seedCount).ToString("0.0") +
                   " | kule " + (towers / seedCount).ToString("0.0") +
                   " (recine " + (resin / seedCount).ToString("0.0") + ")";
        }

        [Test]
        [Explicit]
        public void Scan_Resin()
        {
            var sb = new StringBuilder();
            sb.AppendLine("her-N | yavaslama | tehdit | fiyat || kazanma | sizan | kule");

            int[] everies = { 0, 4, 3, 2 };
            float[] slows = { 1.5f, 2f, 3f };
            bool[] threats = { false, true };
            int[] costs = { 16, 24 };

            foreach (int every in everies)
            foreach (float slow in slows)
            foreach (bool writesThreat in threats)
            foreach (int cost in costs)
            {
                // Recine yoksa diger eksenler anlamsiz; bir kez yaz.
                if (every == 0 && (slow != 1.5f || writesThreat || cost != 16)) continue;

                var s = new Setup
                {
                    ResinEvery = every,
                    SlowFactor = slow,
                    ResinWritesThreat = writesThreat,
                    ResinCost = cost
                };

                sb.AppendLine(
                    (every == 0 ? "yok" : ("1/" + every)) + " | x" + slow + " | " +
                    (writesThreat ? "yazar" : "sessiz") + " | " + cost + " || " +
                    Aggregate(s, seedCount: 20));
            }

            Debug.Log("[RECINE-V2 eslestirilmis]\n" + sb);
        }
    }
}
