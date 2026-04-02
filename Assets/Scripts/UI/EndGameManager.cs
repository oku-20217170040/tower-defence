using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndGameManager : MonoBehaviour
{
    [Header("Lose Condition")]
    [SerializeField] private BaseHealth baseHealth;

    [Header("Panels")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject winPanel;

    [Header("Optional Texts")]
    [SerializeField] private TextMeshProUGUI loseText;
    [SerializeField] private TextMeshProUGUI winText;
    [Tooltip("Win/Lose ekranında skor özeti için TMP alanı (opsiyonel).")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Lose Buttons")]
    [SerializeField] private Button loseRestartButton;
    [SerializeField] private Button loseMainMenuButton;

    [Header("Win Buttons")]
    [SerializeField] private Button winNextLevelButton;
    [SerializeField] private Button winRestartButton;
    [SerializeField] private Button winMainMenuButton;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool ended;

    private void Awake()
    {
        Time.timeScale = 1f;
        ended = false;

        if (losePanel) losePanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        // Tower Defence: cursor görünür kalsın
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        // Lose
        if (loseRestartButton)
        {
            loseRestartButton.onClick.RemoveAllListeners();
            loseRestartButton.onClick.AddListener(RestartLevel);
        }
        if (loseMainMenuButton)
        {
            loseMainMenuButton.onClick.RemoveAllListeners();
            loseMainMenuButton.onClick.AddListener(GoMainMenu);
        }

        // Win
        if (winNextLevelButton)
        {
            winNextLevelButton.onClick.RemoveAllListeners();
            winNextLevelButton.onClick.AddListener(NextLevel);
        }
        if (winRestartButton)
        {
            winRestartButton.onClick.RemoveAllListeners();
            winRestartButton.onClick.AddListener(RestartLevel);
        }
        if (winMainMenuButton)
        {
            winMainMenuButton.onClick.RemoveAllListeners();
            winMainMenuButton.onClick.AddListener(GoMainMenu);
        }
    }

    private void Update()
    {
        // Lose kontrolü
        if (!ended && baseHealth != null && baseHealth.isDestroyed)
        {
            TriggerLose();
        }
    }

    // --- Dışarıdan çağır (Win koşulun neyse oradan) ---
    public void TriggerWin()
    {
        if (ended) return;
        ended = true;

        if (losePanel) losePanel.SetActive(false);
        if (winPanel) winPanel.SetActive(true);

        if (winText) winText.text = "Kazandınız!";

        EndCommon();
        Debug.Log("🏆 WIN!");
    }

    public void TriggerLose()
    {
        if (ended) return;
        ended = true;

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(true);

        if (loseText) loseText.text = "Game Over";

        EndCommon();
        Debug.Log("💀 LOSE!");
    }

    private void EndCommon()
    {
        ScoreManager.Instance?.Stop();
        if (scoreText != null && ScoreManager.Instance != null)
            scoreText.text = ScoreManager.Instance.GetSummary();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- Buttons ---
    public void RestartLevel()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void GoMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
