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

        [Tooltip("Acikken menzildeki TUM boceklere ayni anda vurur (gurultucu kule). " +
                 "Olcum: alan hasari yaklasik 4 kat deger katiyor - 8 bocek gecerken " +
                 "tek hedefli kule 2, alan hasarli 8 olduruyor.")]
        public bool splash;

        private ColonyWorld world;
        private float worldRange;

        public void Bind(ColonyWorld world, float rangeInCells, float dps, bool splash)
        {
            this.world = world;
            this.splash = splash;
            range = rangeInCells;
            damagePerSecond = dps;
            worldRange = rangeInCells * world.Space.CellSize;
        }

        private void Update()
        {
            if (world == null || damagePerSecond <= 0f) return;

            float damage = damagePerSecond * Time.deltaTime;

            if (splash)
            {
                DamageEveryoneInRange(damage);
                return;
            }

            BugHealth target = FindNearestTarget();
            if (target == null) return;

            target.TakeDamage(damage);
        }

        /// <summary>
        /// Menzildeki her bocege ayni anda vurur. Gurultucu kulenin sürüye
        /// karsi degeri buradan gelir - ama koloni ondan kacmayi ogrendigi
        /// icin onunden gecen sayisi zamanla azalir (spec 3.2).
        /// </summary>
        private void DamageEveryoneInRange(float damage)
        {
            var bugs = world.ActiveBugs;
            if (bugs == null || bugs.Count == 0) return;

            Vector2 origin = transform.position;
            float rangeSqr = worldRange * worldRange;

            // Hasar bocegi yok edebilecegi icin geriye dogru gezilir.
            for (int i = bugs.Count - 1; i >= 0; i--)
            {
                BugAgent bug = bugs[i];
                if (bug == null) continue;

                BugHealth health = bug.Health;
                if (health == null || !health.IsAlive) continue;

                if (((Vector2)bug.transform.position - origin).sqrMagnitude <= rangeSqr)
                    health.TakeDamage(damage);
            }
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
