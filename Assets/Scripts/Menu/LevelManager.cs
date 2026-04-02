using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Level Sahne Adlarý")]
    [Tooltip("Yüklenebilecek level sahnelerinin isimleri.\nÖrn: Level_01, Level_02...")]
    public string[] levelSceneNames;

    [Tooltip("Level sahnesi yüklenmeden önce timeScale'i 1'e çeker (pause vs. kaldýysa).")]
    public bool resetTimeScaleOnLoad = true;

    [Tooltip("Index ile yükleme kullanacaksan, index 0 => Level_01 gibi düþün.")]
    public void LoadLevelByIndex(int index)
    {
        if (levelSceneNames == null || levelSceneNames.Length == 0)
        {
            Debug.LogError("[LevelManager] levelSceneNames boþ!");
            return;
        }

        if (index < 0 || index >= levelSceneNames.Length)
        {
            Debug.LogError("[LevelManager] Geçersiz index: " + index);
            return;
        }

        LoadLevel(levelSceneNames[index]);
    }

    [Tooltip("Sahne adýyla level yükler.\nButonlardan direkt çaðýrmak için idealdir.")]
    public void LoadLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[LevelManager] sceneName boþ!");
            return;
        }

        if (resetTimeScaleOnLoad)
            Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);
    }
}
