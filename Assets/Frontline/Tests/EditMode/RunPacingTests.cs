using NUnit.Framework;
using UnityEngine;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// Dalga temposunun sozlesmesi.
    ///
    /// Oyuncu geri bildirimi (20 Agu 2026): "baslangicta dalgalar hizli
    /// geliyor ve az para var, sistem donmuyor." Erken oyunda dusunme
    /// suresi zorluktan daha degerli - oyuncu ilk kulelerini kurmadan
    /// bogulursa oyunun ne hakkinda oldugunu hic ogrenemiyor.
    /// </summary>
    public class RunPacingTests
    {
        private GameObject go;
        private RunController run;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("RunController");
            run = go.AddComponent<RunController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EarlyWaves_ComeSlowerThanLateWaves()
        {
            Assert.Greater(run.DelayAfterWave(1), run.DelayAfterWave(run.slowStartWaves + 1),
                "Erken dalgalar artik daha genis aralikli gelmiyor.");
        }

        [Test]
        public void SlowStart_CoversTheOpeningWaves()
        {
            for (int wave = 1; wave <= run.slowStartWaves; wave++)
                Assert.AreEqual(run.earlyWaveDelay, run.DelayAfterWave(wave),
                    "Dalga " + wave + " yavas baslangic doneminde olmali.");
        }

        [Test]
        public void AfterSlowStart_NormalPacingResumes()
        {
            Assert.AreEqual(run.delayBetweenWaves, run.DelayAfterWave(run.slowStartWaves + 1));
            Assert.AreEqual(run.delayBetweenWaves, run.DelayAfterWave(15));
        }

        [Test]
        public void OpeningDelay_GivesTimeToBuildBeforeTheFirstWave()
        {
            Assert.Greater(run.openingDelay, run.delayBetweenWaves,
                "Ilk dalga oncesi hazirlik suresi, siradan bir dalga arasindan " +
                "uzun olmali; oyuncu haritayi okuyup ilk kulelerini kurmali.");
        }
    }
}
