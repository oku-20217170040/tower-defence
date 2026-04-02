using UnityEngine;

/// <summary>
/// Top kulesi:
/// - Yüksek damage
/// - Yavaþ atýþ
/// - Kurulum + upgrade maliyeti: 200
/// </summary>
[DisallowMultipleComponent]
public class CannonTower : MonoBehaviour
{
    [Header("Cannon Settings")]
    public int buildCost = 200;
    public int upgradeCostPerLevel = 200;
    public bool affectedByRadar = true;

    private Tower baseTower;

    private void Awake()
    {
        baseTower = GetComponent<Tower>();

        if (baseTower == null)
        {
            Debug.LogError("[CannonTower] Ayný GameObject'te Tower.cs bulunamadý!");
            return;
        }

        // TODO: Ýleride topa özel AoE / yavaþ atýþ ayarlarý buradan yönetilebilir.
    }
}
