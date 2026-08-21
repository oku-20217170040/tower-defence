using NUnit.Framework;
using UnityEngine;
using Frontline.Runtime;

namespace Frontline.Tests
{
    public class ReconColorRampTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void EmptyCell_IsFullyTransparent()
        {
            // Bos koridor tertemiz gorunmeli, yoksa oyuncu neyin
            // ogrenildigini ayirt edemez (spec 9.1).
            Color c = ReconColorRamp.Evaluate(0f, 0f);
            Assert.AreEqual(0f, c.a, Tolerance);
        }

        [Test]
        public void FullRecon_ReachesMaximumAlpha()
        {
            Color c = ReconColorRamp.Evaluate(1f, 0f);
            Assert.AreEqual(ReconColorRamp.MaxAlpha, c.a, Tolerance);
        }

        [Test]
        public void Alpha_IncreasesMonotonicallyWithRecon()
        {
            float previous = -1f;
            for (int i = 0; i <= 10; i++)
            {
                float value = i / 10f;
                float alpha = ReconColorRamp.Evaluate(value, 0f).a;

                Assert.Greater(alpha, previous, "Alfa " + value + " degerinde artmadi.");
                previous = alpha;
            }
        }

        [Test]
        public void Inputs_AreClampedAboveOne()
        {
            Color clamped = ReconColorRamp.Evaluate(5f, 0f);
            Color atOne = ReconColorRamp.Evaluate(1f, 0f);

            Assert.AreEqual(atOne.r, clamped.r, Tolerance);
            Assert.AreEqual(atOne.g, clamped.g, Tolerance);
            Assert.AreEqual(atOne.b, clamped.b, Tolerance);
            Assert.AreEqual(atOne.a, clamped.a, Tolerance);
        }

        [Test]
        public void Inputs_AreClampedBelowZero()
        {
            Color clamped = ReconColorRamp.Evaluate(-3f, -2f);
            Assert.AreEqual(0f, clamped.a, Tolerance);
        }

        [Test]
        public void RealRecon_IsWarm()
        {
            // Gercek iz sicak tonda: kirmizi bileseni yesilden baskin.
            Color c = ReconColorRamp.Evaluate(1f, 0f);
            Assert.Greater(c.r, c.g);
            Assert.Greater(c.g, c.b);
        }

        [Test]
        public void Decoy_IsCool_AndDistinguishableFromRealRecon()
        {
            // Oyuncu kendi yalanini gercek izden ayirt edebilmeli (spec 9.1).
            Color decoyOnly = ReconColorRamp.Evaluate(0f, 1f);
            Color reconOnly = ReconColorRamp.Evaluate(1f, 0f);

            Assert.Greater(decoyOnly.g, decoyOnly.r, "Yem soguk tonda olmali.");
            Assert.Greater(reconOnly.r, decoyOnly.r, "Iki ton birbirinden ayrilmali.");
        }

        [Test]
        public void MixedCell_LandsBetweenTheTwoTones()
        {
            Color decoyOnly = ReconColorRamp.Evaluate(0f, 1f);
            Color reconOnly = ReconColorRamp.Evaluate(1f, 0f);
            Color mixed = ReconColorRamp.Evaluate(1f, 1f);

            Assert.Less(mixed.r, reconOnly.r);
            Assert.Greater(mixed.r, decoyOnly.r);
        }

        [Test]
        public void DecoyAlone_IsVisible()
        {
            // Yem tek basina da gorunur olmali; yoksa oyuncu yemin
            // ise yarayip yaramadigini goremez.
            Color c = ReconColorRamp.Evaluate(0f, 1f);
            Assert.Greater(c.a, 0f);
        }
    }
}
