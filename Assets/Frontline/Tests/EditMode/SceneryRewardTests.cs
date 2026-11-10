using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Ortu (agac/kaya) yikilinca kazanilan besin. Kullanici geri
    /// bildirimi: "obje patladiktan sonra buyukluk durumuna gore coin
    /// kazansin."
    /// </summary>
    public sealed class SceneryRewardTests
    {
        [Test]
        public void Sifir_canli_ortu_sifir_odul_verir()
        {
            Assert.AreEqual(0, SceneryReward.CoinsFor(0f));
        }

        [Test]
        public void Negatif_can_sifir_odul_verir()
        {
            Assert.AreEqual(0, SceneryReward.CoinsFor(-5f));
        }

        [Test]
        public void Cok_kucuk_can_bile_en_az_bir_coin_verir()
        {
            Assert.GreaterOrEqual(SceneryReward.CoinsFor(0.5f), 1);
        }

        [Test]
        public void Buyuk_obje_kucukten_fazla_odul_verir()
        {
            int small = SceneryReward.CoinsFor(12f); // agac
            int large = SceneryReward.CoinsFor(30f); // kaya

            Assert.Greater(large, small);
        }

        [Test]
        public void Varsayilan_kaya_cani_onbes_coin_verir()
        {
            // Battlefield.rockHealth varsayilani 30. Referans oyunda
            // gozlenen deger de +15 idi (docs/notes gozlem raporu).
            Assert.AreEqual(15, SceneryReward.CoinsFor(30f));
        }

        [Test]
        public void Varsayilan_agac_cani_alti_coin_verir()
        {
            // Battlefield.treeHealth varsayilani 12.
            Assert.AreEqual(6, SceneryReward.CoinsFor(12f));
        }

        [Test]
        public void Odul_dogrusal_olceklenir()
        {
            Assert.AreEqual(SceneryReward.CoinsFor(20f) * 2, SceneryReward.CoinsFor(40f), 1);
        }
    }
}
