using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class RouteCostModelTests
    {
        private ColonyGrid grid;
        private PheromoneMap pheromone;
        private LureField lure;
        private ThreatField threat;
        private RouteCostModel model;

        [SetUp]
        public void SetUp()
        {
            grid = new ColonyGrid(5, 5);
            pheromone = new PheromoneMap(grid, max: 100f, decayRate: 0.1f);
            lure = new LureField(grid, baseStrength: 10f, diminishFactor: 0.5f);
            threat = new ThreatField(grid);
            model = new RouteCostModel(pheromone, lure, threat)
            {
                ThreatWeight = 2f,
                AttractionWeight = 5f
            };
        }

        [Test]
        public void StepCost_EmptyCell_IsBasePlusFullAttractionPenalty()
        {
            // Cekim yok => 1 + 0 + (1 - 0) * 5 = 6
            Assert.AreEqual(6f, model.StepCost(0), 0.001f);
        }

        [Test]
        public void StepCost_MaxPheromone_RemovesAttractionPenalty()
        {
            pheromone.Deposit(0, 100f);
            Assert.AreEqual(1f, model.StepCost(0), 0.001f);
        }

        [Test]
        public void StepCost_NeverBelowOne()
        {
            pheromone.Deposit(0, 100f);
            for (int i = 0; i < 50; i++) lure.Apply(0);
            Assert.GreaterOrEqual(model.StepCost(0), 1f);
        }

        [Test]
        public void StepCost_ThreatIncreasesCost()
        {
            int cell = grid.Index(2, 2);
            float before = model.StepCost(cell);
            threat.AddSource(cell, radius: 1f, strength: 3f);
            float after = model.StepCost(cell);

            Assert.Greater(after, before);
            Assert.AreEqual(before + 6f, after, 0.001f); // 3 * ThreatWeight(2)
        }

        [Test]
        public void StepCost_LureCountsAsAttraction()
        {
            float before = model.StepCost(0);
            lure.Apply(0);
            float after = model.StepCost(0);
            Assert.Less(after, before);
        }

        [Test]
        public void StepCost_AttractionSaturates_DoesNotOvershoot()
        {
            pheromone.Deposit(3, 100f);
            lure.Apply(3);
            lure.Apply(3);
            Assert.AreEqual(1f, model.StepCost(3), 0.001f);
        }
    }
}
