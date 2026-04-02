using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMover : MonoBehaviour, IWaveAffectable
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public int damageToBase = 1;

    // ✅ Wave çarpanları için temel değerleri sakla
    private float baseMoveSpeed;
    private int baseDamageToBase;
    private bool baseStatsCached;

    private GridManager grid;
    private Pathfinder pathfinder;
    private BaseHealth baseHealth;

    private List<Node> path;
    private int pathIndex;

    [HideInInspector] public Vector2Int startCoords;
    [HideInInspector] public Vector2Int baseCoords;

    public Vector2 CurrentMoveDirection { get; private set; } = Vector2.zero;

    private Rigidbody2D rb;

    private bool isFrozen;
    private float freezeTimer;
    public bool IsFrozen => isFrozen;

    private bool inClockAura;
    [Tooltip("Clock tower alanındayken animasyon çarpanı (0.1 - 1).")]
    public float clockAnimMultiplier = 0.5f;

    private Animator animator;

    private List<QLearningManager.EpisodeStep> episodeSteps;
    private Vector2Int lastCoords;

    private bool hasHitBase;
    public bool HasHitBase => hasHitBase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.isKinematic = true;

        animator = GetComponentInChildren<Animator>();

        // ✅ Base statları cachele
        CacheBaseStatsIfNeeded();
    }

    private void OnEnable()
    {
        EnemyManager.Register(this);
    }

    private void OnDisable()
    {
        EnemyManager.Unregister(this);
    }

    private void CacheBaseStatsIfNeeded()
    {
        if (baseStatsCached) return;
        baseMoveSpeed = moveSpeed;
        baseDamageToBase = damageToBase;
        baseStatsCached = true;
    }

    /// <summary>
    /// ✅ WaveSpawner her spawn sonrası bunu çağıracak.
    /// speedMul: hareket hızını çarpar
    /// dmgMul: base hasarını çarpar (int'e yuvarlanır, min 1)
    /// hpMul: EnemyHealth varsa uygular (opsiyonel)
    /// </summary>
    public void ApplyWaveMultipliers(float speedMul, float hpMul, float dmgMul)
    {
        CacheBaseStatsIfNeeded();

        // Güvenlik clamp
        speedMul = Mathf.Max(0.05f, speedMul);
        dmgMul = Mathf.Max(0.05f, dmgMul);
        hpMul = Mathf.Max(0.05f, hpMul);

        // Hız / hasarı base değerler üzerinden setle (birikmesin)
        moveSpeed = baseMoveSpeed * speedMul;

        int newDmg = Mathf.RoundToInt(baseDamageToBase * dmgMul);
        damageToBase = Mathf.Max(1, newDmg);

        // HP: Eğer EnemyHealth scriptin varsa uygula (yoksa sorun yok)
        var health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.ApplyWaveHpMultiplier(hpMul);
        }
    }

    private void Update()
    {
        UpdateAnimatorSpeed();
    }

    public void Initialize(
        GridManager grid,
        Pathfinder pathfinder,
        BaseHealth baseHealth,
        Vector2Int startCoords,
        Vector2Int baseCoords)
    {
        this.grid = grid;
        this.pathfinder = pathfinder;
        this.baseHealth = baseHealth;
        this.startCoords = startCoords;
        this.baseCoords = baseCoords;

        if (grid != null)
        {
            Node startNode = grid.GetNode(startCoords);
            if (startNode != null)
                transform.position = startNode.worldPosition;
        }

        RecalculatePathFromCurrentTile();
    }

    public void RecalculatePathFromCurrentTile()
    {
        if (grid == null || pathfinder == null)
            return;

        if (baseHealth != null && baseHealth.isDestroyed)
            return;

        Vector2Int currentCoords = grid.WorldToGridPosition(transform.position);
        startCoords = currentCoords;
        lastCoords = currentCoords;

        episodeSteps = new List<QLearningManager.EpisodeStep>();

        path = pathfinder.FindPath(currentCoords, baseCoords);
        pathIndex = 0;

        StopAllCoroutines();
        if (path != null && path.Count > 0)
        {
            StartCoroutine(FollowPath());
        }
    }

    public void RecalculatePath()
    {
        if (pathfinder == null)
            return;

        path = pathfinder.FindPath(startCoords, baseCoords);
        pathIndex = 0;

        StopAllCoroutines();
        if (path != null && path.Count > 0)
        {
            StartCoroutine(FollowPath());
        }
    }

    public void ApplyFreeze(float duration)
    {
        if (duration > freezeTimer)
            freezeTimer = duration;

        if (freezeTimer > 0f)
            isFrozen = true;

        UpdateAnimatorSpeed();
    }

    public void SetClockAura(bool active, float animMultiplier = 0.5f)
    {
        inClockAura = active;
        if (active)
            clockAnimMultiplier = Mathf.Clamp(animMultiplier, 0.05f, 1f);

        UpdateAnimatorSpeed();
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null) return;

        if (isFrozen)
            animator.speed = 0f;
        else if (inClockAura)
            animator.speed = clockAnimMultiplier;
        else
            animator.speed = 1f;
    }

    public void NotifyReachedBase()
    {
        if (hasHitBase) return;
        hasHitBase = true;

        CurrentMoveDirection = Vector2.zero;
        StopAllCoroutines();

        if (QLearningManager.Instance != null && episodeSteps != null && episodeSteps.Count > 0)
        {
            QLearningManager.Instance.ApplySuccessEpisode(episodeSteps);
        }

        GeneticManager.Instance?.NotifyEnemyReachedBase();

        Destroy(gameObject);
    }

    private IEnumerator FollowPath()
    {
        if (path == null || path.Count == 0)
            yield break;

        while (pathIndex < path.Count)
        {
            Node node = path[pathIndex];
            Vector3 targetPos = node.worldPosition;

            Vector2Int from = lastCoords;
            Vector2Int to = node.coordinates;
            int actionIndex = DirToAction(to - from);

            if (actionIndex != -1 &&
                QLearningManager.Instance != null &&
                episodeSteps != null)
            {
                episodeSteps.Add(new QLearningManager.EpisodeStep(from, actionIndex));
            }

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                Vector3 delta3 = targetPos - transform.position;
                Vector2 dir = new Vector2(delta3.x, delta3.y);
                CurrentMoveDirection = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.zero;

                if (isFrozen)
                {
                    CurrentMoveDirection = Vector2.zero;

                    freezeTimer -= Time.deltaTime;
                    if (freezeTimer <= 0f)
                    {
                        freezeTimer = 0f;
                        isFrozen = false;
                        UpdateAnimatorSpeed();
                    }

                    yield return null;
                    continue;
                }

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            CurrentMoveDirection = Vector2.zero;
            lastCoords = node.coordinates;
            pathIndex++;
            yield return null;
        }

        if (!hasHitBase)
        {
            hasHitBase = true;

            if (baseHealth != null && !baseHealth.isDestroyed)
            {
                baseHealth.TakeDamage(damageToBase);
            }

            if (QLearningManager.Instance != null && episodeSteps != null && episodeSteps.Count > 0)
            {
                QLearningManager.Instance.ApplySuccessEpisode(episodeSteps);
            }

            GeneticManager.Instance?.NotifyEnemyReachedBase();
        }

        CurrentMoveDirection = Vector2.zero;
        Destroy(gameObject);
    }

    public List<QLearningManager.EpisodeStep> GetEpisodeSteps()
    {
        return episodeSteps;
    }

    private int DirToAction(Vector2Int delta)
    {
        if (delta == Vector2Int.up) return 0;
        if (delta == Vector2Int.down) return 1;
        if (delta == Vector2Int.left) return 2;
        if (delta == Vector2Int.right) return 3;
        return -1;
    }
}
