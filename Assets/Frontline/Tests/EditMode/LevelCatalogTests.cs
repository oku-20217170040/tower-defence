using System.IO;
using NUnit.Framework;
using Frontline.Core;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// FAZ 4 (seviye ureteci, 3 Eylul gece plani): LevelCatalog'un
    /// ekleme/duzenleme/silme kurallarini ve LevelStore'un disk
    /// kaydet/yukle davranisini dogrular. Editor penceresi (LevelEditorWindow)
    /// yalnizca bu is mantigini cagirir - burada testlenen kurallar
    /// pencereyi acmadan da gecerliligini korur.
    /// </summary>
    public class LevelCatalogTests
    {
        private static LevelDefinition Sample(string name = "Test Seviyesi")
        {
            return new LevelDefinition
            {
                Name = name,
                Width = 13,
                Height = 30,
                CorridorCount = 3,
                CorridorWidth = 2,
                RungCount = 4,
                SceneryPercent = 30,
                TotalWaves = 15,
                BaseUnitsPerWave = 8,
                BaseUnitHp = 10f
            };
        }

        [Test]
        public void Ekle_GecerliSeviyeyi_KatalogaKoyar()
        {
            var catalog = new LevelCatalog();
            string error;

            Assert.IsTrue(catalog.Add(Sample(), out error));
            Assert.IsNull(error);
            Assert.AreEqual(1, catalog.Levels.Count);
            Assert.IsTrue(catalog.Contains("Test Seviyesi"));
        }

        [Test]
        public void Ekle_AyniIsimIkinciKez_Reddedilir()
        {
            var catalog = new LevelCatalog();
            string error;
            catalog.Add(Sample(), out error);

            bool ok = catalog.Add(Sample(), out error);

            Assert.IsFalse(ok);
            Assert.IsNotNull(error);
            Assert.AreEqual(1, catalog.Levels.Count);
        }

        [Test]
        public void Ekle_BosIsim_Reddedilir()
        {
            var catalog = new LevelCatalog();
            string error;

            bool ok = catalog.Add(Sample(""), out error);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, catalog.Levels.Count);
        }

        [Test]
        public void Ekle_GecersizGenislik_Reddedilir()
        {
            var catalog = new LevelCatalog();
            var bad = Sample();
            bad.Width = 1;
            string error;

            Assert.IsFalse(catalog.Add(bad, out error));
            Assert.IsNotNull(error);
        }

        [Test]
        public void Guncelle_VarOlanSeviyeyi_DegistirirVeYeniAdaTasir()
        {
            var catalog = new LevelCatalog();
            string error;
            catalog.Add(Sample("Eski Ad"), out error);

            var updated = Sample("Yeni Ad");
            updated.TotalWaves = 20;

            bool ok = catalog.Update("Eski Ad", updated, out error);

            Assert.IsTrue(ok);
            Assert.IsFalse(catalog.Contains("Eski Ad"));
            Assert.IsTrue(catalog.Contains("Yeni Ad"));
            Assert.AreEqual(20, catalog.Find("Yeni Ad").TotalWaves);
        }

        [Test]
        public void Guncelle_OlmayanSeviye_Reddedilir()
        {
            var catalog = new LevelCatalog();
            string error;

            bool ok = catalog.Update("Yok Boyle Bir Sey", Sample(), out error);

            Assert.IsFalse(ok);
            Assert.IsNotNull(error);
        }

        [Test]
        public void Sil_VarOlanSeviyeyi_KaldirirVeTekrarBulunmaz()
        {
            var catalog = new LevelCatalog();
            string error;
            catalog.Add(Sample(), out error);

            bool removed = catalog.Remove("Test Seviyesi");

            Assert.IsTrue(removed);
            Assert.AreEqual(0, catalog.Levels.Count);
            Assert.IsNull(catalog.Find("Test Seviyesi"));
        }

        [Test]
        public void Sil_OlmayanSeviye_FalseDonerPatlamaz()
        {
            var catalog = new LevelCatalog();
            Assert.IsFalse(catalog.Remove("Yok"));
        }

        [Test]
        public void DiskeKaydetYukle_TumAlanlariKorur()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_levels_test_" + Path.GetRandomFileName() + ".json");
            try
            {
                var catalog = new LevelCatalog();
                string error;
                var level = Sample("Kalici Seviye");
                level.Theme = LevelTheme.Snow;
                level.Seed = 42;
                catalog.Add(level, out error);

                Assert.IsTrue(LevelStore.SaveTo(catalog, path));
                var loaded = LevelStore.LoadFrom(path);

                Assert.AreEqual(1, loaded.Levels.Count);
                var found = loaded.Find("Kalici Seviye");
                Assert.IsNotNull(found);
                Assert.AreEqual(LevelTheme.Snow, found.Theme);
                Assert.AreEqual(42, found.Seed);
                Assert.AreEqual(13, found.Width);
                Assert.AreEqual(15, found.TotalWaves);
            }
            finally
            {
                LevelStore.ClearAt(path);
            }
        }

        [Test]
        public void OlmayanKayitDosyasi_BosKatalogaDuser()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_levels_yok_" + Path.GetRandomFileName() + ".json");

            var loaded = LevelStore.LoadFrom(path);

            Assert.AreEqual(0, loaded.Levels.Count);
        }

        [Test]
        public void BozukKayitDosyasi_PatlamazBosKatalogaDuser()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_levels_bozuk_" + Path.GetRandomFileName() + ".json");
            try
            {
                File.WriteAllText(path, "{ bu gecerli json degil");

                var loaded = LevelStore.LoadFrom(path);

                Assert.AreEqual(0, loaded.Levels.Count);
            }
            finally
            {
                LevelStore.ClearAt(path);
            }
        }
    }
}
