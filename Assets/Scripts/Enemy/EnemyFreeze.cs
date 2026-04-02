using UnityEngine;

/// <summary>
/// Enemy'leri dondurup tekrar çözen yardýmcý bileþen.
/// Hareketi durdurmak için EnemyMover'ý enable/disable eder.
/// </summary>
[RequireComponent(typeof(EnemyMover))]
public class EnemyFreeze : MonoBehaviour
{
    private EnemyMover mover;
    private float freezeTimer;
    private bool isFrozen;

    /// <summary>
    /// Dýþarýdan okunabilir donmuþ mu bilgisi.
    /// </summary>
    public bool IsFrozen => isFrozen;

    private void Awake()
    {
        mover = GetComponent<EnemyMover>();
    }

    /// <summary>
    /// Enemy'i belirtilen süre boyunca dondur.
    /// Eðer zaten donuksa ve yeni süre daha uzunsa süreyi günceller.
    /// </summary>
    public void Freeze(float duration)
    {
        if (duration <= 0f) return;

        // Daha uzun bir donma süresi gelirse onu baz al
        if (duration > freezeTimer)
            freezeTimer = duration;

        if (!isFrozen)
        {
            isFrozen = true;
            if (mover != null)
                mover.enabled = false; // Hareket scriptini kapat
        }
    }

    private void Update()
    {
        if (!isFrozen) return;

        freezeTimer -= Time.deltaTime;
        if (freezeTimer <= 0f)
        {
            freezeTimer = 0f;
            isFrozen = false;

            if (mover != null)
                mover.enabled = true; // Hareket scriptini tekrar aç
        }
    }
}
