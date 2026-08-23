using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Sablondan harita uretir. Dikey tutus duzeni: koridorlar yukaridan
    /// asagi uzanir, aralarinda kurulum seritleri kalir. Ust satir giris
    /// bandi, alt satir us bandidir; ikisi koridorlari birbirine baglar.
    ///
    /// Koridorlar ayrica yol ORTASINDA yatay gecitlerle (rung) baglanir.
    /// Gecitsiz haritada birlik yalnizca girerken koridor secer ve o secim
    /// tek seferliktir; gecitler secimi her kavsakta yeniden acar.
    /// </summary>
    public sealed class MapGenerator
    {
        /// <summary>Bos hucrenin kalici engel olarak kalma olasiligi (yuzde).</summary>
        private const int BlockedChancePercent = 8;

        /// <summary>Kaya orani (kalani agac). Kaya dayanikli, agac ucuz hedeftir.</summary>
        private const int RockSharePercent = 40;

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

            int leftMost = int.MaxValue;
            int rightMost = int.MinValue;

            for (int c = 0; c < template.CorridorCount; c++)
            {
                int corridorStart = gapSize * (c + 1) + c * template.CorridorWidth;
                if (corridorStart < 0) corridorStart = 0;

                for (int w = 0; w < template.CorridorWidth; w++)
                {
                    int x = corridorStart + w;
                    if (x >= template.Width) break;

                    if (x < leftMost) leftMost = x;
                    if (x > rightMost) rightMost = x;

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

            CarveRungs(grid, template, leftMost, rightMost, random);
            FillRemainder(grid, template, random);

            return grid;
        }

        /// <summary>
        /// Yol ortasinda yatay gecitler acar. Gecitler yalnizca EN SOLDAKI ve
        /// EN SAGDAKI koridor arasinda uzanir; kenar sutunlarina dokunmaz,
        /// cunku kenara ulasan bir gecit kenar sutununu de koridor yapar ve
        /// "kenarda koridor olmaz" kuralini bozar.
        ///
        /// Gecit satirlari yuksekligi esit dilimlere bolerek secilir, dilim
        /// icinde seed'e gore kayar. Tamamen rastgele serpilirse iki gecit
        /// ust uste dusup haritanin yarisini catalsiz birakiyor.
        /// </summary>
        private static void CarveRungs(
            BattleGrid grid, MapTemplate template, int leftMost, int rightMost, Random random)
        {
            if (template.RungCount <= 0) return;
            if (leftMost > rightMost) return;
            if (template.Height < 7) return;

            // Ust ve alt baglanti bantlarina bitisik satirlar gecit olamaz:
            // orada zaten baglanti var, gecit bir sey eklemez.
            int firstRow = 2;
            int lastRow = template.Height - 3;
            int usable = lastRow - firstRow + 1;
            if (usable <= 0) return;

            int count = template.RungCount;
            if (count > usable) count = usable;

            int band = usable / count;
            if (band < 1) band = 1;

            for (int r = 0; r < count; r++)
            {
                int bandStart = firstRow + r * band;
                if (bandStart > lastRow) break;

                int bandEnd = (r == count - 1) ? lastRow : bandStart + band - 1;
                if (bandEnd > lastRow) bandEnd = lastRow;

                int y = bandStart + random.Next(0, bandEnd - bandStart + 1);

                for (int x = leftMost; x <= rightMost; x++)
                    grid.Set(grid.Index(x, y), CellKind.Corridor);
            }
        }

        /// <summary>
        /// Kalan bos hucreler kurulum seridi, yikilabilir ortu veya kalici
        /// engel olur. Ortu haritayi degistirilebilir yapar: bugun engel olan
        /// hucre, vurulup yikilinca yarinin mevzisidir.
        /// </summary>
        private static void FillRemainder(BattleGrid grid, MapTemplate template, Random random)
        {
            int sceneryPercent = template.SceneryPercent;
            if (sceneryPercent < 0) sceneryPercent = 0;
            if (sceneryPercent > 100 - BlockedChancePercent) sceneryPercent = 100 - BlockedChancePercent;

            for (int i = 0; i < grid.CellCount; i++)
            {
                if (grid.Get(i) != CellKind.Blocked) continue;

                int roll = random.Next(0, 100);
                if (roll < BlockedChancePercent) continue;                       // kalici engel
                if (roll < BlockedChancePercent + sceneryPercent)
                    grid.Set(i, CellKind.Scenery);
                else
                    grid.Set(i, CellKind.BuildStrip);
            }
        }

        /// <summary>
        /// Uretilen haritadaki Scenery hucrelerine can dagitir. Kaya agactan
        /// dayaniklidir; hangisinin nereye dustugu seed'e baglidir ki ayni
        /// seed ayni haritayi versin.
        /// </summary>
        public SceneryMap BuildScenery(BattleGrid grid, int seed, float treeHealth, float rockHealth)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            var scenery = new SceneryMap(grid);
            var random = new Random(seed);

            // Once topla: Place cagrisi hucre turunu degistirdigi icin
            // dolasirken degistirmek gorunurde calisir ama kirilgan olur.
            var cells = new List<int>();
            for (int i = 0; i < grid.CellCount; i++)
                if (grid.Get(i) == CellKind.Scenery) cells.Add(i);

            for (int i = 0; i < cells.Count; i++)
            {
                bool rock = random.Next(0, 100) < RockSharePercent;
                scenery.Place(cells[i],
                    rock ? SceneryKind.Rock : SceneryKind.Tree,
                    rock ? rockHealth : treeHealth);
            }

            return scenery;
        }
    }
}
