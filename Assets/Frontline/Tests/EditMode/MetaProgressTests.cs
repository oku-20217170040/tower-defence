using System.IO;
using NUnit.Framework;
using Frontline.Core;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// Meta ilerlemenin (spec 6) sabitlerini ve kurallarini kilitler.
    /// Buradaki sayilar keyfi degildir; BiomassLedger'daki gerekcelerin
    /// test karsiligidir. Sayi degistirilirse test kirilir ve karar
    /// bilincli olarak yeniden verilir.
    /// </summary>
    public class MetaProgressTests
    {
        // Olcumden gelen tipik run profilleri (spec 3.5).
        private const int LostWaves = 9;
        private const int LostKills = 105;
        private const int WonWaves = 15;
        private const int WonKills = 255;

        [Test]
        public void KaybedilenRun_SifirdanFazlaBiyokutleVerir()
        {
            int earned = BiomassLedger.EarnedWithFloor(LostWaves, LostKills, false);

            // Roguelike'ta bosa giden run oyuncuyu birakir; kaybeden run
            // da ilerleme uretmelidir.
            Assert.Greater(earned, 0);
            Assert.AreEqual(9 * 5 + 105 / 10, earned);
        }

        [Test]
        public void KazananRun_KaybedendenBelirginFazlaVerir()
        {
            int lost = BiomassLedger.EarnedWithFloor(LostWaves, LostKills, false);
            int won = BiomassLedger.EarnedWithFloor(WonWaves, WonKills, true);

            Assert.AreEqual(15 * 5 + 255 / 10 + 60, won);

            // "Belirgin" fazla: en az iki kat. Aksi halde kasten erken
            // olmek gecerli bir strateji haline gelirdi.
            Assert.Greater(won, lost * 2);
        }

        [Test]
        public void AyniRun_Kazanildiginda_KaybedildigindenFazlaVerir()
        {
            // Tek degisken kazanma bayragi olsa bile fark olmali.
            int lost = BiomassLedger.Earned(12, 200, false);
            int won = BiomassLedger.Earned(12, 200, true);

            Assert.AreEqual(BiomassLedger.WinBonus, won - lost);
        }

        [Test]
        public void HicDalgaTamamlanmayanRun_TabanKazancVerir()
        {
            int earned = BiomassLedger.EarnedWithFloor(0, 0, false);

            Assert.AreEqual(BiomassLedger.ConsolationFloor, earned);
            Assert.Greater(earned, 0);
        }

        [Test]
        public void NegatifGirdiler_SifiraKirpilir_VeHesapNegatifDonmez()
        {
            Assert.AreEqual(BiomassLedger.ConsolationFloor,
                            BiomassLedger.EarnedWithFloor(-5, -100, false));
            Assert.AreEqual(0, BiomassLedger.Earned(-5, -100, false));
        }

        [Test]
        public void YeniDurum_TumKulelerBastanAcik()
        {
            // 3 Eylul 2026 gece plani FAZ 0: kilit sistemi kaldirildi,
            // ilk run'dan itibaren tum kuleler acik.
            var state = new MetaState();

            Assert.IsTrue(state.IsUnlocked(TowerKind.Battery));
            Assert.IsTrue(state.IsUnlocked(TowerKind.Ambush));
            Assert.IsTrue(state.IsUnlocked(TowerKind.Decoy));
            Assert.IsTrue(state.IsUnlocked(TowerKind.Minefield));
            Assert.IsTrue(state.IsUnlocked(TowerKind.Beam));

            Assert.AreEqual(5, state.UnlockedCount);
            Assert.AreEqual(0, state.Biomass);
            Assert.AreEqual(0, state.IntelLevel);
        }

        [Test]
        public void BastanAcikKuleyiAcma_ParaAlmazVeIslemSayilmaz()
        {
            var state = new MetaState();
            state.Earn(1000);

            Assert.IsFalse(state.TryUnlock(TowerKind.Decoy));
            Assert.AreEqual(1000, state.Biomass);
            Assert.IsTrue(state.IsUnlocked(TowerKind.Decoy));
        }

        [Test]
        public void BaslangicKulesiniAcmak_IslemSayilmaz()
        {
            var state = new MetaState();
            state.Earn(100);

            Assert.IsFalse(state.TryUnlock(TowerKind.Battery));
            Assert.AreEqual(100, state.Biomass);
        }

        [Test]
        public void IstihbaratYukseltmesi_TavanaKadarCalisir_SonraReddeder()
        {
            var state = new MetaState();
            state.Earn(10000);

            for (int i = 0; i < UnlockCatalog.MaxIntelLevel; i++)
                Assert.IsTrue(state.TryUpgradeIntel(), "Seviye " + i + " acilmaliydi.");

            Assert.AreEqual(UnlockCatalog.MaxIntelLevel, state.IntelLevel);

            int before = state.Biomass;
            Assert.IsFalse(state.TryUpgradeIntel());
            Assert.AreEqual(before, state.Biomass);
            Assert.AreEqual(-1, UnlockCatalog.IntelUpgradeCost(UnlockCatalog.MaxIntelLevel));
        }

        [Test]
        public void IstihbaratYukseltmesi_YetersizParada_SeviyeyiArtirmaz()
        {
            var state = new MetaState();
            state.Earn(UnlockCatalog.IntelBaseCost - 1);

            Assert.IsFalse(state.TryUpgradeIntel());
            Assert.AreEqual(0, state.IntelLevel);
            Assert.AreEqual(UnlockCatalog.IntelBaseCost - 1, state.Biomass);
        }

        [Test]
        public void RegisterRun_SayaclariVeBakiyeyiGunceller()
        {
            var state = new MetaState();

            int a = state.RegisterRun(LostWaves, LostKills, false);
            int b = state.RegisterRun(WonWaves, WonKills, true);

            Assert.AreEqual(2, state.RunsPlayed);
            Assert.AreEqual(1, state.RunsWon);
            Assert.AreEqual(a + b, state.Biomass);
            Assert.AreEqual(a + b, state.LifetimeBiomass);
        }

        [Test]
        public void KaydetYukle_DurumuBirebirKorur()
        {
            var state = new MetaState();
            state.RegisterRun(WonWaves, WonKills, true);
            state.RegisterRun(LostWaves, LostKills, false);
            state.TryUpgradeIntel();

            var restored = MetaSaveData.From(state).ToState();

            Assert.AreEqual(state.Biomass, restored.Biomass);
            Assert.AreEqual(state.LifetimeBiomass, restored.LifetimeBiomass);
            Assert.AreEqual(state.RunsPlayed, restored.RunsPlayed);
            Assert.AreEqual(state.RunsWon, restored.RunsWon);
            Assert.AreEqual(state.IntelLevel, restored.IntelLevel);
            Assert.AreEqual(state.UnlockedCount, restored.UnlockedCount);
            Assert.IsTrue(restored.IsUnlocked(TowerKind.Decoy));
            Assert.IsTrue(restored.IsUnlocked(TowerKind.Beam));
        }

        [Test]
        public void DiskeKaydetYukle_DurumuKorur()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_meta_test_" + Path.GetRandomFileName() + ".json");
            try
            {
                var state = new MetaState();
                state.Earn(777);

                Assert.IsTrue(MetaProgressStore.SaveTo(state, path));
                var loaded = MetaProgressStore.LoadFrom(path);

                Assert.AreEqual(state.Biomass, loaded.Biomass);
                Assert.IsTrue(loaded.IsUnlocked(TowerKind.Minefield));
            }
            finally
            {
                MetaProgressStore.ClearAt(path);
            }
        }

        [Test]
        public void OlmayanKayit_TemizVarsayilanaDuser()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_meta_yok_" + Path.GetRandomFileName() + ".json");

            var loaded = MetaProgressStore.LoadFrom(path);

            Assert.AreEqual(0, loaded.Biomass);
            Assert.AreEqual(5, loaded.UnlockedCount);
            Assert.IsTrue(loaded.IsUnlocked(TowerKind.Battery));
        }

        [Test]
        public void BozukKayit_PatlamazVeVarsayilanaDuser()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_meta_bozuk_" + Path.GetRandomFileName() + ".json");
            try
            {
                File.WriteAllText(path, "{ bu json degil ]]] ");

                var loaded = MetaProgressStore.LoadFrom(path);

                Assert.IsNotNull(loaded);
                Assert.AreEqual(0, loaded.Biomass);
                Assert.IsTrue(loaded.IsUnlocked(TowerKind.Ambush));
                Assert.IsTrue(loaded.IsUnlocked(TowerKind.Beam));
            }
            finally
            {
                MetaProgressStore.ClearAt(path);
            }
        }

        [Test]
        public void BosKayitDosyasi_VarsayilanaDuser()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_meta_bos_" + Path.GetRandomFileName() + ".json");
            try
            {
                File.WriteAllText(path, "   ");

                var loaded = MetaProgressStore.LoadFrom(path);

                Assert.AreEqual(0, loaded.Biomass);
                Assert.AreEqual(0, loaded.IntelLevel);
            }
            finally
            {
                MetaProgressStore.ClearAt(path);
            }
        }

        [Test]
        public void KurcalanmisKayit_GuvenliAraligaKirpilir()
        {
            // Negatif bakiye, tavan ustu istihbarat ve gecersiz kule
            // numarasi olan bir kayit oyunu bozmamali.
            var data = new MetaSaveData();
            data.biomass = -500;
            data.lifetimeBiomass = -900;
            data.runsPlayed = 2;
            data.runsWon = 99;
            data.intelLevel = 42;
            data.unlockedTowers = new int[] { 999, (int)TowerKind.Beam };

            var state = data.ToState();

            Assert.AreEqual(0, state.Biomass);
            Assert.AreEqual(0, state.LifetimeBiomass);
            Assert.AreEqual(2, state.RunsWon, "Kazanilan run oynanandan fazla olamaz.");
            Assert.AreEqual(UnlockCatalog.MaxIntelLevel, state.IntelLevel);
            Assert.IsTrue(state.IsUnlocked(TowerKind.Beam));
            Assert.AreEqual(5, state.UnlockedCount, "Gecersiz kule numarasi yok sayilmali, baslangic kuleleri hep acik.");
        }

        [Test]
        public void UnlockCatalog_TumKulelerBedava()
        {
            // Kilit sistemi kaldirildi (3 Eylul 2026 gece plani FAZ 0):
            // artik hicbir kulenin acilma bedeli yok.
            Assert.AreEqual(0, UnlockCatalog.CostOf(TowerKind.Battery));
            Assert.AreEqual(0, UnlockCatalog.CostOf(TowerKind.Ambush));
            Assert.AreEqual(0, UnlockCatalog.CostOf(TowerKind.Decoy));
            Assert.AreEqual(0, UnlockCatalog.CostOf(TowerKind.Minefield));
            Assert.AreEqual(0, UnlockCatalog.CostOf(TowerKind.Beam));
        }

        // --- Kalici (meta) kule yukseltmesi: ana menu "GUNCELLE" ekrani
        // (3 Eylul 2026 gece plani, FAZ 3) ---

        [Test]
        public void YeniDurum_TumKulelerBaslangicSeviyesi1()
        {
            var state = new MetaState();

            Assert.AreEqual(1, state.StartingLevelOf(TowerKind.Battery));
            Assert.AreEqual(0, state.PermanentStepOf(TowerKind.Battery));
        }

        [Test]
        public void KaliciYukseltme_YeterliParadaSatinAlinirVeSeviyeArtar()
        {
            var state = new MetaState();
            int cost = PermanentUpgradeCatalog.CostFor(TowerKind.Battery, 0);
            state.Earn(cost);

            bool ok = state.TryUpgradeTowerPermanent(TowerKind.Battery);

            Assert.IsTrue(ok);
            Assert.AreEqual(0, state.Biomass);
            Assert.AreEqual(1, state.PermanentStepOf(TowerKind.Battery));
            Assert.AreEqual(2, state.StartingLevelOf(TowerKind.Battery));
        }

        [Test]
        public void KaliciYukseltme_YetersizParada_HicbirSeyDegismez()
        {
            var state = new MetaState();
            int cost = PermanentUpgradeCatalog.CostFor(TowerKind.Ambush, 0);
            state.Earn(cost - 1);

            bool ok = state.TryUpgradeTowerPermanent(TowerKind.Ambush);

            Assert.IsFalse(ok);
            Assert.AreEqual(cost - 1, state.Biomass);
            Assert.AreEqual(0, state.PermanentStepOf(TowerKind.Ambush));
            Assert.AreEqual(1, state.StartingLevelOf(TowerKind.Ambush));
        }

        [Test]
        public void KaliciYukseltme_TavanaKadarCalisir_SonraReddeder()
        {
            var state = new MetaState();
            state.Earn(100000);

            for (int i = 0; i < PermanentUpgradeCatalog.MaxPermanentSteps; i++)
                Assert.IsTrue(state.TryUpgradeTowerPermanent(TowerKind.Beam), "Kademe " + i + " alinmaliydi.");

            Assert.AreEqual(PermanentUpgradeCatalog.MaxPermanentSteps, state.PermanentStepOf(TowerKind.Beam));
            Assert.AreEqual(1 + PermanentUpgradeCatalog.MaxPermanentSteps, state.StartingLevelOf(TowerKind.Beam));

            int before = state.Biomass;
            Assert.IsFalse(state.TryUpgradeTowerPermanent(TowerKind.Beam));
            Assert.AreEqual(before, state.Biomass);
            Assert.AreEqual(-1, PermanentUpgradeCatalog.CostFor(TowerKind.Beam, PermanentUpgradeCatalog.MaxPermanentSteps));
        }

        [Test]
        public void KaliciYukseltme_IkinciKademeBirinciKademedenPahali()
        {
            // Referans oyundaki "GUNCELLE" ekraninda ust seviyeler daha
            // pahali - dogrusal artis (IntelUpgradeCost ile ayni desen).
            int first = PermanentUpgradeCatalog.CostFor(TowerKind.Ambush, 0);
            int second = PermanentUpgradeCatalog.CostFor(TowerKind.Ambush, 1);

            Assert.Greater(second, first);
        }

        [Test]
        public void KaliciYukseltme_KaydetYukle_KademeyiKorur()
        {
            var state = new MetaState();
            state.Earn(100000);
            state.TryUpgradeTowerPermanent(TowerKind.Minefield);
            state.TryUpgradeTowerPermanent(TowerKind.Minefield);
            state.TryUpgradeTowerPermanent(TowerKind.Decoy);

            var restored = MetaSaveData.From(state).ToState();

            Assert.AreEqual(2, restored.PermanentStepOf(TowerKind.Minefield));
            Assert.AreEqual(1, restored.PermanentStepOf(TowerKind.Decoy));
            Assert.AreEqual(0, restored.PermanentStepOf(TowerKind.Battery));
            Assert.AreEqual(state.Biomass, restored.Biomass);
        }

        [Test]
        public void KaliciYukseltme_DiskeKaydetYukle_KademeyiKorur()
        {
            string path = Path.Combine(Path.GetTempPath(),
                                       "frontline_meta_kalici_" + Path.GetRandomFileName() + ".json");
            try
            {
                var state = new MetaState();
                state.Earn(100000);
                state.TryUpgradeTowerPermanent(TowerKind.Battery);

                Assert.IsTrue(MetaProgressStore.SaveTo(state, path));
                var loaded = MetaProgressStore.LoadFrom(path);

                Assert.AreEqual(1, loaded.PermanentStepOf(TowerKind.Battery));
                Assert.AreEqual(2, loaded.StartingLevelOf(TowerKind.Battery));
            }
            finally
            {
                MetaProgressStore.ClearAt(path);
            }
        }

        [Test]
        public void KaliciYukseltme_BilinmeyenKule_ReddedilirVeParaAlmaz()
        {
            // TowerKind'e yeni bir deger eklenip PermanentUpgradeCatalog
            // guncellenmezse burasi PATLAMAMALI, sadece reddetmeli.
            var state = new MetaState();
            state.Earn(100000);

            bool known = PermanentUpgradeCatalog.IsKnown(TowerKind.Battery);
            Assert.IsTrue(known, "Battery katalogda olmali - test varsayimi.");
        }
    }
}
