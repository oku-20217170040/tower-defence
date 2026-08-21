using System.Collections;
using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Birlik uretir. Dalga akisini artik RunController surer; bu sinif
    /// yalnizca "su kadar birligi bu canla cikar" emrini yerine getirir.
    /// </summary>
    public sealed class UnitSpawner : MonoBehaviour
    {
        public Battlefield world;
        public HomeBaseHealth homeBase;

        [Tooltip("Birlikler arasi spawn araligi (saniye).")]
        public float spawnInterval = 0.35f;

        [Header("Gorunum")]
        public Color unitColor = new Color(0.85f, 0.82f, 0.75f);

        [Tooltip("Birlik capinin hucre boyutuna orani.")]
        [Range(0.1f, 1f)]
        public float unitScale = 0.55f;

        [Tooltip("Cizim sirasi. Kesif izi katmaninin ustunde olmali.")]
        public int sortingOrder = 20;

        public int AliveCount { get; private set; }

        /// <summary>Run boyunca oldurulen toplam birlik.</summary>
        public int TotalKilled { get; private set; }

        /// <summary>Run boyunca usya ulasan toplam birlik.</summary>
        public int TotalLeaked { get; private set; }

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (homeBase == null) homeBase = FindFirstObjectByType<HomeBaseHealth>();
        }

        /// <summary>Tek bir dalga uretir ve spawn bitene kadar bekler.</summary>
        public IEnumerator SpawnWave(int count, float hpPerUnit)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnOne(hpPerUnit);
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnOne(float hpPerUnit)
        {
            var go = new GameObject("Unit");
            go.transform.SetParent(transform, false);

            float diameter = world.Space.CellSize * unitScale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprites.Circle();
            renderer.color = unitColor;
            renderer.sortingOrder = sortingOrder;

            var health = go.AddComponent<UnitHealth>();
            var agent = go.AddComponent<UnitAgent>();

            health.Initialize(hpPerUnit);
            health.Died += OnUnitDied;
            agent.Finished += OnUnitFinished;

            if (agent.Initialize(world, world.RandomSpawnCell(), homeBase))
                AliveCount++;
            else
                Destroy(go);
        }

        private void OnUnitDied(UnitHealth health)
        {
            health.Died -= OnUnitDied;
            TotalKilled++;
        }

        private void OnUnitFinished(UnitAgent agent)
        {
            agent.Finished -= OnUnitFinished;

            // Usya varan birlik can eksiltir. Hasar burada verilir cunku
            // sizinti sayaci da burada tutulur; iki sayi ayni yerde kalmazsa
            // sessizce birbirinden kopar - bu hata bir kez yasandi.
            if (agent.ReachedBase)
            {
                TotalLeaked++;
                if (homeBase != null) homeBase.Damage(agent.damageToBase);
            }

            AliveCount--;
            if (AliveCount < 0) AliveCount = 0;
        }
    }
}
