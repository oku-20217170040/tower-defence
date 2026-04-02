using UnityEngine;

/// <summary>
/// Grid üzerindeki her Node için "tehdit puanı" (threatCost) hesaplar.
/// Kuleler menzilleriyle çevredeki hücrelere threatCost ekler.
/// + EK: Düşman ölünce o hücreye "ölüm tehdidi" ekler, zamanla azalır (yeşile döner).
/// Pathfinder, A* maliyetine bu threatCost'u dahil eder.
/// </summary>
public class ThreatMap : MonoBehaviour
{
    public static ThreatMap Instance { get; private set; }

    [Header("References")]
    public GridManager grid;

    [Header("Threat Settings (Towers)")]
    [Tooltip("Tehdit puanının A* maliyetine ölçeklenmesi. Pathfinder.threatWeight ile birlikte çalışır.")]
    public float baseThreatScale = 1f;

    [Header("Threat Settings (Enemy Death Heat)")]
    [Tooltip("Enemy öldüğünde eklenecek tehdit (merkez hücre).")]
    public float deathThreatAdd = 6f;

    [Tooltip("Ölüm tehdidinin yayılacağı yarıçap (grid hücresi). 0 = sadece merkez.")]
    [Range(0, 6)]
    public int deathThreatRadius = 1;

    [Tooltip("Yarıçap içinde merkeze yakın hücreler daha çok tehdit alır.")]
    public bool deathThreatFalloff = true;

    [Tooltip("Ölüm tehdidi saniyede ne kadar azalsın? (yüksek = daha hızlı yeşile döner)")]
    public float deathThreatDecayPerSecond = 2.5f;

    [Header("Runtime Update")]
    [Tooltip("Death threat güncelleme aralığı (saniye).")]
    [Range(0.02f, 1f)]
    public float updateInterval = 0.1f;

    // Katmanlar:
    // baseLayer = kulelerden gelen statik tehdit
    // deathLayer = enemy ölümlerinden gelen dinamik tehdit (decay)
    private float[,] baseLayer;
    private float[,] deathLayer;

    private int width;
    private int height;
    private float timer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (grid == null)
            grid = FindObjectOfType<GridManager>();

        EnsureBuffers();
    }

    private void Update()
    {
        if (grid == null || grid.grid == null) return;
        if (deathLayer == null) return;

        timer += Time.deltaTime;
        if (timer < updateInterval) return;

        float dt = timer;
        timer = 0f;

        bool changed = DecayDeathLayer(dt);
        if (changed)
        {
            ApplyCombinedThreatToNodes();
        }
    }

    private void EnsureBuffers()
    {
        if (grid == null || grid.grid == null) return;

        width = grid.grid.GetLength(0);
        height = grid.grid.GetLength(1);

        if (baseLayer == null || baseLayer.GetLength(0) != width || baseLayer.GetLength(1) != height)
        {
            baseLayer = new float[width, height];
            deathLayer = new float[width, height];
        }
    }

    /// <summary>
    /// Grid'deki tüm node'ların threatCost değerini yeniden hesaplar.
    /// Tower yerleştirildiğinde / satıldığında / upgrade edildiğinde çağır.
    /// (İlk sürüm akışı korunur; sadece deathLayer sıfırlanmaz.)
    /// </summary>
    public void Recalculate()
    {
        if (grid == null || grid.grid == null)
        {
            Debug.LogWarning("[ThreatMap] Grid bulunamadı.");
            return;
        }

        EnsureBuffers();


        // ✅ Runtime'da yerleştirilen obstacle (Tower/Stone vb.) için walkable güncelle
        grid.RefreshAllWalkability();
        // 1) Base layer'ı SIFIRLA (kule tehdidi yeniden hesaplanacak)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                baseLayer[x, y] = 0f;
            }
        }

        // 2) Sahnedeki AKTİF tüm kuleleri bul ve baseLayer'a tehdit uygula
        var towers = TowerManager.All;

        foreach (Tower tower in towers)
        {
            if (tower == null) continue;
            if (!tower.gameObject.activeInHierarchy) continue;

            float range = tower.CurrentRange;
            float baseThreat = GetThreatFromTowerType(tower.towerType) * baseThreatScale;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Node node = grid.grid[x, y];
                    if (node == null) continue;

                    float dist = Vector2.Distance(node.worldPosition, tower.transform.position);
                    if (dist <= range)
                    {
                        baseLayer[x, y] += baseThreat;
                    }
                }
            }
        }

        // 3) Node'lara birleşik tehdidi yaz (base + death)
        ApplyCombinedThreatToNodes();

        // 4) Sahnedeki TÜM düşmanlara yeni path hesaplat
        var enemies = EnemyManager.Active;
        foreach (EnemyMover enemy in enemies)
        {
            if (enemy != null)
                enemy.RecalculatePathFromCurrentTile();
        }
    }

    /// <summary>
    /// Enemy öldüğü anda çağır.
    /// Ölüm olan hücreyi (ve radius alanını) kırmızılaştıracak şekilde deathLayer'a ek puan yazar.
    /// </summary>
    public void RegisterEnemyDeath(Vector3 worldPos)
    {
        if (grid == null || grid.grid == null) return;

        EnsureBuffers();        // GridManager içindeki dönüşümü kullanıyoruz.
        Vector2Int c = grid.WorldToGridPosition(worldPos);

        for (int dx = -deathThreatRadius; dx <= deathThreatRadius; dx++)
        {
            for (int dy = -deathThreatRadius; dy <= deathThreatRadius; dy++)
            {
                int x = c.x + dx;
                int y = c.y + dy;

                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                if (grid.grid[x, y] == null) continue;

                float add = deathThreatAdd;

                if (deathThreatRadius > 0 && deathThreatFalloff)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float t = Mathf.InverseLerp(deathThreatRadius + 0.0001f, 0f, dist); // merkeze yakın daha yüksek
                    add *= Mathf.Clamp01(t);
                }

                deathLayer[x, y] += add;
            }
        }

        // Anında görünsün
        ApplyCombinedThreatToNodes();
    }

    private bool DecayDeathLayer(float dt)
    {
        bool changed = false;
        float step = deathThreatDecayPerSecond * dt;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float cur = deathLayer[x, y];
                if (cur <= 0f) continue;

                float next = Mathf.Max(0f, cur - step);
                if (!Mathf.Approximately(cur, next))
                {
                    deathLayer[x, y] = next;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private void ApplyCombinedThreatToNodes()
    {
        // node.threatCost = base + death
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid.grid[x, y];
                if (node == null) continue;

                node.threatCost = baseLayer[x, y] + deathLayer[x, y];
            }
        }
    }

    /// <summary>
    /// Kule türüne göre taban tehdit değeri.
    /// Buradan "bu kule ne kadar tehlikeli" skorunu kontrol ediyorsun.
    /// </summary>
    private float GetThreatFromTowerType(TowerType type)
    {
        switch (type)
        {
            case TowerType.MachineGun: return 2f;
            case TowerType.Cannon: return 3f;
            case TowerType.Laser: return 3.5f;
            case TowerType.Ice: return 1.5f;
            case TowerType.Clock: return 1f;
            case TowerType.Radar: return 0.5f; // buff, direkt hasar değil
            default: return 1f;
        }
    }
}
