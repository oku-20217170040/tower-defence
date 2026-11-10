using System.Globalization;

namespace Frontline.Core.UI
{
    /// <summary>
    /// Arayuz metinlerinin biciminden SORUMLU tek yer. Saf C#: hicbir
    /// Unity tipi kullanmaz, bu yuzden tamami EditMode'da test edilir.
    ///
    /// Neden ayri bir sinif: dikey ekranda ust bant dardir. "Besin: 1250"
    /// yerine "1,2K" yazmak bir suslemedir degil, YER acar. Yer acmayan
    /// her karakter haritayi kapatir.
    ///
    /// Sayi ayraci olarak Turkce virgul kullanilir (1,2K), ama kisaltma
    /// harfleri evrenseldir (K/M) - "1,2B" oyuncuya bin mi milyar mi
    /// oldugunu sordururdu.
    /// </summary>
    public static class HudFormat
    {
        /// <summary>Alinamayan seyi isaretleyen on ek.</summary>
        public const string LockedPrefix = "\u2717 ";

        /// <summary>
        /// Kompakt sayi. 0-999 aynen; 1000'den sonra K, 1.000.000'dan
        /// sonra M. Negatif sayi isaretini korur.
        /// </summary>
        public static string Compact(int value)
        {
            if (value < 0) return "-" + Compact(-value);

            if (value < 1000) return value.ToString(CultureInfo.InvariantCulture);

            if (value < 1000000)
                return Short(value / 1000f) + "K";

            return Short(value / 1000000f) + "M";
        }

        /// <summary>"Dalga 3/15". Hazirlik aninda (dalga 0) "Hazirlan".</summary>
        public static string Wave(int current, int total)
        {
            if (current <= 0) return "Hazirlan";
            if (total <= 0) return "Dalga " + current;
            if (current > total) current = total;
            return "Dalga " + current + "/" + total;
        }

        /// <summary>"Kalan 4/12". Negatif kalan sifira kirpilir.</summary>
        public static string Remaining(int remaining, int total)
        {
            if (remaining < 0) remaining = 0;
            if (total < 0) total = 0;
            if (remaining > total) remaining = total;
            return "Kalan " + remaining + "/" + total;
        }

        /// <summary>"26/30" bicimli us cani.</summary>
        public static string Health(int current, int max)
        {
            if (current < 0) current = 0;
            if (max < 0) max = 0;
            if (current > max) current = max;
            return current + "/" + max;
        }

        /// <summary>
        /// Fiyat etiketi. Bedava kule draft odulu olarak gelir ve bunu
        /// "0" diye yazmak oyuncuya odulu gostermez.
        /// </summary>
        public static string Price(int cost)
        {
            if (cost <= 0) return "Bedava";
            return Compact(cost);
        }

        /// <summary>
        /// Alinamayan fiyat isaretlenir. Yalnizca soluklastirma yetmez:
        /// gun isiginda telefonda soluk renk ayirt edilmez, isaret edilir.
        /// </summary>
        public static string Price(int cost, bool affordable)
        {
            string text = Price(cost);
            return affordable ? text : LockedPrefix + text;
        }

        /// <summary>Hiz dugmesi etiketi: "x1", "x2", "x3".</summary>
        public static string Speed(float multiplier)
        {
            if (multiplier <= 0f) return "x1";

            // Tam sayi ise ondalik yazma; "x1,0" gurultudur.
            int whole = (int)(multiplier + 0.001f);
            if (multiplier - whole < 0.01f) return "x" + whole;

            return "x" + Decimal1(multiplier);
        }

        /// <summary>Turkce kule adi. Arayuzun tek dogru kaynagi burasi.</summary>
        public static string TowerName(TowerKind kind)
        {
            switch (kind)
            {
                case TowerKind.Battery: return "Makineli Tufek";
                case TowerKind.Ambush: return "Topcu";
                case TowerKind.Decoy: return "Sahte hedef";
                case TowerKind.Minefield: return "Mayin tarlasi";
                case TowerKind.Beam: return "Isin";
            }

            return kind.ToString();
        }

        /// <summary>Dugmeye sigan kisa ad (tek/iki kelime).</summary>
        public static string TowerShortName(TowerKind kind)
        {
            switch (kind)
            {
                case TowerKind.Battery: return "Mak. Tufek";
                case TowerKind.Ambush: return "Topcu";
                case TowerKind.Decoy: return "Sahte";
                case TowerKind.Minefield: return "Mayin";
                case TowerKind.Beam: return "Isin";
            }

            return kind.ToString();
        }

        /// <summary>
        /// Tek ondalikli, Turkce virgullu sayi. CultureInfo("tr-TR") KULLANILMAZ:
        /// IL2CPP invariant globalization ile derlenirse kultur verisi
        /// bulunmaz ve arayuz calisma aninda patlar. Virgul elle konur.
        /// </summary>
        private static string Decimal1(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        // 1249 -> "1,2"; 12490 -> "12"; gereksiz ",0" yazilmaz.
        private static string Short(float scaled)
        {
            // Asagi yuvarla: 1999 besini "2K" gostermek oyuncuya
            // olmayan parayi vaat eder.
            float truncated = (int)(scaled * 10f) / 10f;

            if (truncated >= 10f || truncated == (int)truncated)
                return ((int)truncated).ToString(CultureInfo.InvariantCulture);

            return Decimal1(truncated);
        }
    }
}
