using System;

namespace Colony.Core
{
    /// <summary>
    /// Recine kulelerinin yavaslatma alani. Hucre basina bir gecis suresi
    /// carpani tutar; 1 = normal hiz, 2 = yarim hiz.
    ///
    /// Neden ThreatField'dan ayri: tehdit koloninin ALGILADIGI bir seydir ve
    /// rota maliyetine girer. Yavaslatma ise algilanmaz, yalnizca fiziksel
    /// olarak yavaslatir. Olcum bu ayrimin sart oldugunu gosterdi: recine
    /// tehdit de yazdiginda koloni ondan kaciyor ve yavaslatma olu mekanige
    /// donusuyor (20 Agu 2026 taramasi: 6 kule butcesinde x2 yavaslatma
    /// tehdit yazmadan %95 kazandiriyor, yazinca %5).
    ///
    /// Ust uste binen recineler CARPILMAZ, en gucluse alinir; carpilsaydi
    /// iki recineyi ayni yere yigmak baskin strateji olurdu.
    /// </summary>
    public sealed class SlowField
    {
        private readonly ColonyGrid grid;
        private readonly float[] factors;

        /// <summary>Yavaslatmasiz hucrenin carpani.</summary>
        public const float None = 1f;

        public SlowField(ColonyGrid grid)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));

            factors = new float[grid.CellCount];
            Clear();
        }

        /// <summary>Bu hucrede gecis suresi kac katina cikiyor.</summary>
        public float Get(int index) => factors[index];

        /// <summary>
        /// Merkez cevresine yavaslatma yazar. Yaricap disinda kalan hucreler
        /// etkilenmez; menzil icinde etki SABITTIR - mesafeyle azalsaydi
        /// recinenin nereye konuldugu bulanik bir karar olurdu.
        /// </summary>
        public void AddSource(int centerIndex, float radius, float factor)
        {
            if (factor <= None) return;

            int cx = grid.X(centerIndex);
            int cy = grid.Y(centerIndex);
            int reach = (int)radius;
            float radiusSq = radius * radius;

            for (int y = cy - reach; y <= cy + reach; y++)
            {
                for (int x = cx - reach; x <= cx + reach; x++)
                {
                    if (!grid.InBounds(x, y)) continue;

                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy > radiusSq) continue;

                    int index = grid.Index(x, y);
                    if (factor > factors[index]) factors[index] = factor;
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < factors.Length; i++) factors[i] = None;
        }
    }
}
