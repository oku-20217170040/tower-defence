using UnityEngine;
using Colony.Core;

namespace Colony.Runtime
{
    /// <summary>
    /// Sandbox arayuzu. OnGUI ile cizilir; Canvas, font asset'i ya da
    /// TextMeshPro kurulumu gerektirmez. Gercek arayuz Plan 5'in isi -
    /// bu yalnizca oyuncunun karar verebilmesi icin gereken asgari bilgi.
    ///
    /// Su uc sey gorunmeden oyun oynanamaz: yuva cani, kacinci dalga,
    /// tuzagin ise yarayip yaramadigi (oldurulen / sizan).
    /// </summary>
    public sealed class RunHud : MonoBehaviour
    {
        public RunController run;
        public NestHealth nest;
        public BugSpawner spawner;
        public TowerPlacer placer;
        public EconomyRunner economy;

        private GUIStyle labelStyle;
        private GUIStyle titleStyle;

        private void Awake()
        {
            if (run == null) run = FindFirstObjectByType<RunController>();
            if (nest == null) nest = FindFirstObjectByType<NestHealth>();
            if (spawner == null) spawner = FindFirstObjectByType<BugSpawner>();
            if (placer == null) placer = FindFirstObjectByType<TowerPlacer>();
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
        }

        private void EnsureStyles()
        {
            if (labelStyle != null) return;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 20;
            labelStyle.normal.textColor = new Color(0.92f, 0.9f, 0.85f);

            titleStyle = new GUIStyle(labelStyle);
            titleStyle.fontSize = 34;
            titleStyle.fontStyle = FontStyle.Bold;
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(16f, 16f, 420f, 260f));

            if (run != null)
            {
                GUILayout.Label("Dalga " + run.CurrentWave + " / " + run.totalWaves, labelStyle);

                // Hazirlik suresi gorunmezse oyuncu bekledigini bilmez, oyunun
                // takildigini sanir.
                if (run.CurrentWave == 0)
                    GUILayout.Label("Hazirlan: kule kur (1-4 tuslari)", labelStyle);
            }

            if (nest != null)
                GUILayout.Label("Yuva cani: " + nest.CurrentHp + " / " + nest.maxHp, labelStyle);

            if (spawner != null)
            {
                GUILayout.Label("Oldurulen: " + spawner.TotalKilled +
                                "   Sizan: " + spawner.TotalLeaked, labelStyle);
                GUILayout.Label("Sahada: " + spawner.AliveCount, labelStyle);
            }

            if (economy != null)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Besin: " + economy.Economy.Balance, labelStyle);
            }

            if (placer != null)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Secili: " + TurkishName(placer.Selected), labelStyle);
                GUILayout.Label(PriceLine(TowerKind.Noisy, "1 Gurultucu") + "   " +
                                PriceLine(TowerKind.Silent, "2 Sessiz"), labelStyle);
                GUILayout.Label(PriceLine(TowerKind.Lure, "3 Yem") + "   " +
                                PriceLine(TowerKind.Resin, "4 Recine"), labelStyle);
            }

            GUILayout.EndArea();

            DrawRunResult();
        }

        private void DrawRunResult()
        {
            if (run == null || run.State == RunState.Running) return;

            string text = run.State == RunState.Won ? "KAZANDIN" : "YUVA DUSTU";
            var rect = new Rect(0f, Screen.height * 0.4f, Screen.width, 60f);

            var centered = new GUIStyle(titleStyle);
            centered.alignment = TextAnchor.MiddleCenter;
            centered.normal.textColor = run.State == RunState.Won
                ? new Color(0.5f, 1f, 0.6f)
                : new Color(1f, 0.45f, 0.4f);

            GUI.Label(rect, text, centered);
        }

        /// <summary>Alinamayan kuleler parantezle isaretlenir.</summary>
        private string PriceLine(TowerKind kind, string label)
        {
            if (economy == null) return label;

            int cost = economy.CostFor(kind);
            return economy.CanAfford(kind) ? (label + " " + cost) : ("(" + label + " " + cost + ")");
        }

        private static string TurkishName(TowerKind kind)
        {
            if (kind == TowerKind.Noisy) return "Gurultucu";
            if (kind == TowerKind.Silent) return "Sessiz";
            if (kind == TowerKind.Resin) return "Recine";
            return "Yem";
        }
    }
}
