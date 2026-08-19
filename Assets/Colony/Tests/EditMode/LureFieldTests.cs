using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class LureFieldTests
    {
        private static ColonyGrid NewGrid() => new ColonyGrid(5, 5);

        [Test]
        public void Apply_AddsBaseStrength_FirstTime()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            Assert.AreEqual(10f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Apply_SecondTime_AddsDiminishedAmount()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            // 10 + (10 * 0.5) = 15
            Assert.AreEqual(15f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Apply_ThirdTime_KeepsDiminishing()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            lure.Apply(4);
            // 10 + 5 + 2.5 = 17.5
            Assert.AreEqual(17.5f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Apply_ManyTimes_ConvergesToFiniteCeiling()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            for (int i = 0; i < 200; i++) lure.Apply(4);
            // Geometrik seri limiti: 10 / (1 - 0.5) = 20
            Assert.Less(lure.Get(4), 20.001f);
            Assert.Greater(lure.Get(4), 19.9f);
        }

        [Test]
        public void Apply_IsPerCell_NotGlobal()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            lure.Apply(7);
            Assert.AreEqual(10f, lure.Get(7), 0.001f);
        }

        [Test]
        public void Clear_ResetsValuesAndCounts()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            lure.Clear();

            Assert.AreEqual(0f, lure.Get(4), 0.001f);
            Assert.AreEqual(0, lure.ApplyCount(4));

            lure.Apply(4);
            Assert.AreEqual(10f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Constructor_RejectsInvalidParameters()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new LureField(NewGrid(), baseStrength: -1f, diminishFactor: 0.5f));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 1f));
        }
    }
}
