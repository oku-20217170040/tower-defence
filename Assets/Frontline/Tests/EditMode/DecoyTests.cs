using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Oyunun ayirt edici iddiasini dogrular: oyuncu ogrenmis bir orduyi
    /// yonlendirebilir, ama ordu once DIRENIR.
    ///
    /// Iki uc de basarisizliktir ve ikisi de olcumle bulundu:
    /// - Cekim 6 iken doymus iz bir duvar olur; hicbir kule suruyu oynatamaz.
    /// - Cekim 2 iken suru ilk dalgada pes eder; ogrenmis olmasinin anlami kalmaz.
    /// Cekim 3 aradaki gerilimi verir.
    /// </summary>
    public class DecoyTests
    {
        private const float DepositPerUnit = 2f;
        private const int UnitsPerWave = 12;
        private const float ExplorationNoise = 0.35f;
        private const float AttractionWeight = 3f;
        private const float ThreatWeight = 3f;

        // Emplacement varsayilanlari ile ayni.
        private const float TowerRadius = 4f;
        private const float TowerThreat = 4f;

        private BattleGrid grid;
        private ReconMap recon;
        private DecoyField decoy;
        private ThreatField threat;
        private RouteCostModel model;
        private RoutePathfinder finder;
        private int baseCell;
        private List<int> spawnCells;
        private List<int[]> corridors;
        private System.Random random;

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

            recon = new ReconMap(grid, max: 100f, decayRate: 0.15f);
            decoy = new DecoyField(grid, baseStrength: 25f, diminishFactor: 0.5f);
            threat = new ThreatField(grid);
            model = new RouteCostModel(recon, decoy, threat)
            {
                ThreatWeight = ThreatWeight,
                AttractionWeight = AttractionWeight,
                ExplorationNoise = ExplorationNoise
            };
            finder = new RoutePathfinder(grid, model);

            baseCell = grid.Index(grid.Width / 2, 0);

            spawnCells = new List<int>();
            int topRow = grid.Height - 1;
            for (int x = 0; x < grid.Width; x++)
            {
                int cell = grid.Index(x, topRow);
                if (grid.IsWalkable(cell)) spawnCells.Add(cell);
            }

            corridors = FindCorridors();
            Assert.AreEqual(3, corridors.Count);

            random = new System.Random(1234);
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

        private int[] RunWave()
        {
            var counts = new int[corridors.Count];
            var path = new List<int>();
            int midRow = grid.Height / 2;

            for (int b = 0; b < UnitsPerWave; b++)
            {
                model.BeginQuery(random.Next(), grid);
                int spawn = spawnCells[random.Next(spawnCells.Count)];
                if (!finder.FindPath(spawn, baseCell, path)) continue;

                recon.DepositTrail(path, DepositPerUnit);

                for (int c = 0; c < corridors.Count; c++)
                {
                    bool used = false;
                    for (int k = 0; k < corridors[c].Length; k++)
                    {
                        if (path.Contains(grid.Index(corridors[c][k], midRow))) { used = true; break; }
                    }
                    if (used) { counts[c]++; break; }
                }
            }

            recon.Decay();
            return counts;
        }

        private int RunUntilLocked()
        {
            int[] counts = null;
            for (int w = 0; w < 13; w++) counts = RunWave();

            int locked = 0;
            for (int i = 1; i < counts.Length; i++)
                if (counts[i] > counts[locked]) locked = i;

            return locked;
        }

        private static float ShareOf(int[] counts, int corridor)
        {
            int total = 0;
            for (int i = 0; i < counts.Length; i++) total += counts[i];
            return total == 0 ? 0f : (float)counts[corridor] / total;
        }

        /// <summary>Kilitli koridora tek batarya kule koyar.</summary>
        private void PlaceNoisyTowerIn(int corridor)
        {
            int midRow = grid.Height / 2;
            threat.AddSource(grid.Index(corridors[corridor][0], midRow), TowerRadius, TowerThreat);
        }

        /// <summary>Sahte hedefni tazeler. Her dalga cagrilmalidir (spec 3.3).</summary>
        private void RefreshDecoyIn(int corridor)
        {
            int midRow = grid.Height / 2;
            for (int y = midRow - 2; y <= midRow + 2; y++)
                for (int k = 0; k < corridors[corridor].Length; k++)
                    decoy.Apply(grid.Index(corridors[corridor][k], y));
        }

        [Test]
        public void BatteryAlone_DoesNotBreakAnEstablishedTrail()
        {
            // Kule tek basina suruyu sarsar ama ordu toparlanip geri doner.
            // Olculen egri: 0.50 0.75 0.58 1.00 0.75 0.75 0.75 0.83
            int locked = RunUntilLocked();
            PlaceNoisyTowerIn(locked);

            float share = 0f;
            for (int w = 0; w < 8; w++)
                share = ShareOf(RunWave(), locked);

            Assert.Greater(share, 0.5f,
                "Batarya kule tek basina izi kirmamali; kirarsa kombinasyon " +
                "gereksizlesir ve oyunun aha ani kaybolur. Pay=" + share);
        }

        [Test]
        public void DecoyAlone_DoesNotBreakAnEstablishedTrail()
        {
            // Yem sabit miktar ekler, oransal degil; oturmus guclu izi
            // tek basina ceviremez (spec 9.2). Olculen: 8 dalga boyunca 1.00
            int locked = RunUntilLocked();
            int target = (locked + 1) % corridors.Count;

            float share = 0f;
            for (int w = 0; w < 8; w++)
            {
                RefreshDecoyIn(target);
                share = ShareOf(RunWave(), locked);
            }

            Assert.Greater(share, 0.8f,
                "Yem tek basina oturmus izi cevirmemeli. Pay=" + share);
        }

        [Test]
        public void BatteryPlusDecoy_RelocatesTheArmy()
        {
            // OYUNUN AHA ANI: ne itmek ne cekmek tek basina yeter; ikisi birden
            // gerekir. Olculen egri: 0.50 0.67 0.25 0.33 0.25 0.08 0.00 0.00
            int locked = RunUntilLocked();
            int target = (locked + 1) % corridors.Count;

            PlaceNoisyTowerIn(locked);

            float firstAfter = 0f;
            float lastAfter = 0f;
            for (int w = 0; w < 8; w++)
            {
                RefreshDecoyIn(target);
                float share = ShareOf(RunWave(), locked);
                if (w == 0) firstAfter = share;
                lastAfter = share;
            }

            // DIRENC: ordu ilk dalgada teslim olmaz. Aninda pes etseydi
            // ogrenmis olmasinin hicbir anlami kalmazdi.
            Assert.Greater(firstAfter, 0.15f,
                "Ordu aninda pes etti; ogrenme anlamsizlasiyor. Pay=" + firstAfter);

            // TESLIMIYET: birkac dalga sonra koridoru terk eder.
            Assert.Less(lastAfter, 0.2f,
                "Kombinasyon suruyu tasimadi. Pay=" + lastAfter);
        }

        [Test]
        public void AmbushTower_IsInvisibleToTheArmy()
        {
            // Pusu mevzisi tehdit alanina HIC yazmaz; bu, oyunun merkezi
            // ayriminin tek teknik karsiligidir (spec 3.1).
            int locked = RunUntilLocked();

            float before = ShareOf(RunWave(), locked);

            float threatSum = 0f;
            for (int i = 0; i < grid.CellCount; i++) threatSum += threat.Get(i);
            Assert.AreEqual(0f, threatSum, 0.0001f,
                "Pusu mevzisi tehdit alanina yazmamali.");

            float after = ShareOf(RunWave(), locked);

            Assert.AreEqual(before, after, 0.35f,
                "Pusu mevzisi ordunin rotasini degistirmemeli.");
        }
    }
}
