using System;
using System.Collections.Generic;

namespace Colony.Core
{
    /// <summary>
    /// Koridor agi uzerinde A*. Adim maliyetini RouteCostModel belirler,
    /// boylece feromon, yem ve tehdit tek bir rota kararinda birlesir.
    /// Sezgisel Manhattan mesafesidir; minimum adim maliyeti 1 oldugu icin
    /// kabul edilebilirdir ve en kisa yol garantisi korunur.
    /// </summary>
    public sealed class ColonyPathfinder
    {
        private readonly ColonyGrid grid;
        private readonly RouteCostModel costModel;

        private readonly float[] gCost;
        private readonly int[] cameFrom;
        private readonly bool[] closed;
        private readonly IndexedMinHeap open;
        private readonly int[] neighborBuffer = new int[4];

        public ColonyPathfinder(ColonyGrid grid, RouteCostModel costModel)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.costModel = costModel ?? throw new ArgumentNullException(nameof(costModel));

            gCost = new float[grid.CellCount];
            cameFrom = new int[grid.CellCount];
            closed = new bool[grid.CellCount];
            open = new IndexedMinHeap(grid.CellCount);
        }

        /// <summary>
        /// Baslangictan hedefe yolu bulur ve result listesine yazar
        /// (baslangic ve hedef dahil). Yol yoksa false doner ve result bos kalir.
        /// </summary>
        public bool FindPath(int start, int goal, List<int> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();

            if (!grid.IsWalkable(start) || !grid.IsWalkable(goal))
                return false;

            for (int i = 0; i < gCost.Length; i++)
            {
                gCost[i] = float.PositiveInfinity;
                cameFrom[i] = -1;
                closed[i] = false;
            }
            open.Clear();

            gCost[start] = 0f;
            open.Push(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                int current = open.Pop();

                if (current == goal)
                {
                    Retrace(start, goal, result);
                    return true;
                }

                closed[current] = true;

                int neighborCount = grid.GetNeighbors(current, neighborBuffer);
                for (int i = 0; i < neighborCount; i++)
                {
                    int neighbor = neighborBuffer[i];

                    if (closed[neighbor]) continue;
                    if (!grid.IsWalkable(neighbor)) continue;

                    float tentative = gCost[current] + costModel.StepCost(neighbor);
                    if (tentative >= gCost[neighbor]) continue;

                    gCost[neighbor] = tentative;
                    cameFrom[neighbor] = current;

                    float priority = tentative + Heuristic(neighbor, goal);
                    if (open.Contains(neighbor))
                        open.Update(neighbor, priority);
                    else
                        open.Push(neighbor, priority);
                }
            }

            return false;
        }

        /// <summary>Manhattan mesafesi. Minimum adim maliyeti 1 oldugu icin kabul edilebilirdir.</summary>
        private float Heuristic(int from, int to)
        {
            int dx = grid.X(from) - grid.X(to);
            int dy = grid.Y(from) - grid.Y(to);
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            return dx + dy;
        }

        private void Retrace(int start, int goal, List<int> result)
        {
            int current = goal;
            while (current != start)
            {
                result.Add(current);
                current = cameFrom[current];
            }
            result.Add(start);
            result.Reverse();
        }
    }
}
