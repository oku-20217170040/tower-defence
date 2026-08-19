using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Dunya ile gorunum katmanlarini birbirine baglar ve kamerayi izgaraya oturtur.
    ///
    /// Ayri bir sinif olmasinin sebebi: ColonyWorld cizimden hicbir sey bilmemeli,
    /// gorunum katmanlari da harita uretiminden. Bu sinif ikisinin arasindaki
    /// tek bagdir; ilerde farkli bir gorunum denemek istersek yalnizca burasi degisir.
    /// </summary>
    public sealed class ColonyViewBinder : MonoBehaviour
    {
        public ColonyWorld world;
        public GridTileRenderer tiles;
        public PheromoneOverlay overlay;
        public Camera targetCamera;

        [Tooltip("Izgaranin kenarlarinda birakilacak bosluk payi (dunya birimi).")]
        public float cameraPadding = 0.5f;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<ColonyWorld>();
            if (targetCamera == null) targetCamera = Camera.main;

            // ColonyWorld.Awake zaten calismis olabilir; bu yuzden hem event'e
            // abone oluruz hem de mevcut duruma bir kez uygulariz.
            if (world != null)
            {
                world.WorldRebuilt += Apply;
                if (world.Grid != null) Apply();
            }
        }

        private void OnDestroy()
        {
            if (world != null)
                world.WorldRebuilt -= Apply;
        }

        private void Apply()
        {
            if (world == null || world.Grid == null) return;

            if (tiles != null)
                tiles.Build(world.Grid, world.Space);

            if (overlay != null)
                overlay.Bind(world.Grid, world.Pheromone, world.Lure, world.Space);

            FitCamera();
        }

        private void FitCamera()
        {
            if (targetCamera == null) return;

            targetCamera.orthographic = true;
            targetCamera.transform.position = new Vector3(0f, 0f, -10f);

            float aspect = targetCamera.aspect;
            if (aspect <= 0f) aspect = 9f / 16f;

            targetCamera.orthographicSize =
                world.Space.RequiredOrthographicSize(aspect) + cameraPadding;
        }
    }
}
