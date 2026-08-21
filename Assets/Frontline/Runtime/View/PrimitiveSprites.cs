using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kod ile uretilen gecici sprite'lar.
    ///
    /// Blender asset'leri Plan 5'in isi. O gune kadar birlikleri ve kuleleri
    /// bir seyle temsil etmemiz gerekiyor; disaridan dosya baglamak yerine
    /// kod ile uretmek hem repoyu temiz tutar hem de asset'ler gelince
    /// tek yerden silinir.
    /// </summary>
    public static class PrimitiveSprites
    {
        private static Sprite circle;

        /// <summary>Beyaz dolu daire. Renk SpriteRenderer.color ile verilir.</summary>
        public static Sprite Circle()
        {
            if (circle != null) return circle;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PrimitiveCircle"
            };

            var pixels = new Color[size * size];
            float radius = size * 0.5f;
            float center = radius - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    // Kenarda bir pikselllik yumusatma; aksi halde daire tirtikli gorunur.
                    float alpha = Mathf.Clamp01(radius - 1f - distance + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);

            circle = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);

            return circle;
        }
    }
}
