using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class ThreatFieldTests
    {
        private static ColonyGrid NewGrid() => new ColonyGrid(9, 9);

        [Test]
        public void AddSource_CenterHasFullStrength()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int center = grid.Index(4, 4);

            threat.AddSource(center, radius: 2f, strength: 10f);

            Assert.AreEqual(10f, threat.Get(center), 0.001f);
        }

        [Test]
        public void AddSource_FallsOffWithDistance()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            // Yaricap 3: olculen iki hucre de (mesafe 1 ve 2) yaricapin ICINDE kalir.
            // Yaricap 2 secilseydi (6,4) tam sinirda olur ve dogrusal dususte sifira inerdi.
            threat.AddSource(grid.Index(4, 4), radius: 3f, strength: 10f);

            float near = threat.Get(grid.Index(5, 4));
            float far = threat.Get(grid.Index(6, 4));

            Assert.Greater(near, far);
            Assert.Greater(far, 0f);
        }

        [Test]
        public void AddSource_OutsideRadius_IsZero()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            threat.AddSource(grid.Index(4, 4), radius: 2f, strength: 10f);

            Assert.AreEqual(0f, threat.Get(grid.Index(8, 4)), 0.001f);
        }

        [Test]
        public void AddSource_MultipleSources_Accumulate()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int cell = grid.Index(4, 4);

            threat.AddSource(cell, radius: 1f, strength: 5f);
            threat.AddSource(cell, radius: 1f, strength: 3f);

            Assert.AreEqual(8f, threat.Get(cell), 0.001f);
        }

        [Test]
        public void Clear_RemovesAllThreat()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int cell = grid.Index(4, 4);

            threat.AddSource(cell, radius: 2f, strength: 10f);
            threat.Clear();

            Assert.AreEqual(0f, threat.Get(cell), 0.001f);
        }

        [Test]
        public void AddSource_NeverProducesNegativeValues()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            threat.AddSource(grid.Index(0, 0), radius: 3f, strength: 10f);

            for (int i = 0; i < grid.CellCount; i++)
                Assert.GreaterOrEqual(threat.Get(i), 0f);
        }

        [Test]
        public void AddSource_IgnoresNonPositiveRadiusOrStrength()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int cell = grid.Index(4, 4);

            threat.AddSource(cell, radius: 0f, strength: 10f);
            threat.AddSource(cell, radius: 2f, strength: 0f);

            Assert.AreEqual(0f, threat.Get(cell), 0.001f);
        }
    }
}
