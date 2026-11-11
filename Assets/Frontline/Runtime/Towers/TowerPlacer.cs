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

        [Tooltip("Kalici (run'lar arasi) kule yukseltmeleri - ana menu " +
                 "'GUNCELLE' ekraninda satin alinir (3 Eylul 2026 gece " +
                 "plani, FAZ 3). Bos birakilirsa Awake'te diskten yuklenir " +
                 "(MetaProgressStore.Load); testler burayi elle atayarak " +
                 "disk erisimini atlayabilir.")]
        public MetaState permanentMeta;

        [Tooltip("Bos birakilirsa sahnede aranir. Atanmissa bos kurulum " +
                 "karesine dokununca DOGRUDAN kurmak yerine bu menu acilir " +
                 "(kullanici geri bildirimi: 'daire seklinde ciksinlar " +
                 "fiyati ve sureti belirsin, hangisine tiklasam o kurulsun').")]
        public Frontline.Runtime.UI.RadialBuildMenu radialMenu;

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
            if (radialMenu == null) radialMenu = FindFirstObjectByType<Frontline.Runtime.UI.RadialBuildMenu>();

            // Diskten yalniz burada, bir kez okunur - Place() her cagrida
            // dosya acmaz. Kayit yoksa/bozuksa MetaProgressStore temiz bir
            // MetaState doner (StartingLevelOf her kule icin 1), yani kalici
            // yukseltme hic alinmamissa davranis ONCEKIYLE BIREBIR aynidir.
            if (permanentMeta == null) permanentMeta = MetaProgressStore.Load();
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
                // Arayuz uzerinde biten dokunus haritaya gecmez: alt
                // banttaki kule dugmesine basmak besini harcayip yanlis
                // yere kule kurmamali (bkz. UiTouchGuard).
                if (Frontline.Runtime.UI.UiTouchGuard.IsPointerOverUi()) return;

                // Radial menu ACIKKEN bu bilesen HICBIR sey yapmaz - ikinci
                // bir savunma katmani. UiTouchGuard'in EventSystem'e
                // dayanmasi tek basina yeterli olmali ama menu her seyi
                // kaplamiyorsa (ornegin dilimler radius disina tasarsa)
                // bu kontrol menunun acik oldugu surece kurmayi tamamen
                // durdurur.
                if (radialMenu != null && radialMenu.IsOpen) return;

                bool panned = MapCamera.Instance != null && MapCamera.Instance.DraggedSincePress;
                if (!panned) TryPlaceAt(Input.mousePosition);
            }
        }

        /// <summary>
        /// Kule secimini degistirir. Public: arayuzdeki kule dugmelerinin
        /// (TowerBarView) dogrudan cagirdigi tek yer budur.
        /// </summary>
        public void Select(TowerKind kind)
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

            // Hucre basina EN FAZLA bir kule (kullanici geri bildirimi,
            // 2 Eylul 2026). Kontrol yoktu; ayni hucreye ust uste kule
            // kurulup gorsel/mantik cakismasi olusabiliyordu.
            if (IsOccupied(cell))
            {
                Debug.Log("[TowerPlacer] Bu hucrede zaten bir kule var.");
                return;
            }

            // Bos kurulum karesine dokununca ARTIK dogrudan kurmuyoruz -
            // dairesel menu aciliyor (kullanici geri bildirimi, 2 Eylul
            // 2026): "daire seklinde ciksinlar fiyati ve sureti belirsin,
            // hangisine tiklasam o kurulsun." Ekonomi kontrolu ve gercek
            // Place() cagrisi artik RadialBuildMenu.ChooseTower'da.
            if (radialMenu != null)
            {
                radialMenu.Open(cell);
                return;
            }

            // Menu sahnede yoksa (ornegin eski bir test kurulumu) eski
            // davranisa duser: alt bardan secili turu dogrudan kurar.
            if (economy != null && !economy.CanAfford(Selected))
            {
                Debug.Log("[TowerPlacer] Besin yetmiyor. Gereken=" +
                          economy.CostFor(Selected) + " eldeki=" + economy.Economy.Balance);
                return;
            }

            int cost = economy != null ? economy.CostFor(Selected) : 0;
            if (economy != null && !economy.TryBuy(Selected))
                return;

            Place(Selected, cell, cost);
        }

        /// <summary>Verilen hucrede zaten bir kule var mi?</summary>
        /// <summary>
        /// Secili kuleyi bir kademe yukseltmeyi dener: ekonomiden dus,
        /// basarirsa Emplacement.Upgrade() cagirir. Kule zaten 3. seviyedeyse
        /// ya da besin yetmiyorsa hicbir sey degismez. Maliyet MEVCUT
        /// kurulum fiyatindan (EconomyRunner.CostFor, ayni turden onceki
        /// kuleler dahil) turetilir - yeni bir fiyat tablosu uydurulmadi.
        /// </summary>
        public bool TryUpgrade(Emplacement tower)
        {
            if (tower == null || tower.Level >= Emplacement.MaxLevel) return false;
            if (economy == null) return false;

            int baseCost = economy.CostFor(tower.kind);
            int cost = Emplacement.UpgradeCost(baseCost, tower.Level);

            if (!economy.Economy.TrySpend(cost)) return false;

            if (tower.Upgrade())
            {
                tower.RecordSpend(cost);
                return true;
            }

            // Yukseltme (beklenmedik sekilde) basarisiz olursa besini geri ver.
            economy.Economy.Earn(cost);
            return false;
        }

        /// <summary>
        /// Kuleyi satar: dokunmatik "Sat" butonu (3 Eylul 2026). Geri odeme
        /// Emplacement.SellValue() ile turetilir (harcanan toplamin %60'i -
        /// bkz. Emplacement.SellRefundRatio, uydurma bir sayi degil). Kule
        /// dunyadan cikar (Emplacement.OnDestroy -> world.UnregisterTower
        /// zaten kendiliginden calisir) ve GameObject yok edilir.
        /// </summary>
        public bool TrySell(Emplacement tower)
        {
            if (tower == null || economy == null) return false;

            int refund = tower.SellValue();
            economy.Economy.Refund(refund);

            Destroy(tower.gameObject);
            return true;
        }

        private bool IsOccupied(int cell)
        {
            var towers = world.Towers;
            for (int i = 0; i < towers.Count; i++)
                if (towers[i].Cell == cell) return true;
            return false;
        }

        /// <summary>
        /// Gercekten odenen tutari da kaydeden yerlestirme - Sat butonunun
        /// geri odemesi buradan turer (bkz. Emplacement.SellValue). Ekonomi
        /// karari (CanAfford/TryBuy) HALA cagiran taraftadir; bu yalnizca
        /// kaydi tutar.
        /// </summary>
        public Emplacement Place(TowerKind kind, int cell, int spentAmount)
        {
            Emplacement tower = Place(kind, cell);
            if (tower != null) tower.RecordSpend(spentAmount);
            return tower;
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
                // Seviye 1 gorseli varsa tercih edilir (3 Eylul 2026,
                // yukseltme sistemi): tek-sprite 'sprite' alani geriye
                // donuk uyumluluk icin kalir.
                if (entry.levelTurrets != null && entry.levelTurrets.Length > 0 &&
                    entry.levelTurrets[0] != null)
                {
                    turretSprite = entry.levelTurrets[0];
                }
                platformSprite = entry.platform;
                rotationOffset = entry.rotationOffset;
                if (entry.scale > 0f) scale = entry.scale;
                color = entry.tint.a > 0f ? entry.tint : Color.white;
            }

            float diameter = world.Space.CellSize * scale;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            // Kaide once cizilir ve ASLA donmez.
            // Golge kaymasi hucre boyutuyla olceklenir (bkz. SpriteShadow).
            Vector2 shadowOffset = new Vector2(0.07f, -0.07f) * world.Space.CellSize;
            SpriteRenderer platformRenderer = null;

            if (platformSprite != null)
            {
                var platformGo = new GameObject("Platform");
                platformGo.transform.SetParent(go.transform, false);

                platformRenderer = platformGo.AddComponent<SpriteRenderer>();
                platformRenderer.sprite = platformSprite;
                platformRenderer.sortingOrder = sortingOrder;
                platformRenderer.color = Color.white;

                // Kaidenin zemine dusen golgesi.
                SpriteShadow.Attach(platformRenderer, go.transform,
                                    shadowOffset * 0.6f, sortingOrder - 1);
            }

            // Donen ust yapi kaidenin ustunde.
            var turretGo = new GameObject("Turret");
            turretGo.transform.SetParent(go.transform, false);

            var renderer = turretGo.AddComponent<SpriteRenderer>();
            renderer.sprite = turretSprite;
            renderer.sortingOrder = sortingOrder + 2;
            renderer.color = color;

            // Taretin kaideye dusen golgesi: kaide ile taret arasinda cizilir.
            // Yalnizca iki parcali (kaideli) kulelerde - tek sprite'li eski
            // kulelerde zemine dusen bir taret golgesi yanlis okunurdu.
            if (platformRenderer != null)
                SpriteShadow.Attach(renderer, go.transform, shadowOffset, sortingOrder + 1);

            var tower = go.AddComponent<Emplacement>();
            tower.kind = kind;
            tower.Place(world, cell);

            // Silahli kuleler: Makineli Tufek (Ambush), Topcu (Battery),
            // Keskin Nisanci (Beam). Radar ve Zaman Bozulmasi ates etmez.
            // Butun sayilar TowerStats'tan gelir (Emplacement.ApplyStats);
            // buradaki batteryDps/ambushDps/beam* alanlari artik OKUNMUYOR.
            TowerWeapon weapon = null;

            if (TowerStats.IsWeapon(kind))
            {
                weapon = go.AddComponent<TowerWeapon>();
                weapon.Configure(world, kind, damageMultiplier, rangeMultiplier);
                weapon.beamHalfWidth = beamHalfWidth;

                // Atis gorseli mekanigi yansitir: Topcu parlak ve seyrek
                // (agir mermi), Makineli Tufek soluk ve cok sik.
                bool loud = kind == TowerKind.Battery;
                weapon.tracerColor = loud ? batteryTracer : ambushTracer;
                weapon.tracerInterval = 0.14f;
                weapon.tracerSeconds = loud ? 0.09f : 0.05f;
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

                // Radar etkisindeki kule ara ara parlar (bkz. AuraFeedback).
                AuraFeedback.Attach(go, weapon, renderer);

                // Ates aninda geri tepme: TowerWeapon kendi Recoil()'ini
                // burada baglanan turret uzerinden cagirir (3 Eylul 2026
                // gece kullanici istegi). Bos birakilirsa TowerWeapon
                // sessizce atlar (ArtLibrary kurali).
                weapon.turret = turret;
            }

            // Destek kuleleri: ust yapi yavasca doner (canak tarar, yayici
            // calisir) ve Zaman Bozulmasi etki alanini zeminde gosterir.
            if (weapon == null)
                SupportFeedback.Attach(go, tower, turretGo.transform,
                                       world.Space.CellSize, sortingOrder - 2);

            // Yukseltme sisteminin (Emplacement.Upgrade) sprite/sayi
            // degistirebilmesi icin referanslari birakir; seviye 1 sayilarini
            // da burada uygular (Emplacement.ApplyStats).
            tower.BindVisual(renderer, weapon);

            // Yildiz rozeti (referans oyundaki gibi, seviye gostergesi):
            // kule sprite'inin USTUNE bindirilir. go.transform.localScale
            // zaten diameter'a esitlendigi icin bu holder'in olcegi 1/diameter
            // ile TERS carpilir - aksi halde yildizlar kule buyudukce
            // orantisiz devasa/minik gorunurdu.
            var starsGo = new GameObject("Stars");
            starsGo.transform.SetParent(go.transform, false);
            starsGo.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            float inv = diameter > 0.0001f ? 1f / diameter : 1f;
            starsGo.transform.localScale = new Vector3(inv, inv, 1f);

            var stars = starsGo.AddComponent<TowerStars>();
            stars.Build(null); // Blender yildiz sprite'i eklenene kadar PrimitiveSprites.Star() kullanilir.
            tower.BindStars(stars);

            // Kalici (meta) yukseltme: oyuncu ana menude bu kule turu icin
            // kademe satin aldiysa, kule BEDAVA olarak o kademede kurulur -
            // ayni Emplacement.Upgrade() yolu kullanilir (sprite/hasar/yildiz
            // tutarli kalsin diye), ama SpentTotal'e YAZILMAZ (bedava kademe
            // satista geri odeme uretmemeli, tutarli: draft odulu kuleyle
            // ayni kural).
            if (permanentMeta != null)
            {
                int startingLevel = permanentMeta.StartingLevelOf(kind);
                for (int i = 1; i < startingLevel; i++) tower.Upgrade();
            }

            // NOT: batarya kulenin surekli titreyen tehdit halkasi
            // kaldirildi (kullanici geri bildirimi, 2 Eylul 2026: "surekli
            // kirmizi cember yayiyor bu da olmasin"). Menzil artik yalnizca
            // OYUNCU o kuleyi SECTIGINDE gorunur (bkz. RangeRingView /
            // TowerSelection) - "hangi kulenin uzerine tiklasam onun
            // kapsama alanini gormek istiyorum, onun disinda gizli olsun."

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
