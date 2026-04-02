using System.Collections.Generic;
using UnityEngine;

// Grid sistemini yöneten sınıf
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 13;          // Grid genişliği (x ekseni)
    public int height = 21;         // Grid yüksekliği (y ekseni)
    public float cellSize = 1f;     // Hücre boyutu
    public Vector3 origin = Vector3.zero; // Başlangıç noktası
    public LayerMask unwalkableMask; // Engel katmanı

    public Node[,] grid; // Grid veri yapısı (Node dizisi)

    [Header("Debug / Heatmap")]
    [Tooltip("Scene görünümünde grid çizilsin mi?")]
    public bool drawGizmos = true;

    [Tooltip("Tehdit / öğrenme ısı haritasını çiz.")]
    public bool drawHeatmap = true;

    [Tooltip("0 veya negatifse, sahnedeki max tehdite göre otomatik ölçeklenir.")]
    public float manualMaxThreat = 0f;

    private void Start()
    {
        // Grid daha önce üretilmediyse üret
        if (grid == null)
        {
            Generate();
        }

        // İlk tehdit hesaplaması
        ThreatMap.Instance?.Recalculate();
    }

    // Grid oluşturma metodu
    public void Generate()
    {
        grid = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 world = origin + new Vector3(x + 0.5f, y + 0.5f) * cellSize;

                bool walkable = !Physics2D.OverlapBox(
                    world,
                    Vector2.one * (cellSize * 0.9f),
                    0f,
                    unwalkableMask
                );

                grid[x, y] = new Node(new Vector2Int(x, y), walkable, world);
            }
        }
    }

    // Belirli koordinattaki node’u döndür
    public Node GetNode(Vector2Int c)
    {
        if (c.x < 0 || c.x >= width || c.y < 0 || c.y >= height)
            return null;

        return grid[c.x, c.y];
    }

    // Komşu node’ları döndür (4 yön)
    public List<Node> GetNeighbors(Node n)
    {
        var list = new List<Node>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var d in dirs)
        {
            var m = GetNode(n.coordinates + d);
            if (m != null && m.isWalkable)
                list.Add(m);
        }

        return list;
    }

    // 🌍 Dünya pozisyonunu grid koordinatlarına çevirir
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - origin.y) / cellSize);

        // Grid sınırlarını kontrol et
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return new Vector2Int(x, y);
    }


    // ─────────────────────────────────────────────
    //  RUNTIME ENGEL GÜNCELLEME (Walkable Refresh)
    //  Oyun çalışırken tower/stone eklenince grid.walkable yeniden hesaplanmalı.
    // ─────────────────────────────────────────────

    // Tek bir hücrenin yürünebilirliğini günceller
    public void RefreshCellWalkability(Vector2Int c)
    {
        if (grid == null) return;
        if (c.x < 0 || c.x >= width || c.y < 0 || c.y >= height) return;

        Node n = grid[c.x, c.y];
        if (n == null) return;

        bool walkable = !Physics2D.OverlapBox(
            n.worldPosition,
            Vector2.one * (cellSize * 0.9f),
            0f,
            unwalkableMask
        );

        n.isWalkable = walkable;
    }

    // Tüm gridin yürünebilirliğini günceller (küçük gridler için en garantisi)
    public void RefreshAllWalkability()
    {
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node n = grid[x, y];
                if (n == null) continue;

                bool walkable = !Physics2D.OverlapBox(
                    n.worldPosition,
                    Vector2.one * (cellSize * 0.9f),
                    0f,
                    unwalkableMask
                );

                n.isWalkable = walkable;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  HEATMAP GÖRSELLEŞTİRME (Scene View)
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (grid == null) return;

        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        // 1) Max tehdit değerini bul (threatCost + learnedCost)
        float maxThreat = 0f;

        if (drawHeatmap)
        {
            if (manualMaxThreat > 0f)
            {
                maxThreat = manualMaxThreat;
            }
            else
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        Node n = grid[x, y];
                        if (n == null) continue;

                        float t = n.threatCost + n.learnedCost;
                        if (t > maxThreat) maxThreat = t;
                    }
                }

                if (maxThreat <= 0f)
                    maxThreat = 1f; // hepsi 0 ise bölme hatası olmasın
            }
        }

        // 2) Her hücreyi çiz
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Node n = grid[x, y];
                if (n == null) continue;

                Color c = Color.white;

                if (drawHeatmap)
                {
                    // toplam tehdit = static threat + Q-learning'den gelen learnedCost
                    float totalThreat = n.threatCost + n.learnedCost;

                    // 0 -> yeşil, maxThreat -> kırmızı
                    float t = Mathf.Clamp01(totalThreat / maxThreat);
                    c = Color.Lerp(Color.green, Color.red, t);

                    // Yürünemez kareler için koyu ton
                    if (!n.isWalkable)
                        c *= 0.4f;
                }
                else
                {
                    // Eski basit görünüm: walkable = beyaz, değilse kırmızı
                    c = n.isWalkable ? Color.white : Color.red;
                }

                Gizmos.color = c;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * cellSize * 0.95f);
            }
        }
    }
}
