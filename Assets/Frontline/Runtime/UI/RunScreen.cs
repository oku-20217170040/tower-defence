using UnityEngine;
using UnityEngine.UI;
using Frontline.Core.UI;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Dikey run arayuzunun gercek MonoBehaviour'u (spec 3.8). RunHud'un
    /// (IMGUI, sandbox/debug) yerini alan uretim ekrani: uc bant kod ile
    /// kurulur (bu projede sahneler kod ile uretilir, YAML elle
    /// duzenlenmez).
    ///
    ///   UST BANT  - us cani, besin, dalga, kalan dusman.
    ///   HARITA    - dokunulmaz, tamami MapCamera/TowerPlacer'a acik.
    ///   ALT BANT  - TowerBarView (bes kule) + hiz/duraklat.
    ///
    /// Bantlar SafeAreaLayout ile hesaplanir ve UiTouchGuard'a kaydedilir;
    /// aksi halde alt banttaki bir dokunus haritaya kule kurardi.
    /// </summary>
    public sealed class RunScreen : MonoBehaviour
    {
        public RunController run;
        public HomeBaseHealth homeBase;
        public UnitSpawner spawner;
        public EconomyRunner economy;
        public GameSpeedControl speed;
        public TowerPlacer placer;
        [Tooltip("Dalga-arasi odul ekrani. Acikken Duraklat devre disi kalir, ikisi ustuste binmesin.")]
        public DraftView draft;

        [Tooltip("Ust bandin guvenli yukseklige orani.")]
        public float topFraction = SafeAreaLayout.DefaultTopFraction;

        [Tooltip("Alt bandin guvenli yukseklige orani.")]
        public float bottomFraction = SafeAreaLayout.DefaultBottomFraction;

        private Canvas canvas;
        private RectTransform topBand;
        private RectTransform bottomBand;
        private RectTransform resultLabel;
        private GameObject resultButtonsA;
        private GameObject resultButtonsB;

        private Text healthText;
        private Text foodText;
        private Text waveText;
        private Text remainingText;
        private Text speedButtonText;
        private Text resultText;

        private Rect registeredTop;
        private Rect registeredBottom;

        private void Awake()
        {
            if (run == null) run = FindFirstObjectByType<RunController>();
            if (homeBase == null) homeBase = FindFirstObjectByType<HomeBaseHealth>();
            if (spawner == null) spawner = FindFirstObjectByType<UnitSpawner>();
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
            if (speed == null) speed = FindFirstObjectByType<GameSpeedControl>();
            if (placer == null) placer = FindFirstObjectByType<TowerPlacer>();
            if (draft == null) draft = FindFirstObjectByType<DraftView>();
        }

        private void Start()
        {
            // Build() Start'tadir, Awake'de DEGIL: EconomyRunner.Economy
            // gibi bagimli alanlar kendi Awake'lerinde kurulur ve Unity
            // nesneler arasi Awake sirasini GARANTI ETMEZ. RunScreen
            // baskalarinin verisini okudugu icin Start'a ertelenmesi
            // gerekiyor (Unity butun Awake'leri, sonra butun Start'lari
            // calistirir).
            Build();
            Frontline.Runtime.Audio.AudioManager.Instance.PlayMusic(
                Frontline.Runtime.Audio.AudioManager.Music);
        }

        private void OnDestroy()
        {
            UiTouchGuard.Unregister(registeredTop);
            UiTouchGuard.Unregister(registeredBottom);
        }

        private void Build()
        {
            var canvasGo = new GameObject("RunScreen_Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Dikey referans: spec 3.8'in dayandigi telefon orani (Redmi
            // Note 14 Pro dahil, ~9:19,5 bandi). matchWidthOrHeight 1 =
            // yukseklige gore olcekle - dar ekranin darligini asla kaybetme.
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var root = (RectTransform)canvasGo.transform;

            topBand = UiKit.Container("UstBant", root);
            bottomBand = UiKit.Container("AltBant", root);

            UiKit.Panel("Zemin", topBand, new Color(0.05f, 0.06f, 0.05f, 0.72f));
            UiKit.Panel("Zemin", bottomBand, new Color(0.05f, 0.06f, 0.05f, 0.88f));

            UiKit.Stretch((RectTransform)topBand.GetChild(0));
            UiKit.Stretch((RectTransform)bottomBand.GetChild(0));

            BuildTopBand();
            BuildBottomBand();
            BuildResultLabel(root);

            ApplyLayout();
        }

        private void BuildTopBand()
        {
            var health = UiKit.Container("Can", topBand);
            UiKit.SetAnchors(health, 0.02f, 0.52f, 0.34f, 0.96f);
            healthText = UiKit.Label("Metin", health, 30, TextAnchor.MiddleLeft,
                new Color(0.95f, 0.55f, 0.5f));

            var food = UiKit.Container("Besin", topBand);
            UiKit.SetAnchors(food, 0.02f, 0.06f, 0.34f, 0.5f);
            foodText = UiKit.Label("Metin", food, 26, TextAnchor.MiddleLeft,
                new Color(0.95f, 0.85f, 0.45f));

            var wave = UiKit.Container("Dalga", topBand);
            UiKit.SetAnchors(wave, 0.34f, 0.52f, 0.98f, 0.96f);
            waveText = UiKit.Label("Metin", wave, 30, TextAnchor.MiddleRight,
                new Color(0.92f, 0.9f, 0.85f));

            var remaining = UiKit.Container("Kalan", topBand);
            UiKit.SetAnchors(remaining, 0.34f, 0.06f, 0.98f, 0.5f);
            remainingText = UiKit.Label("Metin", remaining, 26, TextAnchor.MiddleRight,
                new Color(0.75f, 0.8f, 0.78f));
        }

        /// <summary>
        /// Alt bant artik kule secici BARI iceremez - o UI hicbir zaman
        /// islevsel degildi (TowerPlacer.TryPlaceAt HER zaman RadialBuildMenu
        /// aciyor, buradaki Selected/Select() hicbir yerde okunmuyordu; olu
        /// kod, kullanici geri bildirimiyle bulundu ve kaldirildi, 4 Eylul
        /// 2026). Referans oyunda da boyle bir bar YOK; kule secimi tumuyle
        /// bos kareye dokunma -> radyal menu akisinda. Alt bant artik yalnizca
        /// referans oyunun sol dikey ikon kolonundaki (piksel-olculmus altin
        /// oktagon stili) hiz/ayarlar/duraklat kontrollerini tasiyan kucuk bir
        /// kume; portre ekranda dikey kolon yerine sag-alt kume tercih edildi.
        /// </summary>
        private void BuildBottomBand()
        {
            Sprite goldButton = Resources.Load<Sprite>("UI/gold_button");
            Sprite pauseIcon = Resources.Load<Sprite>("UI/icon_pause");
            Sprite settingsIcon = Resources.Load<Sprite>("UI/icon_settings");

            var speedButton = GoldIconButton("Hiz", bottomBand, goldButton, null,
                0.70f, 0.15f, 0.94f, 0.85f, out speedButtonText, 34);
            speedButton.onClick.AddListener(OnSpeedClicked);

            GoldIconButton("Ayarlar", bottomBand, goldButton, settingsIcon,
                0.38f, 0.15f, 0.62f, 0.85f, out _, 0)
                .onClick.AddListener(OnSettingsClicked);

            GoldIconButton("Duraklat", bottomBand, goldButton, pauseIcon,
                0.06f, 0.15f, 0.30f, 0.85f, out _, 0)
                .onClick.AddListener(OnPauseClicked);
        }

        /// <summary>
        /// Referans oyunun piksel-olculmus altin oktagon buton stiliyle
        /// (arka plan sprite + ortalanmis ikon/metin) tek bir kontrol
        /// uretir. text cikisi null olabilir (yalnizca ikon).
        /// </summary>
        private static Button GoldIconButton(string name, RectTransform parent,
            Sprite background, Sprite icon, float xMin, float yMin, float xMax, float yMax,
            out Text text, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            UiKit.SetAnchors(rect, xMin, yMin, xMax, yMax);

            var bgImage = go.GetComponent<Image>();
            bgImage.sprite = background;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = bgImage;
            button.onClick.AddListener(() =>
                Frontline.Runtime.Audio.AudioManager.Instance.PlaySfx(
                    Frontline.Runtime.Audio.AudioManager.UiClick));

            if (icon != null)
            {
                var iconGo = new GameObject("Ikon", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)iconGo.transform;
                iconRect.SetParent(rect, false);
                UiKit.SetAnchors(iconRect, 0.22f, 0.22f, 0.78f, 0.78f);
                var iconImage = iconGo.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            text = null;
            if (fontSize > 0)
            {
                text = UiKit.Label("Metin", rect, fontSize, TextAnchor.MiddleCenter,
                    new Color(0.35f, 0.16f, 0.05f));
            }

            return button;
        }

        private void OnSettingsClicked()
        {
            SettingsOverlay.Open((RectTransform)canvas.transform);
        }

        private void BuildResultLabel(RectTransform root)
        {
            var holder = UiKit.Container("Sonuc", root);
            UiKit.SetAnchors(holder, 0.1f, 0.42f, 0.9f, 0.58f);
            resultLabel = holder;

            resultText = UiKit.Label("Metin", holder, 64, TextAnchor.MiddleCenter,
                new Color(0.9f, 0.9f, 0.85f));

            // Referans oyunun yenilgi ekraninda CIK/YENIDEN BASLAT/CANLANDIR
            // vardi (bkz. gozlem notu); CANLANDIR parasal donguye bagli,
            // bizde YOK - kalan iki buton eklendi.
            var restart = UiKit.Button("YenidenBasla", root,
                new Color(0.18f, 0.22f, 0.26f, 0.95f));
            UiKit.SetAnchors(restart.GetComponent<RectTransform>(), 0.14f, 0.3f, 0.48f, 0.4f);
            UiKit.Label("Metin", restart.transform, 24, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "YENIDEN BASLAT";
            restart.onClick.AddListener(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex));

            var exit = UiKit.Button("Cikis", root,
                new Color(0.4f, 0.2f, 0.2f, 0.95f));
            UiKit.SetAnchors(exit.GetComponent<RectTransform>(), 0.52f, 0.3f, 0.86f, 0.4f);
            UiKit.Label("Metin", exit.transform, 24, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "CIKIS";
            exit.onClick.AddListener(MainMenuScreen.LoadMainMenuOrQuit);

            resultButtonsA = restart.gameObject;
            resultButtonsB = exit.gameObject;

            resultLabel.gameObject.SetActive(false);
            resultButtonsA.SetActive(false);
            resultButtonsB.SetActive(false);
        }

        /// <summary>
        /// SafeAreaLayout ile bant boyutlarini hesaplar ve RectTransform'lara
        /// uygular. Ekran donduruldugunda ya da editor Game view boyutu
        /// degistiginde yeniden cagrilir (bkz. Update).
        /// </summary>
        private void ApplyLayout()
        {
            Rect safe = Screen.safeArea;
            var uiSafe = new UiRect(safe.x, safe.y, safe.width, safe.height);
            var bands = SafeAreaLayout.Split(uiSafe, topFraction, bottomFraction);

            ApplyBand(topBand, bands.Top);
            ApplyBand(bottomBand, bands.Bottom);

            UiTouchGuard.Unregister(registeredTop);
            UiTouchGuard.Unregister(registeredBottom);
            registeredTop = ToScreenRect(bands.Top);
            registeredBottom = ToScreenRect(bands.Bottom);
            UiTouchGuard.Register(registeredTop);
            UiTouchGuard.Register(registeredBottom);
        }

        private void ApplyBand(RectTransform band, UiRect rect)
        {
            band.anchorMin = Vector2.zero;
            band.anchorMax = Vector2.zero;
            band.pivot = new Vector2(0f, 0f);

            // Referans cozunurluk piksellerine cevir: CanvasScaler'in
            // olcegi 1'e sabitlenmis kok Canvas altinda calisiyoruz, bu
            // yuzden ekran pikseli ile referans pikseli arasindaki oran
            // Screen.width uzerinden turetilir.
            float scale = ((RectTransform)canvas.transform).rect.width / Screen.width;
            band.anchoredPosition = new Vector2(rect.X * scale, rect.Y * scale);
            band.sizeDelta = new Vector2(rect.Width * scale, rect.Height * scale);
        }

        private static Rect ToScreenRect(UiRect r)
        {
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }

        private void Update()
        {
            RefreshTexts();
        }

        private void RefreshTexts()
        {
            if (homeBase != null && healthText != null)
                healthText.text = "♥ " + HudFormat.Health(homeBase.CurrentHp, homeBase.maxHp);

            if (economy != null && economy.Economy != null && foodText != null)
                foodText.text = HudFormat.Compact(economy.Economy.Balance);

            if (run != null && waveText != null)
                waveText.text = HudFormat.Wave(run.CurrentWave, run.totalWaves);

            if (run != null && spawner != null && remainingText != null)
            {
                if (run.CurrentWave > 0)
                {
                    int total = run.CountForWave(run.CurrentWave);
                    int remaining = total - spawner.WaveResolved;
                    remainingText.text = HudFormat.Remaining(remaining, total);
                }
                else
                {
                    remainingText.text = "";
                }
            }

            if (speed != null && speedButtonText != null)
                speedButtonText.text = speed.Label;

            if (run != null && resultLabel != null)
            {
                bool ended = run.State != RunState.Running;
                if (resultLabel.gameObject.activeSelf != ended)
                {
                    resultLabel.gameObject.SetActive(ended);
                    resultButtonsA.SetActive(ended);
                    resultButtonsB.SetActive(ended);
                }

                if (ended)
                {
                    bool won = run.State == RunState.Won;
                    resultText.text = won ? "KAZANDIN" : "YUVA DUSTU";
                    resultText.color = won
                        ? new Color(0.5f, 1f, 0.6f)
                        : new Color(1f, 0.45f, 0.4f);
                }
            }
        }

        private void OnSpeedClicked()
        {
            if (speed != null) speed.Cycle();
        }

        private void OnPauseClicked()
        {
            if (Time.timeScale <= 0f) return; // Overlay zaten acik.
            if (draft != null && draft.IsOpen) return; // Odul ekraniyla ustuste binmesin.
            float resumeScale = speed != null ? speed.Current : 1f;
            PauseOverlay.Open((RectTransform)canvas.transform, resumeScale);
        }
    }
}
