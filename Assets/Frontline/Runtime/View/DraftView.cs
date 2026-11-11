using System.Collections.Generic;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Dalga sonu draft ekrani. OnGUI ile cizilir (RunHud ile ayni gerekce).
    ///
    /// Secenekler DraftGenerator'dan gelir ve ordunin o anki kesif izi
    /// dagilimina gore agirliklandirilir (spec 2.2): ordu bir koridoru
    /// ezberlemisse oraya uygun secenekler daha sik cikar. Ekran acikken run
    /// bekler; secim yapmadan dalga baslamaz.
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        public Battlefield world;
        public EconomyRunner economy;
        public TowerPlacer placer;

        [Tooltip("Pasif odullerin carpan karsiligi. Yuzde olarak gelir.")]
        public bool logChoice = true;

        public bool IsOpen { get; private set; }

        private readonly List<DraftOption> options = new List<DraftOption>();
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
            if (placer == null) placer = FindFirstObjectByType<TowerPlacer>();
        }

        /// <summary>Draft'i acar. Kapanana kadar RunController bekler.</summary>
        public void Open(int nextWave)
        {
            var context = new DraftContext
            {
                Wave = nextWave,
                CorridorShares = MeasureShares()
            };

            options.Clear();
            options.AddRange(DraftGenerator.Generate(context, Random.Range(0, int.MaxValue)));
            IsOpen = true;
        }

        private float[] MeasureShares()
        {
            if (world == null || world.Grid == null || world.Recon == null) return null;

            var template = new MapTemplate
            {
                Width = world.width,
                Height = world.height,
                CorridorCount = world.corridorCount,
                CorridorWidth = world.corridorWidth
            };

            return CorridorPressure.Measure(world.Grid, world.Recon, template);
        }

        private void EnsureStyles()
        {
            if (labelStyle != null) return;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 26;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = new Color(0.95f, 0.93f, 0.85f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;
            buttonStyle.wordWrap = true;
        }

        private void OnGUI()
        {
            if (!IsOpen) return;

            EnsureStyles();

            float width = Mathf.Min(Screen.width - 40f, 640f);
            float height = 260f;
            var area = new Rect((Screen.width - width) * 0.5f,
                                (Screen.height - height) * 0.5f,
                                width, height);

            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(area);
            GUILayout.Space(10f);
            GUILayout.Label("Dalga arasi - bir odul sec", labelStyle);
            GUILayout.Space(10f);

            for (int i = 0; i < options.Count; i++)
            {
                if (GUILayout.Button(Describe(options[i]), buttonStyle, GUILayout.Height(52f)))
                {
                    Apply(options[i]);
                    IsOpen = false;
                }
            }

            GUILayout.EndArea();
        }

        /// <summary>Odulu uygular. Test ve script'ten de cagrilabilir.</summary>
        public void Apply(DraftOption option)
        {
            if (logChoice) Debug.Log("[DraftView] Secilen odul: " + Describe(option));

            switch (option.Kind)
            {
                case DraftOptionKind.FreeTower:
                    if (economy != null) economy.GrantFreeTower(option.Tower);
                    break;

                case DraftOptionKind.Discount:
                    if (economy != null) economy.AddDiscount(option.Tower, option.Amount);
                    break;

                case DraftOptionKind.Food:
                    if (economy != null) economy.Economy.Earn(option.Amount);
                    break;

                case DraftOptionKind.Passive:
                    if (placer == null) break;
                    if (option.Passive == PassiveKind.Damage)
                        placer.damageMultiplier *= 1f + option.Amount / 100f;
                    else
                        placer.rangeMultiplier *= 1f + option.Amount / 100f;
                    break;
            }
        }

        private string Describe(DraftOption option)
        {
            switch (option.Kind)
            {
                case DraftOptionKind.FreeTower:
                    return "Bedava " + TurkishName(option.Tower) + " kule  -  " +
                           CorridorName(option.Corridor) + " koridor ezberlendi";

                case DraftOptionKind.Discount:
                    return TurkishName(option.Tower) + " kulede kalici %" +
                           option.Amount + " indirim";

                case DraftOptionKind.Food:
                    return option.Amount + " besin";

                default:
                    return option.Passive == PassiveKind.Damage
                        ? ("Tum kuleler %" + option.Amount + " daha fazla hasar")
                        : ("Tum kuleler %" + option.Amount + " daha genis menzil");
            }
        }

        private string CorridorName(int corridor)
        {
            int count = world != null ? world.corridorCount : 3;
            if (corridor <= 0) return "sol";
            if (corridor >= count - 1) return "sag";
            return "orta";
        }

        private static string TurkishName(TowerKind kind)
        {
            // Adlarin tek dogru kaynagi HudFormat (11 Kasim 2026).
            return Frontline.Core.UI.HudFormat.TowerName(kind).ToLowerInvariant();
        }
    }
}
