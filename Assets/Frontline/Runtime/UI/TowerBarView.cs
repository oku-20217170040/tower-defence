using UnityEngine;
using UnityEngine.UI;
using Frontline.Core;
using Frontline.Core.UI;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Alt banttaki bes kule dugmesi (spec 3.8: "Alt serit arayuze ayrilir").
    ///
    /// Her dugme uc sey soyler: hangi kule, kac besin, alinabilir mi.
    /// Secili olan cerceveyle isaretlenir; alinamayan hem soluklasir hem
    /// carpi isareti alir - gun isiginda telefonda yalnizca soluk renk
    /// ayirt edilmiyor.
    ///
    /// TowerPlacer.Select(TowerKind) public'tir; dugme dogrudan cagirir.
    /// </summary>
    public sealed class TowerBarView : MonoBehaviour
    {
        public TowerPlacer placer;
        public EconomyRunner economy;

        [Tooltip("Bos birakilirsa sahnede aranir. Bulunamazsa hicbir kule " +
                 "kilitlenmez (meta sistemi olmayan sandbox/test sahnesi).")]
        public MetaProgressRunner meta;

        [Tooltip("Dugmeler arasi bosluk (referans piksel).")]
        public float gap = 10f;

        private static readonly TowerKind[] Order =
        {
            TowerKind.Battery, TowerKind.Ambush, TowerKind.Decoy,
            TowerKind.Minefield, TowerKind.Beam
        };

        private readonly Image[] frames = new Image[5];
        private readonly Image[] chips = new Image[5];
        private readonly Text[] names = new Text[5];
        private readonly Text[] prices = new Text[5];

        /// <summary>
        /// Dugmeleri verilen bant dikdortgeninin icine kurar. RunScreen
        /// cagirir; ayri cagrilmasi da mumkun (test sahnesi).
        /// </summary>
        public void Build(RectTransform band)
        {
            if (placer == null) placer = FindFirstObjectByType<TowerPlacer>();
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
            if (meta == null) meta = FindFirstObjectByType<MetaProgressRunner>();

            float step = 1f / Order.Length;

            for (int i = 0; i < Order.Length; i++)
            {
                TowerKind kind = Order[i];
                int index = i;

                var button = UiKit.Button("Tower_" + kind, band,
                                          new Color(0.13f, 0.15f, 0.18f, 0.95f));
                UiKit.SetAnchors(button.GetComponent<RectTransform>(),
                                 i * step, 0f, (i + 1) * step, 1f);
                UiKit.Inset(button.GetComponent<RectTransform>(),
                            gap * 0.5f, gap * 0.5f, gap * 0.5f, gap * 0.5f);

                frames[i] = button.GetComponent<Image>();

                // Renk yamasi kuleyi TANITIR: haritadaki kule ile ayni renk.
                var chip = UiKit.Panel("Renk", button.transform, ColorFor(kind));
                UiKit.SetAnchors(chip, 0.22f, 0.52f, 0.78f, 0.9f);
                chip.GetComponent<Image>().raycastTarget = false;
                chips[i] = chip.GetComponent<Image>();

                var nameHolder = UiKit.Container("Ad", button.transform);
                UiKit.SetAnchors(nameHolder, 0f, 0.28f, 1f, 0.5f);
                names[i] = UiKit.Label("Metin", nameHolder, 26, TextAnchor.MiddleCenter,
                                       new Color(0.92f, 0.9f, 0.85f));
                names[i].text = HudFormat.TowerShortName(kind);

                var priceHolder = UiKit.Container("Fiyat", button.transform);
                UiKit.SetAnchors(priceHolder, 0f, 0.06f, 1f, 0.28f);
                prices[i] = UiKit.Label("Metin", priceHolder, 26, TextAnchor.MiddleCenter,
                                        new Color(0.95f, 0.85f, 0.45f));

                button.onClick.AddListener(delegate { Choose(Order[index]); });
            }

            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>Fiyat ve secim durumunu tazeler.</summary>
        public void Refresh()
        {
            if (frames[0] == null) return;

            TowerKind selected = placer != null ? placer.Selected : TowerKind.Battery;

            for (int i = 0; i < Order.Length; i++)
            {
                TowerKind kind = Order[i];

                bool unlocked = meta == null || meta.State == null || meta.State.IsUnlocked(kind);
                bool affordable = unlocked && (economy == null || economy.CanAfford(kind));
                int cost = economy != null ? economy.CostFor(kind) : 0;

                if (prices[i] != null)
                {
                    prices[i].text = unlocked
                        ? HudFormat.Price(cost, affordable)
                        : "Kilitli";
                    prices[i].color = affordable
                        ? new Color(0.95f, 0.85f, 0.45f)
                        : new Color(0.85f, 0.4f, 0.35f);
                }

                bool isSelected = kind == selected;

                if (frames[i] != null)
                {
                    frames[i].color = isSelected
                        ? new Color(0.24f, 0.34f, 0.30f, 0.98f)
                        : new Color(0.13f, 0.15f, 0.18f, 0.95f);
                }

                // Soluklastirma alinamayani anlatir; secili olan her
                // durumda tam parlak kalir ki oyuncu neyi tuttugunu gorsun.
                float alpha = (affordable || isSelected) ? 1f : 0.45f;

                if (chips[i] != null)
                {
                    Color c = ColorFor(kind);
                    c.a = alpha;
                    chips[i].color = c;
                }

                if (names[i] != null)
                {
                    Color c = new Color(0.92f, 0.9f, 0.85f);
                    c.a = alpha;
                    names[i].color = c;
                }
            }
        }

        private void Choose(TowerKind kind)
        {
            if (placer == null) return;
            if (placer.Selected == kind) return;

            bool unlocked = meta == null || meta.State == null || meta.State.IsUnlocked(kind);
            if (!unlocked) return;

            placer.Select(kind);
            Refresh();
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
