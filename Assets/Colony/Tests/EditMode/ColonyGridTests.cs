using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class ColonyGridTests
    {
        [Test]
        public void Index_And_Coordinates_RoundTrip()
        {
            var grid = new ColonyGrid(11, 20);
            int index = grid.Index(7, 13);
            Assert.AreEqual(7, grid.X(index));
            Assert.AreEqual(13, grid.Y(index));
        }

        [Test]
        public void NewGrid_AllCellsBlocked()
        {
            var grid = new ColonyGrid(4, 4);
            for (int i = 0; i < grid.CellCount; i++)
                Assert.AreEqual(CellKind.Blocked, grid.Get(i));
        }

        [Test]
        public void IsWalkable_OnlyCorridorCounts()
        {
            var grid = new ColonyGrid(3, 3);
            grid.Set(grid.Index(1, 1), CellKind.Corridor);
            grid.Set(grid.Index(2, 2), CellKind.BuildStrip);

            Assert.IsTrue(grid.IsWalkable(grid.Index(1, 1)));
            Assert.IsFalse(grid.IsWalkable(grid.Index(2, 2)));
            Assert.IsFalse(grid.IsWalkable(grid.Index(0, 0)));
        }

        [Test]
        public void GetNeighbors_CornerCell_ReturnsTwo()
        {
            var grid = new ColonyGrid(5, 5);
            var buffer = new int[4];
            Assert.AreEqual(2, grid.GetNeighbors(grid.Index(0, 0), buffer));
        }

        [Test]
        public void GetNeighbors_CenterCell_ReturnsFour()
        {
            var grid = new ColonyGrid(5, 5);
            var buffer = new int[4];
            Assert.AreEqual(4, grid.GetNeighbors(grid.Index(2, 2), buffer));
        }

        [Test]
        public void Constructor_RejectsNonPositiveSize()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ColonyGrid(0, 5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ColonyGrid(5, -1));
        }
    }
}
