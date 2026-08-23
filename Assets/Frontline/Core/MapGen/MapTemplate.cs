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

        /// <summary>
        /// Koridorlari birbirine baglayan YATAY gecit sayisi. 0 iken koridorlar
        /// birbirinden bagimsiz dikey borulardir: birlik girise adim attigi anda
        /// yolunu secmis olur, yani aslinda hic secmez. Her gecit yolun
        /// ortasinda gercek bir catallanma acar; oyuncunun tehdit ve yem ile
        /// yaptigi yonlendirme ancak boyle bir karar haline gelir.
        /// </summary>
        public int RungCount;

        /// <summary>
        /// Koridor disi hucrelerin yuzde kaci yikilabilir ortu (kaya/agac) olur.
        /// Ortu bastan engeldir; vurulup yikilinca cukur birakir ve o hucre
        /// kule kurulabilir hale gelir (bkz. <see cref="SceneryMap"/>).
        /// </summary>
        public int SceneryPercent;
    }
}
