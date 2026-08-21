using System;

namespace Frontline.Core
{
    /// <summary>
    /// Hucre indeksleri icin float oncelikli ikili min-heap.
    /// Mevcut Utils/MinHeap sinifindan farki: Node nesnesi yerine int indeks tutar,
    /// bu sayede Frontline.Core hicbir Unity tipine bagimli olmaz ve
    /// her hucrenin heap konumu O(1) bulunabilir.
    /// </summary>
    public sealed class IndexedMinHeap
    {
        private readonly int[] heap;        // heap pozisyonu -> hucre indeksi
        private readonly int[] positions;   // hucre indeksi -> heap pozisyonu (-1 = heap disinda)
        private readonly float[] priorities;
        private int count;

        public int Count => count;

        public IndexedMinHeap(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            heap = new int[capacity];
            positions = new int[capacity];
            priorities = new float[capacity];
            Clear();
        }

        public void Clear()
        {
            count = 0;
            for (int i = 0; i < positions.Length; i++)
                positions[i] = -1;
        }

        public bool Contains(int cell) => positions[cell] >= 0;

        public void Push(int cell, float priority)
        {
            priorities[cell] = priority;
            heap[count] = cell;
            positions[cell] = count;
            count++;
            BubbleUp(count - 1);
        }

        /// <summary>Heap icindeki bir hucrenin onceligi DUSTUGUNDE cagrilir.</summary>
        public void Update(int cell, float priority)
        {
            priorities[cell] = priority;
            BubbleUp(positions[cell]);
        }

        public int Pop()
        {
            int top = heap[0];
            positions[top] = -1;
            count--;

            if (count > 0)
            {
                heap[0] = heap[count];
                positions[heap[0]] = 0;
                BubbleDown(0);
            }

            return top;
        }

        private void BubbleUp(int position)
        {
            while (position > 0)
            {
                int parent = (position - 1) / 2;
                if (priorities[heap[position]] >= priorities[heap[parent]]) break;
                Swap(position, parent);
                position = parent;
            }
        }

        private void BubbleDown(int position)
        {
            while (true)
            {
                int left = position * 2 + 1;
                int right = left + 1;
                int smallest = position;

                if (left < count && priorities[heap[left]] < priorities[heap[smallest]])
                    smallest = left;
                if (right < count && priorities[heap[right]] < priorities[heap[smallest]])
                    smallest = right;

                if (smallest == position) break;

                Swap(position, smallest);
                position = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            int cellA = heap[a];
            int cellB = heap[b];
            heap[a] = cellB;
            heap[b] = cellA;
            positions[cellB] = a;
            positions[cellA] = b;
        }
    }
}
