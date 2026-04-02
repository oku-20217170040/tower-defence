using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public EnemyMover prefab;
    public int poolSize = 20;

    private Queue<EnemyMover> pool = new Queue<EnemyMover>();

    void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            EnemyMover obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public EnemyMover GetFromPool()
    {
        if (pool.Count == 0)
        {
            EnemyMover extra = Instantiate(prefab, transform);
            extra.gameObject.SetActive(false);
            pool.Enqueue(extra);
        }

        EnemyMover enemy = pool.Dequeue();
        return enemy;
    }

    public void ReturnToPool(EnemyMover enemy)
    {
        enemy.gameObject.SetActive(false);
        pool.Enqueue(enemy);
    }
}
