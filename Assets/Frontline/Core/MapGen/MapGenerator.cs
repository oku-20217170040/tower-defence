using System;

namespace Frontline.Core
{
    /// <summary>
    /// Sablondan harita uretir. Dikey tutus duzeni: koridorlar yukaridan
    /// asagi uzanir, aralarinda kurulum seritleri kalir. Ust satir giris
    /// bandi, alt satir us bandidir; ikisi koridorlari birbirine baglar.
    /// </summary>
    public sealed class MapGenerator
    {
        /// <summary>Bos hucrenin engel olarak kalma olasiligi (yuzde). Doku varyasyonunu uretir.</summary>
        private const int BlockedChancePercent = 15;

        /// <summary>Ayni seed her zaman ayni haritayi uretir.</summary>
        public BattleGrid Generate(MapTemplate template, int seed)
        {
            if (template.Width <= 0 || template.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "Sablon boyutlari pozitif olmali.");
            if (template.CorridorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "CorridorCount pozitif olmali.");
            if (template.CorridorWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "CorridorWidth pozitif olmali.");

            var grid = new BattleGrid(template.Width, template.Height);
            var random = new Random(seed);

            // Koridorlari dar eksende, ARALARINDA VE KENARLARINDA bosluk kalacak
            // sekilde yerlestir. Kenara yapisik koridorun tek tarafinda kurulum
            // seridi olur; bu onu digerlerinden daha zayif yapar ve koridor secimi
            // gercek bir karar olmaktan cikar (spec 9.4).
            int totalCorridorWidth = template.CorridorCount * template.CorridorWidth;
            int remaining = template.Width - totalCorridorWidth;
            if (remaining < 0) remaining = 0;

            int gapCount = template.CorridorCount + 1;
            int gapSize = remaining / gapCount;
            if (gapSize < 1) gapSize = 1;

            for (int c = 0; c < template.CorridorCount; c++)
            {
                int corridorStart = gapSize * (c + 1) + c * template.CorridorWidth;
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
