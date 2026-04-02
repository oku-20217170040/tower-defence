using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Tower))]
public class LaserTower : MonoBehaviour
{
    [Header("Laser Settings")]
    [Tooltip("Lazer ışınının gidebileceği maksimum mesafe (hasar mesafesi).")]
    public float laserMaxDistance = 50f;

    [Tooltip("Işın kaç saniye görünsün (animasyon süresi).")]
    public float beamDuration = 0.15f;

    [Tooltip("Işının çarpacağı katmanlar (Enemy collider layer'ı). Boş bırakırsan her şeyi tarar.")]
    public LayerMask hitMask = ~0;

    [Header("Beam Visual (Prefab)")]
    [Tooltip("Işın VFX prefabı (Sprite/Animator). Pivot'u sol (başlangıç) tarafta ve +X yönüne bakacak şekilde ayarla.")]
    public GameObject beamPrefab;

    [Tooltip("Beam prefabının child'ında SpriteRenderer varsa bunu kullanıp X ekseninde uzatır. Yoksa sadece prefabı spawn eder.")]
    public bool stretchBeamOnX = true;

    private Tower tower;
    private float fireCooldown;

    private BaseHealth baseHealth;

    private void Awake()
    {
        tower = GetComponent<Tower>();

        // Laser kendi hasarını raycast ile verdiği için klasik mermi sistemini kapat
        if (tower != null)
            tower.bulletPrefab = null;
    }

    private void Start()
    {
        baseHealth = FindObjectOfType<BaseHealth>();
    }

    private void Update()
    {
        if (tower == null) return;
        if (baseHealth != null && baseHealth.isDestroyed) return;

        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0f) return;

        EnemyMover target = FindBestTarget();
        if (target == null)
        {
            fireCooldown = 0.1f;
            return;
        }

        FireLaser(target);

        float rate = Mathf.Max(tower.CurrentFireRate, 0.01f);
        fireCooldown = 1f / rate;
    }

    /// <summary>
    /// Menzil içindeki düşmanlar arasından BASE'E EN YAKIN olanı seçer.
    /// </summary>
    private EnemyMover FindBestTarget()
    {
        float range = tower.CurrentRange;
        Vector3 towerPos = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(towerPos, range);
        if (hits == null || hits.Length == 0) return null;

        Transform baseTransform = baseHealth != null ? baseHealth.transform : null;

        EnemyMover best = null;
        float bestDistToBase = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyMover mover = hit.GetComponent<EnemyMover>();
            if (mover == null) continue;
            if (!mover.gameObject.activeInHierarchy) continue;

            float distToBase = (baseTransform != null)
                ? Vector2.Distance(mover.transform.position, baseTransform.position)
                : Vector2.Distance(mover.transform.position, towerPos);

            if (distToBase < bestDistToBase)
            {
                bestDistToBase = distToBase;
                best = mover;
            }
        }

        return best;
    }

    /// <summary>
    /// Hedefe doğru bir doğrultu seçer ve bu doğrultudaki TÜM EnemyHealth'lere tek seferlik hasar verir.
    /// Görsel olarak beamPrefab spawn eder ve beamDuration sonra yok eder.
    /// </summary>
    private void FireLaser(EnemyMover primaryTarget)
    {
        if (primaryTarget == null || tower == null) return;

        Vector3 origin = transform.position;
        Vector3 dir = (primaryTarget.transform.position - origin).normalized;

        float maxDistance = Mathf.Max(laserMaxDistance, tower.CurrentRange);
        int damage = tower.CurrentDamage;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, maxDistance, hitMask);

        HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();
        float furthestHitDistance = 0f;

        if (hits != null && hits.Length > 0)
        {
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null) continue;

                EnemyHealth eh = hit.collider.GetComponent<EnemyHealth>();
                if (eh != null && !damaged.Contains(eh))
                {
                    eh.TakeDamage(damage);
                    damaged.Add(eh);
                }

                if (hit.distance > furthestHitDistance)
                    furthestHitDistance = hit.distance;
            }
        }

        float lineDistance = furthestHitDistance > 0f ? furthestHitDistance : maxDistance;
        Vector3 endPoint = origin + dir * lineDistance;

        SpawnBeamVFX(origin, endPoint);
    }

    private void SpawnBeamVFX(Vector3 start, Vector3 end)
    {
        if (beamPrefab == null) return;

        Vector3 dir = (end - start);
        float length = dir.magnitude;
        if (length <= 0.001f) return;

        Vector3 mid = start + dir * 0.5f;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject beam = Instantiate(beamPrefab, mid, Quaternion.Euler(0f, 0f, angle));

        if (stretchBeamOnX)
        {
            SpriteRenderer sr = beam.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (sr.drawMode != SpriteDrawMode.Simple)
                {
                    sr.size = new Vector2(length, sr.size.y);
                }
                else
                {
                    Vector3 s = beam.transform.localScale;
                    beam.transform.localScale = new Vector3(length, s.y, s.z);
                }
            }
            else
            {
                Vector3 s = beam.transform.localScale;
                beam.transform.localScale = new Vector3(length, s.y, s.z);
            }
        }

        Destroy(beam, beamDuration);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Tower t = GetComponent<Tower>();
        float r = t != null ? t.CurrentRange : 2f;
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
#endif
}
