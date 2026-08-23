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
            scenery.treeSprite = LoadSprite("scenery_tree");
            scenery.rockSprite = LoadSprite("scenery_rock");
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

            // Blender kule sprite'lari 1,7 birimlik bir cerceveye cekiliyor
            // (namlu donerken cerceveden tasmasin diye). Gorunur icerik
            // cercevenin ~0,68'i; 0,72 olcek kuleyi 0,28 hucreye dusurup
            // gorunmez yapiyordu. 1,15 gorunur govdeyi ~0,8 hucrede tutar.
            placer.towerScale = 1.15f;

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
        /// Birlik, kule ve us gorsellerini tasiyan tek kaynak.
        ///
        /// BUTUN gorseller Blender'da uretiliyor (tools/blender kaynak
        /// dosyasi: Assets/Sprites/Frontline/frontline_sprites.blend) ve
        /// hepsi TAM TEPEDEN cekiliyor. Hazir asset paketleri birakildi:
        /// acilari 3/4 idi, namlu yonleri sayfadan sayfaya degisiyordu ve
        /// bes kule ayni sayfadan gelince birbirinin kopyasi oluyordu.
        ///
        /// Tint'lerin ALPHA'SI 0: ArtLibrary bunu "sprite kendi renkleriyle
        /// cizilsin" diye okuyor.
        /// </summary>
        private static void BuildArtLibrary()
        {
            var go = new GameObject("ArtLibrary");
            var art = go.AddComponent<ArtLibrary>();
            Color own = new Color(1f, 1f, 1f, 0f);

            art.units = new[]
            {
                MakeUnit(UnitType.Infantry, "u_infantry", own),
                MakeUnit(UnitType.Scout,    "u_scout",    own),
                MakeUnit(UnitType.Soldier,  "u_soldier",  own),
                MakeUnit(UnitType.Blind,    "u_blind",    own),
                MakeUnit(UnitType.Brood,    "u_brood",    own),
                MakeUnit(UnitType.Queen,    "u_queen",    own),
            };

            // Bes kule bes AYRI siluet. Kule turu oyunun merkezi ayrimi;
            // oyuncu bir bakista ayirt etmeli (spec 3.1-3.2).
            //
            // rotationOffset HEPSINDE 0: namlular Blender'da YEREL -Y'ye
            // modellendi, yani TowerTurret'in taban kuralina uyuyorlar.
            // Hicbir sprite kendi istisnasini tasimiyor.
            art.towers = new[]
            {
                MakeTower(TowerKind.Battery,   "t_battery",   own),
                MakeTower(TowerKind.Ambush,    "t_ambush",    own),
                MakeTower(TowerKind.Decoy,     "t_decoy",     own),
                MakeTower(TowerKind.Minefield, "t_minefield", own),
                MakeTower(TowerKind.Beam,      "t_beam",      own),
            };

            art.baseSprite = LoadSprite("home_base");
            art.baseTint = own;
        }

        private static ArtLibrary.UnitArt MakeUnit(UnitType type, string file, Color tint)
        {
            return new ArtLibrary.UnitArt
            {
                type = type,
                sprite = LoadSprite(file),
                scale = 0f,      // 0 = spawner'in varsayilani
                tint = tint
            };
        }

        /// <summary>
        /// Kule gorseli iki parcadir: "<prefix>_turret" doner,
        /// "<prefix>_platform" sabit kalir. Ikisi de ayni ortografik
        /// olcekle cekildigi icin Unity'de birebir ust uste oturur.
        /// </summary>
        private static ArtLibrary.TowerArt MakeTower(TowerKind kind, string prefix, Color tint)
        {
            return new ArtLibrary.TowerArt
            {
                kind = kind,
                sprite = LoadSprite(prefix + "_turret"),
                platform = LoadSprite(prefix + "_platform"),
                rotationOffset = 0f,
                scale = 0f,      // 0 = placer'in varsayilani
                tint = tint
            };
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
