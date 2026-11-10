using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Kalici (run'lar arasi) kule seviye yukseltmesi - referans oyunun
    /// ana menu "GUNCELLE" ekrani (3 Eylul 2026 gece plani, FAZ 3).
    ///
    /// Bunu, oyun-ICI gecici yukseltmeden (Emplacement.Upgrade, TrySpend
    /// EconomyRunner uzerinden, run bitince sifirlanir) TAMAMEN AYRI bir
    /// sistem olarak dusun: burada satin alinan seviye MetaState'te
    /// KALICI olarak saklanir, her YENI run'da o kule dogrudan o seviyede
    /// KURULUR (bkz. MetaState.PermanentLevelOf, TowerPlacer okur).
    ///
    /// Fiyatlar YENI bir para birimi/olcek icat ETMEDEN, mevcut in-run
    /// kurulum fiyatlarindan (EconomyPlanner.Costs.Default / EconomyRunner,
    /// 3 Eylul gece ADIM 3'te x5 olcege cekildi: Tufek 100, Topcu 200,
    /// Sahte 130, Mayin 130, Isin 170) TURETILIR - ayni oranlar, biyokutle
    /// biriminde. Kalici bir seviye, TEK bir run'in kurulum fiyatindan
    /// PAHALI olmali (yoksa "kalici" hicbir sey ifade etmez); bu yuzden
    /// carpan uygulanir, ham deger kopyalanmaz.
    /// </summary>
    public static class PermanentUpgradeCatalog
    {
        /// <summary>
        /// Bir kulenin normal (1. seviye) kurulum fiyati - EconomyPlanner.
        /// Costs.Default() ile BIREBIR ayni (tek dogru kaynagi ikiye
        /// bolmemek icin buraya kopyalanir; degisirse iki dosya birden
        /// guncellenmeli, testler bunu yakalar).
        /// </summary>
        private static readonly Dictionary<TowerKind, int> BaseCost = new Dictionary<TowerKind, int>
        {
            { TowerKind.Battery, 100 },
            { TowerKind.Ambush, 200 },
            { TowerKind.Decoy, 130 },
            { TowerKind.Minefield, 130 },
            { TowerKind.Beam, 170 },
        };

        /// <summary>
        /// Kalici yukseltmenin taban fiyati, in-run kurulum fiyatinin kac
        /// kati. Neden 2: bir run'da o kuleyi bir kez kurup bir kez de
        /// oyun-ici yukseltmenin toplam maliyeti kabaca budur (Emplacement.
        /// UpgradeCost: base*1.5^0 = base ilk yukseltme icin) - yani kalici
        /// yukseltme "bir run'lik yatirimi tekrar tekrar odemek yerine bir
        /// kez kalici odemek" hissini verir.
        /// </summary>
        private const int LevelUpMultiplier = 2;

        /// <summary>
        /// En yuksek satin alinabilir kalici kademe sayisi. Emplacement.
        /// MaxLevel=3 (1,2,3) oldugu icin en fazla 2 kalici yukseltme
        /// olabilir (1->2, 2->3).
        /// </summary>
        public const int MaxPermanentSteps = 2;

        /// <summary>
        /// Verilen kule icin, verilen kalici kademeden bir sonrakine
        /// gecis fiyati. fromStep 0 ise (henuz hic kalici yukseltme
        /// alinmamis -> 1 alinacak) taban fiyat doner; fromStep 1 ise
        /// (1 alinmis -> 2 alinacak) taban fiyatin 2 kati doner (ikinci
        /// kademe daha degerli, dogrusal artis - IntelUpgradeCost ile
        /// ayni desen). Tavandaysa ya da bilinmeyen kule ise -1 doner.
        /// </summary>
        public static int CostFor(TowerKind kind, int fromStep)
        {
            if (fromStep < 0 || fromStep >= MaxPermanentSteps) return -1;

            int baseCost;
            if (!BaseCost.TryGetValue(kind, out baseCost)) return -1;

            return baseCost * LevelUpMultiplier * (fromStep + 1);
        }

        /// <summary>Bu kule kalici yukseltme katalogunda var mi.</summary>
        public static bool IsKnown(TowerKind kind)
        {
            return BaseCost.ContainsKey(kind);
        }

        /// <summary>Katalogdaki tum kuleler (UI listeleme icin).</summary>
        public static IEnumerable<TowerKind> AllTowers
        {
            get { return BaseCost.Keys; }
        }
    }
}
