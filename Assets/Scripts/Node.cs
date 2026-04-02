using System;
using UnityEngine;

/// <summary>
/// Grid üzerindeki tek bir hücre (node).
/// A* için maliyetler + threat + learnedCost içerir.
/// IHeapItem implement ederek MinHeap'te O(log n) ile kullanılır.
/// </summary>
public class Node : IHeapItem<Node>
{
    public Vector2Int coordinates;
    public bool isWalkable;
    public bool isOccupied;
    public Vector3 worldPosition;

    // A* maliyetleri
    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;
    public Node parent;

    // Tower'lardan gelen tehdit puanı (ThreatMap)
    public float threatCost;

    // Q-Learning'ten gelen "öğrenilmiş risk" puanı
    public float learnedCost;

    // Genetik Algoritma'dan gelen hücre kaçınma maliyeti
    public float geneticCost;

    // IHeapItem: MinHeap'teki pozisyon
    public int HeapIndex { get; set; }

    // IComparable: fCost'a göre karşılaştır, eşitlik durumunda hCost kullan
    public int CompareTo(Node other)
    {
        int cmp = fCost.CompareTo(other.fCost);
        if (cmp == 0) cmp = hCost.CompareTo(other.hCost);
        return cmp;
    }

    public Node(Vector2Int coordinates, bool isWalkable, Vector3 worldPosition)
    {
        this.coordinates = coordinates;
        this.isWalkable = isWalkable;
        this.worldPosition = worldPosition;

        isOccupied = false;
        threatCost = 0f;
        learnedCost = 0f;
        geneticCost = 0f;

        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }
}
