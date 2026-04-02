using UnityEngine;

[RequireComponent(typeof(Tower))]
public class IceTower : MonoBehaviour
{
    [Header("Freeze Settings")]
    [Tooltip("Seviye 0 için donma süresi (sn).")]
    public float baseFreezeDuration = 1.5f;

    [Tooltip("Her seviye için donma süresine eklenecek değer.")]
    public float freezeDurationPerLevel = 0.5f;

    [Tooltip("Seviye 0 için atış aralığı (sn).")]
    public float baseFireInterval = 1.0f;

    [Tooltip("Her seviye için atış aralığına eklenecek değer (negatif ise hızlanır).")]
    public float fireIntervalPerLevel = -0.1f;

    [Header("Base Referansı (opsiyonel)")]
    [Tooltip("Üzerinde BaseHealth olan obje. Boş bırakırsan otomatik bulunur.")]
    public BaseHealth baseHealth;

    private Tower _tower;
    private float _fireTimer;

    private void Awake()
    {
        _tower = GetComponent<Tower>();
    }

    private void Start()
    {
        // Base inspector’dan atanmadıysa otomatik bul
        if (baseHealth == null)
        {
            baseHealth = FindObjectOfType<BaseHealth>();
        }

        _fireTimer = GetCurrentFireInterval();
    }

    private void Update()
    {
        // Base yoksa veya zaten yok edilmişse çalışmaya gerek yok
        if (baseHealth != null && baseHealth.isDestroyed)
            return;

        _fireTimer -= Time.deltaTime;
        if (_fireTimer > 0f)
            return;

        // Atış zamanı geldi -> uygun bir hedef var mı?
        EnemyMover target = FindBestTarget();

        if (target != null)
        {
            // Sadece HAREKET HALİNDE / DONMAMIŞ enemy dondurulur
            float freezeDuration = GetCurrentFreezeDuration();
            target.ApplyFreeze(freezeDuration);

            // Başarılı atış yaptıysak normal fire interval
            _fireTimer = GetCurrentFireInterval();
        }
        else
        {
            // Uygun hedef yoksa ateş ETME, kısa bir süre sonra tekrar dene
            _fireTimer = 0.1f;
        }
    }

    /// <summary>
    /// Menzil içindeki, BASE'E EN YAKIN ve DONMAMIŞ düşmanı bulur.
    /// Hiç yoksa null döner.
    /// </summary>
    private EnemyMover FindBestTarget()
    {
        float range = _tower != null ? _tower.CurrentRange : 2f;

        // Menzil içindeki tüm collider'lar
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        Transform baseTransform = baseHealth != null ? baseHealth.transform : null;

        EnemyMover best = null;
        float bestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyMover mover = hit.GetComponent<EnemyMover>();
            if (mover == null)
                continue;

            // ❄ ZATEN DONMUŞ enemy'leri ASLA hedef ALMA
            if (mover.IsFrozen)
                continue;

            // Aktif olmayan / disable enemy'leri de alma
            if (!mover.gameObject.activeInHierarchy)
                continue;

            float dist;

            // Base varsa -> base'e en yakın olanı seç
            if (baseTransform != null)
                dist = Vector2.Distance(mover.transform.position, baseTransform.position);
            else
                dist = Vector2.Distance(mover.transform.position, transform.position);

            if (dist < bestDist)
            {
                bestDist = dist;
                best = mover;
            }
        }

        return best; // hiç uygun hedef yoksa null
    }

    private float GetCurrentFreezeDuration()
    {
        int level = _tower != null ? _tower.CurrentLevel : 0;
        float duration = baseFreezeDuration + level * freezeDurationPerLevel;
        return Mathf.Max(0.1f, duration);
    }

    private float GetCurrentFireInterval()
    {
        int level = _tower != null ? _tower.CurrentLevel : 0;
        float interval = baseFireInterval + level * fireIntervalPerLevel;
        return Mathf.Max(0.1f, interval);
    }

    private void OnDrawGizmosSelected()
    {
        if (_tower == null)
            _tower = GetComponent<Tower>();

        float range = _tower != null ? _tower.CurrentRange : 2f;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
