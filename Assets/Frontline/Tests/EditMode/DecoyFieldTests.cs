using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    public class DecoyFieldTests
    {
        private static BattleGrid NewGrid() => new BattleGrid(5, 5);

        [Test]
        public void Apply_AddsBaseStrength_FirstTime()
        {
            var decoy = new DecoyField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            decoy.Apply(4);
            Assert.AreEqual(10f, decoy.Get(4), 0.001f);
        }

        [Test]
        public void Apply_SecondTime_AddsDiminishedAmount()
        {
            var decoy = new DecoyField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            decoy.Apply(4);
            decoy.Apply(4);
            // 10 + (10 * 0.5) = 15
            Assert.AreEqual(15f, decoy.Get(4), 0.001f);
        }

        [Test]
        public void Apply_ThirdTime_KeepsDiminishing()
        {
            var decoy = new DecoyField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            decoy.Apply(4);
            decoy.Apply(4);
            decoy.Apply(4);
            // 10 + 5 + 2.5 = 17.5
            Assert.AreEqual(17.5f, decoy.Get(4), 0.001f);
        }

        [Test]
        public void Apply_ManyTimes_ConvergesToFiniteCeiling()
        {
            var decoy = new DecoyField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            for (int i = 0; i < 200; i++) decoy.Apply(4);
            // Geometrik seri limiti: 10 / (1 - 0.5) = 20
            Assert.Less(decoy.Get(4), 20.001f);
            Assert.Greater(decoy.Get(4), 19.9f);
        }

        [Test]
        public void Apply_IsPerCell_NotGlobal()
        {
            var decoy = new DecoyField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            decoy.Apply(4);
            decoy.Apply(4);
            decoy.Apply(7);
            Assert.AreEqual(10f, decoy.Get(7), 0.001f);
        }

        [Test]
        public void Clear_ResetsValuesAndCounts()
        {
            var decoy = new DecoyField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            decoy.Apply(4);
            decoy.Apply(4);
            decoy.Clear();

            Assert.AreEqual(0f, decoy.Get(4), 0.001f);
            Assert.AreEqual(0, decoy.ApplyCount(4));

            decoy.Apply(4);
            Assert.AreEqual(10f, decoy.Get(4), 0.001f);
        }

        [Test]
        public void Constructor_RejectsInvalidParameters()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new DecoyField(NewGrid(), baseStrength: -1f, diminishFactor: 0.5f));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new DecoyField(NewGrid(), baseStrength: 10f, diminishFactor: 1f));
        }
    }
}
