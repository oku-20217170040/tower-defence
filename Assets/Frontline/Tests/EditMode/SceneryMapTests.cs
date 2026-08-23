using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Yikilabilir ortu: kaya ve agaclarin cani vardir, vurulunca yikilir ve
    /// yerine cukur birakir. Cukur mevzi kurulabilir bir hucredir - haritayi
    /// oyuncunun degistirebilmesinin tek yolu budur.
    /// </summary>
    public class SceneryMapTests
    {
        private static BattleGrid EmptyGrid()
        {
            var grid = new BattleGrid(5, 5);
            for (int i = 0; i < grid.CellCount; i++)
                grid.Set(i, CellKind.BuildStrip);
            return grid;
        }

        [Test]
        public void Place_MarksCellAsScenery()
        {
            var grid = EmptyGrid();
            var scenery = new SceneryMap(grid);
            int cell = grid.Index(2, 2);

            scenery.Place(cell, SceneryKind.Rock, 30f);

            Assert.AreEqual(CellKind.Scenery, grid.Get(cell));
            Assert.AreEqual(SceneryKind.Rock, scenery.KindAt(cell));
            Assert.AreEqual(30f, scenery.HpAt(cell), 0.001f);
            Assert.IsFalse(grid.IsWalkable(cell), "Ayakta ortu gecilebilir olmamali.");
        }

        [Test]
        public void Place_RefusesCorridorCell()
        {
            var grid = EmptyGrid();
            int cell = grid.Index(2, 2);
            grid.Set(cell, CellKind.Corridor);

            var scenery = new SceneryMap(grid);

            // Koridora ortu koymak yolu tikar; harita cozulemez hale gelir.
            Assert.Throws<System.InvalidOperationException>(
                () => scenery.Place(cell, SceneryKind.Tree, 10f));
        }

        [Test]
        public void Damage_BelowHealth_LeavesSceneryStanding()
        {
            var grid = EmptyGrid();
            var scenery = new SceneryMap(grid);
            int cell = grid.Index(1, 1);
            scenery.Place(cell, SceneryKind.Rock, 30f);

            bool destroyed = scenery.Damage(cell, 10f);

            Assert.IsFalse(destroyed);
            Assert.AreEqual(20f, scenery.HpAt(cell), 0.001f);
            Assert.AreEqual(CellKind.Scenery, grid.Get(cell));
            Assert.AreEqual(2f / 3f, scenery.HealthFraction(cell), 0.01f);
        }

        [Test]
        public void Damage_DepletingHealth_LeavesBuildableCrater()
        {
            var grid = EmptyGrid();
            var scenery = new SceneryMap(grid);
            int cell = grid.Index(1, 1);
            scenery.Place(cell, SceneryKind.Tree, 12f);

            int clearedCell = -1;
            scenery.Cleared += c => clearedCell = c;

            bool destroyed = scenery.Damage(cell, 12f);

            Assert.IsTrue(destroyed, "Can bitince ortu yikilmali.");
            Assert.AreEqual(cell, clearedCell, "Cleared olayi tetiklenmedi.");
            Assert.AreEqual(CellKind.BuildStrip, grid.Get(cell),
                "Yikilan ortunun yerinde kule kurulabilir cukur olmali.");
            Assert.IsFalse(scenery.IsStanding(cell));
            CollectionAssert.DoesNotContain(scenery.Occupied, cell);
        }

        [Test]
        public void Damage_OnEmptyCell_IsHarmless()
        {
            var grid = EmptyGrid();
            var scenery = new SceneryMap(grid);

            // Mermiler her yere duser; bos hucreye hasar sessizce yutulmali.
            Assert.IsFalse(scenery.Damage(grid.Index(0, 0), 99f));
            Assert.IsFalse(scenery.Damage(-1, 99f));
            Assert.IsFalse(scenery.Damage(grid.CellCount + 5, 99f));
        }

        [Test]
        public void Damage_TwiceAfterDestruction_DoesNotResurrect()
        {
            var grid = EmptyGrid();
            var scenery = new SceneryMap(grid);
            int cell = grid.Index(3, 3);
            scenery.Place(cell, SceneryKind.Rock, 5f);

            scenery.Damage(cell, 5f);
            bool again = scenery.Damage(cell, 5f);

            Assert.IsFalse(again);
            Assert.AreEqual(CellKind.BuildStrip, grid.Get(cell));
        }

        [Test]
        public void BuildScenery_GivesRocksMoreHealthThanTrees()
        {
            var template = new MapTemplate
            {
                Width = 13,
                Height = 30,
                CorridorCount = 3,
                CorridorWidth = 2,
                RungCount = 4,
                SceneryPercent = 30
            };

            var generator = new MapGenerator();
            var grid = generator.Generate(template, seed: 9);
            var scenery = generator.BuildScenery(grid, seed: 9, treeHealth: 12f, rockHealth: 30f);

            Assert.Greater(scenery.Occupied.Count, 0, "Haritada hic ortu uretilmedi.");

            for (int i = 0; i < scenery.Occupied.Count; i++)
            {
                int cell = scenery.Occupied[i];
                float expected = scenery.KindAt(cell) == SceneryKind.Rock ? 30f : 12f;
                Assert.AreEqual(expected, scenery.MaxHpAt(cell), 0.001f);
                Assert.AreEqual(CellKind.Scenery, grid.Get(cell));
            }
        }

        [Test]
        public void BuildScenery_IsDeterministicForSameSeed()
        {
            var template = new MapTemplate
            {
                Width = 13, Height = 30, CorridorCount = 3,
                CorridorWidth = 2, RungCount = 4, SceneryPercent = 30
            };

            var generator = new MapGenerator();

            var gridA = generator.Generate(template, seed: 4);
            var a = generator.BuildScenery(gridA, 4, 12f, 30f);
            var gridB = generator.Generate(template, seed: 4);
            var b = generator.BuildScenery(gridB, 4, 12f, 30f);

            Assert.AreEqual(a.Occupied.Count, b.Occupied.Count);
            for (int i = 0; i < a.Occupied.Count; i++)
            {
                Assert.AreEqual(a.Occupied[i], b.Occupied[i]);
                Assert.AreEqual(a.KindAt(a.Occupied[i]), b.KindAt(b.Occupied[i]));
            }
        }
    }
}
