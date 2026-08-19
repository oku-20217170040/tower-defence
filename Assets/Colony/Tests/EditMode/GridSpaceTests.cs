using NUnit.Framework;
using UnityEngine;
using Colony.Core;
using Colony.Runtime;

namespace Colony.Tests
{
    public class GridSpaceTests
    {
        private const float Tolerance = 0.0001f;

        /// <summary>Dikey tutus icin tipik izgara: dar eksen 11, uzun eksen 20.</summary>
        private static ColonyGrid PortraitGrid() => new ColonyGrid(11, 20);

        [Test]
        public void Grid_IsCenteredOnWorldOrigin()
        {
            var grid = PortraitGrid();
            var space = new GridSpace(grid, cellSize: 1f);

            // Sol-alt kosenin merkezi ile sag-ust kosenin merkezi
            // dunya orijinine gore simetrik olmalidir.
            Vector2 bottomLeft = space.CellToWorld(grid.Index(0, 0));
            Vector2 topRight = space.CellToWorld(grid.Index(grid.Width - 1, grid.Height - 1));

            Assert.AreEqual(0f, bottomLeft.x + topRight.x, Tolerance);
            Assert.AreEqual(0f, bottomLeft.y + topRight.y, Tolerance);
        }

        [Test]
        public void CellToWorld_RespectsCellSize()
        {
            var grid = PortraitGrid();
            var space = new GridSpace(grid, cellSize: 2f);

            Vector2 a = space.CellToWorld(grid.Index(3, 5));
            Vector2 b = space.CellToWorld(grid.Index(4, 5));

            Assert.AreEqual(2f, b.x - a.x, Tolerance);
            Assert.AreEqual(0f, b.y - a.y, Tolerance);
        }

        [Test]
        public void CellToWorld_YIncreasesUpward()
        {
            // Spec 3.6: y = 0 altta (yuva), y = Height-1 ustte (giris).
            var grid = PortraitGrid();
            var space = new GridSpace(grid, cellSize: 1f);

            Vector2 nest = space.CellToWorld(grid.Index(5, 0));
            Vector2 entrance = space.CellToWorld(grid.Index(5, grid.Height - 1));

            Assert.Less(nest.y, entrance.y);
        }

        [Test]
        public void WorldToCell_RoundTripsForEveryCell()
        {
            var grid = PortraitGrid();
            var space = new GridSpace(grid, cellSize: 1.5f);

            for (int i = 0; i < grid.CellCount; i++)
            {
                Vector2 world = space.CellToWorld(i);
                int roundTripped;

                Assert.IsTrue(space.WorldToCell(world, out roundTripped),
                    "Hucre " + i + " dunya konumundan geri cevrilemedi.");
                Assert.AreEqual(i, roundTripped, "Hucre " + i + " gidis-donusu bozuk.");
            }
        }

        [Test]
        public void WorldToCell_ReturnsFalseOutsideGrid()
        {
            var grid = PortraitGrid();
            var space = new GridSpace(grid, cellSize: 1f);
            int cell;

            Assert.IsFalse(space.WorldToCell(new Vector2(1000f, 0f), out cell));
            Assert.IsFalse(space.WorldToCell(new Vector2(0f, -1000f), out cell));
        }

        [Test]
        public void RequiredOrthographicSize_FitsTallGridOnVerticalAxis()
        {
            // 11x20 izgara, 9:16 dikey ekran. Uzun eksen sinirlayicidir.
            var grid = PortraitGrid();
            var space = new GridSpace(grid, cellSize: 1f);

            float size = space.RequiredOrthographicSize(9f / 16f);

            Assert.AreEqual(10f, size, Tolerance);
        }

        [Test]
        public void RequiredOrthographicSize_FitsWideGridOnHorizontalAxis()
        {
            // Genis izgarada dar eksen sinirlayici olur; kamera buyumelidir.
            var grid = new ColonyGrid(40, 10);
            var space = new GridSpace(grid, cellSize: 1f);

            float aspect = 9f / 16f;
            float size = space.RequiredOrthographicSize(aspect);

            // Yatayda tasma olmamali: size * aspect >= genislik / 2
            Assert.GreaterOrEqual(size * aspect, 20f - Tolerance);
            Assert.Greater(size, 5f);
        }

        [Test]
        public void Constructor_RejectsNonPositiveCellSize()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new GridSpace(PortraitGrid(), cellSize: 0f));
        }
    }
}
