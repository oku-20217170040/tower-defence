using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Oyun hizi kademesi. Kademelerin dongusel olmasi ve x3'u asmamasi
    /// bir denge karari: 4 ve ustunde birlik bir karede birden fazla
    /// hucre ilerleyip pusu menzilini atliyor.
    /// </summary>
    public sealed class SpeedCycleTests
    {
        [Test]
        public void X1_de_baslar()
        {
            var cycle = new SpeedCycle();

            Assert.AreEqual(1f, cycle.Current);
            Assert.AreEqual("x1", cycle.Label);
        }

        [Test]
        public void Kademeler_sirayla_artar()
        {
            var cycle = new SpeedCycle();

            Assert.AreEqual(2f, cycle.Next());
            Assert.AreEqual(3f, cycle.Next());
        }

        [Test]
        public void Sondan_sonra_basa_doner()
        {
            var cycle = new SpeedCycle();
            cycle.Next();
            cycle.Next();

            Assert.AreEqual(1f, cycle.Next(), "tek dugme dongusel olmali");
        }

        [Test]
        public void Hicbir_kademe_uc_katini_asmaz()
        {
            var cycle = new SpeedCycle();

            for (int i = 0; i < 12; i++)
            {
                Assert.LessOrEqual(cycle.Current, 3f);
                Assert.GreaterOrEqual(cycle.Current, 1f);
                cycle.Next();
            }
        }

        [Test]
        public void Reset_x1_e_dondurur()
        {
            var cycle = new SpeedCycle();
            cycle.Next();
            cycle.Reset();

            Assert.AreEqual(1f, cycle.Current);
        }

        [Test]
        public void SetClosest_listede_olmayan_degeri_en_yakina_oturtur()
        {
            var cycle = new SpeedCycle();

            cycle.SetClosest(2.4f);
            Assert.AreEqual(2f, cycle.Current);

            cycle.SetClosest(99f);
            Assert.AreEqual(3f, cycle.Current, "ust sinir asilamaz");

            cycle.SetClosest(0f);
            Assert.AreEqual(1f, cycle.Current, "alt sinir asilamaz");
        }
    }
}
