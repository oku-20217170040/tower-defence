using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Frontline.Runtime.Audio;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Duraklatma menusu (referans oyunda "DURDURULDU": YARDIM/DEVAM ET/
    /// AYAR/YENIDEN BASLAT/CIKIS - bkz.
    /// docs/notes/2026-09-02-referans-oyun-gozlem.md). Bizde YARDIM yok
    /// (kapsam disi, ogretici yazi icat etmiyoruz), CANLANDIR yok (parasal
    /// dongu yok) - kalan 4 buton birebir: DEVAM ET / AYARLAR / YENIDEN
    /// BASLAT / CIKIS.
    /// </summary>
    public sealed class PauseOverlay : MonoBehaviour
    {
        private float previousTimeScale = 1f;

        public static PauseOverlay Open(RectTransform parentCanvas, float resumeScale)
        {
            var go = new GameObject("DuraklatmaOverlay", typeof(RectTransform));
            go.transform.SetParent(parentCanvas, false);
            var overlay = go.AddComponent<PauseOverlay>();
            overlay.previousTimeScale = resumeScale > 0f ? resumeScale : 1f;
            overlay.Build();
            Time.timeScale = 0f;
            return overlay;
        }

        private void Build()
        {
            var root = (RectTransform)transform;
            UiKit.Stretch(root);

            UiKit.Panel("Karartma", root, new Color(0f, 0f, 0f, 0.78f));

            var panel = UiKit.Container("Panel", root);
            UiKit.SetAnchors(panel, 0.16f, 0.2f, 0.84f, 0.8f);
            UiKit.Panel("Zemin", panel, new Color(0.1f, 0.12f, 0.11f, 0.97f));
            UiKit.Stretch((RectTransform)panel.GetChild(0));

            var title = UiKit.Container("Baslik", panel);
            UiKit.SetAnchors(title, 0.05f, 0.86f, 0.95f, 0.98f);
            UiKit.Label("Metin", title, 34, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "DURDURULDU";

            AddButton(panel, "DEVAM ET", 0.66f, 0.82f, Resume,
                new Color(0.22f, 0.4f, 0.24f, 0.95f));
            AddButton(panel, "AYARLAR", 0.48f, 0.64f, OpenSettings,
                new Color(0.18f, 0.22f, 0.26f, 0.95f));
            AddButton(panel, "YENIDEN BASLAT", 0.30f, 0.46f, Restart,
                new Color(0.18f, 0.22f, 0.26f, 0.95f));
            AddButton(panel, "CIKIS", 0.12f, 0.28f, ExitToMenu,
                new Color(0.4f, 0.2f, 0.2f, 0.95f));
        }

        private void AddButton(RectTransform parent, string label, float yMin, float yMax,
                                UnityEngine.Events.UnityAction onClick, Color color)
        {
            var button = UiKit.Button(label, parent, color);
            UiKit.SetAnchors(button.GetComponent<RectTransform>(), 0.1f, yMin, 0.9f, yMax);
            UiKit.Label("Metin", button.transform, 26, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = label;
            button.onClick.AddListener(onClick);
        }

        private void Resume()
        {
            Time.timeScale = previousTimeScale;
            Destroy(gameObject);
        }

        private void OpenSettings()
        {
            SettingsOverlay.Open((RectTransform)transform.parent);
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ExitToMenu()
        {
            Time.timeScale = 1f;
            MainMenuScreen.LoadMainMenuOrQuit();
        }

        private void OnDestroy()
        {
            // Overlay baska bir yoldan (sahne degisimi) yok edilirse
            // zaman akisini kilitli birakmamak icin.
            if (Time.timeScale <= 0f) Time.timeScale = previousTimeScale;
        }
    }
}
