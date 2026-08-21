using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Izgarayi zemin olarak cizer. ReconOverlay ile ayni teknigi kullanir:
    /// hucre basina GameObject yerine tek doku, tek SpriteRenderer.
    ///
    /// Renkler bilerek sonuk secilmistir. Zemin dikkat cekmemelidir; oyuncunun
    /// bakmasi gereken sey kesif izi izleridir. Zemin fazla kontrastli olursa
    /// izler kaybolur.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GridTileRenderer : MonoBehaviour
    {
        [Tooltip("Birliklerin yurudugu koridor.")]
        public Color corridorColor = new Color(0.16f, 0.15f, 0.13f);

        [Tooltip("Kule kurulabilen serit.")]
        public Color buildStripColor = new Color(0.28f, 0.26f, 0.22f);

        [Tooltip("Gecilemez engel.")]
        public Color blockedColor = new Color(0.09f, 0.10f, 0.09f);

        [Header("Blender dosemeleri (istege bagli)")]
        [Tooltip("Koridor dosemesi. UCU DE atanmazsa eski tek-texel zemine " +
                 "dusulur; boylece asset'ler parca parca gelebilir.")]
        public Texture2D corridorTile;

        public Texture2D buildStripTile;
        public Texture2D blockedTile;

        [Tooltip("Cizim sirasi. Kesif izi katmaninin altinda kalmali.")]
        public int sortingOrder = 0;

        private SpriteRenderer spriteRenderer;
        private Texture2D texture;

        /// <summary>Dunya kurulduktan sonra cagrilir.</summary>
        public void Build(BattleGrid grid, GridSpace space)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (texture != null)
                Destroy(texture);

            bool useTiles = corridorTile != null && buildStripTile != null && blockedTile != null;

            if (useTiles) BuildFromTiles(grid);
            else BuildFlat(grid);

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            float pixelsPerCell = texture.width / (float)grid.Width;

            spriteRenderer.sprite = Sprite.Create(
                texture, rect, new Vector2(0.5f, 0.5f), pixelsPerCell / space.CellSize);
            spriteRenderer.sortingOrder = sortingOrder;

            transform.position = Vector3.zero;
        }

        /// <summary>
        /// Doseme yokken kullanilan eski yol: hucre basina TEK texel.
        /// Zemin katmani oyunun calismasi icin zorunlu olmamali.
        /// </summary>
        private void BuildFlat(BattleGrid grid)
        {
            texture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false)
            {
                // Zemin karolari keskin olmali; bulanik zemin izgarayi okunmaz yapar.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "GridTiles"
            };

            var pixels = new Color[grid.CellCount];
            for (int i = 0; i < grid.CellCount; i++)
                pixels[i] = ColorFor(grid.Get(i));

            texture.SetPixels(pixels);
            texture.Apply(false);
        }

        /// <summary>
        /// Blender dosemeleri tek bir atlasa yazilir. Hucre basina
        /// GameObject uretmenin alternatifi bu: izgara ne kadar buyurse
        /// buyusun TEK SpriteRenderer, TEK cizim cagrisi kalir.
        ///
        /// Dosemelerin Read/Write'i acik olmali; kapaliysa GetPixels
        /// patlar ve sessizce duz zemine dusulur.
        /// </summary>
        private void BuildFromTiles(BattleGrid grid)
        {
            int tile = corridorTile.width;

            if (buildStripTile.width != tile || blockedTile.width != tile)
            {
                Debug.LogWarning("[GridTileRenderer] Dosemeler ayni boyutta degil; " +
                                 "duz zemine dusuldu.");
                BuildFlat(grid);
                return;
            }

            Color[] corridorPixels, buildPixels, blockedPixels;
            try
            {
                corridorPixels = corridorTile.GetPixels();
                buildPixels = buildStripTile.GetPixels();
                blockedPixels = blockedTile.GetPixels();
            }
            catch (UnityException)
            {
                Debug.LogWarning("[GridTileRenderer] Doseme dokularinda Read/Write " +
                                 "kapali; duz zemine dusuldu.");
                BuildFlat(grid);
                return;
            }

            texture = new Texture2D(grid.Width * tile, grid.Height * tile,
                                    TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "GridTileAtlas"
            };

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Color[] src;
                    switch (grid.Get(grid.Index(x, y)))
                    {
                        case CellKind.Corridor: src = corridorPixels; break;
                        case CellKind.BuildStrip: src = buildPixels; break;
                        default: src = blockedPixels; break;
                    }

                    texture.SetPixels(x * tile, y * tile, tile, tile, src);
                }
            }

            texture.Apply(false);
        }

        private Color ColorFor(CellKind kind)
        {
            if (kind == CellKind.Corridor) return corridorColor;
            if (kind == CellKind.BuildStrip) return buildStripColor;
            return blockedColor;
        }

        private void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
