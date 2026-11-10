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
        private static Sprite square;
        private static Sprite star;

        /// <summary>
        /// Beyaz dolu kare, tam 1 dunya birimi. Atis izleri bunu gerip
        /// cizgiye cevirir (bkz. ShotFlash).
        /// </summary>
        public static Sprite Square()
        {
            if (square != null) return square;

            const int size = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PrimitiveSquare"
            };

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply(false);

            square = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                                   new Vector2(0.5f, 0.5f), size);
            return square;
        }

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

        /// <summary>
        /// Dolu 5-kollu yildiz, kule seviye rozeti icin (3 Eylul 2026,
        /// referans oyundaki yildiz sistemi talebi). Renk SpriteRenderer.color
        /// ile verilir - Blender asset'i gelene kadar bu yeter.
        /// </summary>
        public static Sprite Star()
        {
            if (star != null) return star;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PrimitiveStar"
            };

            // 5 kollu yildiz cokgeni: dis/ic yaricap donusumlu 10 kose.
            const int points = 5;
            var verts = new Vector2[points * 2];
            float outerR = size * 0.48f;
            float innerR = outerR * 0.42f;
            float center = size * 0.5f - 0.5f;
            for (int i = 0; i < points * 2; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / points;
                float r = (i % 2 == 0) ? outerR : innerR;
                verts[i] = new Vector2(center + Mathf.Cos(angle) * r,
                                        center + Mathf.Sin(angle) * r);
            }

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = PointInPolygon(verts, x, y);
                    pixels[y * size + x] = inside ? Color.white : new Color(1f, 1f, 1f, 0f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);

            star = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                                 new Vector2(0.5f, 0.5f), size);
            return star;
        }

        private static bool PointInPolygon(Vector2[] poly, float px, float py)
        {
            bool inside = false;
            int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 vi = poly[i];
                Vector2 vj = poly[j];
                if ((vi.y > py) != (vj.y > py) &&
                    px < (vj.x - vi.x) * (py - vi.y) / (vj.y - vi.y) + vi.x)
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }
    }
}
