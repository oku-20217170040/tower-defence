using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Neyin kaca acildigi (spec 6). Saf veridir; MetaState bu katalogu
    /// kullanarak harcama yapar.
    /// </summary>
    public static class UnlockCatalog
    {
        /// <summary>
        /// Kule kilit sistemi kaldirildi (3 Eylul 2026 gece plani, FAZ 0):
        /// oyuncu ilk run'dan itibaren TUM kulelere sahiptir. Onceden
        /// Decoy/Minefield/Beam biyokutleyle satin aliniyordu; bu artik
        /// gecerli degil, hepsi baslangic kulesi sayilir.
        /// </summary>
        public static readonly TowerKind[] StartingTowers =
        {
            TowerKind.Battery,
            TowerKind.Ambush,
            TowerKind.Decoy,
            TowerKind.Minefield,
            TowerKind.Beam
        };

        /// <summary>
        /// Kilit sistemi kaldirildigi icin satin alinabilir kule kalmadi -
        /// bu sozluk artik hep bostur, CostOf tum kuleler icin 0 doner.
        /// </summary>
        private static readonly Dictionary<TowerKind, int> Costs =
            new Dictionary<TowerKind, int>();

        /// <summary>Baslangicta acik mi.</summary>
        public static bool IsStartingTower(TowerKind kind)
        {
            for (int i = 0; i < StartingTowers.Length; i++)
                if (StartingTowers[i] == kind) return true;

            return false;
        }

        /// <summary>
        /// Bu kulenin acilma bedeli. Baslangic kuleleri 0 doner.
        /// Katalogda olmayan bir kule icin -1 doner (satin alinamaz).
        /// </summary>
        public static int CostOf(TowerKind kind)
        {
            if (IsStartingTower(kind)) return 0;

            int cost;
            if (Costs.TryGetValue(kind, out cost)) return cost;

            return -1;
        }

        /// <summary>Satin alinabilir (kilitli baslayan) kuleler.</summary>
        public static IEnumerable<TowerKind> PurchasableTowers
        {
            get { return Costs.Keys; }
        }

        /// <summary>
        /// Istihbarat seviyesinin tavani (spec 6). 3 basamak: kaba his,
        /// okunabilir iz, tam hafiza. Daha fazlasi anlamli bir gorsel fark
        /// uretmez; daha azi ise "yukseltme" hissi vermez.
        /// </summary>
        public const int MaxIntelLevel = 3;

        /// <summary>
        /// Istihbarat yukseltmesinin taban fiyati.
        ///
        /// Neden 80: bir kazanan run'in yarisi. Seviye 0'dan 1'e gecis
        /// ilk kule kilidinden (120) UCUZ olmali, cunku ordunun hafizasini
        /// gorebilmek kandirmayi ogrenmenin on kosuludur; oyuncu once
        /// gormeli, sonra arac toplamali.
        /// </summary>
        public const int IntelBaseCost = 80;

        /// <summary>
        /// Verili seviyeden bir sonrakine gecis bedeli: 80, 160, 240.
        /// Dogrusal artis secildi (ussel degil): meta ilerleme ilk kesitte
        /// iskelet halindedir (spec 6) ve ussel egri oyuncuyu son
        /// basamakta duvara toslatirdi.
        /// Tavandaki seviye icin -1 doner.
        /// </summary>
        public static int IntelUpgradeCost(int currentLevel)
        {
            if (currentLevel < 0) return -1;
            if (currentLevel >= MaxIntelLevel) return -1;

            return IntelBaseCost * (currentLevel + 1);
        }
    }
}
