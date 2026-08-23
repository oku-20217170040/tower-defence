using System.Collections.Generic;

namespace Frontline.Core
{
    public struct MapValidationResult
    {
        public bool IsValid;
        public int CorridorCount;
        public float CostGap;

        /// <summary>
        /// Yol ortasindaki gecis hucresi sayisi: koridorlari birbirine baglayan
        /// yatay gecitlerin uzunlugu. Sifir ise koridor secimi girise sikismis
        /// demektir - birlik girdigi boruda sonuna kadar kalir.
        /// </summary>
        public int JunctionCount;

        public string FaidecoyReason;
    }

    /// <summary>
    /// Bir koridorun kapladigi sutun araligi. Koridor birden fazla sutun
    /// genisliginde olabildigi icin tek sutunla temsil edilemez: kurulum
    /// seridi cogu zaman yalnizca kenardaki sutunun komsusudur.
    /// </summary>
    public struct CorridorSpan
    {
        public int StartX;
        public int EndX;
    }

    /// <summary>
    /// Uretilen haritanin oynanabilir oldugunu dogrular (spec 9.4).
    /// Gecemeyen harita atilir ve yeniden uretilir.
    ///
    /// Kurallar:
    /// 1. En az 3 ayrik dikey koridor bulunmali.
    /// 2. Her koridorun yaninda en az bir kurulum seridi bulunmali.
    /// 3. En ucuz iki koridor arasindaki maliyet farki esigin altinda olmali;
    ///    aksi halde bir koridor bastan galiptir ve secim yanilsamaya doner.
    /// 4. Yol ortasinda en az <see cref="MinimumJunctions"/> catallanma bulunmali.
    ///    Catalsiz haritada birlik girise adim attigi anda yolunu secmis olur
    ///    ve oyuncunun yonlendirmesi hicbir sey degistiremez.
    /// </summary>
    public sealed class MapValidator
    {
        public const int MinimumCorridors = 3;

        /// <summary>
        /// Yol ortasinda gereken en az gecis hucresi. Ust ve alt baglanti
        /// bantlari sayilmaz; onlar her haritada zaten vardir ve yol ortasinda
        /// karar uretmezler.
        /// </summary>
        public const int MinimumJunctions = 4;

        public MapValidationResult Validate(BattleGrid grid, float maxCostGap)
        {
            var result = new MapValidationResult { FaidecoyReason = string.Empty };

            List<CorridorSpan> corridors = FindCorridors(grid);
            result.CorridorCount = corridors.Count;

            if (corridors.Count < MinimumCorridors)
            {
                result.IsValid = false;
                result.FaidecoyReason =
                    "Yetersiz koridor sayisi: " + corridors.Count +
                    " (en az " + MinimumCorridors + " gerekli).";
                return result;
            }

            int badColumn;
            if (!EveryCorridorHasBuildStrip(grid, corridors, out badColumn))
            {
                result.IsValid = false;
                result.FaidecoyReason =
                    badColumn + ". sutunda baslayan koridorun yaninda kurulum seridi yok.";
                return result;
            }

            result.CostGap = ComputeCostGap(grid, corridors);
            if (result.CostGap > maxCostGap)
            {
                result.IsValid = false;
                result.FaidecoyReason =
                    "Koridor maliyet farki cok yuksek: " + result.CostGap.ToString("F2") +
                    " > " + maxCostGap.ToString("F2") + ".";
                return result;
            }

            result.JunctionCount = CountJunctions(grid, corridors);
            if (result.JunctionCount < MinimumJunctions)
            {
                result.IsValid = false;
                result.FaidecoyReason =
                    "Yol ortasinda yeterli catallanma yok: " + result.JunctionCount +
                    " (en az " + MinimumJunctions + " gerekli). Gecit sayisini artir.";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Yol ortasindaki gecis hucrelerini sayar: ust ve alt baglanti
        /// bantlari disinda kalan, HICBIR dikey koridora ait olmayan
        /// yurunebilir hucreler. Bunlar yatay gecitlerdir - bir birligin
        /// koridor degistirebildigi tek yer.
        ///
        /// "Ucten fazla yurunebilir komsusu olan hucre" olcusu KULLANILAMAZ:
        /// iki hucre genisligindeki bir koridorun her hucresinin zaten uc
        /// komsusu vardir ve gecitsiz harita da gecer gorunur.
        /// </summary>
        private static int CountJunctions(BattleGrid grid, List<CorridorSpan> corridors)
        {
            int crossings = 0;

            for (int y = 1; y < grid.Height - 1; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (!grid.IsWalkable(grid.Index(x, y))) continue;
                    if (IsInsideCorridor(corridors, x)) continue;

                    crossings++;
                }
            }

            return crossings;
        }

        private static bool IsInsideCorridor(List<CorridorSpan> corridors, int x)
        {
            for (int c = 0; c < corridors.Count; c++)
                if (x >= corridors[c].StartX && x <= corridors[c].EndX) return true;

            return false;
        }

        /// <summary>
        /// Bastan sona kesintisiz koridor olan sutunlari bulur ve bitisik olanlari
        /// tek koridor olarak gruplar. Ust ve alt baglanti bantlari haric tutulur;
        /// onlar zaten her sutunda koridordur.
        /// </summary>
        private static List<CorridorSpan> FindCorridors(BattleGrid grid)
        {
            var spans = new List<CorridorSpan>();
            bool inCorridor = false;
            int spanStart = 0;

            for (int x = 0; x < grid.Width; x++)
            {
                bool fullColumn = true;
                for (int y = 1; y < grid.Height - 1; y++)
                {
                    if (grid.Get(grid.Index(x, y)) != CellKind.Corridor)
                    {
                        fullColumn = false;
                        break;
                    }
                }

                if (fullColumn && !inCorridor)
                {
                    inCorridor = true;
                    spanStart = x;
                }
                else if (!fullColumn && inCorridor)
                {
                    inCorridor = false;
                    spans.Add(new CorridorSpan { StartX = spanStart, EndX = x - 1 });
                }
            }

            if (inCorridor)
                spans.Add(new CorridorSpan { StartX = spanStart, EndX = grid.Width - 1 });

            return spans;
        }

        /// <summary>
        /// Koridorun HERHANGI bir hucminefieldin kurulum seridine komsu olmasi yeterlidir.
        /// Tum sutun araligi taranir; yalnizca ilk sutuna bakmak yanlis sonuc verir.
        /// </summary>
        private static bool EveryCorridorHasBuildStrip(
            BattleGrid grid, List<CorridorSpan> corridors, out int badColumn)
        {
            var buffer = new int[4];

            for (int c = 0; c < corridors.Count; c++)
            {
                CorridorSpan span = corridors[c];
                bool found = false;

                for (int x = span.StartX; x <= span.EndX && !found; x++)
                {
                    for (int y = 1; y < grid.Height - 1 && !found; y++)
                    {
                        int count = grid.GetNeighbors(grid.Index(x, y), buffer);
                        for (int i = 0; i < count; i++)
                        {
                            if (grid.Get(buffer[i]) == CellKind.BuildStrip)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }

                if (!found)
                {
                    badColumn = span.StartX;
                    return false;
                }
            }

            badColumn = -1;
            return true;
        }

        /// <summary>
        /// Bos harita uzerinde (kesif izi, yem, tehdit sifir) her koridorun bastan
        /// sona maliyetini olcer ve en ucuz iki koridor arasindaki farki doner.
        /// Bir koridorun maliyeti, icindeki en ucuz sutunun maliyetidir.
        /// </summary>
        private static float ComputeCostGap(BattleGrid grid, List<CorridorSpan> corridors)
        {
            var recon = new ReconMap(grid, 100f, 0.1f);
            var decoy = new DecoyField(grid, 10f, 0.5f);
            var threat = new ThreatField(grid);
            var model = new RouteCostModel(recon, decoy, threat)
            {
                ThreatWeight = 1f,
                AttractionWeight = 1f
            };

            var costs = new List<float>();
            for (int c = 0; c < corridors.Count; c++)
            {
                CorridorSpan span = corridors[c];
                float cheapest = float.PositiveInfinity;

                for (int x = span.StartX; x <= span.EndX; x++)
                {
                    float total = 0f;
                    for (int y = 1; y < grid.Height - 1; y++)
                        total += model.StepCost(grid.Index(x, y));

                    if (total < cheapest) cheapest = total;
                }

                costs.Add(cheapest);
            }

            costs.Sort();
            return costs.Count >= 2 ? costs[1] - costs[0] : 0f;
        }
    }
}
