using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Tower))]
public class ClockTower : MonoBehaviour
{
    [Header("Clock Tower Ayarları")]
    [Tooltip("Tüm seviyeler için temel yavaşlatma çarpanı (ör: 0.7 = %30 yavaşlatma).")]
    public float baseSlowMultiplier = 0.7f;

    [Tooltip("Her seviye başına ekstra yavaşlatma (ör: 0.1 = ek %10 yavaşlatma).")]
    public float slowPerLevel = 0.0f;

    [Tooltip("Gizmos ve hesaplamalar için kullanılan menzil (runtime'da Tower.CurrentRange ile eşitlenir).")]
    public float clockRange = 2f;

    private Tower _tower;



    [Header("Barrel Spin (Clock Tower Only)")]
    public bool spinBarrels = true;

    [Tooltip("Lv1 dönüş hızı (deg/sn).")]
    public float baseBarrelSpinSpeed = 180f;

    [Tooltip("Level başına eklenecek dönüş hızı (deg/sn). Lv2=base+1*per, Lv3=base+2*per")]
    public float spinSpeedPerLevel = 90f;

    [Tooltip("Saat yönünde dönsün mü?")]
    public bool spinClockwise = true;

    [Tooltip("Local space'te döndür (önerilir).")]
    public bool spinInLocalSpace = true;

    [Tooltip("Tower.cs içindeki barrelVisualsByLevel kullanılacak. Boşsa child'larda bu kelimeyi içeren objeler yakalanır.")]
    public string barrelNameContains = "Barrel";

    private Transform[] _cachedBarrels;
    private readonly Dictionary<Transform, Quaternion> _barrelInitialLocalRot = new Dictionary<Transform, Quaternion>();
    private int _lastLevel = -999;
    private float _spinAngleZ = 0f;
    // Bu Clock Tower’ın şu anda yavaşlattığı düşmanlar
    private readonly HashSet<EnemyMover> _insideEnemies = new HashSet<EnemyMover>();

    // 🔹 TÜM Clock Tower’lar arasında paylaşılan static yapılar 🔹

    // Her düşmanın "orijinal" hızı (ilk kez görüldüğünde kaydedilir)
    private static readonly Dictionary<EnemyMover, float> _baseSpeeds =
        new Dictionary<EnemyMover, float>();

    // Her düşman için: hangi Clock Tower ne kadar yavaşlatıyor
    private static readonly Dictionary<EnemyMover, Dictionary<ClockTower, float>> _slowSources =
        new Dictionary<EnemyMover, Dictionary<ClockTower, float>>();

    private void Awake()
    {
        _tower = GetComponent<Tower>();
        CacheBarrels();
        HandleLevelChange(true);
    }

    private void OnEnable()
    {
        SyncRange();
    }

    private void OnDisable()
    {
        // Bu Clock devre dışıyken, kendi yavaşlatmalarını temizle
        ClearAllSlowFromThisClock();
    }

    private void Update()
    {
        SyncRange();
        UpdateSlowOnEnemies();

        HandleLevelChange();
        SpinBarrels();
    }

    /// <summary>
    /// Clock menzilini, Tower.CurrentRange ile eşitler.
    /// </summary>
    private void SyncRange()
    {
        if (_tower == null) return;
        clockRange = _tower.CurrentRange;
    }

    /// <summary>
    /// Her frame düşmanları tarar, menzil içine giren/çıkanları tespit eder
    /// ve static tablolar üzerinden hızlarını günceller.
    /// </summary>
    private void UpdateSlowOnEnemies()
    {
        // Menzil içindeki tüm collider’ları bul
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, clockRange);

        HashSet<EnemyMover> currentlyInside = new HashSet<EnemyMover>();

        foreach (Collider2D hit in hits)
        {
            EnemyMover em = hit.GetComponent<EnemyMover>();
            if (em == null)
                continue;

            currentlyInside.Add(em);

            // Bu düşman daha önce bu Clock tarafından işlenmemişse ekle
            if (!_insideEnemies.Contains(em))
            {
                _insideEnemies.Add(em);
                AddOrUpdateSlow(em);
                AddTintForThisClock(em);
            }
            else
            {
                // Zaten içerdeyse, sadece slow değerini güncelle (level değişmiş olabilir)
                AddOrUpdateSlow(em);
            }
        }

        // Artık menzil dışında kalan düşmanları tespit et
        // (_insideEnemies - currentlyInside)
        var toRemove = new List<EnemyMover>();
        foreach (EnemyMover em in _insideEnemies)
        {
            if (!currentlyInside.Contains(em))
            {
                toRemove.Add(em);
            }
        }

        // Menzilden çıkanların slow’unu temizle
        foreach (EnemyMover em in toRemove)
        {
            _insideEnemies.Remove(em);
            RemoveTintForThisClock(em);
            RemoveSlow(em);
        }
    }

    /// <summary>
    /// Bu Clock Tower, verilen düşmana slow uygular veya günceller.
    /// (Static tablolarda kayıt tutar, düşmanın hızını yeniden hesaplar.)
    /// </summary>
    private void AddOrUpdateSlow(EnemyMover em)
    {
        if (em == null) return;

        // Düşmanın base hızını kaydet
        if (!_baseSpeeds.ContainsKey(em))
        {
            _baseSpeeds[em] = em.moveSpeed;
        }

        // Bu düşman için slow kaynak tablosu
        if (!_slowSources.TryGetValue(em, out var sources))
        {
            sources = new Dictionary<ClockTower, float>();
            _slowSources[em] = sources;
        }

        float slowMultiplier = GetCurrentSlowMultiplier();

        // Bu Clock için slow değerini güncelle
        sources[this] = slowMultiplier;

        // Şimdi bu düşmanın geçerli hızını güncelle
        RecalculateEnemySpeed(em);
    }

    /// <summary>
    /// Bu Clock’un belirli bir düşman üzerindeki etkisini kaldırır.
    /// </summary>
    private void RemoveSlow(EnemyMover em)
    {
        if (em == null) return;

        if (_slowSources.TryGetValue(em, out var sources))
        {
            if (sources.Remove(this))
            {
                // Bu Clock, bu düşmana slow uygulamayı bıraktı
                if (sources.Count == 0)
                {
                    // Artık hiçbir Clock etkilemiyorsa, düşman orijinal hızına döner
                    if (_baseSpeeds.TryGetValue(em, out float baseSpeed))
                    {
                        em.moveSpeed = baseSpeed;
                    }
                    _slowSources.Remove(em);
                    // baseSpeed kaydını istersen tutmaya devam edebilirsin (ileride tekrar lazım olabilir)
                }
                else
                {
                    // Hâlâ başka Clock’lar etkiliyor, yeniden hesapla
                    RecalculateEnemySpeed(em);
                }
            }
        }
    }

    /// <summary>
    /// Bu Clock Tower’ın etkilediği tüm düşmanlardan yavaşlatma etkisini kaldırır.
    /// (Disable/Destroy durumunda çağrılır.)
    /// </summary>
    private void ClearAllSlowFromThisClock()
    {
        var toClear = new List<EnemyMover>(_insideEnemies);
        foreach (EnemyMover em in toClear)
        {
            RemoveSlow(em);
        }
        _insideEnemies.Clear();
    }

    /// <summary>
    /// Verilen düşman için tüm Clock Tower’ların slow çarpanlarına bakar
    /// ve sadece EN ÇOK yavaşlatanı uygular.
    /// </summary>
    private void RecalculateEnemySpeed(EnemyMover em)
    {
        if (em == null) return;

        if (!_baseSpeeds.TryGetValue(em, out float baseSpeed))
        {
            baseSpeed = em.moveSpeed;
            _baseSpeeds[em] = baseSpeed;
        }

        if (_slowSources.TryGetValue(em, out var sources) && sources.Count > 0)
        {
            // En küçük çarpanı bul (0.5, 0.7, 0.9 -> 0.5 en çok yavaşlatan)
            float bestMultiplier = 1f;

            foreach (float mul in sources.Values)
            {
                if (mul < bestMultiplier)
                    bestMultiplier = mul;
            }

            em.moveSpeed = baseSpeed * bestMultiplier;
        }
        else
        {
            // Hiç slow yoksa orijinal hıza dön
            em.moveSpeed = baseSpeed;
        }
    }

    /// <summary>
    /// Şu anki Clock level’ına göre slow çarpanı.
    /// Örneğin:
    ///   baseSlowMultiplier = 0.7, slowPerLevel = 0.05
    ///   Level 0 -> 0.70
    ///   Level 1 -> 0.65
    ///   Level 2 -> 0.60  (daha çok yavaşlatır)
    /// </summary>
    private float GetCurrentSlowMultiplier()
    {
        int level = _tower != null ? _tower.CurrentLevel : 0;
        float slow = baseSlowMultiplier - level * slowPerLevel;
        // Aşırı saçma değerleri engelle
        return Mathf.Clamp(slow, 0.1f, 1f);
    }

    // ------------------------
    // Visual: Yellow Tint while slowed by this Clock
    // ------------------------
    private void AddTintForThisClock(EnemyMover em)
    {
        if (em == null) return;
        // Enemy prefabına ClockSlowTint ekle (SpriteRenderer'ların bulunduğu root/parent).
        var tint = em.GetComponent<ClockSlowTint>();
        if (tint == null) tint = em.GetComponentInChildren<ClockSlowTint>(true);
        if (tint != null) tint.AddSlowSource();
    }

    private void RemoveTintForThisClock(EnemyMover em)
    {
        if (em == null) return;
        var tint = em.GetComponent<ClockSlowTint>();
        if (tint == null) tint = em.GetComponentInChildren<ClockSlowTint>(true);
        if (tint != null) tint.RemoveSlowSource();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.6f, 1f, 0.25f);

        float r = clockRange;

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


    // ------------------------
    // Barrel Spin (Clock Only)
    // ------------------------
    private void CacheBarrels()
    {
        // 1) Öncelik: Tower.cs içindeki barrelVisualsByLevel
        if (_tower != null && _tower.barrelVisualsByLevel != null && _tower.barrelVisualsByLevel.Length > 0)
        {
            var list = new List<Transform>(_tower.barrelVisualsByLevel.Length);
            for (int i = 0; i < _tower.barrelVisualsByLevel.Length; i++)
            {
                var go = _tower.barrelVisualsByLevel[i];
                if (go == null) continue;
                list.Add(go.transform);
            }
            _cachedBarrels = list.ToArray();
            FinalizeBarrelCache();
            return;
        }

        // 2) Fallback: Child'larda isimden yakala
        var all = GetComponentsInChildren<Transform>(true);
        var dyn = new List<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            var tr = all[i];
            if (tr == null || tr == transform) continue;

            if (!string.IsNullOrEmpty(barrelNameContains) &&
                tr.name.ToLower().Contains(barrelNameContains.ToLower()))
            {
                dyn.Add(tr);
            }
        }

        _cachedBarrels = dyn.ToArray();
        FinalizeBarrelCache();
    }

    private void FinalizeBarrelCache()
    {
        _barrelInitialLocalRot.Clear();
        if (_cachedBarrels == null) return;

        for (int i = 0; i < _cachedBarrels.Length; i++)
        {
            var b = _cachedBarrels[i];
            if (b == null) continue;

            if (!_barrelInitialLocalRot.ContainsKey(b))
                _barrelInitialLocalRot[b] = b.localRotation;
        }
    }

    private void HandleLevelChange(bool force = false)
    {
        int lvl = (_tower != null) ? _tower.CurrentLevel : 0;
        if (!force && lvl == _lastLevel) return;

        _lastLevel = lvl;
        CacheBarrels();
    }

    private float GetCurrentSpinSpeed()
    {
        int lvl = (_tower != null) ? _tower.CurrentLevel : 0;
        return baseBarrelSpinSpeed + (lvl * spinSpeedPerLevel);
    }

    private void SpinBarrels()
    {
        if (!spinBarrels) return;

        if (_cachedBarrels == null || _cachedBarrels.Length == 0)
            CacheBarrels();

        if (_cachedBarrels == null || _cachedBarrels.Length == 0)
            return;

        float dir = spinClockwise ? -1f : 1f;
        float speed = GetCurrentSpinSpeed();
        _spinAngleZ = Mathf.Repeat(_spinAngleZ + (speed * Time.deltaTime * dir), 360f);

        Quaternion spinQ = Quaternion.Euler(0f, 0f, _spinAngleZ);

        for (int i = 0; i < _cachedBarrels.Length; i++)
        {
            var b = _cachedBarrels[i];
            if (b == null) continue;
            if (!b.gameObject.activeInHierarchy) continue;

            if (!_barrelInitialLocalRot.TryGetValue(b, out var baseRot))
            {
                baseRot = b.localRotation;
                _barrelInitialLocalRot[b] = baseRot;
            }

            if (spinInLocalSpace)
                b.localRotation = baseRot * spinQ;
            else
                b.rotation = baseRot * spinQ;
        }
    }
}