using NUnit.Framework;
using UnityEngine;
using Colony.Runtime;

namespace Colony.Tests
{
    /// <summary>
    /// Yuva caninin sozlesmesi.
    ///
    /// Neden var: yuvaya varan bocegin can eksiltmesi bir sure boyunca HIC
    /// calismadi ve tek bir test bunu yakalamadi - cunku denge testleri saf
    /// modeli simule ediyor, sahnedeki zinciri degil. Buradaki testler
    /// zincirin yuva ucunu kilitler.
    /// </summary>
    public class NestHealthTests
    {
        private NestHealth nest;
        private GameObject go;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Nest");
            nest = go.AddComponent<NestHealth>();
            nest.maxHp = 30;
            nest.ResetHealth();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetHealth_FillsToMax()
        {
            Assert.AreEqual(30, nest.CurrentHp);
            Assert.IsTrue(nest.IsAlive);
        }

        [Test]
        public void Damage_ReducesHealth()
        {
            nest.Damage(1);
            nest.Damage(4);

            Assert.AreEqual(25, nest.CurrentHp);
        }

        [Test]
        public void Damage_RaisesChangedEvent()
        {
            int reported = -1;
            nest.Changed += hp => reported = hp;

            nest.Damage(3);

            Assert.AreEqual(27, reported);
        }

        [Test]
        public void Damage_NeverGoesBelowZero()
        {
            nest.Damage(100);

            Assert.AreEqual(0, nest.CurrentHp);
            Assert.IsFalse(nest.IsAlive);
        }

        [Test]
        public void Damage_RaisesEmptiedExactlyOnce()
        {
            int emptied = 0;
            nest.Emptied += () => emptied++;

            nest.Damage(30);
            nest.Damage(5);

            Assert.AreEqual(1, emptied,
                "Yuva bosaldiktan sonra tekrar bosalmamali; run iki kez bitemez.");
        }

        [Test]
        public void Damage_IgnoresNonPositiveAmounts()
        {
            nest.Damage(0);
            nest.Damage(-5);

            Assert.AreEqual(30, nest.CurrentHp);
        }
    }
}
