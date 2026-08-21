using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kule yerlestirme girisi. Sandbox icin klavye + fare; dokunmatik
    /// arayuz Plan 3'un isi.
    ///
    /// 1 = Batarya, 2 = Pusu, 3 = Yem, 4 = Mayin tarlasi, 5 = Isin.
    /// Sol tik yerlestirir.
    /// Kuleler YALNIZCA kurulum seridine kurulur; koridora kule konsaydi
    /// yolu kapatirdi ve oyun labirent kurmaya donerdi (spec 3.6).
    /// </summary>
    public sealed class TowerPlacer : MonoBehaviour
    {
        public Battlefield world;
        public Camera targetCamera;
        public EconomyRunner economy;

        [Header("Gorunum")]
        public Color batteryColor = new Color(0.95f, 0.35f, 0.25f);
        public Color ambushColor = new Color(0.45f, 0.5f, 0.55f);
        public Color decoyColor = new Color(0.3f, 0.9f, 0.5f);
        public Color minefieldColor = new Color(0.95f, 0.8f, 0.3f);
        public Color beamColor = new Color(0.6f, 0.75f, 1f);

        [Tooltip("Kule capinin hucre boyutuna orani.")]
        [Range(0.2f, 1f)]
        public float towerScale = 0.8f;

        [Header("Silah")]
        [Tooltip("Batarya kulenin saniyelik hasari. ALAN hasari verdigi icin " +
                 "dusuktur: olcumde alan hasari yaklasik 4 kat deger katiyor, " +
                 "2,5 alan hasari 10 tek hedefli hasara denk cikti.")]
        public float batteryDps = 2.5f;

        [Tooltip("Pusu mevzisinin saniyelik hasari. Dusuktur ama surekli is gorur - " +
                 "tuzak koridorunda gercek katliami bu yapar.")]
        public float ambushDps = 4f;

        [Tooltip("Isinin bir atista verdigi hasar. 20 olcumle secildi; karisimda " +
                 "10 ile 45 arasi ayni sonucu veriyor, farki SPAM davranisi " +
                 "belirledi (20'de isin spam'i batarya spam'iyle basa bas).")]
        public float beamShotDamage = 20f;

        [Tooltip("Isinin atislari arasindaki sure. Isinin ASIL denge dugmesi " +
                 "budur; hasar degil. 0,5 sn'de isin spam'i oyunu coz")]
        public float beamShotInterval = 1f;

        [Tooltip("Isin hattinin yatay yarim uzunlugu (hucre). Hat butun " +
                 "koridorlari kesecek kadar uzundur.")]
        public float beamHalfLength = 20f;

        [Header("Draft Pasifleri")]
        [Tooltip("Draft'tan gelen kalici hasar carpani.")]
        public float damageMultiplier = 1f;

        [Tooltip("Draft'tan gelen kalici menzil carpani.")]
        public float rangeMultiplier = 1f;

        public int sortingOrder = 15;

        public TowerKind Selected { get; private set; } = TowerKind.Battery;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
        }

        private void Update()
        {
            if (world == null || world.Grid == null) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(TowerKind.Battery);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(TowerKind.Ambush);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Select(TowerKind.Decoy);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Select(TowerKind.Minefield);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Select(TowerKind.Beam);

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

            if (world.Grid.Get(cell) != Frontline.Core.CellKind.BuildStrip)
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
        public Emplacement Place(TowerKind kind, int cell)
        {
            var go = new GameObject("Tower_" + kind);
            go.transform.SetParent(transform, false);

            // Blender asset'i varsa o kullanilir; yoksa daire cizilir.
            float scale = towerScale;
            Sprite sprite = PrimitiveSprites.Circle();
            Color color = ColorFor(kind);

            ArtLibrary art = ArtLibrary.Instance;
            ArtLibrary.TowerArt entry;

            if (art != null && art.TryGetTower(kind, out entry))
            {
                sprite = entry.sprite;
                if (entry.scale > 0f) scale = entry.scale;
                color = entry.tint.a > 0f ? entry.tint : Color.white;
            }

            float diameter = world.Space.CellSize * scale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;

            var tower = go.AddComponent<Emplacement>();
            tower.kind = kind;
            tower.Place(world, cell);

            // Sahte hedef hasar vermez; tek isi suruyu cekmektir.
            float dps = kind == TowerKind.Battery ? batteryDps
                      : (kind == TowerKind.Ambush ? ambushDps : 0f);

            // Isin surekli hasar vermez; kendi baglama yolu var.
            if (kind == TowerKind.Beam)
            {
                go.AddComponent<TowerWeapon>().BindBeam(
                    world, beamHalfLength * rangeMultiplier,
                    beamShotDamage * damageMultiplier, beamShotInterval);
            }
            // Alan hasari YALNIZCA batarya kulede: sürüyü bicen odur.
            else if (dps > 0f)
                go.AddComponent<TowerWeapon>().Bind(
                    world, tower.radius * rangeMultiplier, dps * damageMultiplier,
                    kind == TowerKind.Battery);

            // Titresim halkasi YALNIZCA batarya kulede. Halkasizlik,
            // "ordu beni goremiyor" demenin gorsel yoludur (spec 9.1).
            if (kind == TowerKind.Battery)
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
            if (kind == TowerKind.Battery) return batteryColor;
            if (kind == TowerKind.Decoy) return decoyColor;
            if (kind == TowerKind.Minefield) return minefieldColor;
            if (kind == TowerKind.Beam) return beamColor;
            return ambushColor;
        }
    }
}
