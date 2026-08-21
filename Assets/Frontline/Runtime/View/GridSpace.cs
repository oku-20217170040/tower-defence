using System;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Izgara koordinati ile dunya koordinati arasindaki donusum.
    /// Izgara dunya orijininde ortalanir. Dikey tutus duzeni korunur:
    /// y = 0 altta (us), y = Height-1 ustte (giris) - spec 3.6.
    ///
    /// MonoBehaviour degildir; saf hesaptir ve birim testiyle dogrulanir.
    /// </summary>
    public sealed class GridSpace
    {
        private readonly BattleGrid grid;

        /// <summary>Bir hucrenin dunya birimi cinsinden kenar uzunlugu.</summary>
        public float CellSize { get; }

        /// <summary>Izgaranin sol-alt kosesinin dunya konumu.</summary>
        public Vector2 Origin { get; }

        /// <summary>Izgaranin dunya birimi cinsinden toplam genisligi.</summary>
        public float WorldWidth => grid.Width * CellSize;

        /// <summary>Izgaranin dunya birimi cinsinden toplam yuksekligi.</summary>
        public float WorldHeight => grid.Height * CellSize;

        public GridSpace(BattleGrid grid, float cellSize)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "CellSize pozitif olmali.");

            this.grid = grid;
            CellSize = cellSize;
            Origin = new Vector2(
                -grid.Width * cellSize * 0.5f,
                -grid.Height * cellSize * 0.5f);
        }

        /// <summary>Hucrenin MERKEZININ dunya konumu.</summary>
        public Vector2 CellToWorld(int index)
        {
            return new Vector2(
                Origin.x + (grid.X(index) + 0.5f) * CellSize,
                Origin.y + (grid.Y(index) + 0.5f) * CellSize);
        }

        /// <summary>
        /// Dunya noktasinin dustugu hucreyi bulur.
        /// Nokta izgara disindaysa false doner ve cell degeri anlamsizdir.
        /// </summary>
        public bool WorldToCell(Vector2 world, out int cell)
        {
            float localX = (world.x - Origin.x) / CellSize;
            float localY = (world.y - Origin.y) / CellSize;

            int x = Mathf.FloorToInt(localX);
            int y = Mathf.FloorToInt(localY);

            if (!grid.InBounds(x, y))
            {
                cell = -1;
                return false;
            }

            cell = grid.Index(x, y);
            return true;
        }

        /// <summary>
        /// Izgaranin tamaminin kadraja sigmasi icin gereken ortografik kamera boyutu.
        /// Ortografik boyut DIKEY yari-yuksekliktir; yatay yari-genislik boyut * aspect olur.
        /// Iki eksenden hangisi sinirlayiciysa o belirler.
        /// </summary>
        public float RequiredOrthographicSize(float aspect)
        {
            if (aspect <= 0f)
                throw new ArgumentOutOfRangeException(nameof(aspect), "Aspect pozitif olmali.");

            float verticalNeed = WorldHeight * 0.5f;
            float horizontalNeed = WorldWidth * 0.5f / aspect;

            return verticalNeed > horizontalNeed ? verticalNeed : horizontalNeed;
        }
    }
}
