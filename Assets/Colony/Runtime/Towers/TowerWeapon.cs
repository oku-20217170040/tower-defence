using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Kulenin silahi. Mermi yok; hasar menzildeki en yakin bocege surekli
    /// uygulanir. Mermi gorselleri Plan 5'in isi.
    ///
    /// Tasarimin can alici noktasi burada gorunur: gurultucu kulenin hasari
    /// yuksektir ama onunden zamanla kimse gecmez, dolayisiyla GERCEK hasari
    /// dusuktur. Sessiz kulenin hasari dusuktur ama surekli is gorur.
    /// Oyuncunun kesfetmesi gereken sey bu fark (spec 3.2).
    /// </summary>
    public sealed class TowerWeapon : MonoBehaviour
    {
        [Tooltip("Saniyede verilen hasar.")]
        public float damagePerSecond = 4f;

        [Tooltip("Menzil (hucre). Kulenin tehdit yaricapiyla ayni olmak zorunda degil.")]
        public float range = 4f;

        private ColonyWorld world;
        private float worldRange;

        public void Bind(ColonyWorld world, float rangeInCells, float dps)
        {
            this.world = world;
            range = rangeInCells;
            damagePerSecond = dps;
            worldRange = rangeInCells * world.Space.CellSize;
        }

        private void Update()
        {
            if (world == null || damagePerSecond <= 0f) return;

            BugHealth target = FindNearestTarget();
            if (target == null) return;

            target.TakeDamage(damagePerSecond * Time.deltaTime);
        }

        /// <summary>
        /// Menzildeki en yakin canli bocek. Sahnedeki tum bocekleri taramak
        /// yerine dunyanin tuttugu canli listesi kullanilir.
        /// </summary>
        private BugHealth FindNearestTarget()
        {
            var bugs = world.ActiveBugs;
            if (bugs == null || bugs.Count == 0) return null;

            Vector2 origin = transform.position;
            float bestSqr = worldRange * worldRange;
            BugHealth best = null;

            for (int i = 0; i < bugs.Count; i++)
            {
                BugAgent bug = bugs[i];
                if (bug == null) continue;

                BugHealth health = bug.Health;
                if (health == null || !health.IsAlive) continue;

                float sqr = ((Vector2)bug.transform.position - origin).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = health;
                }
            }

            return best;
        }
    }
}
