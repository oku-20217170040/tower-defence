using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    public class RoutePathfinderTests
    {
        private BattleGrid grid;
        private ReconMap recon;
        private DecoyField decoy;
        private ThreatField threat;
        private RouteCostModel model;
        private RoutePathfinder finder;
        private List<int> path;

        /// <summary>
        /// 5x5 harita. Iki dikey koridor: x=0 ve x=4.
        /// Ust satir (y=4) ve alt satir (y=0) bastan sona koridordur,
        /// boylece iki koridor birbirine baglanir ve gercek bir secim dogar.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            grid = new BattleGrid(5, 5);

            for (int y = 0; y < 5; y++)
            {
                grid.Set(grid.Index(0, y), CellKind.Corridor);
                grid.Set(grid.Index(4, y), CellKind.Corridor);
            }
            for (int x = 0; x < 5; x++)
            {
                grid.Set(grid.Index(x, 4), CellKind.Corridor);
                grid.Set(grid.Index(x, 0), CellKind.Corridor);
            }

            recon = new ReconMap(grid, max: 100f, decayRate: 0.1f);
            decoy = new DecoyField(grid, baseStrength: 10f, diminishFactor: 0.5f);
            threat = new ThreatField(grid);
            model = new RouteCostModel(recon, decoy, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 4f
            };
            finder = new RoutePathfinder(grid, model);
            path = new List<int>();
        }

        [Test]
        public void FindPath_ReturnsTrue_ForConnectedCells()
        {
            Assert.IsTrue(finder.FindPath(grid.Index(0, 4), grid.Index(0, 0), path));
            Assert.Greater(path.Count, 0);
        }

        [Test]
        public void FindPath_StartsAtStart_EndsAtGoal()
        {
            int start = grid.Index(0, 4);
            int goal = grid.Index(4, 0);
            finder.FindPath(start, goal, path);

            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
        }

        [Test]
        public void FindPath_ReturnsFalse_WhenGoalUnreachable()
        {
            var isolated = new BattleGrid(5, 5);
            isolated.Set(isolated.Index(0, 0), CellKind.Corridor);
            isolated.Set(isolated.Index(4, 4), CellKind.Corridor);

            var f = new RoutePathfinder(isolated,
                new RouteCostModel(
                    new ReconMap(isolated, 100f, 0.1f),
                    new DecoyField(isolated, 10f, 0.5f),
                    new ThreatField(isolated)));

            Assert.IsFalse(f.FindPath(isolated.Index(0, 0), isolated.Index(4, 4), path));
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void FindPath_ReturnsFalse_WhenStartNotWalkable()
        {
            Assert.IsFalse(finder.FindPath(grid.Index(2, 2), grid.Index(0, 0), path));
        }

        [Test]
        public void FindPath_PrefersCorridorWithRecon()
        {
            int start = grid.Index(2, 4);
            int goal = grid.Index(2, 0);

            for (int y = 0; y < 5; y++)
                recon.Deposit(grid.Index(4, y), 100f);

            finder.FindPath(start, goal, path);

            Assert.IsTrue(path.Contains(grid.Index(4, 2)), "Kesif izilu sag koridor tercih edilmeliydi.");
            Assert.IsFalse(path.Contains(grid.Index(0, 2)));
        }

        [Test]
        public void FindPath_AvoidsCorridorWithThreat()
        {
            int start = grid.Index(2, 4);
            int goal = grid.Index(2, 0);

            for (int y = 0; y < 5; y++)
                threat.AddSource(grid.Index(0, y), radius: 1f, strength: 20f);

            finder.FindPath(start, goal, path);

            Assert.IsFalse(path.Contains(grid.Index(0, 2)), "Tehditli sol koridordan kacinilmaliydi.");
        }

        [Test]
        public void FindPath_DecoyOverridesWeakRecon()
        {
            int start = grid.Index(2, 4);
            int goal = grid.Index(2, 0);

            for (int y = 0; y < 5; y++)
                recon.Deposit(grid.Index(0, y), 5f);

            for (int y = 0; y < 5; y++)
                for (int i = 0; i < 4; i++)
                    decoy.Apply(grid.Index(4, y));

            finder.FindPath(start, goal, path);

            Assert.IsTrue(path.Contains(grid.Index(4, 2)), "Yem, zayif kesif iziu yenmeliydi.");
        }

        [Test]
        public void FindPath_SameStartAndGoal_ReturnsSingleCell()
        {
            int cell = grid.Index(0, 0);
            Assert.IsTrue(finder.FindPath(cell, cell, path));
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(cell, path[0]);
        }
    }
}
