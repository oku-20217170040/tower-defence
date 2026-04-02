using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Yuvarlak tower seçim menüsü.
/// - Aç/Kapa
/// - Hangi grid karesinde açıldığını tutar
/// - Butonlardan gelen tıklamayı TowerPlacer'a iletir
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TowerSelectionWheel : MonoBehaviour
{
    [Header("Buttons")]
    public Button machineGunButton;
    public Button cannonButton;
    public Button radarButton;
    public Button clockButton;
    public Button laserButton;
    public Button iceButton;

    [Header("Cancel Background")]
    [Tooltip("Panel açıldığında aktif olacak, tam ekran şeffaf buton. Dışarıya tıklayınca kapanır.")]
    public Button cancelBackgroundButton;

    private RectTransform rectTransform;
    private Camera mainCamera;

    // TowerPlacer referansı
    private TowerPlacer towerPlacer;

    // Şu an hangi node üzerinde açıldık?
    private Node currentNode;
    private Vector2Int currentCoords;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        towerPlacer = TowerPlacer.Instance ?? FindObjectOfType<TowerPlacer>();

        // Başlangıçta gizli olsun
        gameObject.SetActive(false);

        if (cancelBackgroundButton != null)
            cancelBackgroundButton.gameObject.SetActive(false);

        // Buton clicklerini bağla (Inspector'dan vermene gerek yok ama verdiysen
        // ekstra listener olur, sorun değil)
        if (machineGunButton != null) machineGunButton.onClick.AddListener(OnMachineGunClicked);
        if (cannonButton != null) cannonButton.onClick.AddListener(OnCannonClicked);
        if (radarButton != null) radarButton.onClick.AddListener(OnRadarClicked);
        if (clockButton != null) clockButton.onClick.AddListener(OnClockClicked);
        if (laserButton != null) laserButton.onClick.AddListener(OnLaserClicked);
        if (iceButton != null) iceButton.onClick.AddListener(OnIceClicked);

        if (cancelBackgroundButton != null)
            cancelBackgroundButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// Grid'teki belli bir world pozisyonunda paneli aç.
    /// </summary>
    public void Open(Vector3 worldPos, Vector2Int coords, Node node)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (towerPlacer == null)
            towerPlacer = TowerPlacer.Instance ?? FindObjectOfType<TowerPlacer>();

        if (mainCamera == null)
        {
            Debug.LogWarning("[TowerSelectionWheel] Main Camera bulunamadı.");
            return;
        }

        if (node == null)
        {
            Debug.LogWarning("[TowerSelectionWheel] Open çağrıldı ama node null.");
            return;
        }

        currentNode = node;
        currentCoords = coords;

        // World pozisyonunu ekrana projeksiyon yap
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        rectTransform.position = screenPos;

        // Panel ve cancel background'u aç
        gameObject.SetActive(true);

        if (cancelBackgroundButton != null)
            cancelBackgroundButton.gameObject.SetActive(true);

        IsOpen = true;
    }

    /// <summary>
    /// Paneli kapat.
    /// </summary>
    public void Close()
    {
        if (!IsOpen && !gameObject.activeSelf)
            return;

        IsOpen = false;
        gameObject.SetActive(false);

        if (cancelBackgroundButton != null)
            cancelBackgroundButton.gameObject.SetActive(false);

        currentNode = null;
    }

    #region Button Events

    private void SelectTower(TowerType type)
    {
        if (towerPlacer == null)
        {
            towerPlacer = TowerPlacer.Instance ?? FindObjectOfType<TowerPlacer>();
        }

        if (towerPlacer == null)
        {
            Debug.LogError("[TowerSelectionWheel] TowerPlacer bulunamadı, kule oluşturulamadı.");
            Close();
            return;
        }

        if (currentNode == null)
        {
            Debug.LogWarning("[TowerSelectionWheel] currentNode null, kule oluşturulmadı.");
            Close();
            return;
        }

        towerPlacer.BuildTowerAt(currentNode, currentCoords, type);
        Close();
    }

    public void OnMachineGunClicked() => SelectTower(TowerType.MachineGun);
    public void OnCannonClicked() => SelectTower(TowerType.Cannon);
    public void OnRadarClicked() => SelectTower(TowerType.Radar);
    public void OnClockClicked() => SelectTower(TowerType.Clock);
    public void OnLaserClicked() => SelectTower(TowerType.Laser);
    public void OnIceClicked() => SelectTower(TowerType.Ice);

    #endregion
}
