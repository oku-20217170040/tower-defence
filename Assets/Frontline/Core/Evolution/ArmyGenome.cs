using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Ordunun KALITSAL durumu: run'lar arasinda tasinan tek sey.
    ///
    /// Iki bolumu var:
    ///   1) Birlik turu paylari - dalga bilesimi (UnitRoster.Default'un
    ///      evrilen hali).
    ///   2) Kule turu direncleri - oyuncunun kule turlerine karsi ordunun
    ///      dayanikliligi. 1 = notr.
    ///
    /// EN ONEMLI KURAL (spec bolum 6, "beceri tavani korunmalidir"):
    /// ikisinin de TOPLAM GUC BUTCESI SABITTIR. Ordu run'lar boyunca
    /// guclenmez; yalnizca dagilimi kayar. Nemesis islevi bundan gelir -
    /// oyuncunun favori stratejisi asinir, ama oyun adaletsizlesmez.
    /// Ordu genel olarak guclenseydi oyuncunun ogrendigi her sey
    /// degersizlesirdi.
    ///
    /// Saf C#'tir; Unity tipi kullanmaz.
    /// </summary>
    public sealed class ArmyGenome
    {
        /// <summary>
        /// Kule turune karsi direnc. Indeks = (int)TowerKind.
        /// 1 = notr, 1'den buyuk = o kuleye karsi dayanikli, kucuk = kirilgan.
        /// Toplami her zaman EvolutionPressure.ResistanceBudget'tir.
        /// </summary>
        public readonly float[] Resistance = new float[EvolutionPressure.TowerKindCount];

        /// <summary>
        /// Birlik turu paylari. Indeks = (int)UnitType.
        /// Infantry (indeks 0) TUREVDIR: kalan paydir, dogrudan yazilmaz.
        /// </summary>
        public readonly float[] Share = new float[EvolutionPressure.UnitTypeCount];

        /// <summary>Bu genomun kac run'dan gectigi. Yalnizca kayit/teshis icin.</summary>
        public int Generation;

        private ArmyGenome() { }

        /// <summary>
        /// Baslangic genomu: butun direncler notr, paylar bugunku olculmus
        /// bilesim (UnitRoster: izci %25, asker %20, kralice %5, gerisi piyade).
        /// Evrim buradan baslar; ilk run'da oyuncu bugunku oyunu oynar.
        /// </summary>
        public static ArmyGenome Default()
        {
            var genome = new ArmyGenome();
            for (int i = 0; i < genome.Resistance.Length; i++)
            {
                genome.Resistance[i] = EvolutionPressure.BaselineResistance;
            }

            genome.Share[(int)UnitType.Scout] = UnitRoster.ScoutShare;
            genome.Share[(int)UnitType.Soldier] = UnitRoster.SoldierShare;
            genome.Share[(int)UnitType.Queen] = UnitRoster.QueenShare;
            genome.RecomputeInfantry();
            return genome;
        }

        public ArmyGenome Clone()
        {
            var copy = new ArmyGenome { Generation = Generation };
            Array.Copy(Resistance, copy.Resistance, Resistance.Length);
            Array.Copy(Share, copy.Share, Share.Length);
            return copy;
        }

        public float ResistanceTo(TowerKind kind)
        {
            return Resistance[(int)kind];
        }

        public float ShareOf(UnitType type)
        {
            return Share[(int)type];
        }

        /// <summary>Piyade disi turlerin toplam payi.</summary>
        public float NonInfantryShare()
        {
            float sum = 0f;
            for (int i = 0; i < Share.Length; i++)
            {
                if (i == (int)UnitType.Infantry) continue;
                sum += Share[i];
            }
            return sum;
        }

        /// <summary>Direnclerin toplami. Butce korunuyorsa sabittir.</summary>
        public float ResistanceTotal()
        {
            float sum = 0f;
            for (int i = 0; i < Resistance.Length; i++) sum += Resistance[i];
            return sum;
        }

        /// <summary>
        /// Bilesimin agirlikli gucu: sum(pay * guc agirligi). Piyade payi
        /// turevdir, o da hesaba girer. Evrim bu sayiyi degistiremez.
        /// </summary>
        public float CompositionPower()
        {
            float power = 0f;
            for (int i = 0; i < Share.Length; i++)
            {
                power += Share[i] * EvolutionPressure.PowerWeight((UnitType)i);
            }
            return power;
        }

        /// <summary>
        /// GENOMU BUTCEYE OTURTUR. Evrimin her adiminda cagrilir.
        ///
        /// Sirasiyla:
        ///   1) Direncler sinirlara kirpilir ve kalan butce sinira
        ///      dayanmayanlara dagitilir (dayanmis olanlar disarida
        ///      birakilir, yoksa kirpma anlamsizlasirdi).
        ///   2) Paylar sinirlara kirpilir.
        ///   3) Paylar, bilesim gucu hedef butceye esit olacak sekilde
        ///      olceklenir; sonra tekrar kirpilir.
        ///
        /// Kirpma en sonda oldugu icin, bir pay sinira DAYANIRSA butce
        /// mikron duzeyinde kayabilir. Bu bilincli bir tercihtir:
        /// SINIRLAR butceden onceliklidir - ordu asla tek turden olusamaz
        /// ve hicbir direnc yenilmezlik seviyesine cikamaz.
        /// </summary>
        public void Normalize()
        {
            NormalizeResistance();
            ClampShares();
            NormalizeCompositionPower();
            ClampShares();
            RecomputeInfantry();
        }

        private void NormalizeResistance()
        {
            // Kirp + kalan butceyi sinira dayanmayanlara esit dagit.
            // Birkac tur yeter: her turda en az bir eleman sabitlenir.
            for (int pass = 0; pass < EvolutionPressure.TowerKindCount + 1; pass++)
            {
                float sum = 0f;
                for (int i = 0; i < Resistance.Length; i++)
                {
                    if (Resistance[i] < EvolutionPressure.MinResistance)
                        Resistance[i] = EvolutionPressure.MinResistance;
                    else if (Resistance[i] > EvolutionPressure.MaxResistance)
                        Resistance[i] = EvolutionPressure.MaxResistance;

                    sum += Resistance[i];
                }

                float diff = EvolutionPressure.ResistanceBudget - sum;
                if (diff > -1e-6f && diff < 1e-6f) return;

                int free = 0;
                for (int i = 0; i < Resistance.Length; i++)
                {
                    if (IsFrozen(i, diff)) continue;
                    free++;
                }

                if (free == 0) return; // Hepsi sinirda: sinir butceden onceliklidir.

                float step = diff / free;
                for (int i = 0; i < Resistance.Length; i++)
                {
                    if (IsFrozen(i, diff)) continue;
                    Resistance[i] += step;
                }
            }
        }

        /// <summary>
        /// Butceyi ARTIRMAMIZ gerekiyorsa tavana dayanmis olan, AZALTMAMIZ
        /// gerekiyorsa tabana dayanmis olan eleman donduruldu sayilir.
        /// </summary>
        private bool IsFrozen(int index, float diff)
        {
            bool atTop = Resistance[index] >= EvolutionPressure.MaxResistance - 1e-6f;
            bool atBottom = Resistance[index] <= EvolutionPressure.MinResistance + 1e-6f;
            if (diff > 0f && atTop) return true;
            if (diff < 0f && atBottom) return true;
            return false;
        }

        private void ClampShares()
        {
            for (int i = 0; i < Share.Length; i++)
            {
                if (i == (int)UnitType.Infantry) continue;
                if (Share[i] < 0f) Share[i] = 0f;
                else if (Share[i] > EvolutionPressure.MaxUnitShare) Share[i] = EvolutionPressure.MaxUnitShare;
            }

            float total = NonInfantryShare();
            if (total > EvolutionPressure.MaxNonInfantryShare && total > 1e-6f)
            {
                float k = EvolutionPressure.MaxNonInfantryShare / total;
                for (int i = 0; i < Share.Length; i++)
                {
                    if (i == (int)UnitType.Infantry) continue;
                    Share[i] *= k;
                }
            }

            RecomputeInfantry();
        }

        /// <summary>
        /// Paylari, agirlikli bilesim gucu hedefe esit olacak sekilde
        /// olcekler.
        ///
        /// Guc = 1 + sum_{piyade disi} pay * (agirlik - 1) oldugundan tek
        /// bir k carpani yeterlidir. Payda sifira yakinsa (bilesim zaten
        /// notr, orn. herkes piyade) olcekleme yapilmaz: sifira bolmek
        /// yerine dokunmamak dogrusudur.
        /// </summary>
        private void NormalizeCompositionPower()
        {
            float target = TargetCompositionPower;

            float d = 0f;
            for (int i = 0; i < Share.Length; i++)
            {
                if (i == (int)UnitType.Infantry) continue;
                d += Share[i] * (EvolutionPressure.PowerWeight((UnitType)i) - 1f);
            }

            if (d < 0.01f && d > -0.01f) return;

            float k = (target - 1f) / d;
            if (k < 0f) return; // Isaret donmesi: bilesimi ters cevirmek yerine birak.

            for (int i = 0; i < Share.Length; i++)
            {
                if (i == (int)UnitType.Infantry) continue;
                Share[i] *= k;
            }

            RecomputeInfantry();
        }

        private void RecomputeInfantry()
        {
            float rest = 1f - NonInfantryShare();
            if (rest < 0f) rest = 0f;
            Share[(int)UnitType.Infantry] = rest;
        }

        /// <summary>
        /// Hedef bilesim gucu: baslangic bilesiminin gucu (1,625).
        /// Sabit olarak yazilmadi, UnitRoster'daki OLCULMUS paylardan
        /// turetildi - roster degisirse butce de onunla birlikte degisir,
        /// ikisi sessizce ayrisamaz.
        /// </summary>
        public static readonly float TargetCompositionPower = ComputeDefaultPower();

        private static float ComputeDefaultPower()
        {
            float nonInfantry = UnitRoster.ScoutShare + UnitRoster.SoldierShare + UnitRoster.QueenShare;

            float power = UnitRoster.ScoutShare * EvolutionPressure.PowerWeight(UnitType.Scout)
                        + UnitRoster.SoldierShare * EvolutionPressure.PowerWeight(UnitType.Soldier)
                        + UnitRoster.QueenShare * EvolutionPressure.PowerWeight(UnitType.Queen)
                        + (1f - nonInfantry) * EvolutionPressure.PowerWeight(UnitType.Infantry);

            return power;
        }

        /// <summary>
        /// Genomu simulatorun/runtime'in anladigi dalga bilesimine cevirir.
        /// Piyade listede yoktur; kalan pay zaten piyadeye gider (UnitMix
        /// belgesindeki kural). Ilk dalga numaralari UnitRoster'dan alinir -
        /// evrim PAYLARI kaydirir, oyunun ritmini degil.
        /// </summary>
        public UnitMix[] ToUnitMix()
        {
            var list = new List<UnitMix>();

            AddMix(list, UnitType.Scout, 1);
            AddMix(list, UnitType.Soldier, UnitRoster.SoldierFirstWave);
            AddMix(list, UnitType.Queen, UnitRoster.QueenFirstWave);
            AddMix(list, UnitType.Blind, 1);
            AddMix(list, UnitType.Brood, 1);

            return list.ToArray();
        }

        private void AddMix(List<UnitMix> list, UnitType type, int firstWave)
        {
            float share = Share[(int)type];
            if (share <= 1e-4f) return;

            list.Add(new UnitMix
            {
                Profile = UnitProfile.For(type),
                Share = share,
                FirstWave = firstWave
            });
        }
    }
}
