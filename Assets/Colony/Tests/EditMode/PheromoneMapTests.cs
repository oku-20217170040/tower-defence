using System.Collections.Generic;
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class PheromoneMapTests
    {
        private static ColonyGrid NewGrid() => new ColonyGrid(5, 5);

        [Test]
        public void Deposit_IncreasesValue()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.Deposit(3, 10f);
            Assert.AreEqual(10f, map.Get(3), 0.001f);
        }

        [Test]
        public void Deposit_ClampsAtMax()
        {
            var map = new PheromoneMap(NewGrid(), max: 20f, decayRate: 0.1f);
            map.Deposit(3, 500f);
            Assert.AreEqual(20f, map.Get(3), 0.001f);
        }

        [Test]
        public void Weaken_NeverGoesBelowZero()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.Deposit(3, 5f);
            map.Weaken(3, 50f);
            Assert.AreEqual(0f, map.Get(3), 0.001f);
        }

        [Test]
        public void DepositTrail_AffectsEveryCellOnPath()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.DepositTrail(new List<int> { 1, 2, 3 }, 7f);

            Assert.AreEqual(7f, map.Get(1), 0.001f);
            Assert.AreEqual(7f, map.Get(2), 0.001f);
            Assert.AreEqual(7f, map.Get(3), 0.001f);
            Assert.AreEqual(0f, map.Get(4), 0.001f);
        }

        [Test]
        public void WeakenTrail_ReducesEveryCellOnPath()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.DepositTrail(new List<int> { 1, 2 }, 10f);
            map.WeakenTrail(new List<int> { 1, 2 }, 4f);

            Assert.AreEqual(6f, map.Get(1), 0.001f);
            Assert.AreEqual(6f, map.Get(2), 0.001f);
        }

        [Test]
        public void Decay_ReducesAllValuesProportionally()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.25f);
            map.Deposit(0, 100f);
            map.Decay();
            Assert.AreEqual(75f, map.Get(0), 0.001f);
        }

        [Test]
        public void Decay_RepeatedlyApproachesZero_NeverNegative()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.5f);
            map.Deposit(0, 100f);
            for (int i = 0; i < 50; i++) map.Decay();

            Assert.GreaterOrEqual(map.Get(0), 0f);
            Assert.Less(map.Get(0), 0.001f);
        }

        [Test]
        public void Constructor_RejectsInvalidParameters()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new PheromoneMap(NewGrid(), max: 0f, decayRate: 0.1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new PheromoneMap(NewGrid(), max: 10f, decayRate: 1.5f));
        }
    }
}
