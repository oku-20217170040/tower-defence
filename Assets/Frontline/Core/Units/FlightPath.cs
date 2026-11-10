using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Ucan birligin rotasi: baslangictan usya DUZ CIZGI.
    ///
    /// Neden yol bulucu degil: RoutePathfinder yurunebilir hucrelerde ve
    /// kesif izi/yem/tehdit maliyetleriyle calisir. Ucan bunlarin hicbirini
    /// tanimaz (spec 5.2, "koridor tanimaz"). Ucanin rotasini maliyet
    /// modeliyle ifade etmeye calismak, "sifir maliyet" gibi sahte bir
    /// ayar ureterek yurüyen birliklerin de dengesini bozardi. Ayri ve
    /// aptal bir hesap, ortak modeli kirletmekten iyidir.
    ///
    /// Bresenham cizgisi: 4-komsu yurumeye uygun olsun diye her adimda
    /// yalnizca TEK eksen degisir; boylece cikan hucre listesi yurüyen
    /// birliklerinkiyle ayni bicimdedir ve hareket kodu tek kalir.
    ///
    /// Saf C#'tir; Unity tipi kullanmaz.
    /// </summary>
    public static class FlightPath
    {
        /// <summary>
        /// start'tan goal'a duz rota. Sonuc start ile baslar, goal ile
        /// biter. Yurunebilirlige BAKMAZ - ucan icin duvar yoktur.
        /// </summary>
        public static void Build(BattleGrid grid, int start, int goal, List<int> result)
        {
            result.Clear();
            if (grid == null) return;

            int x = grid.X(start), y = grid.Y(start);
            int gx = grid.X(goal), gy = grid.Y(goal);

            int dx = gx > x ? 1 : (gx < x ? -1 : 0);
            int dy = gy > y ? 1 : (gy < y ? -1 : 0);

            int spanX = gx > x ? gx - x : x - gx;
            int spanY = gy > y ? gy - y : y - gy;

            result.Add(grid.Index(x, y));

            // Hata terimi: hangi eksende ilerlemenin cizgiyi daha az
            // saptiracagini secer. Capraz adim URETMEZ.
            int error = spanX - spanY;

            while (x != gx || y != gy)
            {
                if (y == gy || (x != gx && error > 0))
                {
                    x += dx;
                    error -= 2 * spanY;
                }
                else
                {
                    y += dy;
                    error += 2 * spanX;
                }

                result.Add(grid.Index(x, y));
            }
        }
    }
}
