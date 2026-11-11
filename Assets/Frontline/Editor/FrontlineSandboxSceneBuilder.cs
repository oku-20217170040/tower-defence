using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Frontline.Core;
using Frontline.Runtime;
using Frontline.Runtime.UI;

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
        private const string MainMenuScenePath = "Assets/Scenes/Frontline_MainMenu.unity";

        [MenuItem("Frontline/Sandbox Sahnesini Uret")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Olay sistemi ────────────────────────────────────────
            // KRITIK: EventSystem sahnede yoksa uGUI Button'lar dokunmayi
            // hic yakalamaz VE UiTouchGuard.IsPointerOverUi() her zaman
            // false doner (EventSystem.current null kalir). Sonuc:
            // radial menudeki bir dilime dokunmak butonu tetiklemek yerine
            // TowerPlacer'in kendi TryPlaceAt'ine dusuyor ve menu yanlis
            // hucreye kayiyor - kullanici geri bildirimi (2 Eylul 2026)
            // tam olarak buydu: "kule, o an yuvarlakta gozuktugu yere
            // kurulmaya calisiyor." Bu, olmadan hicbir uGUI etkilesiminin
            // GUVENILIR calismadigi tek satirlik bir on kosuldur.
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── Kamera ──────────────────────────────────────────────
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 11f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.05f);
            cameraGo.AddComponent<AudioListener>();

            // Harita icinde gezinme: surukle-kaydir + yakinlastir.
            //
            // Kullanici geri bildirimi (2 Eylul 2026): "harita tamami
            // gozukmesin, objeler asiri uzak duruyor". Acilis genisligi
            // 13 hucre (tam harita) yerine 7'ye dusuruldu - kule ve
            // birim siluetleri artik iki katina yakin buyuk. En yakin
            // zoom siniri da 10 -> 5 hucreye indi ki oyuncu istedigi
            // kadar daha da yaklasabilsin.
            var mapCamera = camera.gameObject.AddComponent<MapCamera>();
            //
            // 11 Kasim 2026 (Serhat): surukleyerek kaydirma kalir, zoom
            // YALNIZCA IKI KADEME - genis (haritanin tam genisligi) ve onun
            // biraz darligi. "Cok yakin gozukmesin, yoksa sikar." Eski 7
            // hucrelik acilis bu yuzden 9,5'e acildi.
            mapCamera.wideWidthCells = 0f;      // 0 = haritanin tam genisligi
            mapCamera.narrowWidthCells = 9.5f;
            mapCamera.minVisibleCells = 5f;

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
            scenery.treeSpriteLarge = LoadSprite("scenery_tree_large");
            scenery.rockSpriteLarge = LoadSprite("scenery_rock_large");
            scenery.craterSprite = LoadSprite("scenery_crater");
            scenery.mountainSprite = LoadSprite("scenery_mountain");

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

            // ── Dairesel kule kurma menusu ────────────────────────────
            // Bos kurulum karesine dokununca acilir (kullanici geri
            // bildirimi, 2 Eylul 2026).
            var radialGo = new GameObject("RadialBuildMenu");
            var radial = radialGo.AddComponent<RadialBuildMenu>();
            radial.placer = placer;
            radial.economy = economy;
            radial.targetCamera = camera;
            placer.radialMenu = radial;

            // ── Kule secimi / menzil gosterimi ───────────────────────
            // Var olan bir kuleye dokununca yalnizca ONUN menzili gorunur
            // (kullanici geri bildirimi: "hangi kuleye tiklasam onun
            // kapsama alanini gormek istiyorum, geri kalani gizli olsun").
            var selectionGo = new GameObject("TowerSelection");
            var selection = selectionGo.AddComponent<TowerSelection>();
            selection.world = world;
            selection.targetCamera = camera;
            selection.ringSprite = LoadSprite("fx_range_ring");

            // ── Yukselt / Sat butonlari ──────────────────────────────
            // Secili kulenin uzerinde/altinda beliren dokunmatik panel
            // (kullanici talimati, 3 Eylul 2026: yukseltme sistemi klavye
            // 'U' ile calisiyordu ama telefonda dokunmatik buton yoktu).
            var actionPanelGo = new GameObject("TowerActionPanel");
            var actionPanel = actionPanelGo.AddComponent<TowerActionPanel>();
            actionPanel.selection = selection;
            actionPanel.placer = placer;
            actionPanel.economy = economy;
            actionPanel.world = world;
            actionPanel.targetCamera = camera;

            // ── Ortu hedef kilitleme ──────────────────────────────────
            // Agac/kayaya dokununca menzildeki kuleler oraya kilitlenir
            // (kullanici geri bildirimi, 2 Eylul 2026).
            var focusGo = new GameObject("SceneryFocus");
            var focus = focusGo.AddComponent<SceneryFocusController>();
            focus.world = world;
            focus.targetCamera = camera;
            focus.markerSprite = LoadSprite("fx_focus_marker");

            // ── Oyun hizi ───────────────────────────────────────────
            var speedGo = new GameObject("GameSpeed");
            var speed = speedGo.AddComponent<GameSpeedControl>();
            speed.run = run;

            // ── Meta ilerleme ───────────────────────────────────────
            // Ortu (agac/kaya) yikilinca buyuklugune gore besin verir
            // (kullanici geri bildirimi, 2 Eylul 2026).
            var sceneryRewardGo = new GameObject("SceneryReward");
            var sceneryReward = sceneryRewardGo.AddComponent<SceneryRewardRunner>();
            sceneryReward.world = world;
            sceneryReward.economy = economy;

            var metaGo = new GameObject("MetaProgress");
            var meta = metaGo.AddComponent<MetaProgressRunner>();
            meta.run = run;
            meta.spawner = spawner;

            // ── Arayuz ──────────────────────────────────────────────
            // RunScreen uretim ekranidir (spec 3.8, uc bant, uGUI).
            // Eski RunHud (IMGUI) devre disi birakildi ama silinmedi -
            // hizli bir debug/sandbox denetimi gerektiginde sahneye elle
            // eklenebilir.
            var screenGo = new GameObject("RunScreen");
            var screen = screenGo.AddComponent<RunScreen>();
            screen.run = run;
            screen.homeBase = homeBase;
            screen.spawner = spawner;
            screen.economy = economy;
            screen.speed = speed;
            screen.placer = placer;

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
        /// Ana menu sahnesini uretir: EventSystem + kod ile kurulan
        /// MainMenuScreen. Sandbox sahnesiyle ayni gerekce - sahne YAML'i
        /// elle duzenlenmez, tek tikla yeniden uretilebilir olmali
        /// (2026-09-03 menu/ses gorevi).
        /// </summary>
        [MenuItem("Frontline/Ana Menu Sahnesini Uret")]
        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.05f);
            cameraGo.AddComponent<AudioListener>();

            var menuGo = new GameObject("MainMenuScreen");
            menuGo.AddComponent<MainMenuScreen>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);

            Debug.Log("[FrontlineSandbox] Ana menu sahnesi uretildi: " + MainMenuScenePath);
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
                // Ucan birligin daha once sprite kaydi YOKTU (daire olarak
                // ciziliyordu); 11 Kasim 2026 birlik setiyle eklendi.
                MakeUnit(UnitType.Flyer,    "u_flyer",    own),
            };

            // Bes kule bes AYRI siluet. Kule turu oyunun merkezi ayrimi;
            // oyuncu bir bakista ayirt etmeli (spec 3.1-3.2).
            //
            // rotationOffset HEPSINDE 0: namlular Blender'da YEREL -Y'ye
            // modellendi, yani TowerTurret'in taban kuralina uyuyorlar.
            // Hicbir sprite kendi istisnasini tasimiyor.
            // 4 Eylul gunduz: piksel-olculu referansla yeniden tasarlanan
            // TowerVariety seti (tools/blender/generate_tower_variety_set.py)
            // buraya baglandi. Eslesme ROL'e gore: Battery (alan hasari,
            // agir/ezici) -> agir_top; Ambush (surekli/hizli DPS) ->
            // hizli_ates; Beam (uzun menzil, hic ISKALAMAZ) -> keskin_nisanci
            // (uzun ince namlu = uzun menzil hissi); Decoy/Minefield (SILAHSIZ,
            // dusman cekme/pasif alan) -> namlusuz destek siluetleri
            // (radar_destek/zaman_bozulmasi) - gorsel olarak "atmiyor" hissini
            // veriyorlar. rotationOffset=0: hull() namluyu -Y'ye modelliyor,
            // TowerTurret'in taban kuraliyla (0 = -Y) zaten uyumlu. platform
            // artik AYRI bir parca degil - govde+palet+namlu TEK render'da
            // (bkz. TowerVariety script'i), yani platform=null birakiliyor ve
            // tum sprite Turret olarak doner (kaide de namluyla birlikte
            // doner - eski iki-parcali "sabit kaide" davranisindan bilincli
            // sapma, 4 Eylul gece plani notuna bkz.).
            art.towers = new[]
            {
                MakeSetTower(TowerKind.Battery, "cn"),
                // 11 Kasim 2026: Makineli Tufek (tek hedef, hizli ates) artik
                // kalici asset kutuphanesinden gelir - sabit kaide + donen
                // taret AYRI sprite (tools/blender/library/tower_mg.py).
                MakeSetTower(TowerKind.Ambush, "mg"),
                MakeSetTower(TowerKind.Decoy, "rd"),
                MakeSetTower(TowerKind.Minefield, "tm"),
                MakeSetTower(TowerKind.Beam, "sn"),
            };

            // Atis gorselleri: mermi, namlu cakmasi, carpma. Yalnizca
            // SILAHLI kuleler icin var - yem ve mayin tarlasi ates etmez.
            // Isinin mermisi yok; hat ElectricBeam ile ciziliyor.
            art.fx = new[]
            {
                MakeFx(TowerKind.Battery, "fx_battery_bullet", "fx_battery_muzzle", "fx_battery_impact"),
                MakeFx(TowerKind.Ambush,  "fx_ambush_bullet",  "fx_ambush_muzzle",  "fx_ambush_impact"),
                MakeFx(TowerKind.Beam,    null,                "fx_beam_muzzle",    "fx_beam_impact"),
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

        private static ArtLibrary.FxArt MakeFx(
            TowerKind kind, string bullet, string muzzle, string impact)
        {
            return new ArtLibrary.FxArt
            {
                kind = kind,
                bullet = bullet != null ? LoadSprite(bullet) : null,
                muzzle = LoadSprite(muzzle),
                impact = LoadSprite(impact)
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
        /// TowerVariety setinden (Assets/Frontline/Art/TowerVariety) 3
        /// kademeli kule gorseli yukler. Tint alpha=0: bu sprite'lar Blender'da
        /// zaten renkli render edildi (bkz. generate_tower_variety_set.py),
        /// ArtLibrary'nin genel kurali "alpha 0 ise sprite kendi rengini
        /// kullanir" burada devreye giriyor - eski t_* primitiflerinin aksine
        /// bu sprite'lar TowerPlacer.ColorFor tint'iyle boyanmamali.
        /// </summary>
        private static ArtLibrary.TowerArt MakeVarietyTower(TowerKind kind, string prefix, Color tint)
        {
            var l1 = LoadVarietySprite(prefix, 1);
            return new ArtLibrary.TowerArt
            {
                kind = kind,
                sprite = l1,
                platform = null,
                rotationOffset = 0f,
                levelTurrets = new[]
                {
                    l1,
                    LoadVarietySprite(prefix, 2),
                    LoadVarietySprite(prefix, 3),
                },
                scale = 0f,
                tint = new Color(tint.r, tint.g, tint.b, 0f)
            };
        }

        /// <summary>
        /// Kalici asset kutuphanesinden (tools/blender/library) uretilen
        /// iki parcali kule: {prefix}_base.png sabit kaide, {prefix}_L{n}_turret.png
        /// donen taret. Ikisi AYNI kadrajla render edildigi icin ust uste
        /// tam otururlar. Olcek: tuval 1,4 birim, kaide capi 0,88 birim;
        /// 1,35 kaideyi hucrenin ~%85'ine getirir.
        /// </summary>
        private static ArtLibrary.TowerArt MakeSetTower(TowerKind kind, string prefix)
        {
            var l1 = LoadSetSprite(prefix + "_L1_turret");
            return new ArtLibrary.TowerArt
            {
                kind = kind,
                sprite = l1,
                platform = LoadSetSprite(prefix + "_base"),
                rotationOffset = 0f,
                levelTurrets = new[]
                {
                    l1,
                    LoadSetSprite(prefix + "_L2_turret"),
                    LoadSetSprite(prefix + "_L3_turret"),
                },
                scale = 1.35f,
                tint = new Color(1f, 1f, 1f, 0f)
            };
        }

        private static Sprite LoadSetSprite(string name)
        {
            string path = "Assets/Frontline/Art/TowerSet/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
                Debug.LogWarning("[FrontlineSandbox] TowerSet sprite bulunamadi: " + path);

            return sprite;
        }

        private static Sprite LoadVarietySprite(string prefix, int level)
        {
            string path = "Assets/Frontline/Art/TowerVariety/" + prefix + "_L" + level + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
                Debug.LogWarning("[FrontlineSandbox] TowerVariety sprite bulunamadi: " + path);

            return sprite;
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
