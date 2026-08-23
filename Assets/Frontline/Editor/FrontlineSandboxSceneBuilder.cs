using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Frontline.Core;
using Frontline.Runtime;

namespace Frontline.EditorTools
{
    /// <summary>
    /// Sandbox sahnesini kod ile uretir.
    ///
    /// Sahneyi elle kurmak yerine script ile uretmemizin sebebi: sahne dosyalari
    /// ikili benzeri YAML'dir, diff'i okunmaz ve birlestirmesi acilidir. Kurulum
    /// kod olarak durursa hem surum kontrolunde okunabilir kalir hem de bozuldugunda
    /// tek menu tiklamasiyla yeniden uretilir.
    /// </summary>
    public static class FrontlineSandboxSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Frontline_Sandbox.unity";

        [MenuItem("Frontline/Sandbox Sahnesini Uret")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Kamera ──────────────────────────────────────────────
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 11f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.05f);

            // Harita icinde gezinme: surukle-kaydir + yakinlastir.
            var mapCamera = camera.gameObject.AddComponent<MapCamera>();

            // ── Dunya ───────────────────────────────────────────────
            var worldGo = new GameObject("Battlefield");
            var world = worldGo.AddComponent<Battlefield>();

            mapCamera.world = world;
            mapCamera.targetCamera = camera;

            // ── Gorunum ─────────────────────────────────────────────
            var tilesGo = new GameObject("GridTiles");
            tilesGo.AddComponent<SpriteRenderer>();
            var tiles = tilesGo.AddComponent<GridTileRenderer>();
            tiles.corridorTile = LoadTexture("tile_corridor");
            tiles.buildStripTile = LoadTexture("tile_buildstrip");
            tiles.blockedTile = LoadTexture("tile_blocked");

            var overlayGo = new GameObject("ReconOverlay");
            overlayGo.AddComponent<SpriteRenderer>();
            var overlay = overlayGo.AddComponent<ReconOverlay>();

            // Yikilabilir ortu: kaya ve agaclar, yikilinca cukur.
            var sceneryGo = new GameObject("SceneryView");
            var scenery = sceneryGo.AddComponent<SceneryView>();
            scenery.world = world;
            // Agac ve kaya paketten geliyor (Garden/garden_pieces).
            // Blender'da urettiklerim yaninda uslup olarak yabanci
            // kaliyordu: biri boyali 2B, digeri render edilmis 3B.
            scenery.treeSprite = LoadSubSprite(GardenSheet, "garden_tree");
            scenery.rockSprite = LoadSubSprite(GardenSheet, "garden_stone");
            scenery.craterSprite = LoadSprite("scenery_crater");

            // Sprite'lar Blender'dan KENDI renkleriyle geliyor; tint beyaz
            // olmali ki uzerlerine ikinci bir renk binmesin. Hasar
            // karartmasi yine calisir: SceneryView tinti orana gore soldurur.
            scenery.treeTint = Color.white;
            scenery.rockTint = Color.white;
            scenery.craterTint = Color.white;

            // ── Gorsel kutuphanesi ──────────────────────────────────
            // Sahnede ArtLibrary YOKKEN her birlik ve kule beyaz daireye
            // dusuyordu. Blender'dan gelen sprite'lar buradan dagitiliyor.
            BuildArtLibrary();

            // ── Us ────────────────────────────────────────────────
            var homeBaseGo = new GameObject("HomeBaseHealth");
            var homeBase = homeBaseGo.AddComponent<HomeBaseHealth>();

            // Usun gorseli ve vurulma animasyonu. Sprite ArtLibrary'den
            // gelir; atanmamissa hicbir sey cizilmez ve oyun calismaya
            // devam eder.
            var homeBaseViewGo = new GameObject("HomeBaseView");
            homeBaseViewGo.AddComponent<SpriteRenderer>();
            var homeBaseView = homeBaseViewGo.AddComponent<HomeBaseView>();
            homeBaseView.world = world;
            homeBaseView.homeBase = homeBase;

            // ── Birlikler ────────────────────────────────────────────
            var spawnerGo = new GameObject("UnitSpawner");
            var spawner = spawnerGo.AddComponent<UnitSpawner>();
            spawner.world = world;
            spawner.homeBase = homeBase;

            // ── Run akisi ───────────────────────────────────────────
            var runGo = new GameObject("RunController");
            var run = runGo.AddComponent<RunController>();
            run.world = world;
            run.spawner = spawner;
            run.homeBase = homeBase;

            // ── Ekonomi ─────────────────────────────────────────────
            var ecoGo = new GameObject("EconomyRunner");
            var economy = ecoGo.AddComponent<EconomyRunner>();

            // ── Kule yerlestirme ────────────────────────────────────
            var placerGo = new GameObject("TowerPlacer");
            var placer = placerGo.AddComponent<TowerPlacer>();
            placer.world = world;
            placer.targetCamera = camera;
            placer.economy = economy;

            // Kule sprite'lari 1 dunya birimi cikiyor; varsayilan 1,15 ile
            // carpilinca siluet carpani en buyuk kulede 1,5 HUCREYE ulasip
            // komsu mevzilerin uzerine tasiyordu. 0,72 en buyugu ~0,95
            // hucrede tutuyor.
            placer.towerScale = 0.72f;

            // ── Arayuz ──────────────────────────────────────────────
            var hudGo = new GameObject("RunHud");
            var hud = hudGo.AddComponent<RunHud>();
            hud.run = run;
            hud.homeBase = homeBase;
            hud.spawner = spawner;
            hud.placer = placer;
            hud.economy = economy;

            // ── Dalga sonu draft ────────────────────────────────────
            var draftGo = new GameObject("DraftView");
            var draft = draftGo.AddComponent<DraftView>();
            draft.world = world;
            draft.economy = economy;
            draft.placer = placer;
            run.draft = draft;

            // ── Baglayici ───────────────────────────────────────────
            var binderGo = new GameObject("BattlefieldViewBinder");
            var binder = binderGo.AddComponent<BattlefieldViewBinder>();
            binder.world = world;
            binder.tiles = tiles;
            binder.overlay = overlay;
            binder.targetCamera = camera;

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[FrontlineSandbox] Sahne uretildi: " + ScenePath);
        }

        /// <summary>
        /// Birlik, kule ve us gorsellerini tasiyan tek kaynak. Tint'lerin
        /// ALPHA'SI 0: ArtLibrary bunu "sprite kendi renkleriyle cizilsin"
        /// diye okuyor. Alpha 1 verilirse Blender render'inin uzerine ikinci
        /// bir renk biner ve butun paleti bozar.
        /// </summary>
        private static void BuildArtLibrary()
        {
            var go = new GameObject("ArtLibrary");
            var art = go.AddComponent<ArtLibrary>();
            Color own = new Color(1f, 1f, 1f, 0f);

            // Birlikler paketin TEPEDEN asker gorselinden turetiliyor.
            // Tek bir asker cizimi var; turler renk, kare ve olcekle
            // ayrisiyor. Agir tur ise tank: "yavas, cok canli" tanimina
            // arac silueti askerden cok daha iyi oturuyor.
            art.units = new[]
            {
                MakeUnit(UnitType.Infantry, LoadSprite2(ArmyMiddle), own),
                MakeUnit(UnitType.Scout,    LoadSprite2(ArmyLeft),
                         new Color(0.95f, 0.86f, 0.55f, 1f)),
                MakeUnit(UnitType.Soldier,  LoadSprite2(TankCut),   own),
                MakeUnit(UnitType.Blind,    LoadSprite2(ArmyRight),
                         new Color(0.48f, 0.50f, 0.56f, 1f)),
                MakeUnit(UnitType.Brood,    LoadSprite2(ArmyLeft),
                         new Color(0.72f, 0.45f, 0.85f, 1f)),
                MakeUnit(UnitType.Queen,    LoadSprite2(TankCut),
                         new Color(0.90f, 0.25f, 0.28f, 1f)),
            };

            // Bes kule bes AYRI kaynaktan geliyor. Hepsini tek sayfadan
            // secmek onlari birbirinin kopyasi yapiyordu; oysa kule turu
            // oyunun MERKEZI ayrimi - oyuncu bir bakista ayirt etmeli.
            art.towers = new[]
            {
                // Batarya: agir top. Ordu ondan kacmayi ogrenir, en
                // gosterisli olan bu. Namlu sayfada YUKARI cizilmis.
                MakeTower(TowerKind.Battery,
                          LoadSubSprite(SheetCannonTurret, "turret_0_3"),
                          LoadSubSprite(SheetCannonPlatform, "Platform_0"),
                          180f, own),

                // Pusu: alcak, yesil makineli. Ordu onu ALGILAYAMAZ.
                // Namlu sayfada SOLA cizilmis.
                MakeTower(TowerKind.Ambush,
                          LoadSubSprite(SheetMachineGun, "mg_turret"),
                          LoadSubSprite(SheetMachineGun, "mg_platform"),
                          90f, own),

                // Yem: radar canagi. Namlusu yok, hasar vermez - siluet
                // bunu soylemeli.
                MakeTower(TowerKind.Decoy,
                          LoadSubSprite(SheetRadar, "radar_dish"),
                          LoadSubSprite(SheetRadar, "radar_platform"),
                          0f, own),

                // Mayin tarlasi: buz kulesi. Yavaslatma alani ile kristal
                // ayni seyi anlatiyor. Tek parca; donmez zaten.
                MakeTower(TowerKind.Minefield,
                          LoadSubSprite(SheetIce, "ice_tower"),
                          null,
                          0f, own),

                // Isin: mavi enerji namlusu, gri radar kaidesi uzerinde.
                MakeTower(TowerKind.Beam,
                          LoadSubSprite(SheetCannonTurret, "turret_1_0"),
                          LoadSubSprite(SheetRadar, "radar_platform"),
                          180f, own),
            };

            // Us: paketin tepeden kalesi. Kaynak gorselin arka plani
            // saydam degildi; tasma-doldurma ile kesilip
            // Sprites/Frontline/home_fortress.png olarak kaydedildi.
            art.baseSprite = LoadSprite("home_fortress");
            art.baseTint = own;
        }

        private const string ArmyMiddle = "Assets/Sprites/Enemies/Army Runne/Army_middle.png";
        private const string ArmyLeft = "Assets/Sprites/Enemies/Army Runne/Army_left.png";
        private const string ArmyRight = "Assets/Sprites/Enemies/Army Runne/Army_right.png";
        private const string TankCut = "Assets/Sprites/Frontline/enemy_tank.png";

        private static ArtLibrary.UnitArt MakeUnit(UnitType type, Sprite sprite, Color tint)
        {
            return new ArtLibrary.UnitArt
            {
                type = type,
                sprite = sprite,
                scale = 0f,      // 0 = spawner'in varsayilani
                tint = tint
            };
        }

        /// <summary>Tam yol ile tek sprite yukler.</summary>
        private static Sprite LoadSprite2(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning("[FrontlineSandbox] Sprite bulunamadi: " + path);
            return sprite;
        }

        private const string GardenSheet = "Assets/Sprites/Garden/garden_pieces.png";
        private const string SheetCannonTurret = "Assets/Sprites/Towers/Cannon_tower/Turret.png";
        private const string SheetCannonPlatform = "Assets/Sprites/Towers/Cannon_tower/Platform.png";
        private const string SheetMachineGun = "Assets/Sprites/Towers/Machine_Gun_tower/Machnie_Gun.png";
        private const string SheetRadar = "Assets/Sprites/Towers/Radar_tower/Radar.png";
        private const string SheetIce = "Assets/Sprites/Towers/Ice_tower/Ice_tower.png";

        private static ArtLibrary.TowerArt MakeTower(
            TowerKind kind, Sprite turret, Sprite platform, float rotationOffset, Color tint)
        {
            return new ArtLibrary.TowerArt
            {
                kind = kind,
                sprite = turret,
                platform = platform,
                rotationOffset = rotationOffset,
                scale = 0f,      // 0 = placer'in varsayilani
                tint = tint
            };
        }

        /// <summary>
        /// Bir sprite sayfasindan tek bir dilimi ada gore ceker.
        /// LoadAssetAtPath sayfanin KENDISINI dondurur, dilimleri degil;
        /// dilimlere ancak LoadAllAssetsAtPath ile ulasilir.
        /// </summary>
        private static Sprite LoadSubSprite(string sheetPath, string spriteName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                var sprite = asset as Sprite;
                if (sprite != null && sprite.name == spriteName) return sprite;
            }

            Debug.LogWarning("[FrontlineSandbox] Dilim bulunamadi: " +
                             spriteName + " (" + sheetPath + ")");
            return null;
        }

        /// <summary>
        /// Zemin dosemesini yukler. Read/Write acik olmali; kapaliysa
        /// GridTileRenderer sessizce duz renkli zemine duser.
        /// </summary>
        private static Texture2D LoadTexture(string fileName)
        {
            string path = "Assets/Sprites/Frontline/" + fileName + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
                Debug.LogWarning("[FrontlineSandbox] Doseme bulunamadi: " + path);

            return texture;
        }

        /// <summary>
        /// Blender'dan uretilen ortu gorselini yukler. Bulunamazsa null doner
        /// ve SceneryView daireye duser - eksik asset oyunu durdurmamali.
        /// </summary>
        private static Sprite LoadSprite(string fileName)
        {
            string path = "Assets/Sprites/Frontline/" + fileName + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
                Debug.LogWarning("[FrontlineSandbox] Sprite bulunamadi: " + path);

            return sprite;
        }
    }
}
