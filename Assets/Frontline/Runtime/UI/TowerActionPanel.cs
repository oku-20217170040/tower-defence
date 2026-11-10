using UnityEngine;
using UnityEngine.UI;
using Frontline.Core;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Secili kulenin uzerinde/altinda beliren dokunmatik YUKSELT/SAT
    /// butonlari (3 Eylul 2026, kullanici talimati: yukseltme sistemi
    /// klavye 'U' ile calisiyordu ama telefonda dokunmatik bir buton
    /// yoktu - "biten bir sistem olacak" talimatini karsilamiyordu).
    ///
    /// Duzen referans oyunla teyit edildi (docs/notes/2026-09-03-referans-
    /// oyun-tam-analiz.md): YUKSELT ustte, SAT altta.
    ///
    /// TowerSelection'in Selected'ini her karede okur (event yok - proje
    /// genelinde RunScreen/RadialBuildMenu da polling kullaniyor, ayni
    /// desen). Butonlar EventSystem/GraphicRaycaster'a dayanan bir Canvas
    /// altinda oldugu icin UiTouchGuard.IsPointerOverUi() bunlari otomatik
    /// yakalar - TowerSelection/TowerPlacer'in kendi Update()'lerindeki
    /// IsPointerOverUi() kontrolu, harita dokunuslarinin bu butonlarla
    /// CAKISMAMASINI zaten saglar (3 Eylul'de EventSystem eksikliginden
    /// kaynaklanan hata TEKRARLANMASIN diye FrontlineSandboxSceneBuilder'da
    /// EventSystem'in var oldugu dogrulandi).
    /// </summary>
    public sealed class TowerActionPanel : MonoBehaviour
    {
        public TowerSelection selection;
        public TowerPlacer placer;
        public EconomyRunner economy;
        public Battlefield world;
        public Camera targetCamera;

        [Tooltip("Kule merkezinden YUKSELT dugmesine olan dikey uzaklik (referans piksel).")]
        public float upgradeOffset = 130f;

        [Tooltip("Kule merkezinden SAT dugmesine olan dikey uzaklik (referans piksel).")]
        public float sellOffset = 130f;

        public float buttonWidth = 190f;
        public float buttonHeight = 76f;

        private RectTransform root;
        private RectTransform upgradeButton;
        private Button upgradeButtonComponent;
        private Text upgradeLabel;

        private RectTransform sellButton;
        private Button sellButtonComponent;
        private Text sellLabel;

        private Emplacement lastBound;

        private void Awake()
        {
            if (selection == null) selection = FindFirstObjectByType<TowerSelection>();
            if (placer == null) placer = FindFirstObjectByType<TowerPlacer>();
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (targetCamera == null) targetCamera = Camera.main;

            Build();
        }

        private void Build()
        {
            var canvasGo = new GameObject("TowerActionPanel_Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // RunScreen (100) UZERINDE, RadialBuildMenu (150) ALTINDA:
            // kule secim panelinin alt bandin ustunde durmasi gerekir
            // ama kurulum menusuyle asla ayni anda acik olmaz (ikisi de
            // secim/kurulumla karsilikli disliyor).
            canvas.sortingOrder = 120;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            root = UiKit.Container("Root", canvasGo.transform);
            UiKit.Stretch(root);

            upgradeButton = BuildButton("Yukselt", new Color(0.25f, 0.55f, 0.3f, 0.95f), out upgradeButtonComponent, out upgradeLabel);
            sellButton = BuildButton("Sat", new Color(0.55f, 0.3f, 0.25f, 0.95f), out sellButtonComponent, out sellLabel);

            upgradeButtonComponent.onClick.AddListener(OnUpgradeClicked);
            sellButtonComponent.onClick.AddListener(OnSellClicked);

            SetVisible(false);
        }

        private RectTransform BuildButton(string name, Color color, out Button button, out Text label)
        {
            var b = UiKit.Button(name, root, color);
            button = b;

            var rect = b.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            label = UiKit.Label("Metin", b.transform, 28, TextAnchor.MiddleCenter,
                new Color(0.95f, 0.95f, 0.9f));

            return rect;
        }

        private void Update()
        {
            Emplacement tower = selection != null ? selection.Selected : null;

            if (tower == null)
            {
                SetVisible(false);
                lastBound = null;
                return;
            }

            SetVisible(true);
            lastBound = tower;

            PositionAround(tower);
            RefreshUpgrade(tower);
            RefreshSell(tower);
        }

        private void PositionAround(Emplacement tower)
        {
            if (targetCamera == null || world == null || world.Space == null) return;

            Vector3 worldPos = world.Space.CellToWorld(tower.Cell);
            Vector2 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            float scale = root.rect.width / Screen.width;
            Vector2 localCenter = new Vector2(
                (screenPos.x - Screen.width * 0.5f) * scale,
                (screenPos.y - Screen.height * 0.5f) * scale);

            upgradeButton.anchoredPosition = localCenter + new Vector2(0f, upgradeOffset);
            sellButton.anchoredPosition = localCenter - new Vector2(0f, sellOffset);
        }

        private void RefreshUpgrade(Emplacement tower)
        {
            bool maxed = tower.Level >= Emplacement.MaxLevel;

            // Max seviyedeyse dugme tamamen gizlenir (kullanici talimati:
            // "kule max seviyedeyse Yukselt butonu gizlensin/pasiflessin").
            if (upgradeButton.gameObject.activeSelf == maxed)
                upgradeButton.gameObject.SetActive(!maxed);

            if (maxed) return;

            int baseCost = economy != null ? economy.CostFor(tower.kind) : 0;
            int cost = Emplacement.UpgradeCost(baseCost, tower.Level);
            bool affordable = economy != null && economy.Economy.CanAfford(cost);

            upgradeLabel.text = "YUKSELT\n" + cost;
            upgradeButtonComponent.interactable = affordable;

            var image = upgradeButton.GetComponent<Image>();
            Color c = image.color;
            c.a = affordable ? 0.95f : 0.45f;
            image.color = c;
        }

        private void RefreshSell(Emplacement tower)
        {
            if (!sellButton.gameObject.activeSelf) sellButton.gameObject.SetActive(true);

            int refund = tower.SellValue();
            sellLabel.text = "SAT\n" + refund;
        }

        private void SetVisible(bool visible)
        {
            if (root != null) root.gameObject.SetActive(visible);
        }

        private void OnUpgradeClicked()
        {
            if (selection == null) return;
            selection.TryUpgradeSelected();
        }

        private void OnSellClicked()
        {
            if (selection == null) return;
            selection.TrySellSelected();
        }
    }
}
