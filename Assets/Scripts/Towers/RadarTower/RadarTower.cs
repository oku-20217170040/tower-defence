using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Tower))]
public class RadarTower : MonoBehaviour
{
    [Header("Radar Ayarları")]
    [Tooltip("Tüm seviyeler için temel menzil çarpanı (ör: 1.2 = +%20 range).")]
    public float baseMultiplier = 1.2f;

    [Tooltip("Her seviye başına ekstra çarpan (ör: 0.1 = +%10).")]
    public float multiplierPerLevel = 0.1f;

    [Tooltip("Gizmos ve hesaplamalar için kullanılan menzil (runtime'da Tower.CurrentRange ile eşitlenir).")]
    public float radarRange = 2f;

    private Tower _tower;                 // Aynı GameObject'teki Tower script
    private readonly List<Tower> _affectedTowers = new List<Tower>();

    private int _lastLevel = -1;

    private void Awake()
    {
        _tower = GetComponent<Tower>();
    }

    private void OnEnable()
    {
        UpdateRadarRange();
        RecalculateBuffedTowers();
    }

    private void OnDisable()
    {
        ClearAllBonuses();
    }

    private void Update()
    {
        if (_tower == null)
            return;

        // Seviye değiştiyse buff’ı güncelle
        if (_tower.CurrentLevel != _lastLevel)
        {
            _lastLevel = _tower.CurrentLevel;
            UpdateRadarRange();
            RecalculateBuffedTowers();
        }
    }

    /// <summary>
    /// radarRange'i Tower.CurrentRange ile eşitler.
    /// </summary>
    private void UpdateRadarRange()
    {
        if (_tower == null)
            return;

        radarRange = _tower.CurrentRange;
    }

    /// <summary>
    /// Bu Radar’ın etkileyeceği kuleleri tekrar tarar.
    /// Sadece MachineGun, Cannon, Ice ve Laser kulelerine etki eder.
    /// Kendine ve Clock/Radar kulelerine etki ETMEZ.
    ///
    /// Bir kule aynı anda birden fazla Radar alanındaysa,
    /// Tower.cs tarafında sadece EN YÜKSEK çarpana sahip Radar dikkate alınır.
    /// </summary>
    private void RecalculateBuffedTowers()
    {
        ClearAllBonuses();

        if (_tower == null)
            return;

        UpdateRadarRange();
        float multiplier = GetCurrentMultiplier();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radarRange);

        foreach (Collider2D hit in hits)
        {
            Tower t = hit.GetComponent<Tower>();
            TryBuffTower(t, multiplier);
        }
    }

    /// <summary>
    /// Dışarıdan (örneğin yeni kule spawn olduğunda) çağrılır:
    /// Eğer verilen kule bu radarın menzilindeyse buff uygular.
    /// </summary>
    public void TryBuffTower(Tower t)
    {
        float multiplier = GetCurrentMultiplier();
        TryBuffTower(t, multiplier);
    }

    // Asıl iş burada
    private void TryBuffTower(Tower t, float multiplier)
    {
        if (_tower == null || t == null)
            return;

        // Kendini etkileme
        if (t == _tower)
            return;

        // Clock ve Radar kulelerine etki etme
        if (t.towerType == TowerType.Clock || t.towerType == TowerType.Radar)
            return;

        // Menzil kontrolü
        float dist = Vector2.Distance(transform.position, t.transform.position);
        if (dist > radarRange)
            return;

        t.ApplyRadarBonus(this, multiplier);

        if (!_affectedTowers.Contains(t))
            _affectedTowers.Add(t);
    }

    /// <summary>
    /// Radardan gelen çarpan (ör: 1.2, 1.3, 1.4 vb.)
    /// </summary>
    private float GetCurrentMultiplier()
    {
        int level = _tower != null ? _tower.CurrentLevel : 0;
        return baseMultiplier + level * multiplierPerLevel;
    }

    /// <summary>
    /// Bu Radar’ın verdiği tüm buff’ları temizler (disable/destroy’de çağrılır).
    /// </summary>
    private void ClearAllBonuses()
    {
        foreach (Tower t in _affectedTowers)
        {
            if (t != null)
            {
                t.RemoveRadarBonus(this);
            }
        }
        _affectedTowers.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.1f, 0.8f, 0.25f);

        float r = radarRange;

        // Editörde daha düzgün görmek için Tower varsa ordan çek
        Tower t = GetComponent<Tower>();
        if (t != null)
        {
            if (Application.isPlaying)
            {
                r = t.CurrentRange;
            }
            else
            {
                if (t.rangeByLevel != null && t.rangeByLevel.Length > 0)
                    r = t.rangeByLevel[0];
            }
        }

        Gizmos.DrawWireSphere(transform.position, r);
    }
}
