using NUnit.Framework;
using UnityEngine;
using Frontline.Core;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// 3 Eylul 2026: kule yukseltme sistemi (3 kademeli gorsel/guc).
    /// Sprite degisimi Unity asset yuklemesi gerektirdigi icin burada
    /// sinanmiyor (TowerPlacer/ArtLibrary entegrasyonu manuel + Play Mode
    /// dogrulamasiyla kontrol edildi) - burada yalnizca SAF, sayisal
    /// kademe/hasar/maliyet mantigi sinanir.
    /// </summary>
    public sealed class EmplacementUpgradeTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        private Emplacement NewTower()
        {
            go = new GameObject("Tower");
            return go.AddComponent<Emplacement>();
        }

        [Test]
        public void NewTower_StartsAtLevel1()
        {
            Emplacement tower = NewTower();
            Assert.AreEqual(1, tower.Level);
        }

        [Test]
        public void Upgrade_IncrementsLevel()
        {
            Emplacement tower = NewTower();

            bool ok = tower.Upgrade();

            Assert.IsTrue(ok);
            Assert.AreEqual(2, tower.Level);
        }

        [Test]
        public void Upgrade_StopsAtMaxLevel()
        {
            Emplacement tower = NewTower();
            tower.Upgrade(); // 1 -> 2
            tower.Upgrade(); // 2 -> 3

            bool ok = tower.Upgrade(); // 3 -> reddedilir

            Assert.IsFalse(ok);
            Assert.AreEqual(3, tower.Level);
            Assert.AreEqual(Emplacement.MaxLevel, tower.Level);
        }

        [Test]
        public void StatMultiplier_Level1_IsUnityGain()
        {
            // Mevcut denge testleri (CurrentRunCalibrationTests) seviye 1
            // varsayimiyla yaziliyor - carpan tam 1 olmali, yoksa sessizce
            // her kuleyi guclendirmis oluruz.
            Assert.AreEqual(1f, Emplacement.StatMultiplier(1), 0.0001f);
        }

        [Test]
        public void StatMultiplier_ArtarSeviyeBasina20Yuzde()
        {
            Assert.AreEqual(1.2f, Emplacement.StatMultiplier(2), 0.0001f);
            Assert.AreEqual(1.4f, Emplacement.StatMultiplier(3), 0.0001f);
        }

        [Test]
        public void UpgradeCost_SeviyeBasinaYuzde50Artar()
        {
            // fromLevel=1 -> 2. kademe maliyeti: taban * 1.5
            Assert.AreEqual(150, Emplacement.UpgradeCost(100, 1));
            // fromLevel=2 -> 3. kademe maliyeti: taban * 1.5^2
            Assert.AreEqual(225, Emplacement.UpgradeCost(100, 2));
        }

        [Test]
        public void RecordSpend_AccumulatesAcrossCalls()
        {
            Emplacement tower = NewTower();

            tower.RecordSpend(20);
            tower.RecordSpend(30);

            Assert.AreEqual(50, tower.SpentTotal);
        }

        [Test]
        public void RecordSpend_IgnoresZeroOrNegative()
        {
            Emplacement tower = NewTower();

            tower.RecordSpend(20);
            tower.RecordSpend(0);
            tower.RecordSpend(-5);

            Assert.AreEqual(20, tower.SpentTotal);
        }

        [Test]
        public void SellValue_IsSixtyPercentOfSpentTotal()
        {
            // 3 Eylul 2026, dokunmatik Sat butonu: sell value uydurma bir
            // sayi degil, harcanan toplamin sabit bir orani (bkz.
            // Emplacement.SellRefundRatio).
            Emplacement tower = NewTower();
            tower.RecordSpend(100);

            Assert.AreEqual(60, tower.SellValue());
        }

        [Test]
        public void SellValue_ZeroWhenNothingSpent()
        {
            // Bedava (draft odulu) kule satista hicbir sey getirmemeli -
            // dogru, cunku hic besin harcanmadi.
            Emplacement tower = NewTower();

            Assert.AreEqual(0, tower.SellValue());
        }

        // NOT: TowerWeapon.ApplyLevelMultiplier'in kendisi Bind() uzerinden
        // Battlefield gerektirdigi icin (worldRange hesaplar) EditMode'da
        // sahnesiz sinanmiyor - saf carpan matematigi yukarida
        // Emplacement.StatMultiplier ile kapali, gercek TowerWeapon
        // entegrasyonu Play Mode'da (sandbox sahne, execute_code) dogrudan
        // dogrulandi (bkz. gorev raporu).
    }
}
