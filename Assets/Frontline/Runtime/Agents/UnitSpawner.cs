using System.Collections;
using UnityEngine;
using Frontline.Core;

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

        [Header("Bilesim")]
        [Tooltip("Dalga bilesimi kullanilsin mi. Kapaliyken herkes piyadedir; " +
                 "eski davranisla karsilastirmak icin kapatilabilir.")]
        public bool useRoster = true;

        [Header("Gorunum")]
        public Color unitColor = new Color(0.85f, 0.82f, 0.75f);

        [Tooltip("Izci rengi. Turler BAKISTA ayirt edilebilmeli; ayirt " +
                 "edilemeyen tur oyuncu icin yok demektir.")]
        public Color scoutColor = new Color(0.55f, 0.9f, 0.95f);

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

        /// <summary>Su an uretilen dalga. Bilesim dalga numarasina bakar.</summary>
        private int currentWave = 1;

        /// <summary>Tek bir dalga uretir ve spawn bitene kadar bekler.</summary>
        public IEnumerator SpawnWave(int count, float hpPerUnit)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnOne(hpPerUnit, PickProfile());
                yield return new WaitForSeconds(spawnInterval);
            }

            currentWave++;
        }

        /// <summary>
        /// Dalga bilesiminden bir tur secer. Paylarin toplami 1'den kucukse
        /// kalan pay piyadeye gider - simulatordeki PickProfile ile ayni
        /// kural (ikisi ayrisirsa olculen sey oynanan sey olmaz).
        /// </summary>
        private UnitProfile PickProfile()
        {
            if (!useRoster) return UnitProfile.Infantry();

            UnitMix[] roster = UnitRoster.Default();
            float roll = Random.value;
            float cursor = 0f;

            for (int i = 0; i < roster.Length; i++)
            {
                if (currentWave < roster[i].FirstWave) continue;
                if (roster[i].Share <= 0f) continue;

                cursor += roster[i].Share;
                if (roll < cursor) return roster[i].Profile;
            }

            return UnitProfile.Infantry();
        }

        private void SpawnOne(float hpPerUnit, UnitProfile profile)
        {
            var go = new GameObject("Unit");
            go.transform.SetParent(transform, false);

            // Izci kucuk ve acik mavi: hizli, kirilgan ve kesfi tasiyan
            // birlik oldugu bakisla anlasilmali.
            bool isScout = profile.Type == UnitType.Scout;
            float scale = unitScale * (isScout ? 0.7f : 1f);

            float diameter = world.Space.CellSize * scale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            go.name = isScout ? "Unit_Scout" : "Unit";

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprites.Circle();
            renderer.color = isScout ? scoutColor : unitColor;
            renderer.sortingOrder = sortingOrder;

            var health = go.AddComponent<UnitHealth>();
            var agent = go.AddComponent<UnitAgent>();
            agent.profile = profile;

            health.Initialize(hpPerUnit * profile.HpMultiplier);
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
