using UnityEngine;

public class EnemyLookDirection : MonoBehaviour
{
    EnemyMover mover;

    void Awake()
    {
        mover = GetComponent<EnemyMover>();
    }

    void Update()
    {
        if (mover == null) return;

        Vector2 dir = mover.CurrentMoveDirection;

        // Hareket yoksa döndürme
        if (dir.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Sprite yukarý bakýyorsa -90, saða bakýyorsa 0 kullan
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }
}
