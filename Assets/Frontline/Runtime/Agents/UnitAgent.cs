using System;
using System.Collections.Generic;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Tek bir birlik. Usya dogru yurur ve gectigi hucrelere kesif izi birakir.
    ///
    /// Rota SPAWN ANINDA BIR KEZ hesaplanir, her karede degil. Bu bir performans
    /// tercihi degil, tasarim geregidir: ordu kararini verip yola cikmalidir ki
    /// oyuncunun yaniltmasi anlamli olsun. Her an rotasini duzelten bir surunun
    /// kandirilmasi mumkun olmaz.
    /// </summary>
    public sealed class UnitAgent : MonoBehaviour
    {
        [Tooltip("Saniyede kac hucre ilerlesin.")]
        public float cellsPerSecond = 2.5f;

        [Tooltip("Gecerken her hucreye birakilan kesif izi.")]
        public float depositPerCell = 0.5f;

        [Tooltip("Usya ulasinca tum rotaya eklenen pekistirme. Basarili rota guclenir.")]
        public float reinforceOnSuccess = 1.5f;

        [Tooltip("Olurse o ana kadarki rotadan silinen miktar. Olumle biten rota sonumlenir.")]
        public float weakenOnDeath = 2.5f;

        private Battlefield world;
        private HomeBaseHealth homeBase;
        private readonly List<int> path = new List<int>();
        private readonly List<int> visited = new List<int>();
        private int pathIndex;
        private bool active;

        /// <summary>Usya ulasti ya da oldu; her iki durumda da spawner haberdar olur.</summary>
        public event Action<UnitAgent> Finished;

        public bool ReachedBase { get; private set; }

        /// <summary>Bu birligin cani. Kuleler hedef secerken kullanir.</summary>
        public UnitHealth Health { get; private set; }

        /// <summary>Usya ulasirsa bu kadar can eksiltir.</summary>
        public int damageToBase = 1;

        /// <summary>
        /// Bocegi baslatir. Rota bulunamazsa false doner ve birlik hemen biter -
        /// gecerli haritada bu olmamali, olursa dogrulamada bir sorun var demektir.
        /// </summary>
        public bool Initialize(Battlefield world, int spawnCell, HomeBaseHealth homeBase)
        {
            this.world = world;
            this.homeBase = homeBase;

            visited.Clear();
            pathIndex = 0;
            ReachedBase = false;

            // Her birlik kendi yon tercihiyle yola cikar. Bu olmadan tum birlikler
            // ayni koridoru secer ve ordu hicbir zaman alternatif denemez.
            world.CostModel.BeginQuery(UnityEngine.Random.Range(int.MinValue, int.MaxValue), world.Grid);

            if (!world.Pathfinder.FindPath(spawnCell, world.BaseCell, path))
            {
                Debug.LogWarning("[UnitAgent] " + spawnCell + " hucminefieldden usya rota yok.");
                active = false;
                return false;
            }

            transform.position = world.Space.CellToWorld(spawnCell);
            EnterCell(path[0]);

            Health = GetComponent<UnitHealth>();
            world.RegisterUnit(this);

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

            // Mayin tarlasi alanindaki hucrede gecis suresi uzar. Birlik bunu
            // ALGILAMAZ - rota degismez, yalnizca yavaslar.
            float slow = world.Slow != null ? world.Slow.Get(path[pathIndex]) : SlowField.None;
            float step = cellsPerSecond / slow * world.Space.CellSize * Time.deltaTime;

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
            world.Recon.Deposit(cell, depositPerCell);
        }

        private void Arrive()
        {
            active = false;
            ReachedBase = true;

            // Basarili rota guclenir - ordun ogrenmesi budur.
            world.Recon.DepositTrail(visited, reinforceOnSuccess);

            // Varan birlik de listeden cikmali. Cikmazsa kuleler yok edilmis
            // hedeflere ates etmeye devam eder.
            world.UnregisterUnit(this);

            Finished?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Birlik olur. Rotasi zayiflar; boylece olumle biten yol sonumlenir.
        /// Plan 2'de kule hasari yok, bu elle tetiklenir.
        /// </summary>
        public void Kill()
        {
            if (!active) return;
            active = false;

            world.Recon.WeakenTrail(visited, weakenOnDeath);
            world.UnregisterUnit(this);

            // Olum ACIK renkte soner.
            DeathPuff.Spawn(transform.position, new Color(1f, 0.95f, 0.8f, 0.9f),
                            world.Space.CellSize * 0.7f, 25);

            Finished?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
