namespace Frontline.Core
{
    /// <summary>
    /// Oyunun dalga bilesimi. Simulator ve runtime AYNI listeyi kullanir;
    /// ikisi ayrisirsa olculen sey oynanan sey olmaktan cikar.
    ///
    /// Plan 8 turleri tek tek ekliyor; her tur ancak OLCULDUKTEN sonra
    /// buraya girer.
    /// </summary>
    public static class UnitRoster
    {
        /// <summary>Izci payi. 0,25 olcumle secildi (bkz. UnitProfile.For).</summary>
        public const float ScoutShare = 0.25f;

        /// <summary>
        /// Bugunku bilesim: dortte bir izci, gerisi piyade.
        ///
        /// Izci 1. dalgadan itibaren cikar. Sebebi: taban kesif sapmasi
        /// 0,12'ye dusuruldugu icin kesfi TASIYAN birlik odur; sonradan
        /// girseydi ilk dalgalarda ordu hic alternatif denemezdi.
        /// </summary>
        public static UnitMix[] Default()
        {
            return new[]
            {
                new UnitMix
                {
                    Profile = UnitProfile.For(UnitType.Scout),
                    Share = ScoutShare,
                    FirstWave = 1
                }
            };
        }
    }
}
