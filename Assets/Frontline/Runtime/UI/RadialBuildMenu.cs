using UnityEngine;
using UnityEngine.UI;
using Frontline.Core;
using Frontline.Core.UI;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Bos kurulum karesine dokununca acilan dairesel kule menusu
    /// (kullanici geri bildirimi, 2 Eylul 2026): "kule koyma isini bos
    /// objeye tikladiktan sonra daire seklinde ciksinlar fiyati ve
    /// kendisinin bir sureti belirsin, sonra hangisine tiklasam o birime
    /// kurulsun."
    ///
    /// TowerPlacer artik BOS hucreye dokununca DOGRUDAN kurmuyor - bu
    /// menuyu aciyor. Ekonomi kontrolu (CanAfford/TryBuy) ve gercek
    /// Place() cagrisi burada, secim aninda yapilir.
    /// </summary>
    public sealed class RadialBuildMenu : MonoBehaviour
    {
        public TowerPlacer placer;
        public EconomyRunner economy;
        public MetaProgressRunner meta;
        public Camera targetCamera;

        [Tooltip("Merkezden dilimlere olan uzaklik (referans piksel).")]
        public float radius = 190f;

        [Tooltip("Her dilimin capi (referans piksel).")]
        public float slotDiameter = 92f;

        private static readonly TowerKind[] Order =
        {
            TowerKind.Battery, TowerKind.Ambush, TowerKind.Decoy,
            TowerKind.Minefield, TowerKind.Beam
        };

        private Canvas canvas;
        private RectTransform root;
        private RectTransform centerPreview;
        private Image centerChip;
        private readonly Button[] slotButtons = new Button[5];
        private readonly Image[] slotChips = new Image[5];
        private readonly bool[] slotHasRealSprite = new bool[5];
        private readonly Text[] slotPrices = new Text[5];

        /// <summary>Menu su an acik mi. TowerPlacer bunu okuyup ayni karede
        /// ikinci bir acilis/kurma denemesini engeller.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Sahnedeki tek ornek. MapCamera.Instance ile ayni desen: baska
        /// dokunma bilesenleri (TowerSelection, SceneryFocusController)
        /// referans tasimadan "menu su an acik mi" diye sorabilsin.
        /// </summary>
        public static RadialBuildMenu Instance { get; private set; }

        private int targetCell = -1;

        private void Awake()
        {
            Instance = this;

            if (placer == null) placer = FindFirstObjectByType<TowerPlacer>();
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
            if (meta == null) meta = FindFirstObjectByType<MetaProgressRunner>();
            if (targetCamera == null) targetCamera = Camera.main;

            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Build()
        {
            var canvasGo = new GameObject("RadialBuildMenu_Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // RunScreen'in (100) UZERINDE dursun: radial menu her seyin
            // ustune acilmali, alt bar dahil.
            canvas.sortingOrder = 150;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            root = UiKit.Container("Root", canvasGo.transform);
            UiKit.Stretch(root);

            // GORUNMEZ dokunma yakalayici: BOSLUGA dokununca iptal eder.
            // Kullanici geri bildirimi (2 Eylul 2026): karartilmis arka
            // plan "duraklatma / ana menu" gibi hissettiriyordu - "oyunun
            // bir parcasi olarak acilsin" istendi. Menu artik sahneyi
            // KARARTMIYOR, yalnizca uzerine biniyor.
            var backdropButton = UiKit.Button("Backdrop", root, new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(backdropButton.GetComponent<RectTransform>());
            backdropButton.onClick.AddListener(Close);

            // Merkez: dokunulan kulenin "sureti" - hangi tur oldugu
            // secilene kadar belirsiz oldugu icin notr bir platform ikonu.
            var centerGo = UiKit.Container("Merkez", root);
            centerPreview = centerGo;
            var centerPanel = UiKit.Panel("Zemin", centerGo, new Color(0.12f, 0.13f, 0.12f, 0.9f));
            UiKit.Stretch((RectTransform)centerPanel.transform);
            centerChip = UiKit.Panel("Ikon", centerGo, new Color(0.5f, 0.5f, 0.5f, 0.6f)).GetComponent<Image>();
            var chipRect = centerChip.GetComponent<RectTransform>();
            UiKit.SetAnchors(chipRect, 0.28f, 0.28f, 0.72f, 0.72f);
            centerChip.raycastTarget = false;

            for (int i = 0; i < Order.Length; i++)
            {
                TowerKind kind = Order[i];
                int index = i;

                var slot = UiKit.Button("Dilim_" + kind, root, new Color(0.16f, 0.18f, 0.2f, 0.97f));
                slotButtons[i] = slot;

                // Duz renk kare DEGIL - kullanici geri bildirimi (2 Eylul
                // 2026): "yuvarlagin icinde gercekte kurulanin kucuk bir
                // sureti olsun." Kule sprite'i ArtLibrary'den okunur;
                // yoksa (sandbox/test sahnesi) renkli daireye duser.
                var chipGo = UiKit.Container("Suret", slot.transform);
                UiKit.SetAnchors(chipGo, 0.16f, 0.4f, 0.84f, 0.92f);
                var chip = chipGo.gameObject.AddComponent<Image>();
                chip.raycastTarget = false;
                chip.preserveAspect = true;
                slotChips[i] = chip;
                slotHasRealSprite[i] = ApplyTowerSprite(chip, kind);

                var priceHolder = UiKit.Container("Fiyat", slot.transform);
                UiKit.SetAnchors(priceHolder, 0f, 0.1f, 1f, 0.42f);
                slotPrices[i] = UiKit.Label("Metin", priceHolder, 22, TextAnchor.MiddleCenter,
                    new Color(0.95f, 0.85f, 0.45f));

                slot.onClick.AddListener(delegate { ChooseTower(Order[index]); });
            }

            gameObject.SetActive(false);
        }

        /// <summary>Verilen hucrenin uzerinde menuyu acar.</summary>
        public void Open(int cell)
        {
            if (placer == null || placer.world == null || targetCamera == null) return;

            targetCell = cell;
            IsOpen = true;
            gameObject.SetActive(true);

            Vector3 worldPos = placer.world.Space.CellToWorld(cell);
            Vector2 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            PositionAt(screenPos);
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            targetCell = -1;
            gameObject.SetActive(false);
        }

        private void PositionAt(Vector2 screenPoint)
        {
            float scale = root.rect.width / Screen.width;
            Vector2 localCenter = new Vector2(
                (screenPoint.x - Screen.width * 0.5f) * scale,
                (screenPoint.y - Screen.height * 0.5f) * scale);

            SetSlot(centerPreview, localCenter, slotDiameter * 0.8f);

            // Bes dilim, tam cember uzerinde esit acilarla - yukaridan
            // baslar, saat yonunde ilerler (parmagin dogal yay hareketi).
            for (int i = 0; i < Order.Length; i++)
            {
                float angle = 90f - i * (360f / Order.Length);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                SetSlot((RectTransform)slotButtons[i].transform, localCenter + offset, slotDiameter);
            }
        }

        private static void SetSlot(RectTransform rect, Vector2 localPosition, float diameter)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = localPosition;
            rect.sizeDelta = new Vector2(diameter, diameter);
        }

        private void Refresh()
        {
            for (int i = 0; i < Order.Length; i++)
            {
                TowerKind kind = Order[i];

                bool unlocked = meta == null || meta.State == null || meta.State.IsUnlocked(kind);
                bool affordable = unlocked && (economy == null || economy.CanAfford(kind));
                int cost = economy != null ? economy.CostFor(kind) : 0;

                slotPrices[i].text = unlocked ? HudFormat.Price(cost, affordable) : "Kilitli";
                slotPrices[i].color = affordable
                    ? new Color(0.95f, 0.85f, 0.45f)
                    : new Color(0.85f, 0.4f, 0.35f);

                float alpha = affordable ? 1f : 0.4f;

                // Gercek kule sprite'i varsa RENGINI BOZMADAN yalnizca
                // saydamligini degistir (Image.color sprite'i tint eder -
                // ColorFor uygulamak kuleyi yanlis renge boyardi).
                // Placeholder daireye dusuldugunde ColorFor hala gerekli,
                // aksi halde bes dilim de ayni gri daire olurdu.
                Color c = slotHasRealSprite[i] ? Color.white : ColorFor(kind);
                c.a = alpha;
                slotChips[i].color = c;

                slotButtons[i].interactable = affordable;
            }
        }

        /// <summary>
        /// ArtLibrary'den kulenin gercek sprite'ini okuyup chip'e uygular.
        /// Bulunamazsa placeholder daireye duser. Donus degeri: gercek bir
        /// sprite atanip atanmadigi - Refresh() bunu renk kararinda kullanir.
        /// </summary>
        private static bool ApplyTowerSprite(Image chip, TowerKind kind)
        {
            ArtLibrary art = ArtLibrary.Instance;
            ArtLibrary.TowerArt entry;

            if (art != null && art.TryGetTower(kind, out entry))
            {
                // Seviye 1 gorseli varsa tercih edilir (3 Eylul 2026,
                // yukseltme sistemi): TowerPlacer.Place ile AYNI kural,
                // yoksa menudeki simge kurulan kuleyle uyusmaz.
                Sprite s = (entry.levelTurrets != null && entry.levelTurrets.Length > 0)
                    ? entry.levelTurrets[0] : null;
                if (s == null) s = entry.sprite;

                if (s != null)
                {
                    chip.sprite = s;
                    return true;
                }
            }

            chip.sprite = PrimitiveSprites.Circle();
            return false;
        }

        private void ChooseTower(TowerKind kind)
        {
            if (placer == null || economy == null || targetCell < 0) return;

            bool unlocked = meta == null || meta.State == null || meta.State.IsUnlocked(kind);
            if (!unlocked) return;
            if (!economy.CanAfford(kind)) return;

            int cost = economy.CostFor(kind);
            if (!economy.TryBuy(kind)) return;

            placer.Place(kind, targetCell, cost);
            Frontline.Runtime.Audio.AudioManager.Instance.PlaySfx(
                Frontline.Runtime.Audio.AudioManager.TowerBuild);
            Close();
        }

        private Color ColorFor(TowerKind kind)
        {
            if (placer == null) return Color.gray;

            if (kind == TowerKind.Battery) return placer.batteryColor;
            if (kind == TowerKind.Decoy) return placer.decoyColor;
            if (kind == TowerKind.Minefield) return placer.minefieldColor;
            if (kind == TowerKind.Beam) return placer.beamColor;
            return placer.ambushColor;
        }
    }
}
