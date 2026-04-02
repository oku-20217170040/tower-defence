using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuFixer : MonoBehaviour
{
    void Awake()
    {
        // Pause'tan kalma durumlar
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // EventSystem yoksa oluþtur
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // CanvasGroup kilitlemiþ olabilir -> aç
        foreach (var cg in FindObjectsOfType<CanvasGroup>(true))
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // Butonlar yanlýþlýkla disable/interactable=false kalmýþ olabilir
        foreach (var b in FindObjectsOfType<Button>(true))
            b.interactable = true;
    }
}
