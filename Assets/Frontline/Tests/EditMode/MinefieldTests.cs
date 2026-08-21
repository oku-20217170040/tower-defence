using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Mayin tarlasi (yavaslatici) kulesinin denge kararlarini kilitler.
    ///
    /// Olcum SABIT KULE BUTCESIYLE yapildi: ekonomi devre disi, kuleler
    /// bastan kurulu. Neden: ilk tarama ekonomiyle birlikte kosturulunca 1/3
    /// karisim iki komsusundan da kotu cikiyordu ve mayin tarlasinin savas degeri mi
    /// yoksa ekonominin burulmesi mi oldugu ayirt edilemiyordu. Ekonomi
    /// cikarilinca 1/3 karisim (6 sessiz + 3 mayin tarlasi) %100 kazandi - yani sorun
    /// savas degeri degil, tur basina ayri sayilan fiyat artisiydi.
    ///
    /// Secilen degerler (20 seed, 6 kule butcesi, 3 sessiz + 3 mayin tarlasi):
    ///   x1,5 yavaslatma -> %20 kazanma   (mayin tarlasi olu mekanik)
    ///   x2   yavaslatma -> %95 kazanma   (SECILEN)
    ///   x3   yavaslatma -> %100, sizinti 0 (baskin strateji, oyunu cozer)
    ///   x2 + tehdit yazar -> %5          (ordu kaciyor, mayin tarlasi anlamsiz)
    ///
    /// Esik keskin; bu projede daha once pusu mevzisi DPS'inde de goruldu
    /// (3'te hic oldurmuyor, 4'te hepsini olduruyor).
    ///
    /// !!! MODEL UYARISI (20 Agustos 2026) !!!
    /// Buradaki simulasyon birlikleri SIRAYLA isler: bir birlik butun yolu
    /// yurur, sonra digeri baslar. O modelde tek hedefli bir kule, ayni anda
    /// gecen sekiz birligin HEPSINE tam DPS verir - gercekte bir seferde tek
    /// birlige ates eder. Olcum: 8 birlik / 3 kule kurulumunda sirali model
    /// hepsini olduruyor, eszamanli model 2 olum + 6 sizinti veriyor.
    /// Yani buradaki sayilar tek hedefli kuleleri OLDUGUNDAN GUCLU gosterir.
    /// Frontline.Core.RunSimulator eszamanli olcer; bu dosyadaki degerler onunla
    /// yeniden olculmeden gercek kabul edilmemeli.
    /// </summary>
    public class MinefieldTests
    {
        private const float BaseUnitHp = 10f;
        private const float HpGrowthPerWave = 0.15f;
        private const int BaseUnitsPerWave = 8;
        private const int TotalWaves = 15;
        private const int HomeBaseHp = 30;

        private const float AmbushDps = 4f;
        private const float TowerRadius = 4f;
        private const float TimePerCell = 0.4f;

        /// <summary>Emplacement.slowFactor varsayilani ile ayni. Ikisi birlikte degisir.</summary>
        private const float ChosenSlowFactor = 2f;

        private const int SeedCount = 20;

        private BattleGrid grid;
        private int baseCell;
        private List<int> spawnCells;
        private List<int[]> corridors;

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
            public bool IsMinefield;
        }

        private struct Outcome
        {
            public int WinPercent;
            public float AverageLeaked;
        }

        /// <summary>
        /// Sabit butceli run: ekonomi yok, kuleler bastan kurulu. Mayin tarlasiler
        /// pusu mevzisilerin menziline eslenir - gercek oyuncu da boyle koyar
        /// ve yavaslatma ancak orada hasara donusur.
        /// </summary>
        private Outcome Measure(int silentCount, int minefieldCount, float slowFactor, bool writesThreat)
        {
            int wins = 0;
            float leaked = 0f;

            for (int s = 0; s < SeedCount; s++)
            {
                bool survived;
                leaked += RunOnce(silentCount, minefieldCount, slowFactor, writesThreat,
                                  seed: 1000 + s * 37, survived: out survived);
                if (survived) wins++;
            }

            return new Outcome
            {
                WinPercent = 100 * wins / SeedCount,
                AverageLeaked = leaked / SeedCount
            };
        }

        private int RunOnce(int silentCount, int minefieldCount, float slowFactor,
                            bool writesThreat, int seed, out bool survived)
        {
            var recon = new ReconMap(grid, 100f, 0.15f);
            var decoy = new DecoyField(grid, 25f, 0.5f);
            var threat = new ThreatField(grid);
            var model = new RouteCostModel(recon, decoy, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 3f,
                ExplorationNoise = 0.35f
            };
            var finder = new RoutePathfinder(grid, model);
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

                towers.Add(new Tower { X = corridors[corridor][0], Y = y, IsMinefield = false });
                silentPositions.Add(new[] { corridors[corridor][0], y });
            }

            for (int i = 0; i < minefieldCount && i < silentPositions.Count; i++)
                towers.Add(new Tower
                {
                    X = silentPositions[i][0],
                    Y = silentPositions[i][1] + 1,
                    IsMinefield = true
                });

            if (writesThreat)
                for (int t = 0; t < towers.Count; t++)
                    if (towers[t].IsMinefield)
                        threat.AddSource(grid.Index(towers[t].X, towers[t].Y), TowerRadius, 4f);

            int baseHp = HomeBaseHp;
            int leaked = 0;

            for (int wave = 1; wave <= TotalWaves && baseHp > 0; wave++)
            {
                float unitHp = BaseUnitHp * (1f + HpGrowthPerWave * (wave - 1));
                int count = BaseUnitsPerWave + (wave - 1);

                for (int b = 0; b < count; b++)
                {
                    model.BeginQuery(random.Next(), grid);
                    int spawn = spawnCells[random.Next(spawnCells.Count)];
                    if (!finder.FindPath(spawn, baseCell, path)) continue;

                    var visited = new List<int>();
                    float hp = unitHp;
                    bool died = false;

                    for (int i = 0; i < path.Count; i++)
                    {
                        int cell = path[i];
                        visited.Add(cell);
                        recon.Deposit(cell, 0.5f);

                        int cx = grid.X(cell);
                        int cy = grid.Y(cell);

                        float timeHere = TimePerCell;
                        for (int t = 0; t < towers.Count; t++)
                        {
                            if (!towers[t].IsMinefield) continue;
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
                            if (towers[t].IsMinefield) continue;
                            float dx = cx - towers[t].X;
                            float dy = cy - towers[t].Y;
                            if (dx * dx + dy * dy <= TowerRadius * TowerRadius)
                                hp -= AmbushDps * timeHere;
                        }

                        if (hp <= 0f) { died = true; break; }
                    }

                    if (died)
                    {
                        recon.WeakenTrail(visited, 6f);
                    }
                    else
                    {
                        leaked++;
                        recon.DepositTrail(path, 1.5f);
                        baseHp--;
                        if (baseHp <= 0) break;
                    }
                }

                recon.Decay();
            }

            survived = baseHp > 0;
            return leaked;
        }

        [Test]
        public void ChosenSlowFactor_MakesMinefieldWorthBuying()
        {
            var outcome = Measure(silentCount: 3, minefieldCount: 3,
                                  slowFactor: ChosenSlowFactor, writesThreat: false);

            Assert.GreaterOrEqual(outcome.WinPercent, 80,
                "Mayin tarlasi karisimi artik run'i tasimiyor: %" + outcome.WinPercent);
        }

        [Test]
        public void WeakSlowdown_MakesMinefieldADeadMechanic()
        {
            // x1,5'te mayin tarlasi, yerini aldigi pusu mevzisiyi telafi etmiyor.
            // Bu testin amaci carpanin sessizce dusurulmesini engellemek.
            var outcome = Measure(silentCount: 3, minefieldCount: 3,
                                  slowFactor: 1.5f, writesThreat: false);

            Assert.Less(outcome.WinPercent, 50,
                "Zayif yavaslatma artik yeterli; mayin tarlasi esigi yeniden olculmeli. " +
                "Kazanma=%" + outcome.WinPercent);
        }

        [Test]
        public void StrongSlowdown_WouldTrivializeTheRun()
        {
            // x3 sizintiyi sifira indiriyor. Secilmemesinin sebebi budur:
            // oyunu cozen bir kule secim uretmez.
            var outcome = Measure(silentCount: 3, minefieldCount: 3,
                                  slowFactor: 3f, writesThreat: false);

            Assert.Less(outcome.AverageLeaked, 5f,
                "x3 artik run'i cozmuyor; secim gerekcesi olcume dayanmiyor. " +
                "Sizinti=" + outcome.AverageLeaked);
        }

        [Test]
        public void MinefieldWritingThreat_DestroysItsOwnPurpose()
        {
            // Mayin tarlasi tehdit yazarsa ordu ondan kacar ve yavaslatma hicbir
            // boceğe degmez. SlowField'in ThreatField'dan ayri olmasinin
            // gerekcesi bu olcumdur.
            var outcome = Measure(silentCount: 3, minefieldCount: 3,
                                  slowFactor: ChosenSlowFactor, writesThreat: true);

            Assert.Less(outcome.WinPercent, 40,
                "Tehdit yazan mayin tarlasi artik cezalandirilmiyor; ayrimin gerekcesi " +
                "kalmadi. Kazanma=%" + outcome.WinPercent);
        }

        [Test]
        public void MinefieldMix_IsAGenuineChoiceNotAnUpgrade()
        {
            // Mayin tarlasi karisimi saf sessizden BELIRGIN OLARAK IYI olmamali;
            // olsaydi baskin strateji olur ve secim ortadan kalkardi.
            var mixed = Measure(silentCount: 3, minefieldCount: 3,
                                slowFactor: ChosenSlowFactor, writesThreat: false);
            var pureSilent = Measure(silentCount: 6, minefieldCount: 0,
                                     slowFactor: ChosenSlowFactor, writesThreat: false);

            Assert.GreaterOrEqual(mixed.AverageLeaked, pureSilent.AverageLeaked,
                "Mayin tarlasi karisimi saf sessizi geciyor; baskin strateji olmus. " +
                "Karisik=" + mixed.AverageLeaked + " safSessiz=" + pureSilent.AverageLeaked);

            Assert.Less(mixed.AverageLeaked, pureSilent.AverageLeaked * 2f,
                "Mayin tarlasi karisimi saf sessizin cok gerisinde; gercek bir secenek " +
                "degil. Karisik=" + mixed.AverageLeaked + " safSessiz=" + pureSilent.AverageLeaked);
        }
    }
}
