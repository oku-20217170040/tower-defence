using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// MetaState'in diske yazilabilir duz karsiligi.
    ///
    /// JsonUtility'nin sindirebilecegi sekilde yazildi: yalnizca public
    /// ALANLAR, ilkel tipler ve dizi. Dictionary veya property YOKTUR -
    /// JsonUtility ikisini de serilestiremez, sessizce bos birakir.
    ///
    /// [Serializable] System.SerializableAttribute'tur (mscorlib), Unity
    /// tipi degildir; Core'un noEngineReferences kuralini bozmaz ve
    /// JsonUtility'nin aradigi isarettir.
    /// </summary>
    [Serializable]
    public sealed class MetaSaveData
    {
        /// <summary>
        /// Kayit surumu. Ileride alan eklenirse eski kayitlari ayirt
        /// edebilmek icin bastan tutuluyor; surumsuz kayit formati her
        /// zaman bir gun sorun cikarir.
        /// </summary>
        public int version = CurrentVersion;

        public int biomass;
        public int lifetimeBiomass;
        public int runsPlayed;
        public int runsWon;
        public int intelLevel;

        /// <summary>Acilmis kule turlerinin int karsiliklari.</summary>
        public int[] unlockedTowers = new int[0];

        /// <summary>
        /// Kalici kule yukseltmesi olan kulelerin int karsiliklari
        /// (permanentStepValues ile PARALEL - JsonUtility Dictionary
        /// serilestiremedigi icin iki duz dizi kullanilir, bkz. MetaState).
        /// </summary>
        public int[] permanentStepKinds = new int[0];

        /// <summary>permanentStepKinds[i] icin satin alinmis kademe sayisi.</summary>
        public int[] permanentStepValues = new int[0];

        public const int CurrentVersion = 1;

        public static MetaSaveData From(MetaState state)
        {
            if (state == null) return new MetaSaveData();

            var list = new List<int>();
            foreach (var kind in state.UnlockedTowers)
                list.Add((int)kind);

            // Kayit dosyasi deterministik olsun; ayni durum ayni metni versin.
            list.Sort();

            var data = new MetaSaveData();
            data.version = CurrentVersion;
            data.biomass = state.Biomass;
            data.lifetimeBiomass = state.LifetimeBiomass;
            data.runsPlayed = state.RunsPlayed;
            data.runsWon = state.RunsWon;
            data.intelLevel = state.IntelLevel;
            data.unlockedTowers = list.ToArray();

            var stepKinds = new List<int>();
            var stepValues = new List<int>();
            foreach (var pair in state.PermanentSteps)
            {
                if (pair.Value <= 0) continue; // 0 kademe = varsayilan, kaydetmeye gerek yok
                stepKinds.Add((int)pair.Key);
                stepValues.Add(pair.Value);
            }
            data.permanentStepKinds = stepKinds.ToArray();
            data.permanentStepValues = stepValues.ToArray();

            return data;
        }

        /// <summary>
        /// Veriyi duruma cevirir. Bozuk/eksik alanlar guvenli degerlere
        /// kirpilir; hicbir kosulda istisna atmaz.
        /// </summary>
        public MetaState ToState()
        {
            var towers = new List<TowerKind>();
            if (unlockedTowers != null)
            {
                for (int i = 0; i < unlockedTowers.Length; i++)
                {
                    var kind = (TowerKind)unlockedTowers[i];
                    if (Enum.IsDefined(typeof(TowerKind), kind))
                        towers.Add(kind);
                }
            }

            var permanentKinds = new List<TowerKind>();
            if (permanentStepKinds != null)
            {
                for (int i = 0; i < permanentStepKinds.Length; i++)
                {
                    var kind = (TowerKind)permanentStepKinds[i];
                    if (Enum.IsDefined(typeof(TowerKind), kind))
                        permanentKinds.Add(kind);
                    else
                        permanentKinds.Add((TowerKind)(-1)); // hizayi bozmasin, Restore tanimasiz atar
                }
            }

            var state = new MetaState();
            state.Restore(biomass, lifetimeBiomass, runsPlayed, runsWon,
                          intelLevel, towers.ToArray(),
                          permanentKinds.ToArray(), permanentStepValues);
            return state;
        }
    }
}
