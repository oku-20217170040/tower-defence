using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bullet prefabları için genel amaçlı object pool.
/// Instantiate/Destroy yerine Get/Return kullanılır.
/// Her prefab referansı için ayrı bir Queue tutar.
/// </summary>
public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Her prefab başına önceden oluşturulacak bullet sayısı.")]
    public int initialPoolSize = 20;

    // prefab instance ID -> havuz
    private readonly Dictionary<int, Queue<Bullet>> pools = new Dictionary<int, Queue<Bullet>>();
    // bullet -> hangi prefabdan geldiği
    private readonly Dictionary<Bullet, int> originPrefabId = new Dictionary<Bullet, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Havuzdan bir bullet al. Havuz doluysa genişletilir.
    /// </summary>
    public Bullet Get(Bullet prefab, Vector3 position, Quaternion rotation)
    {
        int id = prefab.GetInstanceID();

        if (!pools.ContainsKey(id))
            pools[id] = new Queue<Bullet>();

        Queue<Bullet> pool = pools[id];

        Bullet bullet;
        if (pool.Count > 0)
        {
            bullet = pool.Dequeue();
            bullet.transform.SetPositionAndRotation(position, rotation);
            bullet.gameObject.SetActive(true);
        }
        else
        {
            bullet = Instantiate(prefab, position, rotation, transform);
            originPrefabId[bullet] = id;
        }

        return bullet;
    }

    /// <summary>
    /// Kullanılan bullet'ı havuza geri ver.
    /// </summary>
    public void Return(Bullet bullet)
    {
        if (bullet == null) return;

        bullet.gameObject.SetActive(false);

        int id;
        if (!originPrefabId.TryGetValue(bullet, out id)) return;

        if (!pools.ContainsKey(id))
            pools[id] = new Queue<Bullet>();

        pools[id].Enqueue(bullet);
    }
}
