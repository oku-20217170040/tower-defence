using UnityEngine;

public class EnemyLifeTracker : MonoBehaviour
{
    private WaveSpawner spawner;
    private bool notified;

    public void Init(WaveSpawner waveSpawner)
    {
        spawner = waveSpawner;
    }

    void OnDisable()
    {
        if (notified) return;
        notified = true;

        if (spawner != null)
            spawner.NotifyEnemyDiedOrDespawned();
    }

    void OnDestroy()
    {
        if (notified) return;
        notified = true;

        if (spawner != null)
            spawner.NotifyEnemyDiedOrDespawned();
    }
}
