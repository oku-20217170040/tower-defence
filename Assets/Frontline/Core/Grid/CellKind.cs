namespace Frontline.Core
{
    /// <summary>Bir grid hucresinin turu.</summary>
    public enum CellKind
    {
        /// <summary>Gecilemez ve uzerine kule kurulamaz.</summary>
        Blocked = 0,

        /// <summary>Birliklerin yurudugu koridor hucresi.</summary>
        Corridor = 1,

        /// <summary>Kule kurulabilen serit. Birlikler buradan gecmez.</summary>
        BuildStrip = 2,

        /// <summary>
        /// Yikilabilir ortu: kaya veya agac. Gecilemez ve uzerine kule
        /// kurulamaz, ama CANI VARDIR. Can bitince hucre BuildStrip'e (cukura)
        /// doner ve oraya mevzi kurulabilir. Bkz. <see cref="SceneryMap"/>.
        /// </summary>
        Scenery = 3
    }
}
