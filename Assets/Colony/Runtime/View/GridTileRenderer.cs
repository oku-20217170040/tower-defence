using UnityEngine;
using Colony.Core;

namespace Colony.Runtime
{
    /// <summary>
    /// Izgarayi zemin olarak cizer. PheromoneOverlay ile ayni teknigi kullanir:
    /// hucre basina GameObject yerine tek doku, tek SpriteRenderer.
    ///
    /// Renkler bilerek sonuk secilmistir. Zemin dikkat cekmemelidir; oyuncunun
    /// bakmasi gereken sey feromon izleridir. Zemin fazla kontrastli olursa
    /// izler kaybolur.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GridTileRenderer : MonoBehaviour
    {
        [Tooltip("Boceklerin yurudugu koridor.")]
        public Color corridorColor = new Color(0.16f, 0.15f, 0.13f);

        [Tooltip("Kule kurulabilen serit.")]
        public Color buildStripColor = new Color(0.28f, 0.26f, 0.22f);

        [Tooltip("Gecilemez engel.")]
        public Color blockedColor = new Color(0.09f, 0.10f, 0.09f);

        [Tooltip("Cizim sirasi. Feromon katmaninin altinda kalmali.")]
        public int sortingOrder = 0;

        private SpriteRenderer spriteRenderer;
        private Texture2D texture;

        /// <summary>Dunya kurulduktan sonra cagrilir.</summary>
        public void Build(ColonyGrid grid, GridSpace space)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (texture != null)
                Destroy(texture);

            texture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false)
            {
                // Zemin karolari keskin olmali; bulanik zemin izgarayi okunmaz yapar.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "GridTiles"
            };

            var pixels = new Color[grid.CellCount];
            for (int i = 0; i < grid.CellCount; i++)
            {
                switch (grid.Get(i))
                {
                    case CellKind.Corridor:
                        pixels[i] = corridorColor;
                        break;
                    case CellKind.BuildStrip:
                        pixels[i] = buildStripColor;
                        break;
                    default:
                        pixels[i] = blockedColor;
                        break;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);

            var rect = new Rect(0f, 0f, grid.Width, grid.Height);
            spriteRenderer.sprite = Sprite.Create(
                texture, rect, new Vector2(0.5f, 0.5f), 1f / space.CellSize);
            spriteRenderer.sortingOrder = sortingOrder;

            transform.position = Vector3.zero;
        }

        private void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
