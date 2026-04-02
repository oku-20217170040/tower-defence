using UnityEngine;
using System.Collections.Generic;

public class Pathfinder : MonoBehaviour
{
    private GridManager grid;

    [Header("Cost Weights")]
    [Tooltip("Tehdit puanı (tower menzili) A* maliyetine ne kadar etki etsin? " +
             "0 = sadece mesafe, 1+ = tehdit önem kazanır.")]
    public float threatWeight = 1f;

    [Tooltip("Q-Learning’den gelen learnedCost A* maliyetine ne kadar etki etsin?")]
    public float learnedWeight = 1f;

    [Tooltip("Genetik Algoritma’dan gelen geneticCost A* maliyetine ne kadar etki etsin?")]
    public float geneticWeight = 1f;

    private void Awake()
    {
        grid = FindObjectOfType<GridManager>();
    }

    // Manhattan heuristik
    int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public List<Node> FindPath(Vector2Int start, Vector2Int target)
    {
        if (grid == null || grid.grid == null) return null;

        Node startNode = grid.GetNode(start);
        Node targetNode = grid.GetNode(target);
        if (startNode == null || targetNode == null) return null;

        int gridSize = grid.grid.GetLength(0) * grid.grid.GetLength(1);
        var open   = new MinHeap<Node>(gridSize);
        var closed = new HashSet<Node>();

        // Tüm node'ların A* maliyetlerini resetle
        foreach (Node n in grid.grid)
        {
            if (n == null) continue;
            n.gCost = int.MaxValue;
            n.hCost = 0;
            n.parent = null;
            n.HeapIndex = -1;
        }

        startNode.gCost = 0;
        startNode.hCost = Heuristic(start, target);
        open.Push(startNode);

        while (open.Count > 0)
        {
            // O(log n) — MinHeap ile en düşük fCost
            Node current = open.Pop();
            closed.Add(current);

            if (current == targetNode)
                return Retrace(startNode, targetNode);

            foreach (Node neighbor in grid.GetNeighbors(current))
            {
                if (!neighbor.isWalkable || closed.Contains(neighbor))
                    continue;

                int threatPenalty   = threatWeight  > 0f && neighbor.threatCost  > 0f
                    ? Mathf.RoundToInt(neighbor.threatCost  * threatWeight)  : 0;
                int learnedPenalty  = learnedWeight > 0f && neighbor.learnedCost > 0f
                    ? Mathf.RoundToInt(neighbor.learnedCost * learnedWeight) : 0;
                int geneticPenalty  = geneticWeight > 0f && neighbor.geneticCost > 0f
                    ? Mathf.RoundToInt(neighbor.geneticCost * geneticWeight) : 0;

                int newCost = current.gCost + 1 + threatPenalty + learnedPenalty + geneticPenalty;

                if (newCost < neighbor.gCost)
                {
                    neighbor.gCost = newCost;
                    neighbor.hCost = Heuristic(neighbor.coordinates, target);
                    neighbor.parent = current;

                    if (open.Contains(neighbor))
                        open.UpdateItem(neighbor);
                    else
                        open.Push(neighbor);
                }
            }
        }

        return null;
    }

    List<Node> Retrace(Node start, Node end)
    {
        List<Node> path = new List<Node>();
        Node current = end;

        while (current != start)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }
}
