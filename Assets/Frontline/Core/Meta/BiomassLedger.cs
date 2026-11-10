using System;

namespace Frontline.Core
{
    /// <summary>
    /// Bir run'in sonucunu kalici para birimine cevirir: BIYOKUTLE (spec 6).
    ///
    /// Neden var: roguelike'ta kaybedilen run oyuncuyu birakmamali. Kaybeden
    /// run da biyokutle vermeli, ama kazanan run belirgin daha fazla vermeli -
    /// yoksa "kazanmak" bir hedef olmaktan cikar ve oyuncu bilerek erken
    /// olmeyi ogrenir.
    ///
    /// Saf hesaptir, MonoBehaviour degildir; birim testiyle sabitlenir.
    /// </summary>
    public static class BiomassLedger
    {
        /// <summary>
        /// Tamamlanan dalga basina biyokutle.
        ///
        /// Neden 5: run 12-15 dalgadir (spec 2.1), yani tam bir run'in dalga
        /// geliri 60-75 bandindadir. Bu, en ucuz kilidin (spec: Sahte hedef,
        /// <see cref="UnlockCatalog"/> icinde 120) yaklasik yarisidir - yani
        /// bir kilit "tam iki run" degil, "bir iyi run + biraz" eder.
        /// Dalga geliri ilerlemenin ANA damari olmalidir cunku oyuncunun
        /// dogrudan kontrol edebildigi tek sey ne kadar dayandigidir.
        /// </summary>
        public const int PerWaveCompleted = 5;

        /// <summary>
        /// Kac oldurme basina 1 biyokutle verilir.
        ///
        /// Neden 10: olcum (spec 3.5) kazanan bir run'da ~255, dalga 10'da
        /// kaybedilen bir run'da ~105 oldurme gosterdi. 1/1 oranda oldurme
        /// geliri dalga gelirinin 3-4 katina cikar ve tek degisken haline
        /// gelirdi; 1/10'da kazanan run'a ~25, kaybedene ~10 biyokutle
        /// ekler - yani oldurme bir CARPAN degil, bir ince ayar kalir.
        /// </summary>
        public const int KillsPerBiomass = 10;

        /// <summary>
        /// Run kazanildiginda verilen ek biyokutle.
        ///
        /// Neden 60: 15 dalgalik tam bir run'in dalga geliri de 75'tir.
        /// Bonus onunla ayni buyuklukte olunca kazanmak, gelirin kabaca
        /// yarisini tasir - kazanan run kaybedenin ~3 katini verir
        /// (bkz. testler). Bonus cok kucuk olsaydi kazanmak anlamsizlasir,
        /// cok buyuk olsaydi yeni oyuncu (kazanamayan) hicbir sey acamaz
        /// ve meta ilerleme onun icin var olmazdi.
        /// </summary>
        public const int WinBonus = 60;

        /// <summary>
        /// Run sonucundan kazanilan biyokutleyi hesaplar.
        /// Negatif girdiler sifira kirpilir; hesap asla negatif donmez.
        /// </summary>
        /// <param name="wavesCompleted">Tamamlanan dalga sayisi.</param>
        /// <param name="kills">Run boyunca oldurulen birlik.</param>
        /// <param name="won">Run kazanildi mi.</param>
        public static int Earned(int wavesCompleted, int kills, bool won)
        {
            int waves = wavesCompleted < 0 ? 0 : wavesCompleted;
            int killed = kills < 0 ? 0 : kills;

            int total = waves * PerWaveCompleted;
            total += killed / KillsPerBiomass;
            if (won) total += WinBonus;

            return total;
        }

        /// <summary>
        /// Hicbir dalga tamamlanmadan kaybedilen run bile bos donmemeli.
        /// Bu taban, oyuncunun "hicbir sey kazanmadim" hissiyle kalkmasini
        /// engeller. Earned() bunu uygulayarak dondurur.
        /// </summary>
        public const int ConsolationFloor = 3;

        /// <summary>
        /// <see cref="Earned"/> uzerine taban uygular. Run bitisinde
        /// cagrilmasi gereken metot budur.
        /// </summary>
        public static int EarnedWithFloor(int wavesCompleted, int kills, bool won)
        {
            int earned = Earned(wavesCompleted, kills, won);
            return earned < ConsolationFloor ? ConsolationFloor : earned;
        }
    }
}
