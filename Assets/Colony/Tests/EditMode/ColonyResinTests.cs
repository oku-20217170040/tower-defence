using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Colony.Core;

namespace Colony.Tests
{
    /// <summary>
    /// Recine (yavaslatici) kulesinin denge kararlarini kilitler.
    ///
    /// Olcum SABIT KULE BUTCESIYLE yapildi: ekonomi devre disi, kuleler
    /// bastan kurulu. Neden: ilk tarama ekonomiyle birlikte kosturulunca 1/3
    /// karisim iki komsusundan da kotu cikiyordu ve recinenin savas degeri mi
    /// yoksa ekonominin burulmesi mi oldugu ayirt edilemiyordu. Ekonomi
    /// cikarilinca 1/3 karisim (6 sessiz + 3 recine) %100 kazandi - yani sorun
    /// savas degeri degil, tur basina ayri sayilan fiyat artisiydi.
    ///
    /// Secilen degerler (20 seed, 6 kule butcesi, 3 sessiz + 3 recine):
    ///   x1,5 yavaslatma -> %20 kazanma   (recine olu mekanik)
    ///   x2   yavaslatma -> %95 kazanma   (SECILEN)
    ///   x3   yavaslatma -> %100, sizinti 0 (baskin strateji, oyunu cozer)
    ///   x2 + tehdit yazar -> %5          (koloni kaciyor, recine anlamsiz)
    ///
    /// Esik keskin; bu projede daha once sessiz kule DPS'inde de goruldu
    /// (3'te hic oldurmuyor, 4'te hepsini olduruyor).
    ///
    /// !!! MODEL UYARISI (20 Agustos 2026) !!!
    /// Buradaki simulasyon bocekleri SIRAYLA isler: bir bocek butun yolu
    /// yurur, sonra digeri baslar. O modelde tek hedefli bir kule, ayni anda
    /// gecen sekiz bocegin HEPSINE tam DPS verir - gercekte bir seferde tek
    /// bocege ates eder. Olcum: 8 bocek / 3 kule kurulumunda sirali model
    /// hepsini olduruyor, eszamanli model 2 olum + 6 sizinti veriyor.
    /// Yani buradaki sayilar tek hedefli kuleleri OLDUGUNDAN GUCLU gosterir.
    /// Colony.Core.RunSimulator eszamanli olcer; bu dosyadaki degerler onunla
    /// yeniden olculmeden gercek kabul edilmemeli.
    /// </summary>
    public class ColonyResinTests
    {
        private const float BaseBugHp = 10f;
        private const float HpGrowthPerWave = 0.15f;
        private const int BaseBugsPerWave = 8;
        private const int TotalWaves = 15;
        private const int NestHp = 30;

        private const float SilentDps = 4f;
        private const float TowerRadius = 4f;
        private const float TimePerCell = 0.4f;

        /// <summary>ColonyTower.slowFactor varsayilani ile ayni. Ikisi birlikte degisir.</summary>
        private const float ChosenSlowFactor = 2f;

        private const int SeedCount = 20;

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

        private struct Outcome
        {
            public int WinPercent;
            public float AverageLeaked;
        }

        /// <summary>
        /// Sabit butceli run: ekonomi yok, kuleler bastan kurulu. Recineler
        /// sessiz kulelerin menziline eslenir - gercek oyuncu da boyle koyar
        /// ve yavaslatma ancak orada hasara donusur.
        /// </summary>
        private Outcome Measure(int silentCount, int resinCount, float slowFactor, bool writesThreat)
        {
            int wins = 0;
            float leaked = 0f;

            for (int s = 0; s < SeedCount; s++)
            {
                bool survived;
                leaked += RunOnce(silentCount, resinCount, slowFactor, writesThreat,
                                  seed: 1000 + s * 37, survived: out survived);
                if (survived) wins++;
            }

            return new Outcome
            {
                WinPercent = 100 * wins / SeedCount,
                AverageLeaked = leaked / SeedCount
            };
        }

        private int RunOnce(int silentCount, int resinCount, float slowFactor,
                            bool writesThreat, int seed, out bool survived)
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
            for (int i = 0; i < silentCount; i++)
            {
                int corridor = i % corridors.Count;
                int slot = Mathf.Min(i / corridors.Count, yOffsets.Length - 1);
                int y = Mathf.Clamp(midRow + yOffsets[slot], 2, grid.Height - 3);

                towers.Add(new Tower { X = corridors[corridor][0], Y = y, IsResin = false });
                silentPositions.Add(new[] { corridors[corridor][0], y });
            }

            for (int i = 0; i < resinCount && i < silentPositions.Count; i++)
                towers.Add(new Tower
                {
                    X = silentPositions[i][0],
                    Y = silentPositions[i][1] + 1,
                    IsResin = true
                });

            if (writesThreat)
                for (int t = 0; t < towers.Count; t++)
                    if (towers[t].IsResin)
                        threat.AddSource(grid.Index(towers[t].X, towers[t].Y), TowerRadius, 4f);

            int nestHp = NestHp;
            int leaked = 0;

            for (int wave = 1; wave <= TotalWaves && nestHp > 0; wave++)
            {
                float bugHp = BaseBugHp * (1f + HpGrowthPerWave * (wave - 1));
                int count = BaseBugsPerWave + (wave - 1);

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

                        float timeHere = TimePerCell;
                        for (int t = 0; t < towers.Count; t++)
                        {
                            if (!towers[t].IsResin) continue;
                            float rdx = cx - towers[t].X;
                            float rdy = cy - towers[t].Y;
                            if (rdx * rdx + rdy * rdy <= TowerRadius * TowerRadius)
                            {
                                timeHere = TimePerCell * slowFactor;
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
                        pheromone.WeakenTrail(visited, 6f);
                    }
                    else
                    {
                        leaked++;
                        pheromone.DepositTrail(path, 1.5f);
                        nestHp--;
                        if (nestHp <= 0) break;
                    }
                }

                pheromone.Decay();
            }

            survived = nestHp > 0;
            return leaked;
        }

        [Test]
        public void ChosenSlowFactor_MakesResinWorthBuying()
        {
            var outcome = Measure(silentCount: 3, resinCount: 3,
                                  slowFactor: ChosenSlowFactor, writesThreat: false);

            Assert.GreaterOrEqual(outcome.WinPercent, 80,
                "Recine karisimi artik run'i tasimiyor: %" + outcome.WinPercent);
        }

        [Test]
        public void WeakSlowdown_MakesResinADeadMechanic()
        {
            // x1,5'te recine, yerini aldigi sessiz kuleyi telafi etmiyor.
            // Bu testin amaci carpanin sessizce dusurulmesini engellemek.
            var outcome = Measure(silentCount: 3, resinCount: 3,
                                  slowFactor: 1.5f, writesThreat: false);

            Assert.Less(outcome.WinPercent, 50,
                "Zayif yavaslatma artik yeterli; recine esigi yeniden olculmeli. " +
                "Kazanma=%" + outcome.WinPercent);
        }

        [Test]
        public void StrongSlowdown_WouldTrivializeTheRun()
        {
            // x3 sizintiyi sifira indiriyor. Secilmemesinin sebebi budur:
            // oyunu cozen bir kule secim uretmez.
            var outcome = Measure(silentCount: 3, resinCount: 3,
                                  slowFactor: 3f, writesThreat: false);

            Assert.Less(outcome.AverageLeaked, 5f,
                "x3 artik run'i cozmuyor; secim gerekcesi olcume dayanmiyor. " +
                "Sizinti=" + outcome.AverageLeaked);
        }

        [Test]
        public void ResinWritingThreat_DestroysItsOwnPurpose()
        {
            // Recine tehdit yazarsa koloni ondan kacar ve yavaslatma hicbir
            // boceğe degmez. SlowField'in ThreatField'dan ayri olmasinin
            // gerekcesi bu olcumdur.
            var outcome = Measure(silentCount: 3, resinCount: 3,
                                  slowFactor: ChosenSlowFactor, writesThreat: true);

            Assert.Less(outcome.WinPercent, 40,
                "Tehdit yazan recine artik cezalandirilmiyor; ayrimin gerekcesi " +
                "kalmadi. Kazanma=%" + outcome.WinPercent);
        }

        [Test]
        public void ResinMix_IsAGenuineChoiceNotAnUpgrade()
        {
            // Recine karisimi saf sessizden BELIRGIN OLARAK IYI olmamali;
            // olsaydi baskin strateji olur ve secim ortadan kalkardi.
            var mixed = Measure(silentCount: 3, resinCount: 3,
                                slowFactor: ChosenSlowFactor, writesThreat: false);
            var pureSilent = Measure(silentCount: 6, resinCount: 0,
                                     slowFactor: ChosenSlowFactor, writesThreat: false);

            Assert.GreaterOrEqual(mixed.AverageLeaked, pureSilent.AverageLeaked,
                "Recine karisimi saf sessizi geciyor; baskin strateji olmus. " +
                "Karisik=" + mixed.AverageLeaked + " safSessiz=" + pureSilent.AverageLeaked);

            Assert.Less(mixed.AverageLeaked, pureSilent.AverageLeaked * 2f,
                "Recine karisimi saf sessizin cok gerisinde; gercek bir secenek " +
                "degil. Karisik=" + mixed.AverageLeaked + " safSessiz=" + pureSilent.AverageLeaked);
        }
    }
}
