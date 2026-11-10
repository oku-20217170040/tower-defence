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

            // Kar temasi (3 Eylul gece plani, FAZ 1): ayri Blender dosemesi
            // uretmek yerine mevcut Blender zeminini buz-mavi/beyaza dogru
            // renk kaydiriyoruz - ayni geometri/detay korunur, sadece tema
            // degisir. Default tema bu blogu hic calistirmaz.
            ArtLibrary art = ArtLibrary.Instance;
            if (art != null && art.theme == SceneryTheme.Snow)
            {
                TintTowardSnow(corridorPixels, art.snowCorridorColor);
                TintTowardSnow(buildPixels, art.snowBuildStripColor);
                TintTowardSnow(blockedPixels, art.snowBlockedColor);
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
                        // Ortunun (kaya/agac) ALTINDAKI zemin de kurulum
                        // seridi dokusuyla cizilir: ortu yikilinca hucre zaten
                        // serit olacak ve atlas yeniden uretilmeyecek.
                        case CellKind.BuildStrip:
                        case CellKind.Scenery: src = buildPixels; break;
                        default: src = blockedPixels; break;
                    }

                    texture.SetPixels(x * tile, y * tile, tile, tile, src);
                }
            }

            texture.Apply(false);
        }

        /// <summary>
        /// ArtLibrary.theme == Snow ise kar renklerine duser (3 Eylul gece
        /// plani, FAZ 1 harita temasi). Default tema ya da ArtLibrary yoksa
        /// mevcut davranis DEGISMEDEN kalir.
        /// </summary>
        /// <summary>
        /// Piksel dizisini VERILEN renge dogru karistirir, alpha'yi korur.
        /// Karistirma orani sabit (0.55): dokunun kendi deseni/golgeleri
        /// hala secilebilir kalsin diye tam beyaza boyanmaz - aksi halde
        /// zemin detaysiz duz bir alan gibi durur.
        /// </summary>
        private static void TintTowardSnow(Color[] pixels, Color snowColor)
        {
            const float mix = 0.55f;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                pixels[i] = new Color(
                    Mathf.Lerp(p.r, snowColor.r, mix),
                    Mathf.Lerp(p.g, snowColor.g, mix),
                    Mathf.Lerp(p.b, snowColor.b, mix),
                    p.a);
            }
        }

        private Color ColorFor(CellKind kind)
        {
            ArtLibrary art = ArtLibrary.Instance;
            bool snow = art != null && art.theme == SceneryTheme.Snow;

            if (kind == CellKind.Corridor) return snow ? art.snowCorridorColor : corridorColor;
            if (kind == CellKind.BuildStrip || kind == CellKind.Scenery)
                return snow ? art.snowBuildStripColor : buildStripColor;
            return snow ? art.snowBlockedColor : blockedColor;
        }

        private void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
