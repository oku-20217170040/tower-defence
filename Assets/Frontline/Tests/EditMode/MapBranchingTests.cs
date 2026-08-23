using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Yol cesitliligi: birlik girise adim attigi anda yolunu secmis olmamali.
    /// Yatay gecitler yol ortasinda catallanma acar; oyuncunun tehdit ve yem
    /// ile yaptigi yonlendirme ancak boyle bir karar haline gelir.
    /// </summary>
    public class MapBranchingTests
    {
        private static MapTemplate WideTemplate() => new MapTemplate
        {
            Width = 13,
            Height = 30,
            CorridorCount = 3,
            CorridorWidth = 2,
            RungCount = 4,
            SceneryPercent = 30
        };

        [Test]
        public void Rungs_CreateMidRouteJunctions()
        {
            var grid = new MapGenerator().Generate(WideTemplate(), seed: 21);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsTrue(result.IsValid, result.FaidecoyReason);
            Assert.GreaterOrEqual(result.JunctionCount, MapValidator.MinimumJunctions,
                "Yol ortasinda catallanma yok: koridor secimi girise sikismis.");
        }

        [Test]
        public void NoRungs_MapIsRejected()
        {
            // Gecitsiz harita gecerli SAYILMAMALI: koridorlar bagimsiz dikey
            // borulardir ve secim yanilsamaya doner.
            var template = WideTemplate();
            template.RungCount = 0;

            var grid = new MapGenerator().Generate(template, seed: 21);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("catallanma", result.FaidecoyReason);
        }

        [Test]
        public void Rungs_StayOffTheGridEdges()
        {
            // Kenara ulasan gecit kenar sutununu koridor yapar ve o koridorun
            // bir tarafinda kurulum seridi kalmaz.
            var grid = new MapGenerator().Generate(WideTemplate(), seed: 8);
            int lastX = grid.Width - 1;

            for (int y = 1; y < grid.Height - 1; y++)
            {
                Assert.AreNotEqual(CellKind.Corridor, grid.Get(grid.Index(0, y)));
                Assert.AreNotEqual(CellKind.Corridor, grid.Get(grid.Index(lastX, y)));
            }
        }

        [Test]
        public void EveryCorridorIsReachableFromEveryEntrance()
        {
            // Girisin herhangi birinden baslayan birlik, uste inmeden butun
            // koridorlara gecebilmeli. Aksi halde secim yalnizca girise ait olur.
            var grid = new MapGenerator().Generate(WideTemplate(), seed: 33);

            int topRow = grid.Height - 1;
            int start = -1;
            for (int x = 0; x < grid.Width && start < 0; x++)
                if (grid.IsWalkable(grid.Index(x, topRow))) start = grid.Index(x, topRow);

            Assert.GreaterOrEqual(start, 0, "Giris hucresi bulunamadi.");

            var reached = Flood(grid, start);

            for (int x = 0; x < grid.Width; x++)
            {
                int baseCell = grid.Index(x, 0);
                if (grid.IsWalkable(baseCell))
                    Assert.IsTrue(reached.Contains(baseCell),
                        "Alt banttaki " + x + ". hucreye ulasilamiyor.");
            }
        }

        [Test]
        public void MultipleDistinctRoutesReachTheBase()
        {
            // Ayni haritada birbirinden ayri en az uc dikey koridor bastan sona
            // gitmeli; "tek yol" haritasi uretilmemeli.
            var grid = new MapGenerator().Generate(WideTemplate(), seed: 12);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.GreaterOrEqual(result.CorridorCount, 3, result.FaidecoyReason);
        }

        [Test]
        public void GeneratedMapsAreValidAcrossManySeeds()
        {
            // Uretim tek seed'de degil, geneli sagliklidir denebilmeli.
            var generator = new MapGenerator();
            var validator = new MapValidator();
            int valid = 0;

            for (int seed = 1; seed <= 30; seed++)
            {
                var grid = generator.Generate(WideTemplate(), seed);
                if (validator.Validate(grid, 10f).IsValid) valid++;
            }

            Assert.GreaterOrEqual(valid, 25,
                "30 seed'in " + valid + " tanesi gecerli; uretim cok eleniyor.");
        }

        private static HashSet<int> Flood(BattleGrid grid, int start)
        {
            var seen = new HashSet<int> { start };
            var stack = new Stack<int>();
            stack.Push(start);
            var buffer = new int[4];

            while (stack.Count > 0)
            {
                int cell = stack.Pop();
                int count = grid.GetNeighbors(cell, buffer);
                for (int i = 0; i < count; i++)
                {
                    int n = buffer[i];
                    if (!grid.IsWalkable(n) || seen.Contains(n)) continue;
                    seen.Add(n);
                    stack.Push(n);
                }
            }

            return seen;
        }
    }
}
