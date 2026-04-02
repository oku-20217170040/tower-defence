using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Pause’tan dönünce donma kalmasýn
        Time.timeScale = 1f;

        // UI týklamasý için cursor serbest
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // EventSystem yoksa oluþtur
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
