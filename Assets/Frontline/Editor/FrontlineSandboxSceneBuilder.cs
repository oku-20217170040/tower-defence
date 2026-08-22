using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
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

            // ── Dunya ───────────────────────────────────────────────
            var worldGo = new GameObject("Battlefield");
            var world = worldGo.AddComponent<Battlefield>();

            // ── Gorunum ─────────────────────────────────────────────
            var tilesGo = new GameObject("GridTiles");
            tilesGo.AddComponent<SpriteRenderer>();
            var tiles = tilesGo.AddComponent<GridTileRenderer>();

            var overlayGo = new GameObject("ReconOverlay");
            overlayGo.AddComponent<SpriteRenderer>();
            var overlay = overlayGo.AddComponent<ReconOverlay>();

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
    }
}
