using System;

namespace Frontline.Core
{
    /// <summary>
    /// Ordunin su an hangi koridoru ezberledigini olcer.
    ///
    /// Koridor kimligi MapGenerator'in yerlesim formulunden turer: koridorlar
    /// esit aralikli dikey bantlardir. Cikti, koridor basina kesif izi payidir
    /// (toplami 1). Hic kesif izi yoksa esit dagilim doner - o an ordunin bir
    /// tercihi yoktur demektir.
    /// </summary>
    public static class CorridorPressure
    {
        public static float[] Measure(BattleGrid grid, ReconMap recon, MapTemplate template)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (recon == null) throw new ArgumentNullException(nameof(recon));
            if (template.CorridorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "CorridorCount pozitif olmali.");

            var shares = new float[template.CorridorCount];

            // MapGenerator ile ayni bosluk hesabi.
            int totalCorridorWidth = template.CorridorCount * template.CorridorWidth;
            int remaining = template.Width - totalCorridorWidth;
            if (remaining < 0) remaining = 0;
            int gapSize = remaining / (template.CorridorCount + 1);

            float total = 0f;

            for (int c = 0; c < template.CorridorCount; c++)
            {
                int corridorStart = gapSize * (c + 1) + c * template.CorridorWidth;
                if (corridorStart < 0) corridorStart = 0;

                float sum = 0f;
                for (int w = 0; w < template.CorridorWidth; w++)
                {
                    int x = corridorStart + w;
                    if (x >= grid.Width) break;

                    for (int y = 0; y < grid.Height; y++)
                        sum += recon.Get(grid.Index(x, y));
                }

                shares[c] = sum;
                total += sum;
            }

            if (total <= 0f)
            {
                float even = 1f / template.CorridorCount;
                for (int c = 0; c < shares.Length; c++) shares[c] = even;
                return shares;
            }

            for (int c = 0; c < shares.Length; c++) shares[c] /= total;
            return shares;
        }
    }
}
