using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Tüm dalgalar bittiğinde "Kazandınız" panelini açar ve oyunu durdurur.
/// Restart butonuyla sahneyi yeniden başlatır.
/// </summary>
public class WinManager : MonoBehaviour
{
    [Header("References")]
    public GameObject winPanel;
    public TextMeshProUGUI winText;
    public Button restartButton;

    private bool hasWon = false;

    void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    /// <summary>
    /// WaveSpawner tarafından çağrılır.
    /// </summary>
    public void ShowWin()
    {
        if (hasWon) return;
        hasWon = true;

        if (winPanel != null)
            winPanel.SetActive(true);

        if (winText != null)
            winText.text = "Kazandınız!";

        // Oyunu durdur
        Time.timeScale = 0f;

        Debug.Log("🏆 KAZANDINIZ!");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
