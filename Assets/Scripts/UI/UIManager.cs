using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD yöneticisi — event tabanlı (her frame poll yok).
/// Gold, HP ve Wave değişince ilgili event fire edilir,
/// UIManager sadece o anda günceller.
///
/// Dalga önizlemesi: sonraki dalganın enemy sprite'larını gösterir.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("References")]
    public TowerPlacer  towerPlacer;
    public BaseHealth   baseHealth;
    public WaveSpawner  waveSpawner;

    [Header("HUD Texts")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI waveText;

    [Header("Wave Preview")]
    [Tooltip("Bir sonraki dalganın enemy ikonlarını göstermek için Image dizisi.")]
    public Image[] nextWaveIcons;
    [Tooltip("Icon'larda gösterilecek sprite'lar — WaveSpawner.waves[next].allowedEnemies ile eşleştir.")]
    public Sprite[] enemySprites;     // Inspector'dan enemy prefab sıralamasına göre ata
    public Sprite unknownSprite;      // Eşleşme bulunamazsa gösterilecek sprite

    private void Start()
    {
        // Başlangıç değerlerini hemen yaz
        if (towerPlacer != null)
        {
            UpdateGold(towerPlacer.CurrentGold);
            towerPlacer.OnGoldChanged += UpdateGold;
        }

        if (baseHealth != null)
        {
            UpdateHp(baseHealth.CurrentHp);
            baseHealth.OnHpChanged += UpdateHp;
        }

        if (waveSpawner != null)
        {
            UpdateWave(waveSpawner.CurrentWave, waveSpawner.TotalWaves);
            waveSpawner.OnWaveChanged += UpdateWave;
        }
    }

    private void OnDestroy()
    {
        if (towerPlacer != null) towerPlacer.OnGoldChanged -= UpdateGold;
        if (baseHealth  != null) baseHealth.OnHpChanged    -= UpdateHp;
        if (waveSpawner != null) waveSpawner.OnWaveChanged -= UpdateWave;
    }

    private void UpdateGold(int value)
    {
        if (goldText != null) goldText.text = $"Gold: {value}";
    }

    private void UpdateHp(int value)
    {
        if (hpText != null) hpText.text = $"Base HP: {value}";
    }

    private void UpdateWave(int current, int total)
    {
        if (waveText != null) waveText.text = $"Wave: {current}/{total}";

        RefreshNextWavePreview(current);
    }

    private void RefreshNextWavePreview(int currentWave)
    {
        if (nextWaveIcons == null || nextWaveIcons.Length == 0) return;
        if (waveSpawner   == null || waveSpawner.waves == null) return;

        int nextIndex = currentWave; // currentWave 1-bazlı, waves 0-bazlı
        bool hasNext = nextIndex < waveSpawner.waves.Length;

        for (int i = 0; i < nextWaveIcons.Length; i++)
        {
            if (nextWaveIcons[i] == null) continue;

            if (!hasNext || waveSpawner.waves[nextIndex] == null ||
                waveSpawner.waves[nextIndex].allowedEnemies == null ||
                i >= waveSpawner.waves[nextIndex].allowedEnemies.Length)
            {
                nextWaveIcons[i].gameObject.SetActive(false);
                continue;
            }

            nextWaveIcons[i].gameObject.SetActive(true);
            nextWaveIcons[i].sprite = unknownSprite; // varsayılan

            // allowedEnemies dizisindeki prefab ile enemySprites eşleşmesi
            // Basit yaklaşım: aynı index kullan
            if (enemySprites != null && i < enemySprites.Length && enemySprites[i] != null)
                nextWaveIcons[i].sprite = enemySprites[i];
        }
    }
}
