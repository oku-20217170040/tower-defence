using NUnit.Framework;
using UnityEngine;
using Colony.Runtime;

namespace Colony.Tests
{
    public class PheromoneColorRampTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void EmptyCell_IsFullyTransparent()
        {
            // Bos koridor tertemiz gorunmeli, yoksa oyuncu neyin
            // ogrenildigini ayirt edemez (spec 9.1).
            Color c = PheromoneColorRamp.Evaluate(0f, 0f);
            Assert.AreEqual(0f, c.a, Tolerance);
        }

        [Test]
        public void FullPheromone_ReachesMaximumAlpha()
        {
            Color c = PheromoneColorRamp.Evaluate(1f, 0f);
            Assert.AreEqual(PheromoneColorRamp.MaxAlpha, c.a, Tolerance);
        }

        [Test]
        public void Alpha_IncreasesMonotonicallyWithPheromone()
        {
            float previous = -1f;
            for (int i = 0; i <= 10; i++)
            {
                float value = i / 10f;
                float alpha = PheromoneColorRamp.Evaluate(value, 0f).a;

                Assert.Greater(alpha, previous, "Alfa " + value + " degerinde artmadi.");
                previous = alpha;
            }
        }

        [Test]
        public void Inputs_AreClampedAboveOne()
        {
            Color clamped = PheromoneColorRamp.Evaluate(5f, 0f);
            Color atOne = PheromoneColorRamp.Evaluate(1f, 0f);

            Assert.AreEqual(atOne.r, clamped.r, Tolerance);
            Assert.AreEqual(atOne.g, clamped.g, Tolerance);
            Assert.AreEqual(atOne.b, clamped.b, Tolerance);
            Assert.AreEqual(atOne.a, clamped.a, Tolerance);
        }

        [Test]
        public void Inputs_AreClampedBelowZero()
        {
            Color clamped = PheromoneColorRamp.Evaluate(-3f, -2f);
            Assert.AreEqual(0f, clamped.a, Tolerance);
        }

        [Test]
        public void RealPheromone_IsWarm()
        {
            // Gercek iz sicak tonda: kirmizi bileseni yesilden baskin.
            Color c = PheromoneColorRamp.Evaluate(1f, 0f);
            Assert.Greater(c.r, c.g);
            Assert.Greater(c.g, c.b);
        }

        [Test]
        public void Lure_IsCool_AndDistinguishableFromRealPheromone()
        {
            // Oyuncu kendi yalanini gercek izden ayirt edebilmeli (spec 9.1).
            Color lureOnly = PheromoneColorRamp.Evaluate(0f, 1f);
            Color pheromoneOnly = PheromoneColorRamp.Evaluate(1f, 0f);

            Assert.Greater(lureOnly.g, lureOnly.r, "Yem soguk tonda olmali.");
            Assert.Greater(pheromoneOnly.r, lureOnly.r, "Iki ton birbirinden ayrilmali.");
        }

        [Test]
        public void MixedCell_LandsBetweenTheTwoTones()
        {
            Color lureOnly = PheromoneColorRamp.Evaluate(0f, 1f);
            Color pheromoneOnly = PheromoneColorRamp.Evaluate(1f, 0f);
            Color mixed = PheromoneColorRamp.Evaluate(1f, 1f);

            Assert.Less(mixed.r, pheromoneOnly.r);
            Assert.Greater(mixed.r, lureOnly.r);
        }

        [Test]
        public void LureAlone_IsVisible()
        {
            // Yem tek basina da gorunur olmali; yoksa oyuncu yemin
            // ise yarayip yaramadigini goremez.
            Color c = PheromoneColorRamp.Evaluate(0f, 1f);
            Assert.Greater(c.a, 0f);
        }
    }
}
