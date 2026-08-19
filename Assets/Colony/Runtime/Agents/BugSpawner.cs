using System.Collections;
using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Dalgalar halinde bocek uretir ve dalga sonunda feromonu sonumler.
    ///
    /// Plan 2'de bocekler olmez, sadece yurur. Amac savas degil, rota
    /// ogrenmesinin GORUNUR olmasi. Ilk dalgalarda surunun dagilmasi,
    /// birkac dalga sonra tek koridorda toplanmasi beklenir.
    /// </summary>
    public sealed class BugSpawner : MonoBehaviour
    {
        public ColonyWorld world;

        [Header("Dalga")]
        [Tooltip("Her dalgada kac bocek cikar.")]
        public int bugsPerWave = 12;

        [Tooltip("Bocekler arasi spawn araligi (saniye).")]
        public float spawnInterval = 0.35f;

        [Tooltip("Dalga bittikten sonra sonraki dalgaya kadar beklenen sure.")]
        public float delayBetweenWaves = 1.5f;

        [Tooltip("Otomatik olarak dalga uretmeye basla.")]
        public bool autoStart = true;

        [Header("Gorunum")]
        public Color bugColor = new Color(0.85f, 0.82f, 0.75f);

        [Tooltip("Bocek capinin hucre boyutuna orani.")]
        [Range(0.1f, 1f)]
        public float bugScale = 0.55f;

        [Tooltip("Cizim sirasi. Feromon katmaninin ustunde olmali.")]
        public int sortingOrder = 20;

        public int WaveNumber { get; private set; }
        public int AliveCount { get; private set; }

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<ColonyWorld>();
        }

        private void Start()
        {
            if (autoStart) StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            // ColonyWorld.Awake'in bitmesini bekle.
            while (world == null || world.Grid == null)
                yield return null;

            while (true)
            {
                WaveNumber++;

                // Yem kalici degildir: basarili tuzak kendini yer ve yemin
                // surekli beslenmesi gerekir (spec 3.3).
                world.ApplyLures();

                yield return SpawnWave(bugsPerWave, spawnInterval);

                while (AliveCount > 0)
                    yield return null;

                // Dalga sonunda izler sonumlenir; kullanilmayan rotalar unutulur.
                world.Pheromone.Decay();

                Debug.Log("[BugSpawner] Dalga " + WaveNumber + " bitti.");
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }

        public IEnumerator SpawnWave(int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnOne();
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnOne()
        {
            var go = new GameObject("Bug");
            go.transform.SetParent(transform, false);

            float diameter = world.Space.CellSize * bugScale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprites.Circle();
            renderer.color = bugColor;
            renderer.sortingOrder = sortingOrder;

            var agent = go.AddComponent<BugAgent>();
            agent.Finished += OnBugFinished;

            if (agent.Initialize(world, world.RandomSpawnCell()))
                AliveCount++;
            else
                Destroy(go);
        }

        private void OnBugFinished(BugAgent agent)
        {
            agent.Finished -= OnBugFinished;
            AliveCount--;
            if (AliveCount < 0) AliveCount = 0;
        }
    }
}
