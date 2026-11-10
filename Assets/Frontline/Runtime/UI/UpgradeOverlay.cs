using UnityEngine;
using UnityEngine.UI;
using Frontline.Core;
using Frontline.Core.UI;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Ana menu "GUNCELLE" ekrani (3 Eylul 2026 gece plani, FAZ 3):
    /// referans oyundaki gibi, HER kule turunun HER seviyesi icin ayri
    /// KALICI (run'lar arasi) yukseltme satin alinir. Bu, oyun-ici gecici
    /// yukseltmeden (Emplacement.Upgrade, TowerActionPanel) TAMAMEN AYRI -
    /// burada satin alinan kademe MetaState'e (biyokutle karsiliginda)
    /// kalici yazilir ve her YENI run'da TowerPlacer o kuleyi dogrudan o
    /// seviyede kurar (bkz. TowerPlacer.permanentMeta / StartingLevelOf).
    ///
    /// SettingsOverlay ile ayni uslup: kod ile kurulur (UiKit), kendi
    /// kendini yok eder. MetaProgressStore'u DOGRUDAN kullanir - Ana Menu
    /// sahnesinde bir RunController olmadigi icin MetaProgressRunner
    /// (RunController'a bagli) burada YOK, State'i kendisi yukler/kaydeder.
    /// </summary>
    public sealed class UpgradeOverlay : MonoBehaviour
    {
        private static readonly TowerKind[] Towers =
        {
            TowerKind.Battery,
            TowerKind.Ambush,
            TowerKind.Decoy,
            TowerKind.Minefield,
            TowerKind.Beam,
        };

        private RectTransform root;
        private Text biomassText;
        private Text[] rowLabels;
        private Button[] rowButtons;
        private MetaState state;

        public static UpgradeOverlay Open(RectTransform parentCanvas)
        {
            var go = new GameObject("GuncelleOverlay", typeof(RectTransform));
            go.transform.SetParent(parentCanvas, false);
            var overlay = go.AddComponent<UpgradeOverlay>();
            overlay.Build();
            return overlay;
        }

        private void Build()
        {
            state = MetaProgressStore.Load();

            root = (RectTransform)transform;
            UiKit.Stretch(root);

            UiKit.Panel("Karartma", root, new Color(0f, 0f, 0f, 0.75f));

            var panel = UiKit.Container("Panel", root);
            UiKit.SetAnchors(panel, 0.06f, 0.1f, 0.94f, 0.9f);
            UiKit.Panel("Zemin", panel, new Color(0.1f, 0.12f, 0.11f, 0.97f));
            UiKit.Stretch((RectTransform)panel.GetChild(0));

            var title = UiKit.Container("Baslik", panel);
            UiKit.SetAnchors(title, 0.05f, 0.9f, 0.95f, 0.98f);
            UiKit.Label("Metin", title, 32, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "GUNCELLE";

            var biomassRow = UiKit.Container("Biyokutle", panel);
            UiKit.SetAnchors(biomassRow, 0.05f, 0.82f, 0.95f, 0.9f);
            biomassText = UiKit.Label("Metin", biomassRow, 22, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.9f, 0.75f));

            int count = Towers.Length;
            rowLabels = new Text[count];
            rowButtons = new Button[count];

            // Basliktan/biyokutleden sonra kalan (0.08 - 0.80 arasi) dikey
            // alani kule basina esit sirit halinde boler.
            float top = 0.80f;
            float bottom = 0.06f;
            float rowHeight = (top - bottom) / count;
            float gap = rowHeight * 0.12f;

            for (int i = 0; i < count; i++)
            {
                TowerKind kind = Towers[i];
                float rowTop = top - i * rowHeight;
                float rowBottom = rowTop - rowHeight + gap;

                var row = UiKit.Button("Satir_" + kind, panel, new Color(0.18f, 0.22f, 0.26f, 0.95f));
                var rowRect = row.GetComponent<RectTransform>();
                UiKit.SetAnchors(rowRect, 0.06f, rowBottom, 0.94f, rowTop);

                var label = UiKit.Label("Metin", row.transform, 22, TextAnchor.MiddleCenter,
                    new Color(0.92f, 0.9f, 0.85f));

                rowLabels[i] = label;
                rowButtons[i] = row;

                TowerKind capturedKind = kind;
                row.onClick.AddListener(() => TryBuy(capturedKind));
            }

            var closeButton = UiKit.Button("Kapat", panel, new Color(0.4f, 0.2f, 0.2f, 0.95f));
            UiKit.SetAnchors(closeButton.GetComponent<RectTransform>(), 0.34f, 0.005f, 0.66f, 0.05f);
            UiKit.Label("Metin", closeButton.transform, 22, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "KAPAT";
            closeButton.onClick.AddListener(Close);

            Refresh();
        }

        private void TryBuy(TowerKind kind)
        {
            if (state.TryUpgradeTowerPermanent(kind))
                MetaProgressStore.Save(state);

            Refresh();
        }

        private void Refresh()
        {
            biomassText.text = "Biyokutle: " + state.Biomass;

            for (int i = 0; i < Towers.Length; i++)
            {
                TowerKind kind = Towers[i];
                int step = state.PermanentStepOf(kind);
                int level = 1 + step; // suan kule bu run'lar arasi kademede kuruluyor
                string name = HudFormat.TowerName(kind);

                if (step >= PermanentUpgradeCatalog.MaxPermanentSteps)
                {
                    rowLabels[i].text = name + " - Seviye " + level + " (TAM)";
                    rowButtons[i].interactable = false;
                    SetRowTint(i, new Color(0.16f, 0.2f, 0.17f, 0.95f));
                    continue;
                }

                int cost = PermanentUpgradeCatalog.CostFor(kind, step);
                bool affordable = state.CanAfford(cost);

                rowLabels[i].text = name + " - Seviye " + level + " -> " + (level + 1) +
                    "   " + HudFormat.Price(cost, affordable);
                rowButtons[i].interactable = affordable;
                SetRowTint(i, affordable
                    ? new Color(0.18f, 0.22f, 0.26f, 0.95f)
                    : new Color(0.14f, 0.14f, 0.14f, 0.85f));
            }
        }

        private void SetRowTint(int index, Color color)
        {
            var image = rowButtons[index].GetComponent<Image>();
            if (image != null) image.color = color;
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
