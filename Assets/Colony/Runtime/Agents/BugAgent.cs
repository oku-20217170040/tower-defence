using System;
using System.Collections.Generic;
using UnityEngine;
using Colony.Core;

namespace Colony.Runtime
{
    /// <summary>
    /// Tek bir bocek. Yuvaya dogru yurur ve gectigi hucrelere feromon birakir.
    ///
    /// Rota SPAWN ANINDA BIR KEZ hesaplanir, her karede degil. Bu bir performans
    /// tercihi degil, tasarim geregidir: koloni kararini verip yola cikmalidir ki
    /// oyuncunun yaniltmasi anlamli olsun. Her an rotasini duzelten bir surunun
    /// kandirilmasi mumkun olmaz.
    /// </summary>
    public sealed class BugAgent : MonoBehaviour
    {
        [Tooltip("Saniyede kac hucre ilerlesin.")]
        public float cellsPerSecond = 2.5f;

        [Tooltip("Gecerken her hucreye birakilan feromon.")]
        public float depositPerCell = 5f;

        [Tooltip("Yuvaya ulasinca tum rotaya eklenen pekistirme. Basarili rota guclenir.")]
        public float reinforceOnSuccess = 22f;

        [Tooltip("Olurse o ana kadarki rotadan silinen miktar. Olumle biten rota sonumlenir.")]
        public float weakenOnDeath = 18f;

        private ColonyWorld world;
        private readonly List<int> path = new List<int>();
        private readonly List<int> visited = new List<int>();
        private int pathIndex;
        private bool active;

        /// <summary>Yuvaya ulasti ya da oldu; her iki durumda da spawner haberdar olur.</summary>
        public event Action<BugAgent> Finished;

        public bool ReachedNest { get; private set; }

        /// <summary>
        /// Bocegi baslatir. Rota bulunamazsa false doner ve bocek hemen biter -
        /// gecerli haritada bu olmamali, olursa dogrulamada bir sorun var demektir.
        /// </summary>
        public bool Initialize(ColonyWorld world, int spawnCell)
        {
            this.world = world;

            visited.Clear();
            pathIndex = 0;
            ReachedNest = false;

            if (!world.Pathfinder.FindPath(spawnCell, world.NestCell, path))
            {
                Debug.LogWarning("[BugAgent] " + spawnCell + " hucresinden yuvaya rota yok.");
                active = false;
                return false;
            }

            transform.position = world.Space.CellToWorld(spawnCell);
            EnterCell(path[0]);
            active = true;
            return true;
        }

        private void Update()
        {
            if (!active) return;

            int nextIndex = pathIndex + 1;
            if (nextIndex >= path.Count)
            {
                Arrive();
                return;
            }

            Vector2 target = world.Space.CellToWorld(path[nextIndex]);
            float step = cellsPerSecond * world.Space.CellSize * Time.deltaTime;

            transform.position = Vector2.MoveTowards(transform.position, target, step);

            if (((Vector2)transform.position - target).sqrMagnitude < 0.0001f)
            {
                pathIndex = nextIndex;
                EnterCell(path[pathIndex]);
            }
        }

        private void EnterCell(int cell)
        {
            visited.Add(cell);
            world.Pheromone.Deposit(cell, depositPerCell);
        }

        private void Arrive()
        {
            active = false;
            ReachedNest = true;

            // Basarili rota guclenir - kolonin ogrenmesi budur.
            world.Pheromone.DepositTrail(visited, reinforceOnSuccess);

            Finished?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Bocek olur. Rotasi zayiflar; boylece olumle biten yol sonumlenir.
        /// Plan 2'de kule hasari yok, bu elle tetiklenir.
        /// </summary>
        public void Kill()
        {
            if (!active) return;
            active = false;

            world.Pheromone.WeakenTrail(visited, weakenOnDeath);

            Finished?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
