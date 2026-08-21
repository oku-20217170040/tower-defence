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
        /// Asker payi. 0,20 olcumle secildi: civileme %20'de doyuyor
        /// (lift %20'de de %30'da da +0,041) ama zorluk paya cok duyarli
        /// (sizinti %10'da 6,4 · %20'de 12,5 · %30'da 20,7).
        /// </summary>
        public const float SoldierShare = 0.20f;

        /// <summary>Asker ilk kez 3. dalgada cikar; ilk iki dalga ogrenme icindir.</summary>
        public const int SoldierFirstWave = 3;

        /// <summary>
        /// Bugunku bilesim: dortte bir izci, beste bir asker, gerisi piyade.
        ///
        /// Izci 1. dalgadan itibaren cikar. Sebebi: taban kesif sapmasi
        /// 0,12'ye dusuruldugu icin kesfi TASIYAN birlik odur; sonradan
        /// girseydi ilk dalgalarda ordu hic alternatif denemezdi.
        ///
        /// Asker 3. dalgadan itibaren cikar: ilk iki dalga ordunun
        /// ogrenmesi icindir, civilenecek bir tercih olusmadan asker
        /// gondermenin anlami olmaz.
        ///
        /// Ikisi birbirinin TERSI yonde calisir - izci dagitir, asker
        /// toplar. Dalga basina koridor entropisi: izci yok 0,428 ·
        /// izci %25 0,575 · izci %25 + asker %20 0,525.
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
                },
                new UnitMix
                {
                    Profile = UnitProfile.For(UnitType.Soldier),
                    Share = SoldierShare,
                    FirstWave = SoldierFirstWave
                }
            };
        }
    }
}
