using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Seçili kule için bilgi paneli.
/// Tower.OnSelected() → TowerInfoPanel.Show(tower)
/// Tower.ClearAllHighlights() → TowerInfoPanel.Hide()
///
/// KURULUM: Canvas altında panel GameObject'e ekle.
/// Gerekli TMP alanları Inspector'dan atanır.
/// </summary>
public class TowerInfoPanel : MonoBehaviour
{
    public static TowerInfoPanel Instance { get; private set; }

    [Header("Panel Root")]
    [Tooltip("Panelin root GameObject'i. Show/Hide için açılır/kapanır.")]
    public GameObject panelRoot;

    [Header("Text Fields")]
    public TextMeshProUGUI txtType;
    public TextMeshProUGUI txtLevel;
    public TextMeshProUGUI txtDPS;
    public TextMeshProUGUI txtKills;
    public TextMeshProUGUI txtUpgradeCost;
    public TextMeshProUGUI txtSellValue;

    [Header("Buttons")]
    public Button btnUpgrade;
    public Button btnSell;

    private Tower currentTower;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot != null) panelRoot.SetActive(false);

        if (btnUpgrade != null) btnUpgrade.onClick.AddListener(OnUpgradeClicked);
        if (btnSell    != null) btnSell.onClick.AddListener(OnSellClicked);
    }

    public void Show(Tower tower)
    {
        if (tower == null) { Hide(); return; }

        currentTower = tower;
        Refresh();

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        currentTower = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Her frame çağrılmaz — sadece Show ve buton sonrası çağrılır.
    /// </summary>
    public void Refresh()
    {
        if (currentTower == null) return;

        if (txtType  != null) txtType.text  = currentTower.towerType.ToString();
        if (txtLevel != null) txtLevel.text = $"Level {currentTower.CurrentLevel + 1}/{currentTower.maxLevel}";
        if (txtKills != null) txtKills.text = $"Kills: {currentTower.KillCount}";

        // DPS = damage * fireRate
        float dps = currentTower.CurrentDamage * currentTower.CurrentFireRate;
        if (txtDPS != null) txtDPS.text = $"DPS: {dps:F1}";

        // Upgrade
        bool maxed = currentTower.CurrentLevel >= currentTower.maxLevel - 1;
        if (btnUpgrade != null) btnUpgrade.interactable = !maxed;
        if (txtUpgradeCost != null)
        {
            if (maxed)
            {
                txtUpgradeCost.text = "MAX";
            }
            else
            {
                int cost = (currentTower.upgradeCostByLevel != null &&
                            currentTower.CurrentLevel < currentTower.upgradeCostByLevel.Length)
                    ? currentTower.upgradeCostByLevel[currentTower.CurrentLevel]
                    : 0;
                txtUpgradeCost.text = $"Upgrade: {cost}g";
            }
        }

        // Sell
        int refund = currentTower.GetSellValue();
        if (txtSellValue != null) txtSellValue.text = $"Sell: {refund}g";
    }

    private void OnUpgradeClicked()
    {
        if (currentTower == null) return;
        currentTower.TryUpgrade();
        Refresh();
    }

    private void OnSellClicked()
    {
        if (currentTower == null) return;
        currentTower.Sell();
        Hide();
    }
}
