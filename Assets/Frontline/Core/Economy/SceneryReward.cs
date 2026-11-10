namespace Frontline.Core
{
    /// <summary>
    /// Bir ortu (agac/kaya) yikilinca kazanilan besin. Saf C#: hesap
    /// birim testiyle sinanabilsin diye MonoBehaviour'a bagli degil.
    ///
    /// Kullanici geri bildirimi (2 Eylul 2026): "obje patladiktan sonra
    /// buyukluk durumuna gore coin kazansin." Buyukluk azami cana (maxHp)
    /// esdeger alindi - kaya (30) agactan (12) daha dayanikli VE daha
    /// buyuk oldugu icin zaten daha fazla can tasiyor, ayri bir "boyut"
    /// tablosu icad etmeye gerek yok.
    ///
    /// Katsayi 0,5: varsayilan kaya cani 30 -> 15 besin, agac cani 12 -> 6
    /// besin. 15, kule fiyatlarinin (20-40 araligi) yaklasik yarisi -
    /// birkac obje patlatmak bir kuleye yeter ama tek basina ekonomiyi
    /// cozmez.
    /// </summary>
    public static class SceneryReward
    {
        public const float CoinsPerMaxHp = 0.5f;

        /// <summary>En az 1 besin garanti eder: sifir odul oyuncuya "bu
        /// bosunaydi" izlenimi verir, cok kucuk bir ortu bile bir sey
        /// vermeli.</summary>
        public static int CoinsFor(float maxHp)
        {
            if (maxHp <= 0f) return 0;

            int coins = (int)(maxHp * CoinsPerMaxHp + 0.5f);
            return coins < 1 ? 1 : coins;
        }
    }
}
