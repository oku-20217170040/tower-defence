using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("General")]
    public TowerType towerType;
    public Bullet bulletPrefab;
    public Transform firePoint;

    [Header("Aiming (2-Part Turret)")]
    [Tooltip("Sadece üst parçayı (namlu/başlık) döndür. Alt platform SABİT kalır.")]
    public Transform rotatingPart;

    [Tooltip("Top-down 2D için Z ekseni etrafında döndürür.")]
    public bool topDown2D = true;

    [Tooltip("Dönüş hızı (derece/saniye).")]
    public float rotationSpeed = 900f;

    [Tooltip("Sprite yön düzeltme. Turret sprite 'ileri' yönü Up ise genelde +90 gerekir.")]
    public float aimOffsetDegrees = 0f;

    [Tooltip("rotatingPart boşsa bu isimde child arar.")]
    public string rotatingPartName = "Turret";

    [Header("Turret Visibility Lock (Flicker Fix)")]
    [Tooltip("Turret render'ı bazen kapanıyorsa (flicker), bu açık kalsın.")]
    public bool lockTurretRendererOn = true;

    [Tooltip("Turret platformun arkasına düşmesin diye sortingOrder zorla düzelt.")]
    public bool lockTurretSorting = true;

    [Tooltip("Turret sortingOrder = platformSortingOrder + bu değer")]
    public int turretSortingOffset = 1;

    private SpriteRenderer[] cachedTurretRenderers;
    private SpriteRenderer platformRenderer;

    [Header("Multi-Barrel (Levels)")]
    [Tooltip("Lv1/Lv2/Lv3 için ateş noktaları. Örn: [0]=FP_1, [1]=FP_2, [2]=FP_3")]
    public Transform[] firePointsByLevel;

    [Tooltip("Lv1/Lv2/Lv3 namlu görselleri. Örn: [0]=Barrel1, [1]=Barrel2, [2]=Barrel3")]
    public GameObject[] barrelVisualsByLevel;

    [Header("Platform Visuals (Levels)")]
    [Tooltip("Radar gibi towerlarda level'a göre değişecek platform görselleri. Örn: [0]=Platform_Lv1, [1]=Platform_Lv2, [2]=Platform_Lv3")]
    [SerializeField] private GameObject[] platformVisualsByLevel;

    [Header("Level Settings")]
    public int maxLevel = 3;
    public float[] rangeByLevel;
    public float[] fireRateByLevel;
    public int[] damageByLevel;
    public int[] upgradeCostByLevel;

    [Header("Sell Settings")]
    [Tooltip("Toplam yatırılan paranın yüzde kaçı geri verilsin? 0.5 = %50")]
    public float sellRefundPercent = 0.5f;

    [Header("Range Visual")]
    public GameObject rangeVisual;
    public float rangeVisualScaleFactor = 1f;

    public int CurrentLevel => currentLevel;
    public int KillCount { get; private set; }

    private int currentLevel = 0;
    private float fireCooldown = 0f;
    private EnemyHealth currentTarget;

    private readonly Dictionary<RadarTower, float> radarMultipliers =
        new Dictionary<RadarTower, float>();

    // Grid bilgisi
    private GridManager grid;
    private Vector2Int gridCoords;
    private Node occupiedNode;
    private int buildCost;

    private static Tower currentlyHighlighted;

    public float CurrentRange
    {
        get
        {
            float baseRange = rangeByLevel != null && rangeByLevel.Length > 0
                ? rangeByLevel[Mathf.Clamp(currentLevel, 0, rangeByLevel.Length - 1)]
                : 1f;

            float maxM = 1f;
            foreach (var m in radarMultipliers.Values)
                if (m > maxM) maxM = m;

            return baseRange * maxM;
        }
    }

    public float CurrentFireRate
    {
        get
        {
            if (fireRateByLevel == null || fireRateByLevel.Length == 0)
                return 1f;
            return fireRateByLevel[Mathf.Clamp(currentLevel, 0, fireRateByLevel.Length - 1)];
        }
    }

    public int CurrentDamage
    {
        get
        {
            if (damageByLevel == null || damageByLevel.Length == 0)
                return 1;
            return damageByLevel[Mathf.Clamp(currentLevel, 0, damageByLevel.Length - 1)];
        }
    }

    private void Awake()
    {
        InitializeInternal();
        CacheTurretRenderers();
        CachePlatformRenderer();
    }

    private void OnEnable()
    {
        TowerManager.Register(this);
    }

    private void OnDisable()
    {
        TowerManager.Unregister(this);
    }

    public void Initialize(Node node, Vector2Int coords, GridManager grid, int buildCost)
    {
        this.grid = grid;
        this.gridCoords = coords;
        this.occupiedNode = node;
        this.buildCost = buildCost;

        InitializeInternal();
        CacheTurretRenderers();
        CachePlatformRenderer();
    }

    private void InitializeInternal()
    {
        currentLevel = Mathf.Clamp(currentLevel, 0, maxLevel - 1);
        fireCooldown = 0f;

        RegisterWithExistingRadars();

        if (rangeVisual != null)
        {
            UpdateRangeVisualScale();
            rangeVisual.SetActive(false);
        }

        // ✅ Level’a göre SADECE ilgili barrel açık
        UpdateBarrelsByLevel();
        UpdatePlatformsByLevel();
    }

    private void Update()
    {
        fireCooldown -= Time.deltaTime;

        if (currentTarget == null || !IsTargetValid(currentTarget))
            AcquireTarget();

        if (currentTarget != null)
            AimToTarget(currentTarget.transform);

        if (currentTarget != null && fireCooldown <= 0f)
        {
            Shoot();
            AudioManager.Instance?.PlayTowerShot(towerType, transform.position);
            fireCooldown = 1f / Mathf.Max(CurrentFireRate, 0.01f);
        }
    }

    private void LateUpdate()
    {
        if (!lockTurretRendererOn && !lockTurretSorting) return;
        if (rotatingPart == null) return;

        if ((cachedTurretRenderers == null || cachedTurretRenderers.Length == 0))
            CacheTurretRenderers();

        if (platformRenderer == null)
            CachePlatformRenderer();

        if (lockTurretRendererOn)
        {
            for (int i = 0; i < cachedTurretRenderers.Length; i++)
            {
                if (cachedTurretRenderers[i] != null && !cachedTurretRenderers[i].enabled)
                    cachedTurretRenderers[i].enabled = true;
            }

            if (!rotatingPart.gameObject.activeSelf)
                rotatingPart.gameObject.SetActive(true);
        }

        if (lockTurretSorting && platformRenderer != null && cachedTurretRenderers != null)
        {
            int baseOrder = platformRenderer.sortingOrder;

            for (int i = 0; i < cachedTurretRenderers.Length; i++)
            {
                var sr = cachedTurretRenderers[i];
                if (sr == null) continue;

                sr.sortingLayerID = platformRenderer.sortingLayerID;
                sr.sortingOrder = baseOrder + turretSortingOffset;
            }
        }
    }

    private void CachePlatformRenderer()
    {
        platformRenderer = GetComponent<SpriteRenderer>();
        if (platformRenderer != null) return;

        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        if (srs != null && srs.Length > 0)
            platformRenderer = srs[0];
    }

    private void CacheTurretRenderers()
    {
        if (rotatingPart == null)
        {
            Transform t = transform.Find(rotatingPartName);
            if (t == null) t = transform.Find("Head");
            rotatingPart = t;
        }

        if (rotatingPart != null)
            cachedTurretRenderers = rotatingPart.GetComponentsInChildren<SpriteRenderer>(true);
    }

    // ✅ SADECE MEVCUT LEVEL BARREL AÇIK
    private void UpdateBarrelsByLevel()
    {
        if (barrelVisualsByLevel == null || barrelVisualsByLevel.Length == 0) return;

        int idx = Mathf.Clamp(CurrentLevel, 0, barrelVisualsByLevel.Length - 1);

        for (int i = 0; i < barrelVisualsByLevel.Length; i++)
        {
            if (barrelVisualsByLevel[i] == null) continue;
            barrelVisualsByLevel[i].SetActive(i == idx);
        }
    }

    private void UpdatePlatformsByLevel()
    {
        if (platformVisualsByLevel == null || platformVisualsByLevel.Length == 0) return;

        int idx = Mathf.Clamp(CurrentLevel, 0, platformVisualsByLevel.Length - 1);
        for (int i = 0; i < platformVisualsByLevel.Length; i++)
        {
            if (platformVisualsByLevel[i] == null) continue;
            platformVisualsByLevel[i].SetActive(i == idx);
        }
    }

    private void AimToTarget(Transform target)
    {
        if (target == null) return;

        if (rotatingPart == null)
        {
            CacheTurretRenderers();
            if (rotatingPart == null) return;
        }

        Vector3 dir = target.position - rotatingPart.position;

        if (topDown2D)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += aimOffsetDegrees;

            Quaternion desired = Quaternion.Euler(0f, 0f, angle);

            rotatingPart.rotation = Quaternion.RotateTowards(
                rotatingPart.rotation,
                desired,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            Quaternion desired = Quaternion.LookRotation(Vector3.forward, dir);
            rotatingPart.rotation = Quaternion.RotateTowards(rotatingPart.rotation, desired, rotationSpeed * Time.deltaTime);
        }
    }

    public int GetSellValue()
    {
        return Mathf.RoundToInt(GetTotalInvestedGold() * sellRefundPercent);
    }

    public void OnSelected()
    {
        if (currentlyHighlighted != null && currentlyHighlighted != this)
            currentlyHighlighted.SetRangeVisualActive(false);

        currentlyHighlighted = this;

        UpdateRangeVisualScale();
        SetRangeVisualActive(true);
        TowerInfoPanel.Instance?.Show(this);
    }

    public static void ClearAllHighlights()
    {
        if (currentlyHighlighted != null)
        {
            currentlyHighlighted.SetRangeVisualActive(false);
            currentlyHighlighted = null;
        }
        TowerInfoPanel.Instance?.Hide();
    }

    public void SetRangeVisualActive(bool active)
    {
        if (rangeVisual != null)
            rangeVisual.SetActive(active);
    }

    public void UpdateRangeVisualScale()
    {
        if (rangeVisual == null) return;

        float r = CurrentRange;
        float diameter = r * 2f * rangeVisualScaleFactor;
        rangeVisual.transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void RegisterWithExistingRadars()
    {
        RadarTower[] radars = FindObjectsOfType<RadarTower>();
        foreach (var r in radars)
            r.TryBuffTower(this);
    }

    // ✅✅ Radar bonus geldi -> range güncelle + glow aç (ilk kez ise)
    public void ApplyRadarBonus(RadarTower radar, float multiplier)
    {
        bool wasAlreadyBuffedByThisRadar = radarMultipliers.ContainsKey(radar);

        radarMultipliers[radar] = multiplier;
        UpdateRangeVisualScale();

        // sadece ilk kez ekleniyorsa glow ref++ yap
        if (!wasAlreadyBuffedByThisRadar)
        {
            var glow = GetComponent<RadarGlowPulse>();
            if (glow != null) glow.AddSource();
        }
    }

    // ✅✅ Radar bonus gitti -> range güncelle + glow kapa (o radar gerçekten vardıysa)
    public void RemoveRadarBonus(RadarTower radar)
    {
        if (radarMultipliers.ContainsKey(radar))
        {
            radarMultipliers.Remove(radar);
            UpdateRangeVisualScale();

            var glow = GetComponent<RadarGlowPulse>();
            if (glow != null) glow.RemoveSource();
        }
    }

    private bool IsTargetValid(EnemyHealth e)
    {
        if (e == null || !e.gameObject.activeInHierarchy)
            return false;

        float dist = Vector2.Distance(transform.position, e.transform.position);
        return dist <= CurrentRange;
    }

    private void AcquireTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, CurrentRange);

        EnemyHealth best = null;
        float bestDist = float.MaxValue;

        foreach (var c in hits)
        {
            EnemyHealth e = c.GetComponent<EnemyHealth>();
            if (e == null) continue;

            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }

        currentTarget = best;
    }

    /// <summary>
    /// EnemyHealth.Die() tarafından çağrılır — bu kule kill aldı.
    /// </summary>
    public void RegisterKill()
    {
        KillCount++;
    }

    // ✅ SADECE MEVCUT LEVEL FIREPOINT’INDEN ATEŞ
    private void Shoot()
    {
        if (bulletPrefab == null || currentTarget == null)
            return;

        Transform fp = null;

        if (firePointsByLevel != null && firePointsByLevel.Length > 0)
        {
            int idx = Mathf.Clamp(CurrentLevel, 0, firePointsByLevel.Length - 1);
            fp = firePointsByLevel[idx];
        }
        else
        {
            fp = firePoint;
        }

        if (fp == null) return;

        Bullet b = BulletPool.Instance != null
            ? BulletPool.Instance.Get(bulletPrefab, fp.position, Quaternion.identity)
            : Instantiate(bulletPrefab, fp.position, Quaternion.identity);

        b.Initialize(CurrentDamage, currentTarget.transform, this);
    }

    public void TryUpgrade()
    {
        if (currentLevel >= maxLevel - 1)
        {
            Debug.Log("[Tower] Max level.");
            return;
        }

        int cost = (upgradeCostByLevel != null && currentLevel < upgradeCostByLevel.Length)
            ? upgradeCostByLevel[currentLevel]
            : 0;

        if (!TowerPlacer.Instance.SpendGold(cost))
        {
            Debug.Log("[Tower] Yeterli gold yok (upgrade).");
            return;
        }

        currentLevel++;

        RegisterWithExistingRadars();
        UpdateRangeVisualScale();
        ThreatMap.Instance?.Recalculate();

        // ✅ Level artınca sadece ilgili barrel açık kalsın
        UpdateBarrelsByLevel();
        UpdatePlatformsByLevel();

        Debug.Log($"[Tower] Upgrade -> Level {currentLevel + 1}");
    }

    public int GetTotalInvestedGold()
    {
        int sum = buildCost;

        if (upgradeCostByLevel != null)
        {
            for (int i = 0; i < currentLevel && i < upgradeCostByLevel.Length; i++)
                sum += upgradeCostByLevel[i];
        }

        return sum;
    }

    public void Sell()
    {
        int totalInvest = GetTotalInvestedGold();
        int refund = Mathf.RoundToInt(totalInvest * sellRefundPercent);

        if (TowerPlacer.Instance != null)
            TowerPlacer.Instance.AddGold(refund);

        if (occupiedNode != null)
            occupiedNode.isOccupied = false;
        else if (grid != null)
        {
            Node node = grid.GetNode(gridCoords);
            if (node != null) node.isOccupied = false;
        }

        if (currentlyHighlighted == this)
            ClearAllHighlights();

        ThreatMap.Instance?.Recalculate();

        Debug.Log($"[Tower] Satıldı. Refund: {refund}, Node: {gridCoords}");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        float r = Application.isPlaying ? CurrentRange
            : (rangeByLevel != null && rangeByLevel.Length > 0 ? rangeByLevel[0] : 1f);

        Gizmos.DrawWireSphere(transform.position, r);
    }
}
