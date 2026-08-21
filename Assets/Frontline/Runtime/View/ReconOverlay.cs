using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kesif izi ve yem izlerini ekrana cizer.
    ///
    /// Hucre basina GameObject uretilmez: tum izgara tek bir Texture2D'ye
    /// yazilir ve tek SpriteRenderer ile cizilir. 11x20 icin 220 texel eder,
    /// yani guncelleme maliyeti ihmal edilebilir. Bilinear filtreleme
    /// izlerin yumusak gorunmesini saglar - keskin kare izler kesif izi
    /// hissini bozar.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ReconOverlay : MonoBehaviour
    {
        [Tooltip("Saniyede kac kez yenilensin. Her karede yenilemek gereksizdir.")]
        public float refreshesPerSecond = 12f;

        [Tooltip("Cizim sirasi. Izgara karolarinin ustunde, birliklerin altinda olmali.")]
        public int sortingOrder = 10;

        [Tooltip("Hucre basina texel sayisi. 1 iken bilinear filtre izi bir hucre " +
                 "boyunca yayip koridorun disina tasiriyor ve hangi hucrenin iz " +
                 "tasidigi okunamiyor. 4 ile yayilma ceyrek hucreye iner.")]
        [Range(1, 8)]
        public int texelsPerCell = 4;

        private BattleGrid grid;
        private ReconMap recon;
        private DecoyField decoy;
        private GridSpace space;

        private SpriteRenderer spriteRenderer;
        private Texture2D texture;
        private Color[] pixels;
        private float saturation = 1f;
        private float refreshTimer;
        private bool bound;

        public void Bind(BattleGrid grid, ReconMap recon, DecoyField decoy, GridSpace space)
        {
            this.grid = grid;
            this.recon = recon;
            this.decoy = decoy;
            this.space = space;

            // Cekim doygunlugu RouteCostModel ile ayni olmali; aksi halde
            // ekranda gordugumuz yogunluk birliklerin hissettigiyle ortusmez.
            saturation = recon.Max;

            BuildTexture();
            bound = true;
            Refresh();
        }

        private void BuildTexture()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            int texWidth = grid.Width * texelsPerCell;
            int texHeight = grid.Height * texelsPerCell;

            texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "ReconOverlay"
            };

            pixels = new Color[texWidth * texHeight];

            // pixelsPerUnit = texelsPerCell / CellSize => sprite tam olarak izgara boyutunu kaplar.
            var rect = new Rect(0f, 0f, texWidth, texHeight);
            spriteRenderer.sprite = Sprite.Create(
                texture, rect, new Vector2(0.5f, 0.5f), texelsPerCell / space.CellSize);
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

            int texWidth = grid.Width * texelsPerCell;

            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                float p = recon.Get(cell) / saturation;
                float l = decoy.Get(cell) / saturation;
                Color color = ReconColorRamp.Evaluate(p, l);

                int baseX = grid.X(cell) * texelsPerCell;
                int baseY = grid.Y(cell) * texelsPerCell;

                for (int ty = 0; ty < texelsPerCell; ty++)
                {
                    int row = (baseY + ty) * texWidth + baseX;
                    for (int tx = 0; tx < texelsPerCell; tx++)
                        pixels[row + tx] = color;
                }
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
