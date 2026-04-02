/// <summary>
/// DEPRECATED — Bu script artık kullanılmıyor.
/// Tüm game over / win logic EndGameManager.cs içinde.
/// Sahnedeki GameObject'ten bu scripti kaldırıp EndGameManager kullan.
/// </summary>
public class GameOverManager : UnityEngine.MonoBehaviour
{
    private void Awake()
    {
        UnityEngine.Debug.LogWarning("[GameOverManager] Bu script deprecated. EndGameManager kullan.");
        enabled = false;
    }
}
