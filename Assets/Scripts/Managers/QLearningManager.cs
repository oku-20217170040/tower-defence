using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Q-Learning y�neticisi.
/// - Enemy'lerin ge�ti�i state (h�cre) + action (y�n) ad�mlar�n� ��renir.
/// - Her episode sonunda (base'e ula�t� / kule alt�nda �ld�) Q tablosunu g�nceller.
/// - Q de�erlerini Node.learnedCost alan�na yans�t�r; Pathfinder learnedCost'u ek maliyet olarak kullan�r.
/// </summary>
public class QLearningManager : MonoBehaviour
{
    public static QLearningManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GridManager grid;
    // Inspector'dan da atayabilirsin; bo� b�rak�l�rsa Awake'te otomatik FindObjectOfType yapacak.

    [Header("Q-Learning Settings")]
    [Tooltip("��renme h�z� (0-1).")]
    public float alpha = 0.2f;

    [Tooltip("Gelecek �d�llere verilen �nem (0-1).")]
    public float gamma = 0.9f;

    [Tooltip("Her ad�m i�in k���k negatif �d�l.")]
    public float stepReward = -1f;

    [Tooltip("Base'e ula�ma �d�l�.")]
    public float successReward = 100f;

    [Tooltip("Kule taraf�ndan �ld�r�lme cezas�.")]
    public float deathReward = -50f;

    [Header("Learned Cost Mapping")]
    [Tooltip("Negatif Q de�erlerinin Node.learnedCost'a d�n���rken �arpan�.")]
    public float learnedScale = 1f;

    [Header("Debug")]
    [Tooltip("Episode i�lendi�inde konsola k�sa log yazs�n m�?")]
    public bool logEpisodes = false;

    /// <summary>
    /// Bir episode i�indeki tek bir ad�m:
    /// state = bulundu�u h�cre, action = 0:Up, 1:Down, 2:Left, 3:Right
    /// </summary>
    public struct EpisodeStep
    {
        public Vector2Int state;
        public int action;

        public EpisodeStep(Vector2Int s, int a)
        {
            state = s;
            action = a;
        }
    }

    // Q tablosu: "x_y_action" -> Q de�eri
    private readonly Dictionary<string, float> qTable =
        new Dictionary<string, float>();

    // JSON kayıt yolu
    private static string SavePath => Path.Combine(Application.persistentDataPath, "qtable.json");

    [System.Serializable]
    private struct QTableEntry { public string key; public float value; }

    [System.Serializable]
    private class QTableData { public List<QTableEntry> entries = new List<QTableEntry>(); }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Grid Inspector�dan atanmad�ysa sahneden otomatik bul
        if (grid == null)
            grid = FindObjectOfType<GridManager>();

        LoadQTable();
    }

    private void OnApplicationQuit() => SaveQTable();

    private void OnDestroy() { if (Instance == this) SaveQTable(); }

    public void SaveQTable()
    {
        var data = new QTableData();
        foreach (var kv in qTable)
            data.entries.Add(new QTableEntry { key = kv.Key, value = kv.Value });

        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
        if (logEpisodes)
            Debug.Log($"[QLearning] Saved {data.entries.Count} entries → {SavePath}");
    }

    public void LoadQTable()
    {
        if (!File.Exists(SavePath)) { if (logEpisodes) Debug.Log("[QLearning] No save found, fresh start."); return; }

        var data = JsonUtility.FromJson<QTableData>(File.ReadAllText(SavePath));
        qTable.Clear();
        if (data?.entries != null)
            foreach (var e in data.entries) qTable[e.key] = e.value;

        Debug.Log($"[QLearning] Loaded {qTable.Count} entries from {SavePath}");
    }

    public void ClearQTable()
    {
        qTable.Clear();
        if (File.Exists(SavePath)) File.Delete(SavePath);
        UpdateLearnedCostsOnGrid();
        Debug.Log("[QLearning] Q-table cleared.");
    }

    // ------------------------------------------------------------
    //  EPISODE SONU: DI�ARIDAN �A�RILAN FONKS�YONLAR
    // ------------------------------------------------------------

    /// <summary>
    /// Enemy base'e ula��nca �a��r.
    /// </summary>
    public void ApplySuccessEpisode(List<EpisodeStep> steps)
    {
        ApplyEpisodeReward(steps, successReward, "SUCCESS");
    }

    /// <summary>
    /// Enemy kule taraf�ndan �ld�r�l�nce �a��r.
    /// </summary>
    public void ApplyDeathEpisode(List<EpisodeStep> steps)
    {
        ApplyEpisodeReward(steps, deathReward, "DEATH");
    }

    // ------------------------------------------------------------
    //  ANA Q-G�NCELLEME
    // ------------------------------------------------------------

    private void ApplyEpisodeReward(List<EpisodeStep> steps, float finalReward, string tag)
    {
        if (steps == null || steps.Count == 0)
            return;

        // Episode'i sondan ba�a do�ru geziyoruz
        float reward = finalReward;

        for (int i = steps.Count - 1; i >= 0; i--)
        {
            EpisodeStep step = steps[i];
            string key = MakeKey(step.state, step.action);

            float oldQ = 0f;
            qTable.TryGetValue(key, out oldQ);

            // Bir sonraki state i�in max Q
            float maxNextQ = 0f;
            if (i < steps.Count - 1)
            {
                Vector2Int nextState = steps[i + 1].state;
                maxNextQ = GetMaxQ(nextState);
            }

            float newQ = oldQ + alpha * (reward + gamma * maxNextQ - oldQ);
            qTable[key] = newQ;

            // Her ad�mdan sonra k���k bir stepReward ekleyelim (negatif)
            reward += stepReward;
        }

        if (logEpisodes)
        {
            Debug.Log($"[QLearning] Episode processed ({tag}), steps = {steps.Count}, finalReward = {finalReward}");
        }

        // Q tablosu g�ncellendi -> Node.learnedCost de�erlerini update et
        UpdateLearnedCostsOnGrid();
    }

    // Belirli bir state i�in t�m aksiyonlar aras�ndaki en b�y�k Q
    private float GetMaxQ(Vector2Int state)
    {
        float maxQ = 0f;
        bool hasAny = false;

        for (int action = 0; action < 4; action++)
        {
            string key = MakeKey(state, action);
            float q;
            if (qTable.TryGetValue(key, out q))
            {
                if (!hasAny || q > maxQ)
                {
                    maxQ = q;
                    hasAny = true;
                }
            }
        }

        return hasAny ? maxQ : 0f;
    }

    private string MakeKey(Vector2Int state, int action)
    {
        return state.x + "_" + state.y + "_" + action;
    }

    // ------------------------------------------------------------
    //  Q -> Node.learnedCost HARITALAMA
    // ------------------------------------------------------------

    private void UpdateLearnedCostsOnGrid()
    {
        if (grid == null || grid.grid == null)
            return;

        int width = grid.grid.GetLength(0);
        int height = grid.grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid.grid[x, y];
                if (node == null) continue;

                Vector2Int state = node.coordinates;

                // Bu node i�in mevcut t�m aksiyonlar�n Q de�erlerine bak
                bool hasAny = false;
                float minQ = 0f;

                for (int action = 0; action < 4; action++)
                {
                    string key = MakeKey(state, action);
                    float q;
                    if (qTable.TryGetValue(key, out q))
                    {
                        if (!hasAny || q < minQ)
                        {
                            minQ = q;
                            hasAny = true;
                        }
                    }
                }

                // E�er hi� Q de�eri yoksa veya Q pozitifse -> learnedCost = 0
                if (!hasAny || minQ >= 0f)
                {
                    node.learnedCost = 0f;
                }
                else
                {
                    // Daha NEGAT�F Q = daha tehlikeli yol => daha B�Y�K cost
                    node.learnedCost = -minQ * learnedScale;
                }
            }
        }
    }
}
