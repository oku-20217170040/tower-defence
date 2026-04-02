using System;

/// <summary>
/// Generic binary min-heap. A* performans optimizasyonu için kullanılır.
/// open.OrderBy().First() → O(n log n) yerine Push/Pop → O(log n)
/// </summary>
public class MinHeap<T> where T : IHeapItem<T>
{
    private T[] items;
    private int count;

    public int Count => count;

    public MinHeap(int maxSize)
    {
        items = new T[maxSize];
    }

    public void Push(T item)
    {
        if (count >= items.Length)
            Resize();

        item.HeapIndex = count;
        items[count] = item;
        count++;
        BubbleUp(item);
    }

    public T Pop()
    {
        T first = items[0];
        count--;

        items[0] = items[count];
        items[0].HeapIndex = 0;
        BubbleDown(items[0]);

        return first;
    }

    public bool Contains(T item)
    {
        if (item.HeapIndex < 0 || item.HeapIndex >= count) return false;
        return items[item.HeapIndex].Equals(item);
    }

    /// <summary>
    /// Heap içindeki bir item'ın değeri azaldıysa (gCost güncellendi) yeniden sırala.
    /// </summary>
    public void UpdateItem(T item)
    {
        BubbleUp(item);
    }

    private void BubbleUp(T item)
    {
        int index = item.HeapIndex;

        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            T parent = items[parentIndex];

            if (item.CompareTo(parent) < 0)
            {
                Swap(item, parent);
                index = item.HeapIndex;
            }
            else
            {
                break;
            }
        }
    }

    private void BubbleDown(T item)
    {
        int index = item.HeapIndex;

        while (true)
        {
            int leftChild  = index * 2 + 1;
            int rightChild = index * 2 + 2;
            int swapIndex  = index;

            if (leftChild < count && items[leftChild].CompareTo(items[swapIndex]) < 0)
                swapIndex = leftChild;

            if (rightChild < count && items[rightChild].CompareTo(items[swapIndex]) < 0)
                swapIndex = rightChild;

            if (swapIndex == index) break;

            Swap(item, items[swapIndex]);
            index = item.HeapIndex;
        }
    }

    private void Swap(T a, T b)
    {
        items[a.HeapIndex] = b;
        items[b.HeapIndex] = a;

        int tmp = a.HeapIndex;
        a.HeapIndex = b.HeapIndex;
        b.HeapIndex = tmp;
    }

    private void Resize()
    {
        T[] newItems = new T[items.Length * 2];
        Array.Copy(items, newItems, items.Length);
        items = newItems;
    }
}

/// <summary>
/// MinHeap'te tutulacak her öğenin implement etmesi gereken arayüz.
/// </summary>
public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}
