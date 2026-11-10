using System;

namespace Frontline.Core
{
    /// <summary>
    /// Genetik katman: bir run'in ozetine (RunOutcome) bakip ordunun
    /// genomunu bir SONRAKI run icin mutasyona ugratir.
    ///
    /// Spec bolum 4: "ordu run'lar boyunca oyuncuya karsi evrilir,
    /// oyuncunun favori stratejisi zamanla asinir" - NEMESIS islevi.
    /// Ama spec bolum 6 da "beceri tavani korunmalidir" diyor. Ikisi
    /// birlikte tek bir kurala cikiyor:
    ///
    ///     ORDU GUCLENMEZ, YALNIZCA KAYAR.
    ///
    /// Oyuncu neyi cok kullandiysa ona karsi direnc ARTAR; ayni miktarda
    /// baska direncler AZALIR. Toplam guc butcesi (ArmyGenome.Normalize)
    /// sabittir. Bu, oyuncuyu CEZALANDIRMAZ, CESITLENDIRIR: favori kule
    /// asinir, terk edilen kule tekrar guclenir.
    ///
    /// TAMAMEN DETERMINISTIK. Rastgelelik yalnizca disaridan verilen
    /// seed'li System.Random'dan gelir; UnityEngine.Random YOKTUR. Bu
    /// projede butun denge olcumleri seed'li ve tekrar uretilebilir olmak
    /// zorunda - aksi halde bassiz simulator taramasi (RunSimulator)
    /// anlamini kaybeder.
    /// </summary>
    public static class EvolutionEngine
    {
        /// <summary>
        /// Bir run'in sonucuna gore genomun BIR SONRAKI halini uretir.
        /// Girdi genomu DEGISTIRILMEZ; yeni bir kopya doner.
        ///
        /// Ayni (genome, outcome, seed) ucluse her zaman birebir ayni
        /// sonucu verir.
        /// </summary>
        public static ArmyGenome Evolve(ArmyGenome genome, RunOutcome outcome, int seed)
        {
            if (genome == null) throw new ArgumentNullException("genome");
            if (outcome == null) throw new ArgumentNullException("outcome");

            ArmyGenome next = genome.Clone();
            var rng = new Random(seed);

            EvolveResistance(next, outcome, rng);
            EvolveShares(next, outcome, rng);

            next.Generation = genome.Generation + 1;
            next.Normalize();

            ClampStep(genome, next);
            next.Normalize();

            return next;
        }

        /// <summary>
        /// Bir run dizisini sirayla isler. Seed her run icin turetilir
        /// (seed + run indeksi), yani dizinin tamami tek seed'den tekrar
        /// uretilebilir.
        /// </summary>
        public static ArmyGenome EvolveSeries(ArmyGenome genome, RunOutcome[] outcomes, int seed)
        {
            ArmyGenome current = genome;
            for (int i = 0; i < outcomes.Length; i++)
            {
                current = Evolve(current, outcomes[i], seed + i);
            }
            return current;
        }

        // ---- Direnc evrimi --------------------------------------------------

        private static void EvolveResistance(ArmyGenome genome, RunOutcome outcome, Random rng)
        {
            int count = EvolutionPressure.TowerKindCount;
            float neutralShare = 1f / count;

            // Kayip run'da baski kisilir. Sonme kisilmaz: kaybedilen
            // run'lar orduyu tabana dogru geri getirir, boylece ust uste
            // kaybeden oyuncunun karsisinda ordu YUMUSAR.
            float pressureScale = outcome.Won ? 1f : EvolutionPressure.LossPressureScale;

            var delta = new float[count];
            for (int i = 0; i < count; i++)
            {
                float share = outcome.TowerShare((TowerKind)i);

                // 1) Ogrenme: baskin kuleye karsi direnc artar. Paylarin
                //    toplami 1 oldugundan (share - 1/N) toplami SIFIRDIR -
                //    yani bu terim butceyi kendiliginden korur.
                float learn = EvolutionPressure.LearnRate * (share - neutralShare) * pressureScale;

                // 2) Sonme: direnc tabana geri cekilir. Butce korunuyorsa
                //    (Baseline - r) toplami da sifirdir, yani bu terim de
                //    butceyi bozmaz. Kullanilmayan stratejiye karsi direnci
                //    soduren mekanizma budur.
                float decay = EvolutionPressure.DecayRate
                              * (EvolutionPressure.BaselineResistance - genome.Resistance[i]);

                delta[i] = learn + decay;
            }

            AddZeroSumNoise(delta, rng, EvolutionPressure.MutationAmplitude);

            for (int i = 0; i < count; i++)
            {
                genome.Resistance[i] += delta[i];
            }
        }

        // ---- Bilesim evrimi -------------------------------------------------

        /// <summary>
        /// Paylari kaydirir. Yon, oyuncunun run'da ne yaptigina bakar:
        ///
        ///   - Oyuncu KAZANDIYSA ordu cesitlenir: izci payi artar
        ///     (dagitici tur), boylece ordu yeni koridor dener ve
        ///     oyuncunun ezberledigi rota bozulur.
        ///   - Oyuncu KAYBETTIYSE ters yone kayar: ordu sadelesir, oyuncu
        ///     ogrendigini uygulayabilsin.
        ///
        /// Miktarlar direnclerden de kucuktur. Guc butcesi zaten
        /// Normalize() tarafindan korunur, yani "izci artti" demek
        /// "ordu guclendi" demek DEGILDIR: karsiliginda agir turlerin
        /// payi duser.
        /// </summary>
        private static void EvolveShares(ArmyGenome genome, RunOutcome outcome, Random rng)
        {
            float dir = outcome.Won ? 1f : -1f;
            float step = EvolutionPressure.SharePressure * dir;

            genome.Share[(int)UnitType.Scout] += step;
            genome.Share[(int)UnitType.Soldier] -= step * 0.5f;
            genome.Share[(int)UnitType.Queen] -= step * 0.5f;

            var noise = new float[3];
            AddZeroSumNoise(noise, rng, EvolutionPressure.ShareMutationAmplitude);
            genome.Share[(int)UnitType.Scout] += noise[0];
            genome.Share[(int)UnitType.Soldier] += noise[1];
            genome.Share[(int)UnitType.Queen] += noise[2];
        }

        // ---- Yardimcilar ----------------------------------------------------

        /// <summary>
        /// Diziye ORTALAMASI SIFIR olan gurultu ekler. Sifir ortalama sart:
        /// aksi halde mutasyonun kendisi butceyi yavas yavas kaydirir ve
        /// ordu 20-30 run icinde sessizce guclenir (ya da erir).
        /// </summary>
        private static void AddZeroSumNoise(float[] values, Random rng, float amplitude)
        {
            int n = values.Length;
            if (n == 0 || amplitude <= 0f) return;

            var raw = new float[n];
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                raw[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * amplitude;
                sum += raw[i];
            }

            float mean = sum / n;
            for (int i = 0; i < n; i++)
            {
                values[i] += raw[i] - mean;
            }
        }

        /// <summary>
        /// Bir run'da tek bir direncin degisimini sinirlar.
        ///
        /// Gerekcesi mekanik degil TASARIMSAL: ordu bir run'da tanınmaz
        /// hale gelirse oyuncu gecen run'da ogrendigini uygulayamaz ve
        /// evrim "adaptasyon" degil "rastgele zorluk" olarak hissedilir.
        /// Normal kosullarda bu kilit hic devreye girmez (en buyuk dogal
        /// adim ~0,05); sabit ayarlari yanlis olcum sonucu buyurse diye
        /// duruyor.
        /// </summary>
        private static void ClampStep(ArmyGenome before, ArmyGenome after)
        {
            float limit = EvolutionPressure.MaxResistanceStepPerRun;
            for (int i = 0; i < after.Resistance.Length; i++)
            {
                float d = after.Resistance[i] - before.Resistance[i];
                if (d > limit) after.Resistance[i] = before.Resistance[i] + limit;
                else if (d < -limit) after.Resistance[i] = before.Resistance[i] - limit;
            }
        }
    }
}
