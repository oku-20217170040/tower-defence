using UnityEngine;

/// <summary>
/// Oyunun baþlangýcýnda grid'i üretir ve sahnedeki spawner & base referanslarýna göre düþmaný spawn eder.
/// Artýk Spawner ve Base pozisyonlarýný sahnede manuel olarak belirleyebilirsin.
/// </summary>
public class GameInit : MonoBehaviour
{
    [Header("References")]
    public GridManager grid;            // Grid sistemi
    public Transform baseTransform;     // Base objesi (manuel sahnede seçilir)
    public Transform spawnerTransform;  // Spawner objesi (manuel sahnede seçilir)
    public EnemyMover enemyPrefab;      // Düþman prefab

    void Start()
    {
        // Grid oluþturulmadýysa bir defa oluþtur
        if (grid.grid == null)
            grid.Generate();

        // Base koordinatýný grid'e göre hesapla
        Vector2Int baseCoords = WorldToGridCoords(baseTransform.position);
        Vector2Int startCoords = WorldToGridCoords(spawnerTransform.position);

        // Düþmaný instantiate et
        EnemyMover enemy = Instantiate(enemyPrefab, spawnerTransform.position, Quaternion.identity);

        // Baþlangýç ve hedef koordinatlarýný enemy'e ata
        enemy.startCoords = startCoords;
        enemy.baseCoords = baseCoords;
    }

    /// <summary>
    /// Dünya (Unity) pozisyonunu grid koordinatýna çevirir.
    /// </summary>
    private Vector2Int WorldToGridCoords(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x - grid.origin.x - 0.5f);
        int y = Mathf.RoundToInt(worldPos.y - grid.origin.y - 0.5f);
        return new Vector2Int(x, y);
    }
}
