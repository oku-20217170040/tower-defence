using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Sahne Ayarlarý")]
    [Tooltip("Play butonuna basýldýðýnda yüklenecek oyun sahnesinin adý.")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Paneller")]
    [Tooltip("Açýlýp kapanacak olan Ayarlar (Settings) paneli.\nHierarchy'deki Settings Panel objesini buraya sürükle.")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("Level seçim paneli.\nLevels butonuna basýnca açýlýr.")]
    [SerializeField] private GameObject levelsPanel;

    [Tooltip("Ana menü paneli.\nSettings veya Levels açýldýðýnda bu panel kapanýr.")]
    [SerializeField] private GameObject menuPanel;

    private void Awake()
    {
        // Oyun açýldýðýnda panellerin baþlangýç durumu
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (levelsPanel != null) levelsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    [Tooltip("Oyunu baþlatýr ve belirtilen oyun sahnesini yükler.")]
    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    [Tooltip("Oyundan çýkýþ yapar.\n(Sadece build alýnmýþ oyunda çalýþýr.)")]
    public void Quit()
    {
        Application.Quit();
    }

    // ---------------- SETTINGS ----------------

    [Tooltip("Settings butonuna basýldýðýnda çaðrýlýr.\nAyarlar panelini açar, ana menüyü kapatýr.")]
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (levelsPanel != null) levelsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    [Tooltip("Settings panelindeki Geri / Kapat butonuna basýldýðýnda çaðrýlýr.\nAyarlar panelini kapatýr, ana menüyü açar.")]
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    // ---------------- LEVELS ----------------

    [Tooltip("Levels butonuna basýldýðýnda çaðrýlýr.\nLevel seçim panelini açar, ana menüyü kapatýr.")]
    public void OpenLevels()
    {
        if (levelsPanel != null) levelsPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    [Tooltip("Level seçim panelindeki Geri butonuna basýldýðýnda çaðrýlýr.\nLevel panelini kapatýr, ana menüyü açar.")]
    public void CloseLevels()
    {
        if (levelsPanel != null) levelsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }
}
