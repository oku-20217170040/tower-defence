using System;

namespace Colony.Core
{
    /// <summary>
    /// Sablondan harita uretir. Dikey tutus duzeni: koridorlar yukaridan
    /// asagi uzanir, aralarinda kurulum seritleri kalir. Ust satir giris
    /// bandi, alt satir yuva bandidir; ikisi koridorlari birbirine baglar.
    /// </summary>
    public sealed class MapGenerator
    {
        /// <summary>Bos hucrenin engel olarak kalma olasiligi (yuzde). Doku varyasyonunu uretir.</summary>
        private const int BlockedChancePercent = 15;

        /// <summary>Ayni seed her zaman ayni haritayi uretir.</summary>
        public ColonyGrid Generate(MapTemplate template, int seed)
        {
            if (template.Width <= 0 || template.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "Sablon boyutlari pozitif olmali.");
            if (template.CorridorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "CorridorCount pozitif olmali.");
            if (template.CorridorWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "CorridorWidth pozitif olmali.");

            var grid = new ColonyGrid(template.Width, template.Height);
            var random = new Random(seed);

            // Koridorlari dar eksende esit araliklarla yerlestir.
            int span = template.Width / template.CorridorCount;

            for (int c = 0; c < template.CorridorCount; c++)
            {
                int corridorStart = c * span + (span - template.CorridorWidth) / 2;
                if (corridorStart < 0) corridorStart = 0;

                for (int w = 0; w < template.CorridorWidth; w++)
                {
                    int x = corridorStart + w;
                    if (x >= template.Width) break;

                    for (int y = 0; y < template.Height; y++)
                        grid.Set(grid.Index(x, y), CellKind.Corridor);
                }
            }

            // Ust ve alt bantlar koridorlari birbirine baglar.
            for (int x = 0; x < template.Width; x++)
            {
                grid.Set(grid.Index(x, template.Height - 1), CellKind.Corridor);
                grid.Set(grid.Index(x, 0), CellKind.Corridor);
            }

            // Kalan Blocked hucreler kurulum seridi olur; bir kismi doku icin engel kalir.
            for (int i = 0; i < grid.CellCount; i++)
            {
                if (grid.Get(i) != CellKind.Blocked) continue;

                bool keepBlocked = random.Next(0, 100) < BlockedChancePercent;
                grid.Set(i, keepBlocked ? CellKind.Blocked : CellKind.BuildStrip);
            }

            return grid;
        }
    }
}
