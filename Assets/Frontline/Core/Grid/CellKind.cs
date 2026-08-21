namespace Frontline.Core
{
    /// <summary>Bir grid hucminefieldin turu.</summary>
    public enum CellKind
    {
        /// <summary>Gecilemez ve uzerine kule kurulamaz.</summary>
        Blocked = 0,

        /// <summary>Birliklerin yurudugu koridor hucresi.</summary>
        Corridor = 1,

        /// <summary>Kule kurulabilen serit. Birlikler buradan gecmez.</summary>
        BuildStrip = 2
    }
}
