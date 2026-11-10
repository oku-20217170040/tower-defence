using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// <see cref="SpeedCycle"/>'i Time.timeScale'e baglar.
    ///
    /// Run bittiginde hizi x1'e dondurur: kazandin/kaybettin ekrani
    /// x3'te acilip bir sonraki run da hizli baslarsa oyuncu bunu
    /// bir hata sanir.
    /// </summary>
    public sealed class GameSpeedControl : MonoBehaviour
    {
        public RunController run;

        /// <summary>Klavyeden hiz degistirme (masaustu test icin).</summary>
        public KeyCode cycleKey = KeyCode.Space;

        private readonly SpeedCycle cycle = new SpeedCycle();

        /// <summary>Arayuzun okudugu etiket: "x1", "x2", "x3".</summary>
        public string Label
        {
            get { return cycle.Label; }
        }

        public float Current
        {
            get { return cycle.Current; }
        }

        private void Awake()
        {
            if (run == null) run = FindFirstObjectByType<RunController>();
        }

        private void OnEnable()
        {
            Apply();
            if (run != null) run.RunEnded += OnRunEnded;
        }

        private void OnDisable()
        {
            if (run != null) run.RunEnded -= OnRunEnded;

            // Sahne kapanirken timeScale'i birakma: bir sonraki sahne
            // hizli baslar ve sebebi bulunmasi zor bir hata olur.
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(cycleKey)) Cycle();
        }

        /// <summary>Bir sonraki kademeye gecer ve uygular.</summary>
        public void Cycle()
        {
            cycle.Next();
            Apply();
        }

        private void OnRunEnded(RunState state)
        {
            cycle.Reset();
            Apply();
        }

        private void Apply()
        {
            Time.timeScale = cycle.Current;
        }
    }
}
