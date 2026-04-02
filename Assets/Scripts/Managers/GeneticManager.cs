using System.Collections;
using UnityEngine;

/// <summary>
/// Genetik Algoritma yöneticisi.
/// - Her dalga bir "kromozom" (grid üzerinde hücre kaçınma maliyetleri) dener.
/// - Dalga sonunda fitness hesaplanır: base'e ulaşan düşman sayısı / spawn sayısı.
/// - Her N dalgada bir nesil geçer: seçilim, çaprazlama, mutasyon.
/// - En iyi kromozom node.geneticCost olarak Pathfinder'a yansır.
/// </summary>
public class GeneticManager : MonoBehaviour
{
    public static GeneticManager Instance { get; private set; }

    [Header("GA Ayarları")]
    [Tooltip("Popülasyon büyüklüğü: her nesilde kaç strateji bulunur.")]
    public int populationSize = 8;

    [Tooltip("Mutasyon olasılığı (0-1). Her gen bu ihtimalle değişir.")]
    [Range(0f, 1f)]
    public float mutationRate = 0.12f;

    [Tooltip("Mutasyon gücü: değiştirilen gen ne kadar sapabilir.")]
    public float mutationStrength = 6f;

    [Tooltip("Maksimum genetik maliyet değeri.")]
    public float maxGeneticCost = 40f;

    [Tooltip("Elite bireylerin sayısı: en iyi N strateji doğrudan sonraki nesle aktarılır.")]
    public int eliteCount = 2;

    [Tooltip("GA kaçıncı dalgadan itibaren devreye girsin?")]
    public int activateFromWave = 5;

    [Header("Referanslar")]
    public GridManager grid;

    // ── Popülasyon ──────────────────────────────────────────────────────────
    private float[][] population;
    private float[]   fitnessScores;
    private int       chromosomeLength;

    // ── Dalga takibi ────────────────────────────────────────────────────────
    private int  activeChromosomeIndex;
    private int  waveEnemiesSpawned;
    private int  waveEnemiesReachedBase;
    private int  wavesEvaluated;          // kaç dalga fitness aldı
    private bool isActive;

    // ── Nesil sayacı ────────────────────────────────────────────────────────
    public int Generation { get; private set; }

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        if (grid == null) grid = FindObjectOfType<GridManager>();
        StartCoroutine(InitWhenReady());
    }

    private IEnumerator InitWhenReady()
    {
        while (grid == null || grid.grid == null)
            yield return null;

        chromosomeLength = grid.width * grid.height;
        InitPopulation();
        Debug.Log($"[GA] Başlatıldı. Kromozom uzunluğu={chromosomeLength}, Popülasyon={populationSize}");
    }

    // ── Popülasyon başlatma ──────────────────────────────────────────────────
    private void InitPopulation()
    {
        population     = new float[populationSize][];
        fitnessScores  = new float[populationSize];

        for (int i = 0; i < populationSize; i++)
        {
            population[i] = new float[chromosomeLength];
            // İlk nesil sıfırdan başlar (tarafsız): GA zamanla öğrenir
        }
    }

    // ── Dışarıdan çağrılan API ───────────────────────────────────────────────

    /// <summary>WaveSpawner her dalga başında çağırır.</summary>
    public void OnWaveStart(int waveNumber)
    {
        isActive               = waveNumber >= activateFromWave;
        waveEnemiesSpawned     = 0;
        waveEnemiesReachedBase = 0;

        if (!isActive) return;

        // Bu dalga için round-robin ile bir kromozom seç
        activeChromosomeIndex = wavesEvaluated % populationSize;
        ApplyChromosome(population[activeChromosomeIndex]);

        Debug.Log($"[GA] Dalga {waveNumber} başladı | Nesil={Generation} | " +
                  $"Kromozom={activeChromosomeIndex}");
    }

    /// <summary>WaveSpawner her spawn'da çağırır.</summary>
    public void NotifyEnemySpawned() => waveEnemiesSpawned++;

    /// <summary>EnemyMover base'e ulaşınca çağırır.</summary>
    public void NotifyEnemyReachedBase() => waveEnemiesReachedBase++;

    /// <summary>WaveSpawner dalga tamamen bitince çağırır.</summary>
    public void OnWaveEnd()
    {
        if (!isActive || waveEnemiesSpawned == 0) return;

        // Fitness: hayatta kalma oranı [0..100]
        float survival = (float)waveEnemiesReachedBase / waveEnemiesSpawned;
        fitnessScores[activeChromosomeIndex] = survival * 100f;

        Debug.Log($"[GA] Dalga bitti | Kromozom={activeChromosomeIndex} | " +
                  $"Fitness={fitnessScores[activeChromosomeIndex]:F1} " +
                  $"({waveEnemiesReachedBase}/{waveEnemiesSpawned})");

        wavesEvaluated++;

        // Tüm popülasyon bir kez denendikten sonra nesil geç
        if (wavesEvaluated > 0 && wavesEvaluated % populationSize == 0)
            Evolve();
    }

    // ── Evrim ───────────────────────────────────────────────────────────────
    private void Evolve()
    {
        Generation++;
        Debug.Log($"[GA] Nesil {Generation} başlıyor (evrim)...");

        int[] ranked = RankByFitness();

        float[][] next = new float[populationSize][];

        // Elite: en iyiler kopyalanır
        for (int i = 0; i < eliteCount && i < populationSize; i++)
            next[i] = (float[])population[ranked[i]].Clone();

        // Geri kalan: çaprazlama + mutasyon
        for (int i = eliteCount; i < populationSize; i++)
        {
            float[] p1 = population[TournamentSelect(ranked)];
            float[] p2 = population[TournamentSelect(ranked)];
            float[] child = Crossover(p1, p2);
            Mutate(child);
            next[i] = child;
        }

        population    = next;
        fitnessScores = new float[populationSize];

        // En iyi stratejiyi hemen uygula
        ApplyChromosome(population[0]);

        float bestFitness = fitnessScores[ranked[0]]; // önceki neslin en iyisi
        Debug.Log($"[GA] Nesil {Generation} tamamlandı. Önceki nesil en iyi fitness: {bestFitness:F1}");
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────

    private int[] RankByFitness()
    {
        int[] idx = new int[populationSize];
        for (int i = 0; i < populationSize; i++) idx[i] = i;
        System.Array.Sort(idx, (a, b) => fitnessScores[b].CompareTo(fitnessScores[a]));
        return idx;
    }

    private int TournamentSelect(int[] ranked)
    {
        // Üst yarıdan rastgele seç — daha başarılı bireyler daha olası
        int half = Mathf.Max(1, ranked.Length / 2);
        return ranked[Random.Range(0, half)];
    }

    private float[] Crossover(float[] p1, float[] p2)
    {
        float[] child = new float[chromosomeLength];
        int cut = Random.Range(0, chromosomeLength);
        for (int i = 0; i < chromosomeLength; i++)
            child[i] = i < cut ? p1[i] : p2[i];
        return child;
    }

    private void Mutate(float[] c)
    {
        for (int i = 0; i < c.Length; i++)
        {
            if (Random.value < mutationRate)
            {
                c[i] += Random.Range(-mutationStrength, mutationStrength);
                c[i]  = Mathf.Clamp(c[i], 0f, maxGeneticCost);
            }
        }
    }

    /// <summary>Kromozomu node.geneticCost olarak grid'e yazar.</summary>
    private void ApplyChromosome(float[] chromosome)
    {
        if (grid == null || grid.grid == null || chromosome == null) return;

        int h = grid.height;
        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Node node = grid.GetNode(new Vector2Int(x, y));
                if (node == null) continue;

                int idx       = x * h + y;
                node.geneticCost = idx < chromosome.Length ? chromosome[idx] : 0f;
            }
        }
    }
}
