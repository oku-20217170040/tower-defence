using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Genetik katman: ordu run'lar boyunca guclenmez, yalnizca kayar
    /// (spec bolum 4 ve 6). Bu testlerin cogu butcenin korundugunu ve
    /// mutasyonun deterministik oldugunu dogrular - ikisi bozulursa
    /// nemesis islevi ya adaletsizlesir ya da olculemez hale gelir.
    /// </summary>
    public sealed class EvolutionTests
    {
        [Test]
        public void Ayni_seed_ayni_outcome_birebir_ayni_genome_uretir()
        {
            var outcome = RunOutcome.MonoTower(TowerKind.Battery);

            var a = EvolutionEngine.Evolve(ArmyGenome.Default(), outcome, 42);
            var b = EvolutionEngine.Evolve(ArmyGenome.Default(), outcome, 42);

            for (int i = 0; i < a.Resistance.Length; i++)
                Assert.AreEqual(a.Resistance[i], b.Resistance[i], 1e-9f);
            for (int i = 0; i < a.Share.Length; i++)
                Assert.AreEqual(a.Share[i], b.Share[i], 1e-9f);
        }

        [Test]
        public void Farkli_seed_farkli_sonuc_uretebilir()
        {
            var outcome = RunOutcome.Balanced();

            var a = EvolutionEngine.Evolve(ArmyGenome.Default(), outcome, 1);
            var b = EvolutionEngine.Evolve(ArmyGenome.Default(), outcome, 2);

            bool identical = true;
            for (int i = 0; i < a.Resistance.Length; i++)
            {
                if (System.Math.Abs(a.Resistance[i] - b.Resistance[i]) > 1e-9f)
                    identical = false;
            }

            Assert.IsFalse(identical, "mutasyon gurultusu seed'e bagli olmali");
        }

        [Test]
        public void Direnc_toplam_guc_butcesi_evrim_sonrasi_korunur()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.MonoTower(TowerKind.Beam, count: 20);

            var next = EvolutionEngine.Evolve(genome, outcome, 7);

            Assert.AreEqual(EvolutionPressure.ResistanceBudget, next.ResistanceTotal(),
                1e-3f, "direnc butcesi sabit kalmali");
        }

        [Test]
        public void Bilesim_guc_butcesi_evrim_sonrasi_korunur()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.MonoTower(TowerKind.Battery, count: 20);

            var next = EvolutionEngine.Evolve(genome, outcome, 7);

            Assert.AreEqual(ArmyGenome.TargetCompositionPower, next.CompositionPower(),
                1e-3f, "bilesim gucu sabit kalmali");
        }

        [Test]
        public void Oyuncu_surekli_ayni_kuleyi_kullanirsa_ona_direnc_artar()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.MonoTower(TowerKind.Ambush, count: 30);

            var next = EvolutionEngine.Evolve(genome, outcome, 11);

            Assert.Greater(next.ResistanceTo(TowerKind.Ambush), genome.ResistanceTo(TowerKind.Ambush));
        }

        [Test]
        public void Kullanilmayan_kuleye_karsi_direnc_dusme_yonunde_kalir()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.MonoTower(TowerKind.Ambush, count: 30);

            var next = EvolutionEngine.Evolve(genome, outcome, 11);

            // Kullanilmayan kulelerin toplami: butce sabit oldugu icin
            // Ambush artarken geri kalanlarin TOPLAMI dusmek zorunda.
            float usedResistance = genome.ResistanceTo(TowerKind.Ambush);
            float nextUsedResistance = next.ResistanceTo(TowerKind.Ambush);
            float baseRestSum = genome.ResistanceTotal() - usedResistance;
            float nextRestSum = next.ResistanceTotal() - nextUsedResistance;

            Assert.Less(nextRestSum, baseRestSum);
        }

        [Test]
        public void Direnc_alt_siniri_asilamaz()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.MonoTower(TowerKind.Battery, count: 50);

            ArmyGenome current = genome;
            for (int i = 0; i < 60; i++)
                current = EvolutionEngine.Evolve(current, outcome, 100 + i);

            for (int i = 0; i < current.Resistance.Length; i++)
                Assert.GreaterOrEqual(current.Resistance[i], EvolutionPressure.MinResistance - 1e-3f);
        }

        [Test]
        public void Direnc_ust_siniri_asilamaz()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.MonoTower(TowerKind.Minefield, count: 50);

            ArmyGenome current = genome;
            for (int i = 0; i < 60; i++)
                current = EvolutionEngine.Evolve(current, outcome, 200 + i);

            for (int i = 0; i < current.Resistance.Length; i++)
                Assert.LessOrEqual(current.Resistance[i], EvolutionPressure.MaxResistance + 1e-3f);
        }

        [Test]
        public void Dengeli_kullanimda_baski_notre_yakin_kalir()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.Balanced();

            var next = EvolutionEngine.Evolve(genome, outcome, 3);

            for (int i = 0; i < next.Resistance.Length; i++)
            {
                Assert.AreEqual(EvolutionPressure.BaselineResistance, next.Resistance[i], 0.02f,
                    "esit kullanimda hicbir kule digerinden belirgin sapmamali");
            }
        }

        [Test]
        public void Bos_outcome_notr_pay_varsayar_ve_patlamaz()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.Empty();

            Assert.DoesNotThrow(() => EvolutionEngine.Evolve(genome, outcome, 5));
        }

        [Test]
        public void Yirmi_ardisik_run_boyunca_genome_sinirlar_icinde_kalir()
        {
            ArmyGenome current = ArmyGenome.Default();

            for (int i = 0; i < 20; i++)
            {
                var kind = (TowerKind)(i % EvolutionPressure.TowerKindCount);
                var outcome = RunOutcome.MonoTower(kind, count: 10, won: i % 2 == 0);
                current = EvolutionEngine.Evolve(current, outcome, 1000 + i);

                for (int r = 0; r < current.Resistance.Length; r++)
                {
                    Assert.GreaterOrEqual(current.Resistance[r], EvolutionPressure.MinResistance - 1e-3f);
                    Assert.LessOrEqual(current.Resistance[r], EvolutionPressure.MaxResistance + 1e-3f);
                }

                Assert.AreEqual(EvolutionPressure.ResistanceBudget, current.ResistanceTotal(), 1e-2f);
            }
        }

        [Test]
        public void Birlik_paylari_toplami_biri_asmaz()
        {
            ArmyGenome current = ArmyGenome.Default();

            for (int i = 0; i < 15; i++)
            {
                var outcome = RunOutcome.MonoTower(TowerKind.Battery, count: 5, won: true);
                current = EvolutionEngine.Evolve(current, outcome, 500 + i);

                float sum = 0f;
                for (int s = 0; s < current.Share.Length; s++) sum += current.Share[s];

                Assert.AreEqual(1f, sum, 1e-3f);
            }
        }

        [Test]
        public void Kaybeden_run_direnci_tabana_dogru_yumusatir()
        {
            var strong = ArmyGenome.Default();
            strong.Resistance[(int)TowerKind.Battery] = EvolutionPressure.MaxResistance - 0.05f;
            strong.Normalize();

            var outcome = RunOutcome.Empty(won: false);
            var next = EvolutionEngine.Evolve(strong, outcome, 9);

            Assert.LessOrEqual(next.ResistanceTo(TowerKind.Battery), strong.ResistanceTo(TowerKind.Battery));
        }

        [Test]
        public void Tek_run_adimi_MaxResistanceStepPerRun_sinirini_asmaz()
        {
            var genome = ArmyGenome.Default();
            var outcome = RunOutcome.MonoTower(TowerKind.Battery, count: 100);

            var next = EvolutionEngine.Evolve(genome, outcome, 3);

            for (int i = 0; i < genome.Resistance.Length; i++)
            {
                float step = System.Math.Abs(next.Resistance[i] - genome.Resistance[i]);
                Assert.LessOrEqual(step, EvolutionPressure.MaxResistanceStepPerRun + 0.02f,
                    "normalize sonrasi kucuk tasma payi birakildi");
            }
        }

        [Test]
        public void EvolveSeries_tek_seed_den_tekrar_uretilebilir()
        {
            var outcomes = new[]
            {
                RunOutcome.MonoTower(TowerKind.Battery, count: 5, won: true),
                RunOutcome.MonoTower(TowerKind.Ambush, count: 8, won: false),
                RunOutcome.Balanced()
            };

            var a = EvolutionEngine.EvolveSeries(ArmyGenome.Default(), outcomes, 777);
            var b = EvolutionEngine.EvolveSeries(ArmyGenome.Default(), outcomes, 777);

            for (int i = 0; i < a.Resistance.Length; i++)
                Assert.AreEqual(a.Resistance[i], b.Resistance[i], 1e-9f);
        }

        [Test]
        public void Clone_bagimsiz_kopya_uretir()
        {
            var genome = ArmyGenome.Default();
            var clone = genome.Clone();
            clone.Resistance[0] = 99f;

            Assert.AreNotEqual(genome.Resistance[0], clone.Resistance[0]);
        }

        [Test]
        public void Genome_null_ise_ArgumentNullException_firlatir()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => EvolutionEngine.Evolve(null, RunOutcome.Empty(), 1));
        }

        [Test]
        public void Outcome_null_ise_ArgumentNullException_firlatir()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => EvolutionEngine.Evolve(ArmyGenome.Default(), null, 1));
        }
    }
}
