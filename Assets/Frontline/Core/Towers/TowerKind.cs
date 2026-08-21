namespace Frontline.Core
{
    /// <summary>
    /// Kule sinifi. Oyunun merkezi ayrimi budur (spec 3.1-3.2).
    ///
    /// Klasik tower defense sezgisi tersine cevrilir: guclu kule dusmani
    /// OLDURMEK istedigin yere degil, dusmani KACIRMAK istedigin yere konur.
    /// Gercek katliami ordunin goremedigi pusu mevzisiler yapar.
    /// </summary>
    public enum TowerKind
    {
        /// <summary>
        /// Tehdit alanina yazar. Yuksek hasar verir ama ordu ondan kacmayi
        /// ogrenir; zamanla onunden kimse gecmez. Bir silah degil, yonlendirme aracidir.
        /// </summary>
        Battery = 0,

        /// <summary>
        /// Tehdit alanina HIC yazmaz. Birlikler onu algilayamaz ve ogrenemez.
        /// Karsiliginda hasari dusuktur. Tuzak koridorunun asil silahi budur.
        /// </summary>
        Ambush = 1,

        /// <summary>
        /// Hasar vermez. Koridora yapay kesif izi basarak suruyu oraya ceker.
        /// Etkisi kalici degildir; her dalga tazelenmesi gerekir.
        /// </summary>
        Decoy = 2,

        /// <summary>
        /// Hasar vermez, tehdit YAZMAZ. Menzilindeki birlikleri yavaslatir;
        /// birlik pusu mevzisilerin menzilinde daha uzun kalir. Tek basina
        /// degersizdir - degeri pusu mevzisiyle eslesmesinden gelir.
        /// </summary>
        Minefield = 3,

        /// <summary>
        /// Delip gecen hat hasari. Yalnizca kendi SUTUNUNDA, menzili
        /// icindeki birliklere vurur - ama oradaki HERKESE ayni anda.
        /// Tehdit yazmaz.
        ///
        /// Roldeki fark: batarya bir dairedeki herkese vurur ve tehdit
        /// yazdigi icin ordu ondan kacar; isin dar bir hatta vurur ve
        /// gorunmez oldugu icin ordu kacmaz. Boylece spec 3.5'in
        /// "korunmayan koridor her zaman bulunur" gercegiyle dogrudan
        /// catisir: ordu tek koridora yigildiginda isin en pahaliya
        /// mal olan sey haline gelir.
        /// </summary>
        Beam = 4
    }
}
