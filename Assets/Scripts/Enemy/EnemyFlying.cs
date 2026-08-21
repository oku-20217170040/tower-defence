using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uçan düşman — EnemyMover'dan türer.
/// - Tehditlerden (kulelerden) kaçar ancak duvar/engel tanımaz.
/// - Buz ve Clock aura etkisine kısmi direnç gösterir.
/// </summary>
public class EnemyFlying : EnemyMover
{
    [Header("Uçuş Ayarları")]
    [Tooltip("Buz efektine direnç (0 = tam etki, 1 = hiç etki yok).")]
    [Range(0f, 1f)]
    public float freezeResistance = 0.4f;

    [Tooltip("Clock aura yavaşlatma direnci (0 = tam etki, 1 = hiç etki yok).")]
    [Range(0f, 1f)]
    public float slowResistance = 0.5f;

    // Engelleri yok sayan, ama kule tehdit puanlarını (threatCost) hesaba katan A* patika bulucu
    protected override List<Node> GetPathForEnemy(Vector2Int start, Vector2Int basePos)
    {
        if (pathfinder == null) return null;
        return pathfinder.FindPathIgnoreTerrain(start, basePos);
    }

    void LateUpdate()
    {
        // Uçan düşmanın rotasyonunu kendi içinde çöz (Çünkü prefab'ında EnemyLookDirection yok)
        Vector2 dir = CurrentMoveDirection;
        if (dir.sqrMagnitude > 0.001f)
        {
            // Orijinal kodda da olduğu gibi uçan düşman spritı için ayarlama (angle - 90f)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    // ── Efekt override'ları (direnç uygula) ─────────────────────

    public override void ApplyFreeze(float duration)
    {
        // Dirençle kısalt, sonra baz sınıfa ilet
        float reduced = duration * (1f - freezeResistance);
        if (reduced > 0f)
            base.ApplyFreeze(reduced);
    }

    public override void SetClockAura(bool active, float animMultiplier = 0.5f)
    {
        // Yavaşlamayı dirençle yumuşat
        float resistedMultiplier = Mathf.Lerp(animMultiplier, 1f, slowResistance);
        base.SetClockAura(active, resistedMultiplier);
    }
}
