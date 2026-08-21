using NUnit.Framework;
using UnityEngine;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// Us caninin sozlesmesi.
    ///
    /// Neden var: usya varan birligin can eksiltmesi bir sure boyunca HIC
    /// calismadi ve tek bir test bunu yakalamadi - cunku denge testleri saf
    /// modeli simule ediyor, sahnedeki zinciri degil. Buradaki testler
    /// zincirin us ucunu kilitler.
    /// </summary>
    public class HomeBaseHealthTests
    {
        private HomeBaseHealth homeBase;
        private GameObject go;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("HomeBase");
            homeBase = go.AddComponent<HomeBaseHealth>();
            homeBase.maxHp = 30;
            homeBase.ResetHealth();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetHealth_FillsToMax()
        {
            Assert.AreEqual(30, homeBase.CurrentHp);
            Assert.IsTrue(homeBase.IsAlive);
        }

        [Test]
        public void Damage_ReducesHealth()
        {
            homeBase.Damage(1);
            homeBase.Damage(4);

            Assert.AreEqual(25, homeBase.CurrentHp);
        }

        [Test]
        public void Damage_RaisesChangedEvent()
        {
            int reported = -1;
            homeBase.Changed += hp => reported = hp;

            homeBase.Damage(3);

            Assert.AreEqual(27, reported);
        }

        [Test]
        public void Damage_NeverGoesBelowZero()
        {
            homeBase.Damage(100);

            Assert.AreEqual(0, homeBase.CurrentHp);
            Assert.IsFalse(homeBase.IsAlive);
        }

        [Test]
        public void Damage_RaisesEmptiedExactlyOnce()
        {
            int emptied = 0;
            homeBase.Emptied += () => emptied++;

            homeBase.Damage(30);
            homeBase.Damage(5);

            Assert.AreEqual(1, emptied,
                "Us bosaldiktan sonra tekrar bosalmamali; run iki kez bitemez.");
        }

        [Test]
        public void Damage_IgnoresNonPositiveAmounts()
        {
            homeBase.Damage(0);
            homeBase.Damage(-5);

            Assert.AreEqual(30, homeBase.CurrentHp);
        }
    }
}
