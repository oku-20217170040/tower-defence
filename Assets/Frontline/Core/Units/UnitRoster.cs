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
        /// Kralice payi. 0,05 olcumle secildi: %3'te hafiza silmenin etkisi
        /// zar zor gorunuyor (entropi +0,026), %8'te run %65 kazanmaya
        /// dusuyordu. %5'te entropi +0,032 ve kazanma %95.
        /// </summary>
        public const float QueenShare = 0.05f;

        /// <summary>
        /// Kralice 5. dalgadan itibaren cikar: silinecek bir hafiza
        /// olusmadan hafiza silmenin anlami olmaz.
        /// </summary>
        public const int QueenFirstWave = 5;

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
        /// Kralice 5. dalgadan itibaren, %5 payla. Usya varirsa ordunun
        /// kesif izini siler: ogrendigi her sey gider.
        ///
        /// Uc tur ucgen kurar: izci DAGITIR, asker TOPLAR, kralice
        /// UNUTTURUR. Dalga basina koridor entropisi: izci yok 0,428 ·
        /// izci %25 0,575 · izci + asker 0,525 · uzerine kralice ~0,55.
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
                },
                new UnitMix
                {
                    Profile = UnitProfile.For(UnitType.Queen),
                    Share = QueenShare,
                    FirstWave = QueenFirstWave
                }
            };
        }
    }
}
