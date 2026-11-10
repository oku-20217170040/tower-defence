using NUnit.Framework;
using Frontline.Core;
using Frontline.Core.UI;

namespace Frontline.Tests
{
    /// <summary>
    /// Dikey run arayuzunun saf yerlesim ve bicimlendirme mantigi
    /// (spec 3.8: ust bant / harita / alt bant). MonoBehaviour icermez,
    /// bu yuzden tamami EditMode'da kosar.
    /// </summary>
    public sealed class UiLayoutTests
    {
        [Test]
        public void Uc_bant_ekrani_tamamen_kaplar()
        {
            var safeArea = new UiRect(0f, 0f, 1080f, 2340f);
            var bands = SafeAreaLayout.Split(safeArea);

            float totalHeight = bands.Top.Height + bands.Map.Height + bands.Bottom.Height;
            Assert.AreEqual(safeArea.Height, totalHeight, 0.01f);
        }

        [Test]
        public void Bantlar_birbiriyle_cakismaz()
        {
            var safeArea = new UiRect(0f, 0f, 1080f, 2340f);
            var bands = SafeAreaLayout.Split(safeArea);

            Assert.IsFalse(bands.Top.Overlaps(bands.Map));
            Assert.IsFalse(bands.Map.Overlaps(bands.Bottom));
            Assert.IsFalse(bands.Top.Overlaps(bands.Bottom));
        }

        [Test]
        public void Harita_bandi_en_buyuk_paya_sahiptir()
        {
            var safeArea = new UiRect(0f, 0f, 1080f, 2340f);
            var bands = SafeAreaLayout.Split(safeArea);

            Assert.Greater(bands.Map.Height, bands.Top.Height);
            Assert.Greater(bands.Map.Height, bands.Bottom.Height);
        }

        [Test]
        public void Asiri_buyuk_oranlar_haritayi_yok_etmez()
        {
            var safeArea = new UiRect(0f, 0f, 1080f, 2340f);
            var bands = SafeAreaLayout.Split(safeArea, 0.7f, 0.7f);

            Assert.GreaterOrEqual(bands.Map.Height, 0f);
            float total = bands.Top.Height + bands.Map.Height + bands.Bottom.Height;
            Assert.AreEqual(safeArea.Height, total, 0.01f);
        }

        [Test]
        public void Negatif_yukseklik_kirpilir_patlamaz()
        {
            var safeArea = new UiRect(0f, 0f, 1080f, -50f);
            Assert.DoesNotThrow(() => SafeAreaLayout.Split(safeArea));
        }

        [Test]
        public void Centikli_ekranda_ust_bant_guvenli_alanin_icinde_kalir()
        {
            var screen = new UiRect(0f, 0f, 1080f, 2400f);
            var safeArea = new UiRect(0f, 0f, 1080f, 2340f); // ustte 60px centik

            float topInset = SafeAreaLayout.TopInset(screen, safeArea);
            Assert.AreEqual(60f, topInset, 0.01f);

            var bands = SafeAreaLayout.Split(safeArea);
            Assert.LessOrEqual(bands.Top.Top, safeArea.Top);
        }

        [Test]
        public void Guvenli_alan_ekranla_ayniysa_inset_sifirdir()
        {
            var screen = new UiRect(0f, 0f, 1080f, 2340f);
            Assert.AreEqual(0f, SafeAreaLayout.TopInset(screen, screen), 0.01f);
            Assert.AreEqual(0f, SafeAreaLayout.BottomInset(screen, screen), 0.01f);
        }

        [Test]
        public void Dikey_ekran_IsPortrait_true_doner()
        {
            Assert.IsTrue(SafeAreaLayout.IsPortrait(new UiRect(0f, 0f, 1080f, 1920f)));
            Assert.IsFalse(SafeAreaLayout.IsPortrait(new UiRect(0f, 0f, 1920f, 1080f)));
        }

        [Test]
        public void Bes_kule_sutunu_bandi_esit_boler_ve_tasmaz()
        {
            var band = new UiRect(0f, 0f, 1000f, 200f);
            var columns = SafeAreaLayout.SplitColumns(band, 5, 8f);

            Assert.AreEqual(5, columns.Length);

            float rightmost = columns[4].Right;
            Assert.LessOrEqual(rightmost, band.Right + 0.01f);

            for (int i = 1; i < columns.Length; i++)
                Assert.Greater(columns[i].X, columns[i - 1].X);
        }

        [Test]
        public void Sutun_sayisi_sifirsa_bos_dizi_doner()
        {
            var band = new UiRect(0f, 0f, 1000f, 200f);
            Assert.AreEqual(0, SafeAreaLayout.SplitColumns(band, 0, 8f).Length);
        }

        // ---- HudFormat -------------------------------------------------

        [Test]
        public void Compact_bin_altini_aynen_yazar()
        {
            Assert.AreEqual("0", HudFormat.Compact(0));
            Assert.AreEqual("999", HudFormat.Compact(999));
        }

        [Test]
        public void Compact_bin_ve_uzeri_K_ile_kisaltir()
        {
            Assert.AreEqual("1K", HudFormat.Compact(1000));
            Assert.AreEqual("1,2K", HudFormat.Compact(1249));
            Assert.AreEqual("12K", HudFormat.Compact(12490));
        }

        [Test]
        public void Compact_milyon_M_ile_kisaltir()
        {
            Assert.AreEqual("1M", HudFormat.Compact(1000000));
            Assert.AreEqual("2,5M", HudFormat.Compact(2500000));
        }

        [Test]
        public void Compact_negatif_isareti_korur()
        {
            Assert.AreEqual("-1K", HudFormat.Compact(-1000));
        }

        [Test]
        public void Wave_hazirlik_asamasinda_Hazirlan_yazar()
        {
            Assert.AreEqual("Hazirlan", HudFormat.Wave(0, 15));
        }

        [Test]
        public void Wave_normal_durumda_x_bolu_y_yazar()
        {
            Assert.AreEqual("Dalga 3/15", HudFormat.Wave(3, 15));
        }

        [Test]
        public void Remaining_negatif_sifira_kirpilir()
        {
            Assert.AreEqual("Kalan 0/12", HudFormat.Remaining(-3, 12));
        }

        [Test]
        public void Remaining_toplami_asamaz()
        {
            Assert.AreEqual("Kalan 12/12", HudFormat.Remaining(99, 12));
        }

        [Test]
        public void Price_sifir_Bedava_yazar()
        {
            Assert.AreEqual("Bedava", HudFormat.Price(0));
        }

        [Test]
        public void Price_alinamayan_isaretlenir()
        {
            string text = HudFormat.Price(500, false);
            StringAssert.StartsWith(HudFormat.LockedPrefix, text);
        }

        [Test]
        public void Speed_tam_sayida_ondalik_yazmaz()
        {
            Assert.AreEqual("x2", HudFormat.Speed(2f));
            Assert.AreEqual("x3", HudFormat.Speed(3f));
        }

        [Test]
        public void TowerName_bes_kule_icin_bos_donmez()
        {
            foreach (TowerKind kind in System.Enum.GetValues(typeof(TowerKind)))
                Assert.IsNotEmpty(HudFormat.TowerName(kind));
        }
    }
}
