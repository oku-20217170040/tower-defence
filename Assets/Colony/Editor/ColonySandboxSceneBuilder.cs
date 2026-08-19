using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Colony.Runtime;

namespace Colony.EditorTools
{
    /// <summary>
    /// Sandbox sahnesini kod ile uretir.
    ///
    /// Sahneyi elle kurmak yerine script ile uretmemizin sebebi: sahne dosyalari
    /// ikili benzeri YAML'dir, diff'i okunmaz ve birlestirmesi acilidir. Kurulum
    /// kod olarak durursa hem surum kontrolunde okunabilir kalir hem de bozuldugunda
    /// tek menu tiklamasiyla yeniden uretilir.
    /// </summary>
    public static class ColonySandboxSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Colony_Sandbox.unity";

        [MenuItem("Colony/Sandbox Sahnesini Uret")]
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
            var worldGo = new GameObject("ColonyWorld");
            var world = worldGo.AddComponent<ColonyWorld>();

            // ── Gorunum ─────────────────────────────────────────────
            var tilesGo = new GameObject("GridTiles");
            tilesGo.AddComponent<SpriteRenderer>();
            var tiles = tilesGo.AddComponent<GridTileRenderer>();

            var overlayGo = new GameObject("PheromoneOverlay");
            overlayGo.AddComponent<SpriteRenderer>();
            var overlay = overlayGo.AddComponent<PheromoneOverlay>();

            // ── Yuva ────────────────────────────────────────────────
            var nestGo = new GameObject("NestHealth");
            var nest = nestGo.AddComponent<NestHealth>();

            // ── Bocekler ────────────────────────────────────────────
            var spawnerGo = new GameObject("BugSpawner");
            var spawner = spawnerGo.AddComponent<BugSpawner>();
            spawner.world = world;
            spawner.nest = nest;

            // ── Run akisi ───────────────────────────────────────────
            var runGo = new GameObject("RunController");
            var run = runGo.AddComponent<RunController>();
            run.world = world;
            run.spawner = spawner;
            run.nest = nest;

            // ── Kule yerlestirme ────────────────────────────────────
            var placerGo = new GameObject("TowerPlacer");
            var placer = placerGo.AddComponent<TowerPlacer>();
            placer.world = world;
            placer.targetCamera = camera;

            // ── Arayuz ──────────────────────────────────────────────
            var hudGo = new GameObject("RunHud");
            var hud = hudGo.AddComponent<RunHud>();
            hud.run = run;
            hud.nest = nest;
            hud.spawner = spawner;
            hud.placer = placer;

            // ── Baglayici ───────────────────────────────────────────
            var binderGo = new GameObject("ColonyViewBinder");
            var binder = binderGo.AddComponent<ColonyViewBinder>();
            binder.world = world;
            binder.tiles = tiles;
            binder.overlay = overlay;
            binder.targetCamera = camera;

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[ColonySandbox] Sahne uretildi: " + ScenePath);
        }
    }
}
