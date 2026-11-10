using System;

namespace Frontline.Core
{
    /// <summary>
    /// Bir run'dan geriye kalan OZET. Genetik katmanin okudugu tek girdi
    /// budur; simulator de runtime da ayni yapiyi doldurur.
    ///
    /// Duz veridir: karar vermez, yorumlamaz. Yorumu EvolutionEngine yapar.
    /// Boylece "ordu neye gore evrildi" sorusunun cevabi tek yerde durur.
    ///
    /// Saf C#'tir; Unity tipi kullanmaz - denge olcumleri bassiz kosulabilsin
    /// diye (bu projede en cok ise yarayan sey oydu).
    /// </summary>
    public sealed class RunOutcome
    {
        /// <summary>
        /// Kule turune gore KURULAN kule sayisi. Indeks = (int)TowerKind.
        /// Evrimin asil girdisi budur: oyuncunun neyi sevdigi.
        /// </summary>
        public readonly int[] TowerBuilds = new int[EvolutionPressure.TowerKindCount];

        /// <summary>
        /// Koridor basina, o koridordan GECEN birlik sayisi. Uzunlugu
        /// haritanin koridor sayisidir; evrim bunu dogrudan direnc
        /// hesabinda kullanmaz, kayit ve ileriki olcumler icindir.
        /// </summary>
        public int[] CorridorUses = new int[0];

        /// <summary>Koridor basina OLEN birlik sayisi. Oyuncunun oldurme yogunlugu.</summary>
        public int[] CorridorDeaths = new int[0];

        /// <summary>Birlik turune gore olen sayisi. Indeks = (int)UnitType.</summary>
        public readonly int[] UnitDeaths = new int[EvolutionPressure.UnitTypeCount];

        /// <summary>Birlik turune gone USSE VARAN sayisi. Indeks = (int)UnitType.</summary>
        public readonly int[] UnitLeaks = new int[EvolutionPressure.UnitTypeCount];

        /// <summary>Oyuncu run'i kazandi mi (us ayakta kaldi mi).</summary>
        public bool Won;

        /// <summary>Tamamlanan dalga sayisi.</summary>
        public int WavesCompleted;

        public void RegisterTower(TowerKind kind)
        {
            TowerBuilds[(int)kind]++;
        }

        public void RegisterDeath(UnitType type)
        {
            UnitDeaths[(int)type]++;
        }

        public void RegisterLeak(UnitType type)
        {
            UnitLeaks[(int)type]++;
        }

        /// <summary>Kurulan toplam kule sayisi.</summary>
        public int TotalTowers()
        {
            int total = 0;
            for (int i = 0; i < TowerBuilds.Length; i++) total += TowerBuilds[i];
            return total;
        }

        /// <summary>
        /// Bir kule turunun oyuncunun savunmasindaki PAYI [0, 1].
        /// Hic kule yoksa notr pay (1/tur sayisi) dondurulur: hicbir kule
        /// kurulmamis bir run evrime yon veremez.
        /// </summary>
        public float TowerShare(TowerKind kind)
        {
            int total = TotalTowers();
            if (total <= 0) return 1f / EvolutionPressure.TowerKindCount;
            return (float)TowerBuilds[(int)kind] / total;
        }

        /// <summary>Olen toplam birlik.</summary>
        public int TotalDeaths()
        {
            int total = 0;
            for (int i = 0; i < UnitDeaths.Length; i++) total += UnitDeaths[i];
            return total;
        }

        /// <summary>Usse varan toplam birlik (sizinti).</summary>
        public int TotalLeaks()
        {
            int total = 0;
            for (int i = 0; i < UnitLeaks.Length; i++) total += UnitLeaks[i];
            return total;
        }

        /// <summary>
        /// Tek kule turunu spamlayan bir oyuncunun run'i. Test ve olcum
        /// kolayligi icin; evrimin en sert girdisidir.
        /// </summary>
        public static RunOutcome MonoTower(TowerKind kind, int count = 10, bool won = true)
        {
            var outcome = new RunOutcome { Won = won, WavesCompleted = 10 };
            for (int i = 0; i < count; i++) outcome.RegisterTower(kind);
            return outcome;
        }

        /// <summary>Butun kule turlerini esit kullanan run: baski sifir olmali.</summary>
        public static RunOutcome Balanced(int perKind = 2, bool won = true)
        {
            var outcome = new RunOutcome { Won = won, WavesCompleted = 10 };
            for (int k = 0; k < EvolutionPressure.TowerKindCount; k++)
            {
                for (int i = 0; i < perKind; i++) outcome.TowerBuilds[k]++;
            }
            return outcome;
        }

        /// <summary>Hicbir kule kurulmamis run (bos girdi savunmasi).</summary>
        public static RunOutcome Empty(bool won = false)
        {
            return new RunOutcome { Won = won, WavesCompleted = 0 };
        }

        public RunOutcome Clone()
        {
            var copy = new RunOutcome
            {
                Won = Won,
                WavesCompleted = WavesCompleted,
                CorridorUses = (int[])CorridorUses.Clone(),
                CorridorDeaths = (int[])CorridorDeaths.Clone()
            };
            Array.Copy(TowerBuilds, copy.TowerBuilds, TowerBuilds.Length);
            Array.Copy(UnitDeaths, copy.UnitDeaths, UnitDeaths.Length);
            Array.Copy(UnitLeaks, copy.UnitLeaks, UnitLeaks.Length);
            return copy;
        }
    }
}
