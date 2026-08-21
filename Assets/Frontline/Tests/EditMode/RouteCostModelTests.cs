using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    public class RouteCostModelTests
    {
        private BattleGrid grid;
        private ReconMap recon;
        private DecoyField decoy;
        private ThreatField threat;
        private RouteCostModel model;

        [SetUp]
        public void SetUp()
        {
            grid = new BattleGrid(5, 5);
            recon = new ReconMap(grid, max: 100f, decayRate: 0.1f);
            decoy = new DecoyField(grid, baseStrength: 10f, diminishFactor: 0.5f);
            threat = new ThreatField(grid);
            model = new RouteCostModel(recon, decoy, threat)
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
        public void StepCost_MaxRecon_RemovesAttractionPenalty()
        {
            recon.Deposit(0, 100f);
            Assert.AreEqual(1f, model.StepCost(0), 0.001f);
        }

        [Test]
        public void StepCost_NeverBelowOne()
        {
            recon.Deposit(0, 100f);
            for (int i = 0; i < 50; i++) decoy.Apply(0);
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
        public void StepCost_DecoyCountsAsAttraction()
        {
            float before = model.StepCost(0);
            decoy.Apply(0);
            float after = model.StepCost(0);
            Assert.Less(after, before);
        }

        [Test]
        public void StepCost_AttractionSaturates_DoesNotOvershoot()
        {
            recon.Deposit(3, 100f);
            decoy.Apply(3);
            decoy.Apply(3);
            Assert.AreEqual(1f, model.StepCost(3), 0.001f);
        }
    }
}
