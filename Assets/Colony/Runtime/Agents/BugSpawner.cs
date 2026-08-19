using System.Collections;
using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Bocek uretir. Dalga akisini artik RunController surer; bu sinif
    /// yalnizca "su kadar bocegi bu canla cikar" emrini yerine getirir.
    /// </summary>
    public sealed class BugSpawner : MonoBehaviour
    {
        public ColonyWorld world;
        public NestHealth nest;

        [Tooltip("Bocekler arasi spawn araligi (saniye).")]
        public float spawnInterval = 0.35f;

        [Header("Gorunum")]
        public Color bugColor = new Color(0.85f, 0.82f, 0.75f);

        [Tooltip("Bocek capinin hucre boyutuna orani.")]
        [Range(0.1f, 1f)]
        public float bugScale = 0.55f;

        [Tooltip("Cizim sirasi. Feromon katmaninin ustunde olmali.")]
        public int sortingOrder = 20;

        public int AliveCount { get; private set; }

        /// <summary>Run boyunca oldurulen toplam bocek.</summary>
        public int TotalKilled { get; private set; }

        /// <summary>Run boyunca yuvaya ulasan toplam bocek.</summary>
        public int TotalLeaked { get; private set; }

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<ColonyWorld>();
            if (nest == null) nest = FindFirstObjectByType<NestHealth>();
        }

        /// <summary>Tek bir dalga uretir ve spawn bitene kadar bekler.</summary>
        public IEnumerator SpawnWave(int count, float hpPerBug)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnOne(hpPerBug);
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnOne(float hpPerBug)
        {
            var go = new GameObject("Bug");
            go.transform.SetParent(transform, false);

            float diameter = world.Space.CellSize * bugScale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprites.Circle();
            renderer.color = bugColor;
            renderer.sortingOrder = sortingOrder;

            var health = go.AddComponent<BugHealth>();
            var agent = go.AddComponent<BugAgent>();

            health.Initialize(hpPerBug);
            health.Died += OnBugDied;
            agent.Finished += OnBugFinished;

            if (agent.Initialize(world, world.RandomSpawnCell(), nest))
                AliveCount++;
            else
                Destroy(go);
        }

        private void OnBugDied(BugHealth health)
        {
            health.Died -= OnBugDied;
            TotalKilled++;
        }

        private void OnBugFinished(BugAgent agent)
        {
            agent.Finished -= OnBugFinished;

            if (agent.ReachedNest) TotalLeaked++;

            AliveCount--;
            if (AliveCount < 0) AliveCount = 0;
        }
    }
}
