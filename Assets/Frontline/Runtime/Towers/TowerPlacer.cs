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

        [Tooltip("Kule capinin hucre boyutuna orani. 0,8 denendi ve kuleler " +
                 "kucuk duruyordu; 1,0 hucreyi tam dolduruyor ama telefonda " +
                 "hala kucuk okunuyordu (23 Agu). Tur basina carpan TowerFx." +
                 "silhouetteScale ile gelir - alcak siluetli sessiz kule " +
                 "ayni sayida daha az piksel kapliyor.")]
        [Range(0.2f, 2f)]
        public float towerScale = 1.15f;

        [Header("Silah")]
        [Tooltip("Batarya kulenin saniyelik hasari. ALAN hasari verdigi icin " +
                 "dusuktur: olcumde alan hasari yaklasik 4 kat deger katiyor, " +
                 "2,5 alan hasari 10 tek hedefli hasara denk cikti.")]
        public float batteryDps = 2.5f;

        [Tooltip("Pusu mevzisinin saniyelik hasari. Dusuktur ama surekli is gorur - " +
                 "tuzak koridorunda gercek katliami bu yapar.")]
        public float ambushDps = 4f;

        [Tooltip("Isinin bir atista verdigi hasar. 3 olcumle secildi: isin " +
                 "namlusunu hedefe dogrulttugu icin ISKALAMIYOR, o yuzden " +
                 "atis basina hasari dusuk.")]
        public float beamShotDamage = 3f;

        [Tooltip("Isinin atislari arasindaki sure. Isinin ASIL denge dugmesi " +
                 "budur; hasar degil. Olculen: 0,5 sn'de isin tek basina " +
                 "oyunu cozuyor (sizinti 0,2), 2 sn'de tek basina ise " +
                 "yaramiyor (30) ama karisimda deger uretiyor (20,7 -> 15,7).")]
        public float beamShotInterval = 2f;

        [Tooltip("Isinin menzili (hucre). 40 = haritayi bastan basa gecer. " +
                 "Gorsel olarak isin ekran disinda bittigi icin menzil de " +
                 "sinirsiz olmali; 8'de birakmak gorseli YALANCI yapardi. " +
                 "Olculdu: karisimda sizinti 15,7 -> 11,9, tek basina hala " +
                 "ise yaramiyor (30,0).")]
        public float beamRange = 40f;

        [Tooltip("Isin hattinin yarim KALINLIGI (hucre). Hat incedir; " +
                 "kalinlasirsa bir koridoru bastan sona kaplar ve spec 3.5'i " +
                 "siler.")]
        public float beamHalfWidth = 0.6f;

        [Header("Atis izleri (KOZMETIK)")]
        [Tooltip("Pusu izi: SOLUK ve ince. Oyuncu pusunun calistigini gorur " +
                 "ama ekranda gurultu yapan batarya olur (spec 9.1).")]
        public Color ambushTracer = new Color(0.72f, 0.80f, 0.86f, 0.40f);

        [Tooltip("Batarya izi: PARLAK ve kalin. Tek gurultulu kule odur.")]
        public Color batteryTracer = new Color(1f, 0.62f, 0.25f, 0.85f);

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

            // Yerlestirme parmak KALKINCA olur, basinca degil: basma ani
            // ayni zamanda haritayi kaydirmanin baslangicidir. Kaydirma
            // esigini asmis bir dokunus kule kurmaz (bkz. MapCamera).
            if (Input.GetMouseButtonUp(0))
            {
                bool panned = MapCamera.Instance != null && MapCamera.Instance.DraggedSincePress;
                if (!panned) TryPlaceAt(Input.mousePosition);
            }
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

            // Gorsel IKI PARCA: sabit kaide + donen ust yapi.
            // Tek parca oldugunda TowerTurret butun kuleyi ceviriyordu ve
            // tepeden bakista beton kaide yerinde donuyordu.
            // Tur carpani siluetin YOGUNLUGUNU esitler, sayisini degil.
            float scale = towerScale * TowerFx.For(kind).silhouetteScale;
            Sprite turretSprite = PrimitiveSprites.Circle();
            Sprite platformSprite = null;
            float rotationOffset = 0f;
            Color color = ColorFor(kind);

            ArtLibrary art = ArtLibrary.Instance;
            ArtLibrary.TowerArt entry;

            if (art != null && art.TryGetTower(kind, out entry))
            {
                if (entry.sprite != null) turretSprite = entry.sprite;
                platformSprite = entry.platform;
                rotationOffset = entry.rotationOffset;
                if (entry.scale > 0f) scale = entry.scale;
                color = entry.tint.a > 0f ? entry.tint : Color.white;
            }

            float diameter = world.Space.CellSize * scale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            // Kaide once cizilir ve ASLA donmez.
            if (platformSprite != null)
            {
                var platformGo = new GameObject("Platform");
                platformGo.transform.SetParent(go.transform, false);

                var platformRenderer = platformGo.AddComponent<SpriteRenderer>();
                platformRenderer.sprite = platformSprite;
                platformRenderer.sortingOrder = sortingOrder;
                platformRenderer.color = Color.white;
            }

            // Donen ust yapi kaidenin ustunde.
            var turretGo = new GameObject("Turret");
            turretGo.transform.SetParent(go.transform, false);

            var renderer = turretGo.AddComponent<SpriteRenderer>();
            renderer.sprite = turretSprite;
            renderer.sortingOrder = sortingOrder + 1;
            renderer.color = color;

            var tower = go.AddComponent<Emplacement>();
            tower.kind = kind;
            tower.Place(world, cell);

            // Sahte hedef hasar vermez; tek isi suruyu cekmektir.
            float dps = kind == TowerKind.Battery ? batteryDps
                      : (kind == TowerKind.Ambush ? ambushDps : 0f);

            // Isin surekli hasar vermez; kendi baglama yolu var.
            TowerWeapon weapon = null;

            if (kind == TowerKind.Beam)
            {
                weapon = go.AddComponent<TowerWeapon>();
                weapon.BindBeam(world, beamRange * rangeMultiplier,
                                beamShotDamage * damageMultiplier,
                                beamShotInterval, beamHalfWidth);
            }
            // Alan hasari YALNIZCA batarya kulede: sürüyü bicen odur.
            else if (dps > 0f)
            {
                weapon = go.AddComponent<TowerWeapon>();
                weapon.Bind(world, tower.radius * rangeMultiplier,
                            dps * damageMultiplier, kind == TowerKind.Battery);

                // Atis gorseli mekanigi yansitir: batarya parlak ve
                // seyrek (agir top), pusu soluk ve sik (surekli is gorur).
                bool loud = kind == TowerKind.Battery;
                weapon.tracerColor = loud ? batteryTracer : ambushTracer;
                weapon.tracerInterval = loud ? 0.55f : 0.30f;
                weapon.tracerSeconds = loud ? 0.09f : 0.06f;
            }

            // Atis gorselleri: namlu ucu, mermi, cakma, carpma. Ayni
            // yerden baglanir ki namlu ucu sayisi tek olsun (TowerFx).
            if (weapon != null)
            {
                weapon.fx = TowerFx.For(kind);
                weapon.visualScale = diameter;

                ArtLibrary.FxArt fxArt;
                if (art != null && art.TryGetFx(kind, out fxArt))
                {
                    weapon.bulletSprite = fxArt.bullet;
                    weapon.muzzleSprite = fxArt.muzzle;
                    weapon.impactSprite = fxArt.impact;
                }
            }

            // Namlu YALNIZCA hasar veren kulelerde doner. Donen bir sey
            // oyuncuya "bu beni hedefliyor" der; hasarsiz kule (yem,
            // mayin tarlasi) bunu soylememeli.
            if (weapon != null)
            {
                var turret = turretGo.AddComponent<TowerTurret>();
                turret.weapon = weapon;
                turret.rotationOffset = rotationOffset;
                turret.degreesPerSecond = kind == TowerKind.Battery ? 260f : 420f;
            }

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
