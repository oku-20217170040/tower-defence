using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FloatingText prefabları için object pool.
/// EnemyHealth.TakeDamage() → FloatingTextPool.Instance.Show(...)
///
/// KURULUM: Sahneye boş GameObject ekle, bu scripti ekle,
/// FloatingText prefabını "prefab" alanına ata.
/// </summary>
public class FloatingTextPool : MonoBehaviour
{
    public static FloatingTextPool Instance { get; private set; }

    [Header("References")]
    [Tooltip("FloatingText bileşeni olan TextMeshPro prefab.")]
    public FloatingText prefab;

    [Header("Pool Settings")]
    public int poolSize = 20;

    [Header("Colors")]
    public Color normalColor  = Color.white;
    public Color criticalColor = new Color(1f, 0.9f, 0.2f);  // Sarı (boss/yüksek hasar)
    public int   criticalThreshold = 10;                       // Bu eşiğin üstü sarı

    private readonly Queue<FloatingText> pool = new Queue<FloatingText>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            FloatingText ft = Instantiate(prefab, transform);
            ft.gameObject.SetActive(false);
            pool.Enqueue(ft);
        }
    }

    public void Show(int damage, Vector3 worldPos)
    {
        if (prefab == null) return;

        FloatingText ft;
        if (pool.Count > 0)
        {
            ft = pool.Dequeue();
            ft.gameObject.SetActive(true);
        }
        else
        {
            ft = Instantiate(prefab, transform);
        }

        Color color = damage >= criticalThreshold ? criticalColor : normalColor;

        // Küçük rastgele offset — aynı anda birden fazla hasar üst üste gelmesin
        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.3f), 0f);
        ft.Show(damage, worldPos + offset, color);
    }

    public void Return(FloatingText ft)
    {
        ft.gameObject.SetActive(false);
        pool.Enqueue(ft);
    }
}
