using UnityEngine;

/// <summary>
/// Düşmanın canını tutar ve hasar aldığında ölmesini sağlar.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public int goldReward = 10;   // Bu enemy öldüğünde verilecek altın

    [Header("Health Settings")]
    public int maxHp = 3;   // Düşmanın maksimum canı
    private int currentHp;  // Şu anki can

    // ✅ Wave çarpanı için base değer
    private int baseMaxHp;
    private bool baseCached;

    // UI için read-only property'ler
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    void Awake()
    {
        CacheBaseIfNeeded();
    }

    void OnEnable()
    {
        // Her aktif olduğunda canı yenile
        // (pool kullanıyorsan bu önemli)
        currentHp = maxHp;
    }

    private void CacheBaseIfNeeded()
    {
        if (baseCached) return;
        baseMaxHp = maxHp;
        baseCached = true;
    }

    /// <summary>
    /// ✅ WaveSpawner/EnemyMover spawn sonrası çağırır.
    /// hpMul: maxHP'yi çarpar ve currentHP'yi max'a eşitler.
    /// </summary>
    public void ApplyWaveHpMultiplier(float hpMul)
    {
        CacheBaseIfNeeded();

        hpMul = Mathf.Max(0.05f, hpMul); // güvenlik

        // base üzerinden setle ki tekrar tekrar çarpılıp şişmesin
        maxHp = Mathf.Max(1, Mathf.RoundToInt(baseMaxHp * hpMul));

        // Spawn sonrası full can başlasın
        currentHp = maxHp;
    }

    /// <summary>
    /// Kule mermilerinden hasar alındığında çağrılır.
    /// killer: öldüren kule (kill sayacı için, opsiyonel)
    /// </summary>
    public void TakeDamage(int damage, Tower killer = null)
    {
        currentHp -= damage;

        // Floating damage number
        FloatingTextPool.Instance?.Show(damage, transform.position);

        if (currentHp <= 0)
        {
            Die(killer);

            if (TowerPlacer.Instance != null)
                TowerPlacer.Instance.AddGold(goldReward);
        }
    }

    /// <summary>
    /// Düşman öldüğünde yapılacak işlem.
    /// </summary>
    void Die(Tower killer = null)
    {
        // Kill sayacı
        killer?.RegisterKill();

        // Skor
        ScoreManager.Instance?.RegisterKill(goldReward);

        // Q-Learning: ölüm episode'i bildir
        EnemyMover mover = GetComponent<EnemyMover>();
        if (mover != null && QLearningManager.Instance != null)
        {
            var steps = mover.GetEpisodeSteps();
            if (steps != null && steps.Count > 0)
                QLearningManager.Instance.ApplyDeathEpisode(steps);
        }

        // ThreatMap: ölüm ısısı ekle
        ThreatMap.Instance?.RegisterEnemyDeath(transform.position);

        gameObject.SetActive(false);
    }
}
