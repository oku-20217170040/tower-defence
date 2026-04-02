using UnityEngine;
using UnityEngine.EventSystems;

public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    [Header("References")]
    public GridManager grid;
    public Camera mainCamera;

    [Header("Tower Prefabs")]
    public Tower machineGunPrefab;
    public Tower cannonPrefab;
    public Tower radarPrefab;
    public Tower clockPrefab;
    public Tower laserPrefab;
    public Tower icePrefab;

    [Header("Tower Costs")]
    public int machineGunCost = 100;
    public int cannonCost = 200;
    public int radarCost = 200;
    public int clockCost = 250;
    public int laserCost = 200;
    public int iceCost = 180;

    [Header("Gold Settings")]
    public int startingGold = 1000;

    [Header("UI")]
    public TowerSelectionWheel selectionWheel;

    [Header("Optional")]
    public Transform hoverIndicator;

    private int currentGold;
    public int CurrentGold => currentGold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentGold = startingGold;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (grid != null && grid.grid == null)
            grid.Generate();

        if (selectionWheel == null)
            selectionWheel = FindObjectOfType<TowerSelectionWheel>();

        Debug.Log("[TowerPlacer] Started with gold: " + currentGold);
    }

    private void Update()
    {
        UpdateHoverIndicator();

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }
    }

    // ────────────────────── HOVER ──────────────────────

    private void UpdateHoverIndicator()
    {
        if (hoverIndicator == null || mainCamera == null || grid == null) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        Vector2Int coords = grid.WorldToGridPosition(mouseWorld);
        Node node = grid.GetNode(coords);
        if (node == null) return;

        hoverIndicator.position = node.worldPosition;
    }

    // ────────────────────── CLICKS ──────────────────────

    private void HandleLeftClick()
    {
        // Wheel açıksa ve UI üzerindeysek, tıklamayı oyun dünyasına geçirme
        if (selectionWheel != null && selectionWheel.IsOpen)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
        }

        if (TryHitTower(out Tower tower))
        {
            tower.OnSelected();
            tower.TryUpgrade();
            return;
        }

        OpenWheelAtMouse();
    }

    private void HandleRightClick()
    {
        if (TryHitTower(out Tower tower))
        {
            tower.Sell();
        }
    }

    private bool TryHitTower(out Tower tower)
    {
        tower = null;
        if (mainCamera == null) return false;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
        if (hit.collider == null) return false;

        tower = hit.collider.GetComponentInParent<Tower>();
        return tower != null;
    }

    // ────────────────────── HELPER: Bu node’un ÜSTÜNDE GERÇEK KULE VAR MI? ──────────────────────

    /// <summary>
    /// Sadece node’un MERKEZİNDE duran kuleleri gerçek say.
    /// Menzil collider’ları bu node’u kesiyorsa ama kule başka karedeyse, onu sayma.
    /// </summary>
    private bool HasRealTowerExactlyOnNode(Node node)
    {
        if (node == null) return false;

        float radius = (grid != null ? grid.cellSize * 0.4f : 0.4f);
        float centerTolerance = (grid != null ? grid.cellSize * 0.1f : 0.1f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(node.worldPosition, radius);
        foreach (var h in hits)
        {
            Tower t = h.GetComponentInParent<Tower>();
            if (t == null) continue;

            // Kule merkezine gerçekten bu node’de mi?
            float dist = Vector2.Distance(t.transform.position, node.worldPosition);
            if (dist <= centerTolerance)
            {
                return true;
            }
        }

        return false;
    }

    // ────────────────────── WHEEL ──────────────────────

    private void OpenWheelAtMouse()
    {
        // Yeni kule yerleştireceğim için eski range highlight'ını kapat
        Tower.ClearAllHighlights();

        if (mainCamera == null || grid == null) return;

        if (selectionWheel == null)
        {
            selectionWheel = FindObjectOfType<TowerSelectionWheel>();
            if (selectionWheel == null)
            {
                Debug.LogWarning("[TowerPlacer] TowerSelectionWheel bulunamadı.");
                return;
            }
        }

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2Int coords = grid.WorldToGridPosition(mouseWorld);
        Node node = grid.GetNode(coords);

        if (node == null)
        {
            Debug.Log("[TowerPlacer] Grid dışında tıkladın.");
            return;
        }

        if (!node.isWalkable)
        {
            Debug.Log("[TowerPlacer] Bu kareye kule konamaz (walkable değil).");
            return;
        }

        // 🔥 Ghost node kontrolü
        if (node.isOccupied)
        {
            bool hasRealTower = HasRealTowerExactlyOnNode(node);

            if (!hasRealTower)
            {
                Debug.Log("[TowerPlacer] Ghost node yakalandı, isOccupied = false yapıyorum.");
                node.isOccupied = false;
            }
            else
            {
                Debug.Log("[TowerPlacer] Bu karede gerçekten kule var.");
                return;
            }
        }

        selectionWheel.Open(node.worldPosition, coords, node);
    }

    // ────────────────────── BUILD ──────────────────────

    public void BuildTowerAt(Node node, Vector2Int coords, TowerType type)
    {
        if (node == null)
        {
            Debug.LogWarning("[TowerPlacer] BuildTowerAt: node null.");
            return;
        }

        // Ghost node kontrolü
        if (node.isOccupied)
        {
            bool hasRealTower = HasRealTowerExactlyOnNode(node);

            if (hasRealTower)
            {
                Debug.Log("[TowerPlacer] BuildTowerAt: Bu kare zaten dolu.");
                return;
            }
            else
            {
                Debug.Log("[TowerPlacer] BuildTowerAt: Ghost node temizlendi.");
                node.isOccupied = false;
            }
        }

        Tower prefab = null;
        int cost = 0;

        switch (type)
        {
            case TowerType.MachineGun: prefab = machineGunPrefab; cost = machineGunCost; break;
            case TowerType.Cannon: prefab = cannonPrefab; cost = cannonCost; break;
            case TowerType.Radar: prefab = radarPrefab; cost = radarCost; break;
            case TowerType.Clock: prefab = clockPrefab; cost = clockCost; break;
            case TowerType.Laser: prefab = laserPrefab; cost = laserCost; break;
            case TowerType.Ice: prefab = icePrefab; cost = iceCost; break;
        }

        if (prefab == null)
        {
            Debug.LogWarning("[TowerPlacer] BuildTowerAt: prefab null (" + type + ")");
            return;
        }

        if (!SpendGold(cost))
        {
            Debug.Log("[TowerPlacer] Yeterli gold yok. (" + type + ")");
            return;
        }

        Tower newTower = Instantiate(prefab, node.worldPosition, Quaternion.identity);

        // Node işaretle
        node.isOccupied = true;
        // node.isWalkable = false;

        // Kuleye Node + grid + maliyet bilgisini ver
        newTower.Initialize(node, coords, grid, cost);

        // 🔹 Kuleyi otomatik seçili yap: range halkası hemen görünsün
        newTower.OnSelected();

        // Threat + path yenile
        ThreatMap.Instance?.Recalculate();
        WaveSpawner.Instance?.RecalculateAllEnemyPaths();

        Debug.Log($"[TowerPlacer] {type} placed @ {coords}. Gold left: {currentGold}");

    }

    // ────────────────────── GOLD ──────────────────────

    // UIManager bu eventi subscribe eder — her frame poll yerine
    public event System.Action<int> OnGoldChanged;

    public bool SpendGold(int amount)
    {
        if (currentGold < amount) return false;
        currentGold -= amount;
        OnGoldChanged?.Invoke(currentGold);
        return true;
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold);
    }
}
