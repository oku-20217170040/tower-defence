using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// 11 Kasim 2026 kule kararlarini kilitler
    /// (docs/specs/2026-11-11-kule-yetenek-ve-yukseltme-kararlari.md).
    ///
    /// Buradaki testler SAYI degil KARAR sinar: "sv3'te cift namlu",
    /// "nisanci en uzun menzilli", "radarin ates hizi eki hafif" gibi.
    /// Sayilar taramayla degisebilir; kararlar degisirse bu dosya kirilmali.
    /// </summary>
    public class TowerStatsTests
    {
        private static TowerLevelStats S(TowerKind kind, int level)
        {
            return TowerStats.For(kind, level);
        }

        // ------------------------------------------------- Makineli Tufek

        [Test]
        public void MachineGun_FiresAtTwoTargets_OnlyAtLevel3()
        {
            Assert.AreEqual(1, S(TowerKind.Ambush, 1).Targets);
            Assert.AreEqual(1, S(TowerKind.Ambush, 2).Targets);
            Assert.AreEqual(2, S(TowerKind.Ambush, 3).Targets);
        }

        [Test]
        public void MachineGun_Level2_FiresFaster()
        {
            Assert.Greater(S(TowerKind.Ambush, 2).Dps, S(TowerKind.Ambush, 1).Dps);
        }

        // ----------------------------------------------------------- Topcu

        [Test]
        public void Cannon_Level2_HasBiggerBlastAndMoreDamage()
        {
            Assert.Greater(S(TowerKind.Battery, 2).BlastRadius, S(TowerKind.Battery, 1).BlastRadius);
            Assert.Greater(S(TowerKind.Battery, 2).ShotDamage, S(TowerKind.Battery, 1).ShotDamage);
        }

        [Test]
        public void Cannon_BurningCrater_OnlyAtLevel3()
        {
            Assert.AreEqual(0f, S(TowerKind.Battery, 1).BurnDps);
            Assert.AreEqual(0f, S(TowerKind.Battery, 2).BurnDps);
            Assert.Greater(S(TowerKind.Battery, 3).BurnDps, 0f);
            Assert.Greater(S(TowerKind.Battery, 3).BurnSeconds, 0f);
        }

        [Test]
        public void Cannon_Level1Shell_MatchesTheSimulatorDps()
        {
            // Simulator Topcu'yu surekli hasar olarak modeller; mermi ayni
            // hasari aralikli verir. Ikisi ayrisirsa kalibrasyon yalan soyler.
            TowerLevelStats s = S(TowerKind.Battery, 1);
            Assert.AreEqual(TowerStats.CannonDps, s.ShotDamage / s.ShotInterval, 0.0001f);
            Assert.AreEqual(TowerStats.CannonBlastRadius, s.BlastRadius, 0.0001f);
        }

        // -------------------------------------------------- Keskin Nisanci

        [Test]
        public void Sniper_HasTheLongestRange_ButNotTheWholeMap()
        {
            for (int level = 1; level <= TowerStats.MaxLevel; level++)
            {
                float sniper = S(TowerKind.Beam, level).Range;
                Assert.Greater(sniper, S(TowerKind.Ambush, level).Range);
                Assert.Greater(sniper, S(TowerKind.Battery, level).Range);

                // Harita 30 hucre boyunda; "sinirsiz menzil olmasin".
                Assert.Less(sniper, 20f, "Nisanci menzili haritayi kapsiyor: " + sniper);
            }
        }

        [Test]
        public void Sniper_RangeGrows_WithEveryLevel()
        {
            Assert.Greater(S(TowerKind.Beam, 2).Range, S(TowerKind.Beam, 1).Range);
            Assert.Greater(S(TowerKind.Beam, 3).Range, S(TowerKind.Beam, 2).Range);
        }

        [Test]
        public void Sniper_Level2_FiresFasterAndHarder()
        {
            Assert.Less(S(TowerKind.Beam, 2).ShotInterval, S(TowerKind.Beam, 1).ShotInterval);
            Assert.Greater(S(TowerKind.Beam, 2).ShotDamage, S(TowerKind.Beam, 1).ShotDamage);
        }

        [Test]
        public void Sniper_ArmorPierceAndFollowUp_OnlyAtLevel3()
        {
            Assert.AreEqual(0f, S(TowerKind.Beam, 2).PierceBonus);
            Assert.IsFalse(S(TowerKind.Beam, 2).FollowUpOnKill);
            Assert.Greater(S(TowerKind.Beam, 3).PierceBonus, 0f);
            Assert.IsTrue(S(TowerKind.Beam, 3).FollowUpOnKill);
        }

        // ------------------------------------------------- Zaman Bozulmasi

        [Test]
        public void TimeDistortion_Level1_SlowsToTheMeasuredSpeed()
        {
            // 0,6: ilk karar 0,75'ti, olcum sonrasi Serhat 0,6'yi onayladi.
            Assert.AreEqual(0.6f, TowerStats.SlowSpeedLevel1, 0.0001f);
            Assert.AreEqual(1f / 0.6f, S(TowerKind.Minefield, 1).SlowFactor, 0.0001f);
        }

        [Test]
        public void TimeDistortion_GetsStronger_WithEveryLevel()
        {
            Assert.Greater(S(TowerKind.Minefield, 2).SlowFactor, S(TowerKind.Minefield, 1).SlowFactor);
            Assert.Greater(S(TowerKind.Minefield, 3).SlowFactor, S(TowerKind.Minefield, 2).SlowFactor);
            Assert.Greater(S(TowerKind.Minefield, 2).AuraRadius, S(TowerKind.Minefield, 1).AuraRadius);
        }

        [Test]
        public void TimeDistortion_Fragility_OnlyAtLevel3_AndSubtle()
        {
            Assert.AreEqual(1f, S(TowerKind.Minefield, 1).Fragility);
            Assert.AreEqual(1f, S(TowerKind.Minefield, 2).Fragility);

            // "Cok bariz olmasin, cok hissedilebilir bir hasar olmasin."
            Assert.That(S(TowerKind.Minefield, 3).Fragility, Is.InRange(1.01f, 1.2f));
        }

        [Test]
        public void TimeDistortion_NeverFires()
        {
            for (int level = 1; level <= TowerStats.MaxLevel; level++)
            {
                Assert.AreEqual(0f, S(TowerKind.Minefield, level).Range);
                Assert.IsFalse(TowerStats.IsWeapon(TowerKind.Minefield));
            }
        }

        // ----------------------------------------------------------- Radar

        [Test]
        public void Radar_RangeBonusGrows_WithEveryLevel()
        {
            Assert.Greater(S(TowerKind.Decoy, 1).RangeBonus, 0f);
            Assert.Greater(S(TowerKind.Decoy, 2).RangeBonus, S(TowerKind.Decoy, 1).RangeBonus);
            Assert.Greater(S(TowerKind.Decoy, 3).RangeBonus, S(TowerKind.Decoy, 2).RangeBonus);
            Assert.Greater(S(TowerKind.Decoy, 2).AuraRadius, S(TowerKind.Decoy, 1).AuraRadius);
        }

        [Test]
        public void Radar_FireRateBonus_OnlyAtLevel3_AndSubtle()
        {
            Assert.AreEqual(0f, S(TowerKind.Decoy, 1).FireRateBonus);
            Assert.AreEqual(0f, S(TowerKind.Decoy, 2).FireRateBonus);

            // "Ates hizi biraz artsin ama cok asiri bariz olmasin."
            Assert.That(S(TowerKind.Decoy, 3).FireRateBonus, Is.InRange(0.01f, 0.15f));
        }

        [Test]
        public void Radar_BoostsWeapons_ButNotSupportTowers()
        {
            Assert.AreEqual(0.3f, TowerStats.RangeBonusFor(TowerKind.Ambush, 0.3f), 0.0001f);
            Assert.AreEqual(0.3f, TowerStats.RangeBonusFor(TowerKind.Battery, 0.3f), 0.0001f);

            // Keskin Nisanci'yi da artirir (karar), ama tam oranda degil.
            float sniper = TowerStats.RangeBonusFor(TowerKind.Beam, 0.3f);
            Assert.Greater(sniper, 0f);
            Assert.Less(sniper, 0.3f);

            // Zaman Bozulmasi'ni ve baska radari ETKILEMEZ.
            Assert.AreEqual(0f, TowerStats.RangeBonusFor(TowerKind.Minefield, 0.3f));
            Assert.AreEqual(0f, TowerStats.RangeBonusFor(TowerKind.Decoy, 0.3f));
            Assert.AreEqual(0f, TowerStats.FireRateBonusFor(TowerKind.Minefield, 0.1f));
        }

        // ------------------------------------------- simulator ile hizalama

        [Test]
        public void SimulatorPolicy_UsesLevel1Stats()
        {
            EconomyPlanner.Policy policy = EconomyPlanner.Policy.Default();

            Assert.AreEqual(S(TowerKind.Ambush, 1).Dps, policy.AmbushDps, 0.0001f);
            Assert.AreEqual(TowerStats.CannonDps, policy.BatteryDps, 0.0001f);
            Assert.AreEqual(TowerStats.CannonBlastRadius, policy.BatteryBlastRadius, 0.0001f);
            Assert.AreEqual(RunSimulator.Targeting.Blast, policy.BatteryTargeting);
            Assert.AreEqual(S(TowerKind.Beam, 1).Range, policy.BeamRange, 0.0001f);
            Assert.AreEqual(S(TowerKind.Beam, 1).ShotDamage, policy.BeamShotDamage, 0.0001f);
            Assert.AreEqual(S(TowerKind.Decoy, 1).RangeBonus, policy.RadarRangeBonus, 0.0001f);
            Assert.AreEqual(S(TowerKind.Decoy, 1).AuraRadius, policy.RadarRadius, 0.0001f);
        }

        [Test]
        public void LevelsOutsideTheRange_AreClamped()
        {
            Assert.AreEqual(S(TowerKind.Beam, 1).Range, S(TowerKind.Beam, 0).Range);
            Assert.AreEqual(S(TowerKind.Beam, 3).Range, S(TowerKind.Beam, 9).Range);
        }
    }
}
