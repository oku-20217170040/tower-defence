using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class MapGeneratorTests
    {
        private static MapTemplate ThreeCorridorTemplate() => new MapTemplate
        {
            Width = 11,
            Height = 20,
            CorridorCount = 3,
            CorridorWidth = 2
        };

        [Test]
        public void Generate_ProducesGridOfTemplateSize()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 1);
            Assert.AreEqual(11, grid.Width);
            Assert.AreEqual(20, grid.Height);
        }

        [Test]
        public void Generate_IsDeterministicForSameSeed()
        {
            var a = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 42);
            var b = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 42);

            for (int i = 0; i < a.CellCount; i++)
                Assert.AreEqual(a.Get(i), b.Get(i), "Hucre " + i + " farkli.");
        }

        [Test]
        public void Generate_ProducesBuildStrips()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 7);

            int buildStrips = 0;
            for (int i = 0; i < grid.CellCount; i++)
                if (grid.Get(i) == CellKind.BuildStrip) buildStrips++;

            Assert.Greater(buildStrips, 0, "Kurulum seridi uretilmedi.");
        }

        [Test]
        public void Generate_RejectsInvalidTemplate()
        {
            var bad = new MapTemplate { Width = 0, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new MapGenerator().Generate(bad, seed: 1));
        }

        [Test]
        public void Validate_AcceptsGeneratedMap()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 3);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsTrue(result.IsValid, result.FailureReason);
            Assert.AreEqual(3, result.CorridorCount);
        }

        [Test]
        public void Validate_RejectsMapWithTooFewCorridors()
        {
            var template = ThreeCorridorTemplate();
            template.CorridorCount = 2;

            var grid = new MapGenerator().Generate(template, seed: 3);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("koridor", result.FailureReason);
        }

        [Test]
        public void Validate_RejectsMapWhenCostGapExceedsThreshold()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 3);
            // Esigi negatife cekince hicbir maliyet farki kabul edilmez
            var result = new MapValidator().Validate(grid, maxCostGap: -1f);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("maliyet", result.FailureReason);
        }

        [Test]
        public void Validate_ReportsCostGap_ForValidMap()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 11);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsTrue(result.IsValid, result.FailureReason);
            Assert.GreaterOrEqual(result.CostGap, 0f);
        }
    }
}
