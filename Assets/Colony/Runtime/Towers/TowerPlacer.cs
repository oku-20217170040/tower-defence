using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Kule yerlestirme girisi. Sandbox icin klavye + fare; dokunmatik
    /// arayuz Plan 3'un isi.
    ///
    /// 1 = Gurultucu, 2 = Sessiz, 3 = Yem. Sol tik yerlestirir.
    /// Kuleler YALNIZCA kurulum seridine kurulur; koridora kule konsaydi
    /// yolu kapatirdi ve oyun labirent kurmaya donerdi (spec 3.6).
    /// </summary>
    public sealed class TowerPlacer : MonoBehaviour
    {
        public ColonyWorld world;
        public Camera targetCamera;
        public EconomyRunner economy;

        [Header("Gorunum")]
        public Color noisyColor = new Color(0.95f, 0.35f, 0.25f);
        public Color silentColor = new Color(0.45f, 0.5f, 0.55f);
        public Color lureColor = new Color(0.3f, 0.9f, 0.5f);

        [Tooltip("Kule capinin hucre boyutuna orani.")]
        [Range(0.2f, 1f)]
        public float towerScale = 0.8f;

        [Header("Silah")]
        [Tooltip("Gurultucu kulenin saniyelik hasari. Yuksektir ama onunden " +
                 "zamanla kimse gecmez; gercek hasari cok daha dusuk kalir.")]
        public float noisyDps = 10f;

        [Tooltip("Sessiz kulenin saniyelik hasari. Dusuktur ama surekli is gorur - " +
                 "tuzak koridorunda gercek katliami bu yapar.")]
        public float silentDps = 4f;

        public int sortingOrder = 15;

        public TowerKind Selected { get; private set; } = TowerKind.Noisy;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<ColonyWorld>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
        }

        private void Update()
        {
            if (world == null || world.Grid == null) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(TowerKind.Noisy);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(TowerKind.Silent);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Select(TowerKind.Lure);

            if (Input.GetMouseButtonDown(0))
                TryPlaceAt(Input.mousePosition);
        }

        private void Select(TowerKind kind)
        {
            Selected = kind;
            Debug.Log("[TowerPlacer] Secili kule: " + kind);
        }

        private void TryPlaceAt(Vector3 screenPosition)
        {
            if (targetCamera == null) return;

            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(screenPosition);

            int cell;
            if (!world.Space.WorldToCell(worldPoint, out cell))
                return;

            if (world.Grid.Get(cell) != Colony.Core.CellKind.BuildStrip)
            {
                Debug.Log("[TowerPlacer] Buraya kurulamaz: yalnizca kurulum seridine.");
                return;
            }

            // Parasi yetmiyorsa yerlestirme reddedilir ve sebebi soylenir.
            if (economy != null && !economy.CanAfford(Selected))
            {
                Debug.Log("[TowerPlacer] Besin yetmiyor. Gereken=" +
                          economy.CostFor(Selected) + " eldeki=" + economy.Economy.Balance);
                return;
            }

            if (economy != null && !economy.TryBuy(Selected))
                return;

            Place(Selected, cell);
        }

        /// <summary>Test ve script'ten cagrilabilir yerlestirme.</summary>
        public ColonyTower Place(TowerKind kind, int cell)
        {
            var go = new GameObject("Tower_" + kind);
            go.transform.SetParent(transform, false);

            float diameter = world.Space.CellSize * towerScale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprites.Circle();
            renderer.sortingOrder = sortingOrder;
            renderer.color = ColorFor(kind);

            var tower = go.AddComponent<ColonyTower>();
            tower.kind = kind;
            tower.Place(world, cell);

            // Yem kulesi hasar vermez; tek isi suruyu cekmektir.
            float dps = kind == TowerKind.Noisy ? noisyDps
                      : (kind == TowerKind.Silent ? silentDps : 0f);

            if (dps > 0f)
                go.AddComponent<TowerWeapon>().Bind(world, tower.radius, dps);

            // Titresim halkasi YALNIZCA gurultucu kulede. Halkasizlik,
            // "koloni beni goremiyor" demenin gorsel yoludur (spec 9.1).
            if (kind == TowerKind.Noisy)
            {
                var ringGo = new GameObject("ThreatRing");
                ringGo.transform.SetParent(go.transform, false);

                // Kule olcegini geri al ki halka dunya olceginde buyusun.
                float inverse = 1f / Mathf.Max(0.0001f, diameter);
                ringGo.transform.localScale = new Vector3(inverse, inverse, 1f);

                ringGo.AddComponent<SpriteRenderer>();
                ringGo.AddComponent<ThreatRingView>()
                      .Bind(tower.radius, world.Space.CellSize, sortingOrder - 1);
            }

            Debug.Log("[TowerPlacer] " + kind + " kuruldu, hucre=" + cell);
            return tower;
        }

        private Color ColorFor(TowerKind kind)
        {
            if (kind == TowerKind.Noisy) return noisyColor;
            if (kind == TowerKind.Lure) return lureColor;
            return silentColor;
        }
    }
}
