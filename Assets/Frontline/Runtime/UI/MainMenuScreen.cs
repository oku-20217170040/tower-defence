using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Frontline.Runtime.Audio;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Acilis ana menusu: OYNA / AYARLAR / CIKIS - referans oyunun acilis
    /// ekraninda gozlenen sadelikte (logo alani yerine oyunumuzun adi).
    /// Diger ekranlar gibi tamamen kod ile kurulur (UiKit uslubu).
    /// </summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        public const string MainMenuSceneName = "Frontline_MainMenu";
        public const string RunSceneName = "Frontline_Sandbox";

        private Canvas canvas;

        private void Awake()
        {
            var canvasGo = new GameObject("AnaMenu_Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var root = (RectTransform)canvasGo.transform;
            UiKit.Panel("Zemin", root, new Color(0.05f, 0.06f, 0.05f, 1f));
            UiKit.Stretch((RectTransform)root.GetChild(0));

            var title = UiKit.Container("Baslik", root);
            UiKit.SetAnchors(title, 0.08f, 0.62f, 0.92f, 0.82f);
            UiKit.Label("Metin", title, 64, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "FRONTLINE";

            var play = UiKit.Button("Oyna", root, new Color(0.22f, 0.4f, 0.24f, 0.95f));
            UiKit.SetAnchors(play.GetComponent<RectTransform>(), 0.26f, 0.4f, 0.74f, 0.5f);
            UiKit.Label("Metin", play.transform, 30, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "OYNA";
            play.onClick.AddListener(Play);

            var upgrade = UiKit.Button("Guncelle", root, new Color(0.24f, 0.32f, 0.42f, 0.95f));
            UiKit.SetAnchors(upgrade.GetComponent<RectTransform>(), 0.26f, 0.28f, 0.74f, 0.38f);
            UiKit.Label("Metin", upgrade.transform, 26, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "GUNCELLE";
            upgrade.onClick.AddListener(OpenUpgrade);

            var settings = UiKit.Button("Ayarlar", root, new Color(0.18f, 0.22f, 0.26f, 0.95f));
            UiKit.SetAnchors(settings.GetComponent<RectTransform>(), 0.26f, 0.16f, 0.74f, 0.26f);
            UiKit.Label("Metin", settings.transform, 26, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "AYARLAR";
            settings.onClick.AddListener(OpenSettings);

            var quit = UiKit.Button("Cikis", root, new Color(0.4f, 0.2f, 0.2f, 0.95f));
            UiKit.SetAnchors(quit.GetComponent<RectTransform>(), 0.26f, 0.04f, 0.74f, 0.14f);
            UiKit.Label("Metin", quit.transform, 26, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "CIKIS";
            quit.onClick.AddListener(Quit);
        }

        private void Start()
        {
            AudioManager.Instance.PlayMusic(AudioManager.Music);
        }

        private void Play()
        {
            SceneManager.LoadScene(RunSceneName);
        }

        private void OpenSettings()
        {
            SettingsOverlay.Open((RectTransform)canvas.transform);
        }

        private void OpenUpgrade()
        {
            UpgradeOverlay.Open((RectTransform)canvas.transform);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Duraklatma/yenilgi ekranlarindaki CIKIS butonu buradan cagrilir.
        /// Ana menu build ayarlarindaysa ona doner, degilse (ornegin
        /// sandbox tek sahne ile calisiyorsa) uygulamayi kapatir.
        /// </summary>
        public static void LoadMainMenuOrQuit()
        {
            if (Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
            {
                SceneManager.LoadScene(MainMenuSceneName);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
