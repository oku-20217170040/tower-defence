using UnityEngine;

/// <summary>
/// Oyun içi skor takibi.
/// EndGameManager.TriggerWin/Lose() → skor panelinde gösterilir.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int   TotalKills      { get; private set; }
    public int   GoldEarned      { get; private set; }
    public int   WavesCompleted  { get; private set; }
    public float TimeSurvived    { get; private set; }

    private bool running;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        running = true;
    }

    private void Update()
    {
        if (running)
            TimeSurvived += Time.deltaTime;
    }

    public void RegisterKill(int goldValue = 0)
    {
        TotalKills++;
        GoldEarned += goldValue;
    }

    public void RegisterWaveComplete()
    {
        WavesCompleted++;
    }

    public void Stop()
    {
        running = false;
    }

    /// <summary>
    /// Formatlanmış skor özeti — EndGameManager panelinde kullanılır.
    /// </summary>
    public string GetSummary()
    {
        int minutes = Mathf.FloorToInt(TimeSurvived / 60f);
        int seconds = Mathf.FloorToInt(TimeSurvived % 60f);
        return $"Kills: {TotalKills}   Gold: {GoldEarned}   Waves: {WavesCompleted}   Time: {minutes:00}:{seconds:00}";
    }
}
