namespace Frontline.Core.UI
{
    /// <summary>
    /// Piksel dikdortgeni. UnityEngine.Rect'in saf C# karsiligi -
    /// Frontline.Core'a Unity tipi sizmasin diye burada tanimlandi.
    /// Kokeni SOL ALT kosedir (Unity ekran koordinatlariyla ayni).
    /// </summary>
    public struct UiRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public UiRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float Left { get { return X; } }
        public float Right { get { return X + Width; } }
        public float Bottom { get { return Y; } }
        public float Top { get { return Y + Height; } }

        public bool Overlaps(UiRect other)
        {
            // Kenar temasi cakisma sayilmaz: bantlar birbirine YAPISIK durur.
            return Left < other.Right && other.Left < Right &&
                   Bottom < other.Top && other.Bottom < Top;
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + " " + Width + "x" + Height + ")";
        }
    }

    /// <summary>
    /// Dikey run arayuzunun uc bandi (spec 3.8):
    ///
    ///   UST BANT  - ince. Us cani, besin, dalga, kalan dusman.
    ///   HARITA    - ortada. Hicbir sey kapatmaz.
    ///   ALT BANT  - kalin. Kule secimi, hiz, duraklat.
    ///               "Ekranin alt bandi ... bas parmagin dogal erisim alanidir."
    ///
    /// Bantlar GUVENLI ALANIN icine yerlesir; cerceve disinda kalan
    /// (centik / gezinme cubugu) piksellere hicbir sey konmaz. Redmi Note 14
    /// Pro'nun centigi altinda kalan bir ust bant okunamaz olurdu.
    ///
    /// Saf C#: bu dosya test edilebilir olsun diye Unity tipi kullanmaz.
    /// </summary>
    public static class SafeAreaLayout
    {
        /// <summary>Ust bandin varsayilan orani (guvenli alan yuksekliginin).</summary>
        public const float DefaultTopFraction = 0.09f;

        /// <summary>
        /// Alt bandin varsayilan orani. Ustten kalin: bes kule dugmesi +
        /// fiyat etiketi + hiz/duraklat oraya sigacak ve dokunma hedefleri
        /// parmak boyunda (>= ~9 mm) olmali.
        /// </summary>
        public const float DefaultBottomFraction = 0.22f;

        /// <summary>
        /// Ucunun toplami. Ust bant + harita + alt bant = guvenli alan.
        /// Oranlar disaridan verilir; sabit degildir.
        /// </summary>
        public struct Bands
        {
            public UiRect Top;
            public UiRect Map;
            public UiRect Bottom;
        }

        /// <summary>
        /// Guvenli alani uce boler.
        /// </summary>
        /// <param name="safeArea">Ekranin guvenli dikdortgeni (piksel).</param>
        /// <param name="topFraction">Ust bandin guvenli yukseklige orani.</param>
        /// <param name="bottomFraction">Alt bandin guvenli yukseklige orani.</param>
        public static Bands Split(UiRect safeArea, float topFraction, float bottomFraction)
        {
            float top = Clamp01(topFraction);
            float bottom = Clamp01(bottomFraction);

            // Iki bant birlikte ekrani yerse harita yok olur ve oyun
            // oynanamaz hale gelir. Toplami 0,9'da kes ve orani koru.
            const float maxBands = 0.9f;
            float sum = top + bottom;
            if (sum > maxBands && sum > 0f)
            {
                float scale = maxBands / sum;
                top *= scale;
                bottom *= scale;
            }

            float h = safeArea.Height < 0f ? 0f : safeArea.Height;
            float topHeight = h * top;
            float bottomHeight = h * bottom;
            float mapHeight = h - topHeight - bottomHeight;
            if (mapHeight < 0f) mapHeight = 0f;

            Bands bands;
            bands.Bottom = new UiRect(safeArea.X, safeArea.Y, safeArea.Width, bottomHeight);
            bands.Map = new UiRect(safeArea.X, safeArea.Y + bottomHeight, safeArea.Width, mapHeight);
            bands.Top = new UiRect(safeArea.X, safeArea.Y + bottomHeight + mapHeight,
                                   safeArea.Width, topHeight);
            return bands;
        }

        /// <summary>Varsayilan oranlarla boler.</summary>
        public static Bands Split(UiRect safeArea)
        {
            return Split(safeArea, DefaultTopFraction, DefaultBottomFraction);
        }

        /// <summary>
        /// Guvenli alanin ekranin USTUNDE biraktigi bosluk (centik yuksekligi).
        /// Sifirdan buyukse ust bant o kadar asagi inmis demektir.
        /// </summary>
        public static float TopInset(UiRect screen, UiRect safeArea)
        {
            float inset = screen.Top - safeArea.Top;
            return inset < 0f ? 0f : inset;
        }

        /// <summary>Guvenli alanin ALTTA biraktigi bosluk (gezinme cubugu).</summary>
        public static float BottomInset(UiRect screen, UiRect safeArea)
        {
            float inset = safeArea.Bottom - screen.Bottom;
            return inset < 0f ? 0f : inset;
        }

        /// <summary>Ekran dikey mi? Dikey tutus spec 3.8'in geregidir.</summary>
        public static bool IsPortrait(UiRect screen)
        {
            return screen.Height >= screen.Width;
        }

        /// <summary>
        /// Alt bandi n esit sutuna boler (kule dugmeleri). Aralarinda ve
        /// kenarlarda <paramref name="gap"/> kadar bosluk birakir.
        /// </summary>
        public static UiRect[] SplitColumns(UiRect band, int count, float gap)
        {
            if (count <= 0) return new UiRect[0];
            if (gap < 0f) gap = 0f;

            float usable = band.Width - gap * (count + 1);
            if (usable < 0f)
            {
                gap = 0f;
                usable = band.Width;
            }

            float cell = usable / count;
            var result = new UiRect[count];
            for (int i = 0; i < count; i++)
            {
                float x = band.X + gap + i * (cell + gap);
                result[i] = new UiRect(x, band.Y, cell, band.Height);
            }

            return result;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
