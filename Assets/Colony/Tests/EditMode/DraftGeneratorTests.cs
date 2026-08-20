using System.Collections.Generic;
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class DraftGeneratorTests
    {
        private static DraftContext Context(int wave, params float[] shares)
        {
            return new DraftContext { Wave = wave, CorridorShares = shares };
        }

        [Test]
        public void Generate_ReturnsThreeOptions()
        {
            var options = DraftGenerator.Generate(Context(3, 0.33f, 0.34f, 0.33f), seed: 1);

            Assert.AreEqual(DraftGenerator.OptionCount, options.Count);
        }

        [Test]
        public void Generate_SameSeed_GivesSameOptions()
        {
            var context = Context(5, 0.2f, 0.5f, 0.3f);

            var first = DraftGenerator.Generate(context, seed: 42);
            var second = DraftGenerator.Generate(context, seed: 42);

            for (int i = 0; i < first.Count; i++)
                Assert.AreEqual(first[i].Id, second[i].Id);
        }

        [Test]
        public void Generate_DifferentSeeds_EventuallyDiffer()
        {
            var context = Context(5, 0.33f, 0.34f, 0.33f);
            var seen = new HashSet<string>();

            for (int seed = 0; seed < 30; seed++)
            {
                var options = DraftGenerator.Generate(context, seed);
                seen.Add(options[0].Id + "|" + options[1].Id + "|" + options[2].Id);
            }

            Assert.Greater(seen.Count, 1, "Farkli seed'ler ayni draft'i uretiyor.");
        }

        [Test]
        public void Generate_NeverRepeatsAnOption()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var options = DraftGenerator.Generate(Context(4, 0.6f, 0.2f, 0.2f), seed);
                var ids = new HashSet<string>();

                for (int i = 0; i < options.Count; i++)
                    Assert.IsTrue(ids.Add(options[i].Id), "Ayni secenek iki kez sunuldu.");
            }
        }

        [Test]
        public void Generate_MemorizedCorridor_AppearsMoreOften()
        {
            // Koloni sol koridoru ezberlemis: seceneklerin ona kaymasi gerekir
            // (spec 2.2). Draft'i siradan bir kart seciminden ayiran sey bu.
            var biased = Context(6, 0.8f, 0.1f, 0.1f);

            int left = 0;
            int right = 0;

            for (int seed = 0; seed < 500; seed++)
            {
                var options = DraftGenerator.Generate(biased, seed);
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].Kind != DraftOptionKind.FreeTower) continue;
                    if (options[i].Corridor == 0) left++;
                    if (options[i].Corridor == 2) right++;
                }
            }

            Assert.Greater(left, right * 2, "Ezberlenen koridor yeterince one cikmiyor.");
        }

        [Test]
        public void Generate_WithoutCorridorData_StillWorks()
        {
            var options = DraftGenerator.Generate(new DraftContext { Wave = 1 }, seed: 7);

            Assert.AreEqual(DraftGenerator.OptionCount, options.Count);
        }

        [Test]
        public void Generate_FoodGrowsWithWave()
        {
            int early = FoodAmount(wave: 1);
            int late = FoodAmount(wave: 15);

            Assert.Greater(late, early);
        }

        private static int FoodAmount(int wave)
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var options = DraftGenerator.Generate(Context(wave, 0.33f, 0.34f, 0.33f), seed);
                for (int i = 0; i < options.Count; i++)
                    if (options[i].Kind == DraftOptionKind.Food) return options[i].Amount;
            }

            Assert.Fail("Besin secenegi hic sunulmadi.");
            return 0;
        }

        [Test]
        public void CorridorPressure_NoPheromone_IsEven()
        {
            var template = new MapTemplate { Width = 11, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            var grid = new MapGenerator().Generate(template, seed: 1);
            var pheromone = new PheromoneMap(grid, 100f, 0.15f);

            var shares = CorridorPressure.Measure(grid, pheromone, template);

            Assert.AreEqual(3, shares.Length);
            for (int i = 0; i < shares.Length; i++)
                Assert.AreEqual(1f / 3f, shares[i], 0.001f);
        }

        [Test]
        public void CorridorPressure_TrailOnOneCorridor_DominatesIt()
        {
            var template = new MapTemplate { Width = 11, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            var grid = new MapGenerator().Generate(template, seed: 1);
            var pheromone = new PheromoneMap(grid, 100f, 0.15f);

            // Ilk koridorun sutunlarina iz birak.
            int gapSize = (template.Width - template.CorridorCount * template.CorridorWidth)
                          / (template.CorridorCount + 1);
            int x = gapSize;

            for (int y = 0; y < grid.Height; y++)
                pheromone.Deposit(grid.Index(x, y), 10f);

            var shares = CorridorPressure.Measure(grid, pheromone, template);

            Assert.Greater(shares[0], 0.9f);
            Assert.Less(shares[1], 0.05f);
            Assert.Less(shares[2], 0.05f);
        }
    }
}
