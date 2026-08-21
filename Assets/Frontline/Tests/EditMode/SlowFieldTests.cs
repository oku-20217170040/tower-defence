using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    public class SlowFieldTests
    {
        private BattleGrid grid;
        private SlowField slow;

        [SetUp]
        public void SetUp()
        {
            grid = new BattleGrid(11, 20);
            slow = new SlowField(grid);
        }

        [Test]
        public void NewField_HasNoSlowdownAnywhere()
        {
            for (int i = 0; i < grid.CellCount; i++)
                Assert.AreEqual(SlowField.None, slow.Get(i));
        }

        [Test]
        public void AddSource_SlowsCellsInsideRadius()
        {
            int center = grid.Index(5, 10);
            slow.AddSource(center, radius: 2f, factor: 2f);

            Assert.AreEqual(2f, slow.Get(center));
            Assert.AreEqual(2f, slow.Get(grid.Index(5, 12)));
            Assert.AreEqual(2f, slow.Get(grid.Index(7, 10)));
        }

        [Test]
        public void AddSource_LeavesCellsOutsideRadiusUntouched()
        {
            slow.AddSource(grid.Index(5, 10), radius: 2f, factor: 2f);

            Assert.AreEqual(SlowField.None, slow.Get(grid.Index(5, 14)));
            Assert.AreEqual(SlowField.None, slow.Get(grid.Index(0, 0)));
        }

        [Test]
        public void AddSource_EffectIsFlatInsideRadius()
        {
            // Menzil icinde etki mesafeyle azalmaz. Azalsaydi mayin tarlasinin nereye
            // konuldugu bulanik bir karar olurdu.
            int center = grid.Index(5, 10);
            slow.AddSource(center, radius: 3f, factor: 2f);

            Assert.AreEqual(slow.Get(center), slow.Get(grid.Index(5, 13)));
        }

        [Test]
        public void OverlappingSources_TakeTheStrongestNotTheProduct()
        {
            // Carpilsaydi iki mayin tarlasiyi ayni yere yigmak baskin strateji olurdu.
            int center = grid.Index(5, 10);
            slow.AddSource(center, radius: 2f, factor: 2f);
            slow.AddSource(center, radius: 2f, factor: 3f);
            slow.AddSource(center, radius: 2f, factor: 1.5f);

            Assert.AreEqual(3f, slow.Get(center));
        }

        [Test]
        public void AddSource_IgnoresFactorsThatDoNotSlow()
        {
            slow.AddSource(grid.Index(5, 10), radius: 2f, factor: 1f);
            slow.AddSource(grid.Index(5, 10), radius: 2f, factor: 0.5f);

            Assert.AreEqual(SlowField.None, slow.Get(grid.Index(5, 10)));
        }

        [Test]
        public void Clear_RemovesAllSlowdown()
        {
            slow.AddSource(grid.Index(5, 10), radius: 3f, factor: 2f);
            slow.Clear();

            for (int i = 0; i < grid.CellCount; i++)
                Assert.AreEqual(SlowField.None, slow.Get(i));
        }
    }
}
