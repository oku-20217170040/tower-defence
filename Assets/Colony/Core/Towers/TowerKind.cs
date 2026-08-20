namespace Colony.Core
{
    /// <summary>
    /// Kule sinifi. Oyunun merkezi ayrimi budur (spec 3.1-3.2).
    ///
    /// Klasik tower defense sezgisi tersine cevrilir: guclu kule dusmani
    /// OLDURMEK istedigin yere degil, dusmani KACIRMAK istedigin yere konur.
    /// Gercek katliami koloninin goremedigi sessiz kuleler yapar.
    /// </summary>
    public enum TowerKind
    {
        /// <summary>
        /// Tehdit alanina yazar. Yuksek hasar verir ama koloni ondan kacmayi
        /// ogrenir; zamanla onunden kimse gecmez. Bir silah degil, yonlendirme aracidir.
        /// </summary>
        Noisy = 0,

        /// <summary>
        /// Tehdit alanina HIC yazmaz. Bocekler onu algilayamaz ve ogrenemez.
        /// Karsiliginda hasari dusuktur. Tuzak koridorunun asil silahi budur.
        /// </summary>
        Silent = 1,

        /// <summary>
        /// Hasar vermez. Koridora yapay feromon basarak suruyu oraya ceker.
        /// Etkisi kalici degildir; her dalga tazelenmesi gerekir.
        /// </summary>
        Lure = 2,

        /// <summary>
        /// Hasar vermez, tehdit YAZMAZ. Menzilindeki bocekleri yavaslatir;
        /// bocek sessiz kulelerin menzilinde daha uzun kalir. Tek basina
        /// degersizdir - degeri sessiz kuleyle eslesmesinden gelir.
        /// </summary>
        Resin = 3
    }
}
