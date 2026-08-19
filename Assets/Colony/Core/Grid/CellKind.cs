namespace Colony.Core
{
    /// <summary>Bir grid hucresinin turu.</summary>
    public enum CellKind
    {
        /// <summary>Gecilemez ve uzerine kule kurulamaz.</summary>
        Blocked = 0,

        /// <summary>Boceklerin yurudugu koridor hucresi.</summary>
        Corridor = 1,

        /// <summary>Kule kurulabilen serit. Bocekler buradan gecmez.</summary>
        BuildStrip = 2
    }
}
