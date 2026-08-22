using NUnit.Framework;
using UnityEngine;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// Kamera sinirlari. Gezinmenin gorsel olmayan tek parcasi budur:
    /// "kamera haritanin disina cikmasin" bir sanat karari degil, bir
    /// aralik hesabi ve sessizce bozulabilir.
    ///
    /// Harita 11x30, hucre 1 birim, merkez orijinde: sol-alt kose
    /// (-5,5, -15), sag-ust (5,5, 15).
    /// </summary>
    public sealed class MapCameraTests
    {
        private static readonly Vector2 Origin = new Vector2(-5.5f, -15f);
        private const float W = 11f;
        private const float H = 30f;

        [Test]
        public void IcerdeKalanMerkez_Degismez()
        {
            Vector2 result = MapCamera.ClampCenter(new Vector2(0f, 5f), Origin, W, H, 5f, 9f);

            Assert.AreEqual(0f, result.x, 0.0001f);
            Assert.AreEqual(5f, result.y, 0.0001f);
        }

        [Test]
        public void UstKenardanTasan_GeriCekilir()
        {
            // Yari-yukseklik 9 iken merkez en fazla 15 - 9 = 6 olabilir.
            Vector2 result = MapCamera.ClampCenter(new Vector2(0f, 99f), Origin, W, H, 5f, 9f);

            Assert.AreEqual(6f, result.y, 0.0001f);
        }

        [Test]
        public void AltKenardanTasan_GeriCekilir()
        {
            Vector2 result = MapCamera.ClampCenter(new Vector2(0f, -99f), Origin, W, H, 5f, 9f);

            Assert.AreEqual(-6f, result.y, 0.0001f);
        }

        [Test]
        public void GorunumHaritadanGenisse_OEksendeOrtalanir()
        {
            // Yari-genislik 8 > haritanin yarisi 5,5: kenara yapismak
            // yerine ortalanmali, yoksa haritanin yarisi bos kalir.
            Vector2 result = MapCamera.ClampCenter(new Vector2(99f, 0f), Origin, W, H, 8f, 9f);

            Assert.AreEqual(0f, result.x, 0.0001f);
        }

        [Test]
        public void IkiEksenDeTasarsa_TamOrtalanir()
        {
            Vector2 result = MapCamera.ClampCenter(new Vector2(50f, -50f), Origin, W, H, 20f, 20f);

            Assert.AreEqual(0f, result.x, 0.0001f);
            Assert.AreEqual(0f, result.y, 0.0001f);
        }

        [Test]
        public void MerkezOlmayanOrijin_DogruCalisir()
        {
            // Izgara her zaman orijinde olmak zorunda degil; siniri
            // haritanin KENDI koordinatlari belirler.
            Vector2 origin = new Vector2(10f, 100f);
            Vector2 result = MapCamera.ClampCenter(new Vector2(0f, 0f), origin, W, H, 5f, 9f);

            Assert.AreEqual(15f, result.x, 0.0001f);
            Assert.AreEqual(109f, result.y, 0.0001f);
        }
    }
}
