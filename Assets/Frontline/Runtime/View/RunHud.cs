using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Sandbox arayuzu. OnGUI ile cizilir; Canvas, font asset'i ya da
    /// TextMeshPro kurulumu gerektirmez. Gercek arayuz Plan 5'in isi -
    /// bu yalnizca oyuncunun karar verebilmesi icin gereken asgari bilgi.
    ///
    /// Su uc sey gorunmeden oyun oynanamaz: us cani, kacinci dalga,
    /// tuzagin ise yarayip yaramadigi (oldurulen / sizan).
    /// </summary>
    public sealed class RunHud : MonoBehaviour
    {
        public RunController run;
        public HomeBaseHealth homeBase;
        public UnitSpawner spawner;
        public TowerPlacer placer;
        public EconomyRunner economy;

        private GUIStyle labelStyle;
        private GUIStyle titleStyle;

        private void Awake()
        {
            if (run == null) run = FindFirstObjectByType<RunController>();
            if (homeBase == null) homeBase = FindFirstObjectByType<HomeBaseHealth>();
            if (spawner == null) spawner = FindFirstObjectByType<UnitSpawner>();
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

            if (homeBase != null)
                GUILayout.Label("Us cani: " + homeBase.CurrentHp + " / " + homeBase.maxHp, labelStyle);

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
                GUILayout.Label(PriceLine(TowerKind.Battery, "1 Batarya") + "   " +
                                PriceLine(TowerKind.Ambush, "2 Pusu"), labelStyle);
                GUILayout.Label(PriceLine(TowerKind.Decoy, "3 Sahte") + "   " +
                                PriceLine(TowerKind.Minefield, "4 Mayin tarlasi"), labelStyle);
                GUILayout.Label(PriceLine(TowerKind.Beam, "5 Isin"), labelStyle);
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
            if (kind == TowerKind.Battery) return "Batarya";
            if (kind == TowerKind.Ambush) return "Pusu";
            if (kind == TowerKind.Minefield) return "Mayin";
            if (kind == TowerKind.Beam) return "Isin";
            return "Sahte hedef";
        }
    }
}
