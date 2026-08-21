using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kulenin silahi. Mermi yok; hasar menzildeki en yakin birlige surekli
    /// uygulanir. Mermi gorselleri Plan 5'in isi.
    ///
    /// Tasarimin can alici noktasi burada gorunur: batarya kulenin hasari
    /// yuksektir ama onunden zamanla kimse gecmez, dolayisiyla GERCEK hasari
    /// dusuktur. Pusu mevzisinin hasari dusuktur ama surekli is gorur.
    /// Oyuncunun kesfetmesi gereken sey bu fark (spec 3.2).
    /// </summary>
    public sealed class TowerWeapon : MonoBehaviour
    {
        [Tooltip("Saniyede verilen hasar.")]
        public float damagePerSecond = 4f;

        [Tooltip("Menzil (hucre). Kulenin tehdit yaricapiyla ayni olmak zorunda degil.")]
        public float range = 4f;

        [Tooltip("Acikken menzildeki TUM birliklere ayni anda vurur (batarya kule). " +
                 "Olcum: alan hasari yaklasik 4 kat deger katiyor - 8 birlik gecerken " +
                 "tek hedefli kule 2, alan hasarli 8 olduruyor.")]
        public bool splash;

        private Battlefield world;
        private float worldRange;

        public void Bind(Battlefield world, float rangeInCells, float dps, bool splash)
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

            UnitHealth target = FindNearestTarget();
            if (target == null) return;

            target.TakeDamage(damage);
        }

        /// <summary>
        /// Menzildeki her birlige ayni anda vurur. Batarya kulenin sürüye
        /// karsi degeri buradan gelir - ama ordu ondan kacmayi ogrendigi
        /// icin onunden gecen sayisi zamanla azalir (spec 3.2).
        /// </summary>
        private void DamageEveryoneInRange(float damage)
        {
            var units = world.ActiveUnits;
            if (units == null || units.Count == 0) return;

            Vector2 origin = transform.position;
            float rangeSqr = worldRange * worldRange;

            // Hasar birligi yok edebilecegi icin geriye dogru gezilir.
            for (int i = units.Count - 1; i >= 0; i--)
            {
                UnitAgent unit = units[i];
                if (unit == null) continue;

                UnitHealth health = unit.Health;
                if (health == null || !health.IsAlive) continue;

                if (((Vector2)unit.transform.position - origin).sqrMagnitude <= rangeSqr)
                    health.TakeDamage(damage);
            }
        }

        /// <summary>
        /// Menzildeki en yakin canli birlik. Sahnedeki tum birlikleri taramak
        /// yerine dunyanin tuttugu canli listesi kullanilir.
        /// </summary>
        private UnitHealth FindNearestTarget()
        {
            var units = world.ActiveUnits;
            if (units == null || units.Count == 0) return null;

            Vector2 origin = transform.position;
            float bestSqr = worldRange * worldRange;
            UnitHealth best = null;

            for (int i = 0; i < units.Count; i++)
            {
                UnitAgent unit = units[i];
                if (unit == null) continue;

                UnitHealth health = unit.Health;
                if (health == null || !health.IsAlive) continue;

                float sqr = ((Vector2)unit.transform.position - origin).sqrMagnitude;
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
