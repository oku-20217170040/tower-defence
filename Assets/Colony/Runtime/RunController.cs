using System;
using System.Collections;
using UnityEngine;

namespace Colony.Runtime
{
    public enum RunState
    {
        Running = 0,
        Won = 1,
        Lost = 2
    }

    /// <summary>
    /// Bir run'i yonetir: dalgalari sirayla baslatir, kazanma ve kaybetme
    /// kosullarini takip eder.
    ///
    /// Dalgalar artik sonsuz degil. Run 12-15 dalgadir (spec 2.1) ve bocek
    /// cani her dalga artar - sabit bir tuzagin zamanla yetersiz kalmasi
    /// icin gereken baski budur.
    /// </summary>
    public sealed class RunController : MonoBehaviour
    {
        public ColonyWorld world;
        public BugSpawner spawner;
        public NestHealth nest;

        [Header("Run")]
        [Tooltip("Run kac dalga surer.")]
        public int totalWaves = 15;

        [Tooltip("Dalga arasi bekleme (saniye).")]
        public float delayBetweenWaves = 2f;

        [Header("Zorluk")]
        [Tooltip("Ilk dalgadaki bocek cani.")]
        public float baseBugHp = 10f;

        [Tooltip("Her dalga canin artis orani. 0.15 = dalga basina %15.")]
        public float hpGrowthPerWave = 0.15f;

        [Tooltip("Ilk dalgadaki bocek sayisi. 8 olcumle secildi: 10 iken oyuncu " +
                 "yeterli kuleyi kuramadan 4. dalgada kaybediyordu.")]
        public int baseBugsPerWave = 8;

        [Tooltip("Her dalga eklenen bocek sayisi.")]
        public float bugsAddedPerWave = 1f;

        public int CurrentWave { get; private set; }
        public RunState State { get; private set; } = RunState.Running;

        /// <summary>Run bitti. Argument sonuctur.</summary>
        public event Action<RunState> RunEnded;

        /// <summary>Dalga degisti. Argumentler (guncel, toplam).</summary>
        public event Action<int, int> WaveChanged;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<ColonyWorld>();
            if (spawner == null) spawner = FindFirstObjectByType<BugSpawner>();
            if (nest == null) nest = FindFirstObjectByType<NestHealth>();
        }

        private void OnEnable()
        {
            if (nest != null) nest.Emptied += OnNestEmptied;
        }

        private void OnDisable()
        {
            if (nest != null) nest.Emptied -= OnNestEmptied;
        }

        private void Start()
        {
            StartCoroutine(RunWaves());
        }

        /// <summary>Bu dalgadaki bocegin cani.</summary>
        public float HpForWave(int wave)
        {
            int index = wave < 1 ? 0 : wave - 1;
            return baseBugHp * (1f + hpGrowthPerWave * index);
        }

        /// <summary>Bu dalgadaki bocek sayisi.</summary>
        public int CountForWave(int wave)
        {
            int index = wave < 1 ? 0 : wave - 1;
            return Mathf.RoundToInt(baseBugsPerWave + bugsAddedPerWave * index);
        }

        private IEnumerator RunWaves()
        {
            while (world == null || world.Grid == null)
                yield return null;

            while (State == RunState.Running && CurrentWave < totalWaves)
            {
                CurrentWave++;
                if (WaveChanged != null) WaveChanged(CurrentWave, totalWaves);

                // Yem kalici degildir; her dalga tazelenir (spec 3.3).
                world.ApplyLures();

                yield return spawner.SpawnWave(
                    CountForWave(CurrentWave),
                    HpForWave(CurrentWave));

                while (spawner.AliveCount > 0 && State == RunState.Running)
                    yield return null;

                if (State != RunState.Running) break;

                // Dalga sonunda kullanilmayan izler unutulur.
                world.Pheromone.Decay();

                yield return new WaitForSeconds(delayBetweenWaves);
            }

            if (State == RunState.Running)
                Finish(RunState.Won);
        }

        private void OnNestEmptied()
        {
            if (State == RunState.Running) Finish(RunState.Lost);
        }

        private void Finish(RunState result)
        {
            State = result;
            Debug.Log("[RunController] Run bitti: " + result +
                      " (dalga " + CurrentWave + "/" + totalWaves + ")");

            if (RunEnded != null) RunEnded(result);
        }
    }
}
