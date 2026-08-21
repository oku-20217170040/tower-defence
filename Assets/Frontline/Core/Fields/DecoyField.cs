using System;

namespace Frontline.Core
{
    /// <summary>
    /// Sahte Kesif izi Yayici'nin urettigi yapay cekim alani.
    /// Gercek kesif izidan ayri tutulur cunku kurallari farklidir:
    /// sabit miktar ekler (oransal degil) ve ayni hucrede azalan getiri verir.
    /// Boylece yem zayif izleri kolayca yonlendirir, oturmus guclu izi ceviremez.
    /// </summary>
    public sealed class DecoyField
    {
        private readonly float[] values;
        private readonly int[] applyCounts;

        /// <summary>Ilk uygulamada eklenen miktar. Tum yem dengesi bu sayiya baglidir.</summary>
        public float BaseStrength { get; }

        /// <summary>Her tekrarda onceki eklemenin carpani. [0, 1) araliginda.</summary>
        public float DiminishFactor { get; }

        public DecoyField(BattleGrid grid, float baseStrength, float diminishFactor)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (baseStrength < 0f)
                throw new ArgumentOutOfRangeException(nameof(baseStrength), "BaseStrength negatif olamaz.");
            if (diminishFactor < 0f || diminishFactor >= 1f)
                throw new ArgumentOutOfRangeException(nameof(diminishFactor), "DiminishFactor [0, 1) araliginda olmali.");

            values = new float[grid.CellCount];
            applyCounts = new int[grid.CellCount];
            BaseStrength = baseStrength;
            DiminishFactor = diminishFactor;
        }

        public float Get(int index) => values[index];

        public int ApplyCount(int index) => applyCounts[index];

        /// <summary>
        /// Hucreye yem uygular. n'inci uygulama BaseStrength * DiminishFactor^(n-1) ekler.
        /// Toplam, geometrik seri limiti olan BaseStrength / (1 - DiminishFactor) degerini asmaz.
        /// </summary>
        public void Apply(int index)
        {
            float amount = BaseStrength * Pow(DiminishFactor, applyCounts[index]);
            values[index] += amount;
            applyCounts[index]++;
        }

        /// <summary>
        /// Tek bir hucredeki yemi siler. Kor birlik icindir: yalani
        /// ortaya cikarmak, o hucrenin sayacini da sifirlar ki oyuncu
        /// tazeledigende yem yeniden TAM guc uygulansin.
        /// </summary>
        public void Erase(int index)
        {
            values[index] = 0f;
            applyCounts[index] = 0;
        }

        public void Clear()
        {
            Array.Clear(values, 0, values.Length);
            Array.Clear(applyCounts, 0, applyCounts.Length);
        }

        /// <summary>
        /// Assembly noEngineReferences oldugu icin Mathf kullanilamaz.
        /// Us tam sayi oldugundan basit dongu, Math.Pow'un double donusumunden ucuzdur.
        /// </summary>
        private static float Pow(float baseValue, int exponent)
        {
            float result = 1f;
            for (int i = 0; i < exponent; i++)
                result *= baseValue;
            return result;
        }
    }
}
