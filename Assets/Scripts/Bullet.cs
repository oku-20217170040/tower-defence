using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;

    private int damage = 1;
    private EnemyHealth target;

    // Kill sayacı için: bu bullet'ı ateşleyen kule
    [HideInInspector] public Tower sourceTower;

    /// <summary>
    /// Kule ateş ederken çağrılır. Hasar, hedef ve kaynak kule set edilir.
    /// </summary>
    public void Initialize(int damage, Transform newTarget, Tower source = null)
    {
        this.damage = damage;
        this.sourceTower = source;
        Seek(newTarget);
    }

    public void Seek(EnemyHealth newTarget)
    {
        target = newTarget;
    }

    public void Seek(Transform newTarget)
    {
        if (newTarget == null) { target = null; return; }
        target = newTarget.GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        // Havuzdan geri alındığında state temizle
        target = null;
        sourceTower = null;
        damage = 1;
    }

    private void Update()
    {
        if (target == null ||
            !target.gameObject.activeInHierarchy ||
            target.CurrentHp <= 0)
        {
            ReturnToPool();
            return;
        }

        Vector3 dir = target.transform.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
    }

    private void HitTarget()
    {
        if (target != null)
            target.TakeDamage(damage, sourceTower);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (BulletPool.Instance != null)
            BulletPool.Instance.Return(this);
        else
            Destroy(gameObject);
    }
}
