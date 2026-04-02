using System.Collections.Generic;

/// <summary>
/// Sahnedeki tüm aktif düşmanların merkezi kaydı.
/// FindObjectsOfType<EnemyMover>() yerine kullanılır — performans kazanımı.
/// EnemyMover.OnEnable / OnDisable kendiliğinden kayıt/çıkış yapar.
/// </summary>
public static class EnemyManager
{
    private static readonly List<EnemyMover> enemies = new List<EnemyMover>();

    public static IReadOnlyList<EnemyMover> Active => enemies;

    public static void Register(EnemyMover e)
    {
        if (!enemies.Contains(e))
            enemies.Add(e);
    }

    public static void Unregister(EnemyMover e)
    {
        enemies.Remove(e);
    }
}
