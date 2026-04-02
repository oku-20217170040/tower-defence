using System.Collections.Generic;

/// <summary>
/// Sahnedeki tüm aktif kulelerin merkezi kaydı.
/// FindObjectsOfType<Tower>() yerine kullanılır — performans kazanımı.
/// Tower.OnEnable / OnDisable kendiliğinden kayıt/çıkış yapar.
/// </summary>
public static class TowerManager
{
    private static readonly List<Tower> towers = new List<Tower>();

    public static IReadOnlyList<Tower> All => towers;

    public static void Register(Tower t)
    {
        if (!towers.Contains(t))
            towers.Add(t);
    }

    public static void Unregister(Tower t)
    {
        towers.Remove(t);
    }
}
