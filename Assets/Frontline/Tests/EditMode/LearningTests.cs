using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Oyunun temel iddiasini belirlenimci olarak dogrular:
    /// "Birlikler dalgalar boyunca bir koridoru ogrenir ve surun oraya toplanir."
    ///
    /// Bu testler goz ile degil olcumle dogrular. Gercek zamanli oynanis
    /// gerektirmedikleri icin her calistirmada, saniyeler icinde kosarlar.
    /// UnitAgent'in yaptigi isi birebir taklit ederler; sabitler oradan alinmistir.
    /// </summary>
    public class LearningTests
    {
        // UnitAgent ve UnitSpawner ile ayni degerler.
        private const float DepositPerCell = 0.5f;
        private const float ReinforceOnSuccess = 1.5f;
        private const int UnitsPerWave = 12;

        /// <summary>
        /// Olcumle secildi. 0 iken tum birlikler ayni koridoru secer ve ogrenme
        /// diye bir sey olmaz; 0.5 iken ordu hicbir zaman karar veremez.
        /// 0.35 dagilarak baslayip 6-8 dalgada yerlesen egriyi verir.
        /// </summary>
        private const float ExplorationNoise = 0.35f;

        private BattleGrid grid;
        private ReconMap recon;
        private DecoyField decoy;
        private ThreatField threat;
        private RouteCostModel model;
        private RoutePathfinder finder;

        private int baseCell;
        private List<int> spawnCells;
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

            var validation = new MapValidator().Validate(grid, maxCostGap: 10f);
            Assert.IsTrue(validation.IsValid, validation.FaidecoyReason);

            recon = new ReconMap(grid, max: 100f, decayRate: 0.15f);
            decoy = new DecoyField(grid, baseStrength: 25f, diminishFactor: 0.5f);
            threat = new ThreatField(grid);
            model = new RouteCostModel(recon, decoy, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 6f,
                ExplorationNoise = ExplorationNoise
            };
            finder = new RoutePathfinder(grid, model);

            baseCell = grid.Index(grid.Width / 2, 0);
            Assert.IsTrue(grid.IsWalkable(baseCell), "Us hucresi koridor olmali.");

            spawnCells = new List<int>();
            int topRow = grid.Height - 1;
            for (int x = 0; x < grid.Width; x++)
            {
                int cell = grid.Index(x, topRow);
                if (grid.IsWalkable(cell)) spawnCells.Add(cell);
            }
            Assert.Greater(spawnCells.Count, 0, "Giris hucresi yok.");

            random = new System.Random(1234);
        }

        /// <summary>
        /// Bir dalga kosar ve her koridordan kac birligin gectigini doner.
        /// Indeks = koridor sirasi (soldan saga).
        /// </summary>
        private int[] RunWave(List<int[]> corridorColumns)
        {
            var counts = new int[corridorColumns.Count];
            var path = new List<int>();
            int midRow = grid.Height / 2;

            for (int b = 0; b < UnitsPerWave; b++)
            {
                model.BeginQuery(random.Next(), grid);

                int spawn = spawnCells[random.Next(spawnCells.Count)];
                if (!finder.FindPath(spawn, baseCell, path)) continue;

                // UnitAgent: gecerken her hucreye birakir, varinca rotayi pekistirir.
                recon.DepositTrail(path, DepositPerCell);
                recon.DepositTrail(path, ReinforceOnSuccess);

                // Bu birlik orta yukseklikte hangi koridordan gecti?
                for (int c = 0; c < corridorColumns.Count; c++)
                {
                    bool used = false;
                    for (int k = 0; k < corridorColumns[c].Length; k++)
                    {
                        if (path.Contains(grid.Index(corridorColumns[c][k], midRow)))
                        {
                            used = true;
                            break;
                        }
                    }
                    if (used) { counts[c]++; break; }
                }
            }

            recon.Decay();
            return counts;
        }

        /// <summary>Orta yukseklikte koridor olan sutunlari bitisiklerine gore gruplar.</summary>
        private List<int[]> FindCorridorColumns()
        {
            var groups = new List<int[]>();
            var current = new List<int>();
            int midRow = grid.Height / 2;

            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.IsWalkable(grid.Index(x, midRow)))
                {
                    current.Add(x);
                }
                else if (current.Count > 0)
                {
                    groups.Add(current.ToArray());
                    current = new List<int>();
                }
            }
            if (current.Count > 0) groups.Add(current.ToArray());

            return groups;
        }

        private static float MaxShare(int[] counts)
        {
            int total = 0;
            int max = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                total += counts[i];
                if (counts[i] > max) max = counts[i];
            }
            return total == 0 ? 0f : (float)max / total;
        }

        private static int UsedCorridorCount(int[] counts)
        {
            int used = 0;
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > 0) used++;
            return used;
        }

        [Test]
        public void Map_HasThreeCorridorsAtMidHeight()
        {
            var corridors = FindCorridorColumns();
            Assert.AreEqual(3, corridors.Count,
                "Orta yukseklikte 3 ayrik koridor bekleniyordu.");
        }

        [Test]
        public void Army_ConvergesOnASingleCorridorOverWaves()
        {
            var corridors = FindCorridorColumns();

            int[] firstWave = RunWave(corridors);

            int[] lastWave = firstWave;
            for (int w = 0; w < 11; w++)
                lastWave = RunWave(corridors);

            float firstShare = MaxShare(firstWave);
            float lastShare = MaxShare(lastWave);

            // Ilk dalga dagilmis olmali; aksi halde yakinsama olcumu anlamsizdir.
            Assert.GreaterOrEqual(UsedCorridorCount(firstWave), 2,
                "Ilk dalgada suru zaten tek koridordaysa ogrenme olculemez.");

            // Son dalgada suru tek koridorda toplanmali - oyunun temel iddiasi budur.
            Assert.GreaterOrEqual(lastShare, 0.9f,
                "Suru yakinsamadi. Ilk dalga payi=" + firstShare +
                " son dalga payi=" + lastShare);

            Assert.Greater(lastShare, firstShare, "Yogunlasma artmadi.");
        }

        [Test]
        public void ChosenCorridor_HasTheStrongestTrail()
        {
            var corridors = FindCorridorColumns();
            for (int w = 0; w < 12; w++) RunWave(corridors);

            int midRow = grid.Height / 2;
            float best = -1f;
            int bestCorridor = -1;

            for (int c = 0; c < corridors.Count; c++)
            {
                float sum = 0f;
                for (int k = 0; k < corridors[c].Length; k++)
                    sum += recon.Get(grid.Index(corridors[c][k], midRow));

                if (sum > best) { best = sum; bestCorridor = c; }
            }

            Assert.GreaterOrEqual(bestCorridor, 0);
            Assert.Greater(best, 0f, "Hicbir koridorda iz olusmadi.");

            // Secilen koridorun izi digerlerinden belirgin sekilde guclu olmali;
            // aksi halde ekranda hangisinin ogrenildigi okunamaz (spec 9.1).
            for (int c = 0; c < corridors.Count; c++)
            {
                if (c == bestCorridor) continue;

                float sum = 0f;
                for (int k = 0; k < corridors[c].Length; k++)
                    sum += recon.Get(grid.Index(corridors[c][k], midRow));

                Assert.Less(sum, best * 0.5f,
                    "Koridor " + c + " izi secilen koridora fazla yakin; ayirt edilemez.");
            }
        }

        [Test]
        public void Decay_ErasesTrailsWhenArmyStopsUsingThem()
        {
            var corridors = FindCorridorColumns();
            for (int w = 0; w < 5; w++) RunWave(corridors);

            int midRow = grid.Height / 2;
            float beforeIdle = 0f;
            for (int i = 0; i < grid.CellCount; i++) beforeIdle += recon.Get(i);
            Assert.Greater(beforeIdle, 0f);

            // Ordu durur: birlik yok, yalnizca sonumleme calisir.
            for (int i = 0; i < 40; i++) recon.Decay();

            float afterIdle = 0f;
            for (int i = 0; i < grid.CellCount; i++) afterIdle += recon.Get(i);

            Assert.Less(afterIdle, beforeIdle * 0.01f,
                "Kullanilmayan izler unutulmuyor.");
        }
    }
}
