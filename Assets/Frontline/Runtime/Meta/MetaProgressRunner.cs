using System;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Meta ilerlemeyi run'a baglayan koprü. Sahnede tek bir bos nesneye
    /// eklenir.
    ///
    /// Isi sadece budur: acilista kaydi yukle, run bitisini dinle,
    /// biyokutleyi hesaplayip kaydet. Hesabin kendisi Core'dadir
    /// (BiomassLedger / MetaState); burasi yalnizca kablolamadir.
    ///
    /// RunController'a DOKUNMAZ; ona yalnizca RunEnded event'iyle baglanir.
    /// </summary>
    public sealed class MetaProgressRunner : MonoBehaviour
    {
        [Tooltip("Dinlenecek run. Bos birakilirsa sahnede aranir.")]
        public RunController run;

        [Tooltip("Oldurme sayisini okuyacagimiz spawner. Bos birakilirsa " +
                 "sahnede aranir.")]
        public UnitSpawner spawner;

        [Tooltip("Kapaliyken diske yazilmaz. Denge olcumu yaparken kalici " +
                 "kaydin kirlenmemesi icin.")]
        public bool persist = true;

        /// <summary>Yuklenmis kalici durum.</summary>
        public MetaState State { get; private set; }

        /// <summary>Son run'da kazanilan biyokutle. Sonuc ekrani bunu okur.</summary>
        public int LastEarned { get; private set; }

        /// <summary>Run bitti ve kazanc islendi. Argument kazanilan miktardir.</summary>
        public event Action<int> BiomassAwarded;

        private bool awarded;

        private void Awake()
        {
            if (run == null) run = FindFirstObjectByType<RunController>();
            if (spawner == null) spawner = FindFirstObjectByType<UnitSpawner>();

            State = persist ? MetaProgressStore.Load() : new MetaState();
        }

        private void OnEnable()
        {
            // Abonelik birakilmazsa sahne yeniden yuklendiginde olu
            // RunController'lar dinlenmeye devam eder; bu projede daha
            // once tam olarak bu hata cikti.
            if (run != null) run.RunEnded += OnRunEnded;
        }

        private void OnDisable()
        {
            if (run != null) run.RunEnded -= OnRunEnded;
        }

        private void OnRunEnded(RunState result)
        {
            // Run basina tek odul; event iki kez tetiklenirse cifte odeme olmaz.
            if (awarded) return;
            awarded = true;

            int waves = run != null ? run.CurrentWave : 0;
            int kills = spawner != null ? spawner.TotalKilled : 0;
            bool won = result == RunState.Won;

            LastEarned = State.RegisterRun(waves, kills, won);

            if (persist) MetaProgressStore.Save(State);

            Debug.Log("[MetaProgressRunner] Run " + result + " - kazanilan biyokutle: " +
                      LastEarned + " (toplam " + State.Biomass + ")");

            if (BiomassAwarded != null) BiomassAwarded(LastEarned);
        }

        /// <summary>Menuden kule acmak icin. Basarili olursa hemen kaydeder.</summary>
        public bool TryUnlock(TowerKind kind)
        {
            if (State == null) return false;
            if (!State.TryUnlock(kind)) return false;

            if (persist) MetaProgressStore.Save(State);
            return true;
        }

        /// <summary>Menuden istihbarat yukseltmek icin.</summary>
        public bool TryUpgradeIntel()
        {
            if (State == null) return false;
            if (!State.TryUpgradeIntel()) return false;

            if (persist) MetaProgressStore.Save(State);
            return true;
        }

        /// <summary>
        /// Ana menu "GUNCELLE" ekranindan bir kulenin kalici kademesini
        /// satin almayi dener. Basarirsa hemen kaydeder - oyuncu ekrandan
        /// cikip uygulamayi kapatsa bile satin alma kaybolmaz.
        /// </summary>
        public bool TryUpgradeTowerPermanent(TowerKind kind)
        {
            if (State == null) return false;
            if (!State.TryUpgradeTowerPermanent(kind)) return false;

            if (persist) MetaProgressStore.Save(State);
            return true;
        }
    }
}
