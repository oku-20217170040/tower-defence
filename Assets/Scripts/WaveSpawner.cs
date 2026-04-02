using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [Header("References")]
    [Tooltip("Grid sistemini yöneten GridManager. Düşmanların yol bulması için kullanılır.")]
    public GridManager grid;

    [Tooltip("Base (hedef) objesinin Transform'u. Düşmanlar buraya ulaşmaya çalışır.")]
    public Transform baseTransform;

    [Header("Spawn Points")]
    [Tooltip("Düşmanların çıkacağı spawn noktaları. Birden fazla koyarsan her birinden spawn eder.")]
    public Transform[] spawners;

    [Header("Wave Plan (Inspector)")]
    [Tooltip("Her dalganın ayarları burada yapılır. (Hangi enemy'ler, kaç tane, spawn aralığı, hız/HP/hasar çarpanları vs.)")]
    public WaveConfig[] waves;

    [Tooltip("Bir dalga tamamen bittikten sonra, diğer dalgaya geçmeden önce beklenecek süre (saniye).")]
    public float waveDelay = 5f;

    [Header("Rewards")]
    [Tooltip("Her dalga bittiğinde oyuncuya eklenecek altın miktarı.")]
    public int goldPerWave = 10;

    [Header("End Game")]
    [Tooltip("Tüm dalgalar bitince WIN ekranını açtırmak için kullanılan yönetici.")]
    public EndGameManager endGameManager;

    [Header("Enemy Tracking")]
    [Tooltip("Sahnedeki aktif düşman sayısı. Win için 0 olmasını bekleriz.")]
    public int aliveEnemyCount = 0;

    private Pathfinder pf;
    private BaseHealth baseHealth;
    private TowerPlacer towerPlacer;

    private int currentWave = 0;

    // UIManager bu eventi subscribe eder
    public event System.Action<int, int> OnWaveChanged; // (current, total)

    [Tooltip("Şu an kaçıncı dalgada olduğumuzu verir (1'den başlar).")]
    public int CurrentWave => currentWave;

    [Tooltip("Toplam dalga sayısı (waves dizisinin uzunluğu).")]
    public int TotalWaves => waves != null ? waves.Length : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        pf = GetComponent<Pathfinder>();
        baseHealth = baseTransform.GetComponent<BaseHealth>();
        towerPlacer = TowerPlacer.Instance != null ? TowerPlacer.Instance : FindObjectOfType<TowerPlacer>();

        if (endGameManager == null)
            endGameManager = FindObjectOfType<EndGameManager>();

        if (grid.grid == null)
            grid.Generate();

        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("[WaveSpawner] waves boş! Inspector'dan wave ekle.");
            yield break;
        }

        for (int w = 0; w < waves.Length; w++)
        {
            currentWave = w + 1;
            OnWaveChanged?.Invoke(currentWave, waves.Length);

            if (baseHealth.isDestroyed) yield break;

            WaveConfig wave = waves[w];
            if (wave == null)
            {
                Debug.LogError($"[WaveSpawner] Wave {currentWave} null!");
                yield break;
            }

            foreach (Transform spawner in spawners)
            {
                for (int i = 0; i < wave.enemiesPerSpawner; i++)
                {
                    if (baseHealth.isDestroyed) yield break;

                    EnemyMover prefab = wave.GetEnemyPrefab();
                    if (prefab == null)
                    {
                        Debug.LogError($"[WaveSpawner] Wave {currentWave}: Enemy prefab yok!");
                        yield break;
                    }

                    EnemyMover enemy = Instantiate(prefab, spawner.position, Quaternion.identity);

                    // ✅ spawn oldu -> canlı düşman say
                    aliveEnemyCount++;

                    // ✅ enemy ölünce / disable olunca bize haber versin
                    var tracker = enemy.GetComponent<EnemyLifeTracker>();
                    if (tracker == null) tracker = enemy.gameObject.AddComponent<EnemyLifeTracker>();
                    tracker.Init(this);

                    Vector2Int start = grid.WorldToGridPosition(spawner.position);
                    Vector2Int target = grid.WorldToGridPosition(baseTransform.position);

                    enemy.Initialize(grid, pf, baseHealth, start, target);

                    ApplyMultipliers(enemy, wave);

                    yield return new WaitForSeconds(wave.spawnDelay);
                }
            }

            // Boss wave: normal düşmanlara ek olarak boss spawn
            if (wave.isBossWave && wave.bossPrefab != null)
            {
                for (int b = 0; b < wave.bossCount; b++)
                {
                    if (baseHealth.isDestroyed) yield break;

                    // Boss ilk spawner noktasından çıkar
                    Transform bossSpawner = spawners.Length > 0 ? spawners[0] : transform;
                    EnemyMover boss = Instantiate(wave.bossPrefab, bossSpawner.position, Quaternion.identity);
                    aliveEnemyCount++;

                    var tracker = boss.GetComponent<EnemyLifeTracker>();
                    if (tracker == null) tracker = boss.gameObject.AddComponent<EnemyLifeTracker>();
                    tracker.Init(this);

                    Vector2Int bStart  = grid.WorldToGridPosition(bossSpawner.position);
                    Vector2Int bTarget = grid.WorldToGridPosition(baseTransform.position);
                    boss.Initialize(grid, pf, baseHealth, bStart, bTarget);

                    // Boss'a güçlendirilmiş çarpanlar uygula
                    boss.ApplyWaveMultipliers(
                        wave.speedMultiplier,
                        wave.hpMultiplier * wave.bossHpMultiplier,
                        wave.damageMultiplier * wave.bossDamageMultiplier
                    );

                    yield return new WaitForSeconds(wave.spawnDelay * 2f);
                }
            }

            if (towerPlacer != null)
                towerPlacer.AddGold(goldPerWave);

            ScoreManager.Instance?.RegisterWaveComplete();

            yield return new WaitForSeconds(waveDelay);
        }

        // tüm dalga spawn bitti ama sahnede düşman kalmış olabilir:
        while (!baseHealth.isDestroyed && aliveEnemyCount > 0)
            yield return null;

        if (!baseHealth.isDestroyed && endGameManager != null)
            endGameManager.TriggerWin();
    }

    void ApplyMultipliers(EnemyMover enemy, WaveConfig wave)
    {
        // EnemyMover IWaveAffectable implement ediyorsa, wave çarpanlarını uygular.
        if (enemy is IWaveAffectable affectable)
        {
            affectable.ApplyWaveMultipliers(wave.speedMultiplier, wave.hpMultiplier, wave.damageMultiplier);
        }
    }

    [Tooltip("Enemy öldüğünde / yok olduğunda WaveSpawner'a haber verir ve aliveEnemyCount'u düşürür.")]
    public void NotifyEnemyDiedOrDespawned()
    {
        aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);
    }

    [Tooltip("Kule yerleştirince yol kapanırsa, sahnedeki tüm düşmanların yolunu tekrar hesaplatır.")]
    public void RecalculateAllEnemyPaths()
    {
        var enemies = EnemyManager.Active;
        foreach (var e in enemies)
            if (e != null) e.RecalculatePathFromCurrentTile();
    }
}

[System.Serializable]
public class WaveConfig
{
    [Header("How many?")]
    [Tooltip("Her bir spawner noktasından kaç düşman çıkacağını belirler.\nÖrn: 3 spawner varsa ve burası 5 ise toplam 15 düşman çıkar.")]
    public int enemiesPerSpawner = 3;

    [Header("Timing")]
    [Tooltip("Aynı dalgada, iki düşmanın arka arkaya çıkması arasındaki süre (saniye).")]
    public float spawnDelay = 1f;

    [Header("What enemies can spawn in this wave?")]
    [Tooltip("Bu dalgada çıkmasına izin verilen enemy prefabları.\nSadece 1 prefab koyarsan o dalga %100 o enemy olur.\nBirden fazla koyarsan rastgele seçilir.")]
    public EnemyMover[] allowedEnemies;

    [Header("Multipliers")]
    [Tooltip("Bu dalgadaki düşmanların hız çarpanı.\nÖrn: 1.2 => %20 daha hızlı.")]
    public float speedMultiplier = 1f;

    [Tooltip("Bu dalgadaki düşmanların can çarpanı.\nEnemyHealth varsa maxHP buna göre artar/azalır.")]
    public float hpMultiplier = 1f;

    [Tooltip("Bu dalgadaki düşmanların base'e vurduğu hasar çarpanı.\nÖrn: 2 => 2 kat hasar.")]
    public float damageMultiplier = 1f;

    [Header("Boss Wave")]
    [Tooltip("Bu dalga bir boss dalgasıysa işaretle.")]
    public bool isBossWave = false;
    [Tooltip("Boss enemy prefabı. isBossWave true ise spawn olur.")]
    public EnemyMover bossPrefab;
    [Tooltip("Kaç boss spawn olacak?")]
    public int bossCount = 1;
    [Tooltip("Boss HP çarpanı (hpMultiplier üzerine ek).")]
    public float bossHpMultiplier = 3f;
    [Tooltip("Boss hasar çarpanı.")]
    public float bossDamageMultiplier = 2f;

    public EnemyMover GetEnemyPrefab()
    {
        if (allowedEnemies == null || allowedEnemies.Length == 0) return null;
        return allowedEnemies[Random.Range(0, allowedEnemies.Length)];
    }
}
