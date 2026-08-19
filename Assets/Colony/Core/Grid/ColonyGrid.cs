using System;

namespace Colony.Core
{
    /// <summary>
    /// Oyunun harita izgarasi. Dikey tutus icin tasarlanmistir:
    /// y = Height - 1 ust kenar (boceklerin girisi), y = 0 alt kenar (yuva).
    /// Unity'ye bagimli degildir; koordinatlar duz int indeks olarak tasinir.
    /// </summary>
    public sealed class ColonyGrid
    {
        private readonly CellKind[] cells;

        public int Width { get; }
        public int Height { get; }
        public int CellCount => cells.Length;

        public ColonyGrid(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Genislik pozitif olmali.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Yukseklik pozitif olmali.");

            Width = width;
            Height = height;
            cells = new CellKind[width * height];
        }

        public int Index(int x, int y) => y * Width + x;
        public int X(int index) => index % Width;
        public int Y(int index) => index / Width;

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public CellKind Get(int index) => cells[index];
        public void Set(int index, CellKind kind) => cells[index] = kind;

        /// <summary>Yalnizca koridor hucreleri yurunebilir.</summary>
        public bool IsWalkable(int index) => cells[index] == CellKind.Corridor;

        /// <summary>
        /// 4 yonlu komsulari buffer'a yazar ve sayisini dondurur.
        /// Buffer en az 4 elemanli olmalidir. Yurunebilirlik kontrolu yapmaz.
        /// </summary>
        public int GetNeighbors(int index, int[] buffer)
        {
            if (buffer == null || buffer.Length < 4)
                throw new ArgumentException("Buffer en az 4 elemanli olmali.", nameof(buffer));

            int x = X(index);
            int y = Y(index);
            int count = 0;

            if (y + 1 < Height) buffer[count++] = Index(x, y + 1);
            if (y - 1 >= 0)     buffer[count++] = Index(x, y - 1);
            if (x - 1 >= 0)     buffer[count++] = Index(x - 1, y);
            if (x + 1 < Width)  buffer[count++] = Index(x + 1, y);

            return count;
        }
    }
}
