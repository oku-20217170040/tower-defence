using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Run'lar arasi kalici durum (spec 6): toplam biyokutle, acilmis
    /// kuleler ve istihbarat seviyesi.
    ///
    /// Gecersiz islemler sessizce yutulmaz - false doner ve durum
    /// DEGISMEZ. Kismi harcama diye bir sey yoktur.
    ///
    /// Saf C#'tir; dosya G/C bilmez. Kaydetme isi Runtime katmanindadir.
    /// </summary>
    public sealed class MetaState
    {
        private readonly HashSet<TowerKind> unlocked = new HashSet<TowerKind>();

        /// <summary>
        /// Kule basina satin alinmis KALICI yukseltme adimi (0..
        /// PermanentUpgradeCatalog.MaxPermanentSteps). 0 = hic alinmamis,
        /// kule normal seviye 1'de baslar. Sozlukte olmayan kule 0 sayilir.
        /// </summary>
        private readonly Dictionary<TowerKind, int> permanentSteps = new Dictionary<TowerKind, int>();

        /// <summary>Eldeki toplam biyokutle.</summary>
        public int Biomass { get; private set; }

        /// <summary>Oyun boyunca kazanilan toplam biyokutle (harcama dusmez).</summary>
        public int LifetimeBiomass { get; private set; }

        /// <summary>Tamamlanan run sayisi (kazanilan + kaybedilen).</summary>
        public int RunsPlayed { get; private set; }

        /// <summary>Kazanilan run sayisi.</summary>
        public int RunsWon { get; private set; }

        /// <summary>Istihbarat seviyesi. 0..UnlockCatalog.MaxIntelLevel.</summary>
        public int IntelLevel { get; private set; }

        /// <summary>Biyokutle degisti. Argument guncel bakiyedir.</summary>
        public event Action<int> BiomassChanged;

        /// <summary>Yeni bir kule acildi.</summary>
        public event Action<TowerKind> TowerUnlocked;

        /// <summary>Temiz baslangic durumu: yalnizca baslangic kuleleri acik.</summary>
        public MetaState()
        {
            var starters = UnlockCatalog.StartingTowers;
            for (int i = 0; i < starters.Length; i++)
                unlocked.Add(starters[i]);
        }

        /// <summary>Acilmis kuleler (salt okunur).</summary>
        public IEnumerable<TowerKind> UnlockedTowers
        {
            get { return unlocked; }
        }

        public int UnlockedCount
        {
            get { return unlocked.Count; }
        }

        public bool IsUnlocked(TowerKind kind)
        {
            return unlocked.Contains(kind);
        }

        /// <summary>Biyokutle ekler. Pozitif olmayan miktar yok sayilir.</summary>
        public void Earn(int amount)
        {
            if (amount <= 0) return;

            Biomass += amount;
            LifetimeBiomass += amount;
            RaiseBiomass();
        }

        public bool CanAfford(int cost)
        {
            return cost >= 0 && cost <= Biomass;
        }

        /// <summary>
        /// Yeterli biyokutle varsa harcar. Yetmezse HICBIR SEY degistirmez.
        /// </summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0) return false;
            if (cost > Biomass) return false;

            Biomass -= cost;
            RaiseBiomass();
            return true;
        }

        /// <summary>
        /// Kuleyi acmayi dener. Zaten aciksa PARA ALMAZ ve false doner -
        /// "islem yapilmadi" demektir; cift tiklama oyuncuyu soymaz.
        /// Satin alinamaz bir kule icin de false doner.
        /// </summary>
        public bool TryUnlock(TowerKind kind)
        {
            if (unlocked.Contains(kind)) return false;

            int cost = UnlockCatalog.CostOf(kind);
            if (cost < 0) return false;
            if (!TrySpend(cost)) return false;

            unlocked.Add(kind);
            if (TowerUnlocked != null) TowerUnlocked(kind);
            return true;
        }

        /// <summary>
        /// Istihbarat seviyesini bir artirmayi dener. Tavandaysa veya
        /// biyokutle yetmiyorsa false doner ve bakiye degismez.
        /// </summary>
        public bool TryUpgradeIntel()
        {
            int cost = UnlockCatalog.IntelUpgradeCost(IntelLevel);
            if (cost < 0) return false;
            if (!TrySpend(cost)) return false;

            IntelLevel++;
            return true;
        }

        /// <summary>
        /// Bu kule icin satin alinmis kalici yukseltme adimi (0..
        /// PermanentUpgradeCatalog.MaxPermanentSteps).
        /// </summary>
        public int PermanentStepOf(TowerKind kind)
        {
            int step;
            return permanentSteps.TryGetValue(kind, out step) ? step : 0;
        }

        /// <summary>
        /// Bu kulenin, kalici yukseltmeler sayilinca, YENI bir run'da
        /// hangi seviyede kurulacagi (1..Emplacement.MaxLevel). Runtime
        /// katmani (TowerPlacer) kule kurarken bunu okuyup Emplacement'i
        /// dogrudan bu seviyeye getirir - Core, Emplacement'i (Runtime/
        /// MonoBehaviour) bilmez, yalnizca sayiyi doner.
        /// </summary>
        public int StartingLevelOf(TowerKind kind)
        {
            return 1 + PermanentStepOf(kind);
        }

        /// <summary>
        /// Bir kulenin bir sonraki kalici kademesini satin almayi dener.
        /// Tavandaysa, katalogda yoksa veya biyokutle yetmiyorsa false
        /// doner ve HICBIR SEY degismez (MetaState'in genel kurali:
        /// kismi/basarisiz islem sessizce yutulmaz).
        /// </summary>
        public bool TryUpgradeTowerPermanent(TowerKind kind)
        {
            int step = PermanentStepOf(kind);
            int cost = PermanentUpgradeCatalog.CostFor(kind, step);
            if (cost < 0) return false;
            if (!TrySpend(cost)) return false;

            permanentSteps[kind] = step + 1;
            return true;
        }

        /// <summary>
        /// Bir run'in bitisini isler: sayaclari ve biyokutleyi gunceller.
        /// Kazanilan biyokutleyi doner.
        /// </summary>
        public int RegisterRun(int wavesCompleted, int kills, bool won)
        {
            RunsPlayed++;
            if (won) RunsWon++;

            int earned = BiomassLedger.EarnedWithFloor(wavesCompleted, kills, won);
            Earn(earned);
            return earned;
        }

        /// <summary>
        /// Kayittan yuklerken kullanilir. Bozuk degerler guvenli araliga
        /// kirpilir; baslangic kuleleri her kosulda acik kalir ki bozuk bir
        /// kayit oyuncuyu oynanamaz bir duruma dusuremesin.
        /// </summary>
        internal void Restore(int biomass, int lifetime, int runsPlayed,
                              int runsWon, int intelLevel, TowerKind[] towers)
        {
            Restore(biomass, lifetime, runsPlayed, runsWon, intelLevel, towers, null, null);
        }

        /// <summary>
        /// Kalici kule kademeleri de dahil tam geri yukleme. permanentKinds/
        /// permanentValues paraleldir (JsonUtility Dictionary serilestiremedigi
        /// icin MetaSaveData iki duz dizi tutar). Bozuk/eksik/tutarsiz uzunluk
        /// sessizce yok sayilir - kayit hicbir kosulda oyunu acilmaz yapmamali.
        /// </summary>
        internal void Restore(int biomass, int lifetime, int runsPlayed,
                              int runsWon, int intelLevel, TowerKind[] towers,
                              TowerKind[] permanentKinds, int[] permanentValues)
        {
            Biomass = biomass < 0 ? 0 : biomass;
            LifetimeBiomass = lifetime < Biomass ? Biomass : lifetime;
            RunsPlayed = runsPlayed < 0 ? 0 : runsPlayed;
            RunsWon = runsWon < 0 ? 0 : runsWon;
            if (RunsWon > RunsPlayed) RunsWon = RunsPlayed;

            int maxIntel = UnlockCatalog.MaxIntelLevel;
            IntelLevel = intelLevel < 0 ? 0 : (intelLevel > maxIntel ? maxIntel : intelLevel);

            unlocked.Clear();
            var starters = UnlockCatalog.StartingTowers;
            for (int i = 0; i < starters.Length; i++)
                unlocked.Add(starters[i]);

            if (towers != null)
            {
                for (int i = 0; i < towers.Length; i++)
                {
                    if (Enum.IsDefined(typeof(TowerKind), towers[i]))
                        unlocked.Add(towers[i]);
                }
            }

            permanentSteps.Clear();
            if (permanentKinds != null && permanentValues != null &&
                permanentKinds.Length == permanentValues.Length)
            {
                for (int i = 0; i < permanentKinds.Length; i++)
                {
                    if (!Enum.IsDefined(typeof(TowerKind), permanentKinds[i])) continue;

                    int step = permanentValues[i];
                    int max = PermanentUpgradeCatalog.MaxPermanentSteps;
                    if (step < 0) step = 0;
                    if (step > max) step = max;

                    permanentSteps[permanentKinds[i]] = step;
                }
            }
        }

        /// <summary>Kalici yukseltmesi olan kuleler (kayit icin, salt okunur).</summary>
        public IEnumerable<KeyValuePair<TowerKind, int>> PermanentSteps
        {
            get { return permanentSteps; }
        }

        private void RaiseBiomass()
        {
            if (BiomassChanged != null) BiomassChanged(Biomass);
        }
    }
}
