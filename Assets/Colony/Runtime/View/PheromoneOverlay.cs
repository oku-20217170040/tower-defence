using UnityEngine;
using Colony.Core;

namespace Colony.Runtime
{
    /// <summary>
    /// Feromon ve yem izlerini ekrana cizer.
    ///
    /// Hucre basina GameObject uretilmez: tum izgara tek bir Texture2D'ye
    /// yazilir ve tek SpriteRenderer ile cizilir. 11x20 icin 220 texel eder,
    /// yani guncelleme maliyeti ihmal edilebilir. Bilinear filtreleme
    /// izlerin yumusak gorunmesini saglar - keskin kare izler feromon
    /// hissini bozar.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PheromoneOverlay : MonoBehaviour
    {
        [Tooltip("Saniyede kac kez yenilensin. Her karede yenilemek gereksizdir.")]
        public float refreshesPerSecond = 12f;

        [Tooltip("Cizim sirasi. Izgara karolarinin ustunde, boceklerin altinda olmali.")]
        public int sortingOrder = 10;

        private ColonyGrid grid;
        private PheromoneMap pheromone;
        private LureField lure;
        private GridSpace space;

        private SpriteRenderer spriteRenderer;
        private Texture2D texture;
        private Color[] pixels;
        private float saturation = 1f;
        private float refreshTimer;
        private bool bound;

        public void Bind(ColonyGrid grid, PheromoneMap pheromone, LureField lure, GridSpace space)
        {
            this.grid = grid;
            this.pheromone = pheromone;
            this.lure = lure;
            this.space = space;

            // Cekim doygunlugu RouteCostModel ile ayni olmali; aksi halde
            // ekranda gordugumuz yogunluk boceklerin hissettigiyle ortusmez.
            saturation = pheromone.Max;

            BuildTexture();
            bound = true;
            Refresh();
        }

        private void BuildTexture()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            texture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PheromoneOverlay"
            };

            pixels = new Color[grid.Width * grid.Height];

            // pixelsPerUnit = 1 / CellSize => sprite tam olarak izgara boyutunu kaplar.
            var rect = new Rect(0f, 0f, grid.Width, grid.Height);
            spriteRenderer.sprite = Sprite.Create(
                texture, rect, new Vector2(0.5f, 0.5f), 1f / space.CellSize);
            spriteRenderer.sortingOrder = sortingOrder;

            // Izgara dunya orijininde ortalandigi icin overlay de orada durur.
            transform.position = Vector3.zero;
        }

        private void Update()
        {
            if (!bound) return;

            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f) return;

            refreshTimer = 1f / Mathf.Max(1f, refreshesPerSecond);
            Refresh();
        }

        /// <summary>Dokuyu alanlarin guncel degerlerinden yeniden doldurur.</summary>
        public void Refresh()
        {
            if (!bound) return;

            for (int i = 0; i < pixels.Length; i++)
            {
                float p = pheromone.Get(i) / saturation;
                float l = lure.Get(i) / saturation;
                pixels[i] = PheromoneColorRamp.Evaluate(p, l);
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }

        private void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
