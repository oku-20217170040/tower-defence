using System;

namespace Frontline.Core
{
    /// <summary>
    /// Birliklerin algiladigi tehlike haritasi. YALNIZCA batarya kuleler buraya yazar.
    /// Pusu mevzisiler hic AddSource cagirmaz; ordu onlari ogrenemez.
    /// Bu ayrim, oyunun merkezi mekaniginin tek teknik karsiligidir (spec 3.1).
    /// </summary>
    public sealed class ThreatField
    {
        private readonly BattleGrid grid;
        private readonly float[] values;

        public ThreatField(BattleGrid grid)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            values = new float[grid.CellCount];
        }

        public float Get(int index) => values[index];

        /// <summary>
        /// Bir batarya kulenin tehdidini alana ekler.
        /// Merkezde tam guc, yaricapin kenarinda sifira dogrusal duser.
        /// Yaricap disina hic yazmaz. Degerler birikimlidir.
        /// </summary>
        public void AddSource(int centerIndex, float radius, float strength)
        {
            if (radius <= 0f || strength <= 0f) return;

            int cx = grid.X(centerIndex);
            int cy = grid.Y(centerIndex);
            int reach = (int)radius;

            int minX = cx - reach; if (minX < 0) minX = 0;
            int maxX = cx + reach; if (maxX > grid.Width - 1) maxX = grid.Width - 1;
            int minY = cy - reach; if (minY < 0) minY = 0;
            int maxY = cy + reach; if (maxY > grid.Height - 1) maxY = grid.Height - 1;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance > radius) continue;

                    float falloff = 1f - (distance / radius);
                    values[grid.Index(x, y)] += strength * falloff;
                }
            }
        }

        public void Clear() => Array.Clear(values, 0, values.Length);
    }
}
