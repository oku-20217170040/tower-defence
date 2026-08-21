namespace Frontline.Core
{
    /// <summary>
    /// Elle tasarlanmis bir harita topolojisi. Prosedurel uretim yalnizca
    /// bu sablonun icindeki dokuyu degistirir, topolojiyi degistirmez.
    /// Boylece her harita okunabilir kalir (spec 9.4).
    /// </summary>
    public struct MapTemplate
    {
        /// <summary>Grid genisligi (dikey tutus: dar eksen).</summary>
        public int Width;

        /// <summary>Grid yuksekligi (dikey tutus: uzun eksen).</summary>
        public int Height;

        /// <summary>Kac dikey koridor uretilecek.</summary>
        public int CorridorCount;

        /// <summary>Her koridorun hucre cinsinden genisligi.</summary>
        public int CorridorWidth;
    }
}
