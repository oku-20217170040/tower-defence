using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Yaban Arisi (ucan birlik, spec 5.2: "ucar, koridor tanimaz").
    ///
    /// BU DOSYA BIR DENGE TESTI DEGILDIR. Ucanin sayilari (can 0,6 · hiz
    /// 1,3 · pay 0,06 · giris dalgasi 8) OLCULMEDI - tahmindir. Buradaki
    /// testler o sayilarin DOGRU oldugunu degil, secilen TASARIM
    /// KISITLARININ kodda hala gecerli oldugunu sabitler:
    ///
    ///   1. ucan yolu yok sayar (duz cizgi, duvar tanimaz),
    ///   2. ucan kesif izi birakmaz (ogrenme katmanini kirletmez),
    ///   3. ucan erken dalgalarda cikmaz ve payi kucuktur,
    ///   4. yururyen birliklerin davranisi hic degismemistir.
    ///
    /// 4. madde bu dosyanin en onemli kismidir: ucan, oyunun merkez
    /// mekanigini (koridor + kandirma, spec 3) atlayan tek birliktir.
    /// Yanlis tasarlanirsa oyuncunun butun tuzak kurgusunu anlamsizlastirir.
    /// </summary>
    public class FlyingUnitTests
    {
        private const int Width = 11;
        private const int Height = 20;

        /// <summary>
        /// Ortadan tam bir duvar seridi gecen grid. Yururyen birlik icin
        /// gecilemez; ucan icin yok hukmunde olmali.
        /// </summary>
        private static BattleGrid WalledGrid()
        {
            var grid = new BattleGrid(Width, Height);

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    grid.Set(grid.Index(x, y), CellKind.Corridor);

            // 10. satirin tamami duvar.
            for (int x = 0; x < Width; x++)
                grid.Set(grid.Index(x, 10), CellKind.Blocked);

            return grid;
        }

        private static UnitMix? Find(UnitMix[] roster, UnitType type)
        {
            foreach (var mix in roster)
                if (mix.Profile.Type == type) return mix;

            return null;
        }

        // ---- 1. Kisit: yolu yok sayar -------------------------------------

        [Test]
        public void Flyer_IgnoresTerrain_AndWalkingUnitsDoNot()
        {
            Assert.IsTrue(UnitProfile.Flyer().IgnoresTerrain,
                "Ucan araziyi tanimali - bayrak kapaliysa yol bulucuya duser " +
                "ve koridora hapsolur.");

            UnitType[] walking =
            {
                UnitType.Infantry, UnitType.Scout, UnitType.Soldier,
                UnitType.Blind, UnitType.Brood, UnitType.Queen
            };

            foreach (var type in walking)
                Assert.IsFalse(UnitProfile.For(type).IgnoresTerrain,
                    type + " artik araziyi yok sayiyor - ucan bayragi " +
                    "yururyen birliklere sizmis, oyunun koridor mekanigi coker.");
        }

        [Test]
        public void DefaultProfileStruct_StaysOnTheGround()
        {
            // Varsayilanlarin ESKI DAVRANISI korumasi, mevcut 200 testin
            // bagli oldugu varsayim. Bu yuzden iki bayrak da negatif
            // polaritede secildi.
            var bare = default(UnitProfile);

            Assert.IsFalse(bare.IgnoresTerrain, "default(UnitProfile) ucuyor.");
            Assert.IsFalse(bare.LeavesNoRecon, "default(UnitProfile) iz birakmiyor.");
        }

        [Test]
        public void FlightPath_CrossesWalls_AndReachesTheBase()
        {
            var grid = WalledGrid();
            int spawn = grid.Index(0, Height - 1);
            int baseCell = grid.Index(Width / 2, 0);

            var path = new List<int>();
            FlightPath.Build(grid, spawn, baseCell, path);

            Assert.AreEqual(spawn, path[0], "Ucus rotasi dogum hucresinde baslamiyor.");
            Assert.AreEqual(baseCell, path[path.Count - 1], "Ucus rotasi usya varmiyor.");

            bool crossedWall = false;
            foreach (int cell in path)
                if (!grid.IsWalkable(cell)) crossedWall = true;

            Assert.IsTrue(crossedWall,
                "Ucan tam bir duvar seridini gecmeden usya ulasti - rota " +
                "araziyi hala hesaba katiyor demektir.");
        }

        [Test]
        public void FlightPath_IsContiguous_OneCellPerStep()
        {
            var grid = WalledGrid();
            var path = new List<int>();
            FlightPath.Build(grid, grid.Index(10, 19), grid.Index(1, 0), path);

            for (int i = 1; i < path.Count; i++)
            {
                int dx = grid.X(path[i]) - grid.X(path[i - 1]);
                int dy = grid.Y(path[i]) - grid.Y(path[i - 1]);
                int step = (dx < 0 ? -dx : dx) + (dy < 0 ? -dy : dy);

                Assert.AreEqual(1, step,
                    "Ucus rotasi " + i + ". adimda atladi ya da capraz gitti; " +
                    "hareket kodu her adimda tek hucre ilerlemeye bagli.");
            }
        }

        [Test]
        public void FlightPath_IsTheShortestPossibleRoute()
        {
            // Ucanin degeri "kestirme gitmesi"; rota Manhattan mesafesinden
            // uzunsa duz cizgi kirilmis demektir.
            var grid = WalledGrid();
            int spawn = grid.Index(0, Height - 1);
            int baseCell = grid.Index(Width / 2, 0);

            var path = new List<int>();
            FlightPath.Build(grid, spawn, baseCell, path);

            int manhattan = (Width / 2 - 0) + (Height - 1 - 0);

            Assert.AreEqual(manhattan + 1, path.Count,
                "Ucus rotasi en kisa yol degil (hucre sayisi = Manhattan + 1 olmali).");
        }

        [Test]
        public void FlightPath_SameCell_ProducesASingleCell()
        {
            var grid = WalledGrid();
            var path = new List<int>();
            int cell = grid.Index(5, 5);

            FlightPath.Build(grid, cell, cell, path);

            Assert.AreEqual(1, path.Count, "Yerinde dogan ucan sonsuz doneryor olabilir.");
            Assert.AreEqual(cell, path[0]);
        }

        // ---- 2. Kisit: iz birakmaz ----------------------------------------

        [Test]
        public void Flyer_LeavesNoRecon_OnEveryChannel()
        {
            var flyer = UnitProfile.Flyer();

            Assert.IsTrue(flyer.LeavesNoRecon,
                "Ucan iz birakiyor - duvar uzerindeki hucrelere kesif izi " +
                "yazilir ve yururyen ordu ulasamayacagi bir rotayi ogrenir.");

            Assert.AreEqual(0f, flyer.DepositMultiplier, "Ucan yururken iz birakiyor.");
            Assert.AreEqual(0f, flyer.ReinforceMultiplier, "Ucan varinca rota pekisiyor.");
            Assert.AreEqual(0f, flyer.WeakenMultiplier, "Ucan olunce rota sonumleniyor.");
        }

        [Test]
        public void WalkingUnits_StillLeaveRecon()
        {
            // Regresyon: ogrenme katmani yururyen birlikler uzerinden isler.
            // Bu bozulursa spec 3'un tamami calismaz.
            UnitType[] walking =
            {
                UnitType.Infantry, UnitType.Scout, UnitType.Soldier,
                UnitType.Blind, UnitType.Brood, UnitType.Queen
            };

            foreach (var type in walking)
            {
                var profile = UnitProfile.For(type);

                Assert.IsFalse(profile.LeavesNoRecon, type + " artik iz birakmiyor.");
                Assert.Greater(profile.DepositMultiplier, 0f, type + " deposit carpani sifirlandi.");
            }
        }

        // ---- 3. Kisit: seyrek ve gec ---------------------------------------

        [Test]
        public void Flyer_DoesNotShowUpInTheEarlyWaves()
        {
            // Oyuncu once cekirdek mekanigi ogrenmeli (spec 3.4: itmek +
            // cekmek birlikte). Koridoru atlayan bir dusman erken gelirse
            // koridorun onemi hic ogrenilmez. Ucan orduya en son katilan
            // tur olmali.
            Assert.GreaterOrEqual(UnitRoster.FlyerFirstWave, 8,
                "Ucan cok erken giriyor: FlyerFirstWave=" + UnitRoster.FlyerFirstWave);

            Assert.Greater(UnitRoster.FlyerFirstWave, UnitRoster.QueenFirstWave,
                "Ucan kraliceden once giriyor; ogrenme sirasi bozulur.");
            Assert.Greater(UnitRoster.FlyerFirstWave, UnitRoster.SoldierFirstWave,
                "Ucan askerden once giriyor; ogrenme sirasi bozulur.");

            var roster = UnitRoster.WithFlyers();
            var mix = Find(roster, UnitType.Flyer);

            Assert.IsTrue(mix.HasValue, "Ucan roster'da yok.");
            Assert.AreEqual(UnitRoster.FlyerFirstWave, mix.Value.FirstWave);

            for (int wave = 1; wave < UnitRoster.FlyerFirstWave; wave++)
                Assert.Less(wave, mix.Value.FirstWave,
                    wave + ". dalgada ucan cikabiliyor.");
        }

        [Test]
        public void Flyer_IsRare_AndFragileEnoughToHaveAnAnswer()
        {
            // Payi kucuk: ucan kandirilamaz, dolayisiyla payi buyudukce
            // oyuncunun tuzak kurgusunun odulu erir.
            Assert.LessOrEqual(UnitRoster.FlyerShare, 0.10f,
                "Ucan payi buyudu: " + UnitRoster.FlyerShare +
                " - kandirma mekanigi odulunu kaybeder.");
            Assert.Less(UnitRoster.FlyerShare, UnitRoster.ScoutShare);
            Assert.Less(UnitRoster.FlyerShare, UnitRoster.SoldierShare);

            // Cani dusuk: menzili yeten bir kule ona GERCEKTEN cevap
            // verebilmeli (spec 5.2 "cevap birak" kosulu).
            Assert.Less(UnitProfile.Flyer().HpMultiplier, 1f,
                "Ucan piyadeden dayanikli - kuleler ona yetismekte zorlanirken " +
                "bir de cani yuksekse cevapsiz bir tehdit olur.");
        }

        // ---- 4. Kisit: yururyen ordu degismedi ------------------------------

        [Test]
        public void DefaultRoster_StillHasNoFlyer()
        {
            // Kalibrasyon korumasi: Default() ScoutTests, BeamTowerTests ve
            // UnitSpawner tarafindan kullaniliyor. Ucan olculmeden oraya
            // girerse ucanla ilgisi olmayan olculmus testler sessizce kayar.
            Assert.IsFalse(Find(UnitRoster.Default(), UnitType.Flyer).HasValue,
                "Ucan olculmeden Default() bilesimine girdi; kalibrasyona " +
                "bagli testler artik baska bir oyunu olcuyor.");
        }

        [Test]
        public void WithFlyers_KeepsEveryWalkingEntryIdentical()
        {
            var baseline = UnitRoster.Default();
            var withFlyers = UnitRoster.WithFlyers();

            Assert.AreEqual(baseline.Length + 1, withFlyers.Length,
                "WithFlyers() yalnizca tek bir giris eklemeli.");

            for (int i = 0; i < baseline.Length; i++)
            {
                Assert.AreEqual(baseline[i].Profile.Type, withFlyers[i].Profile.Type);
                Assert.AreEqual(baseline[i].Share, withFlyers[i].Share,
                    baseline[i].Profile.Type + " payi degisti.");
                Assert.AreEqual(baseline[i].FirstWave, withFlyers[i].FirstWave,
                    baseline[i].Profile.Type + " giris dalgasi degisti.");
            }
        }

        [Test]
        public void RosterShares_StillLeaveRoomForInfantry()
        {
            // Paylarin toplami 1'i asarsa piyade dalgadan tamamen kalkar ve
            // taban birlik yok olur; RunSimulator.PickProfile kalan payi
            // piyadeye veriyor.
            float total = 0f;
            foreach (var mix in UnitRoster.WithFlyers()) total += mix.Share;

            Assert.Less(total, 1f,
                "Paylarin toplami 1'i asti - piyade dalgadan silindi. toplam=" + total);
            Assert.Greater(1f - total, 0.3f,
                "Piyade payi 0,3'un altina dustu; taban birlik dalganin " +
                "govdesi olmaktan cikti. piyade payi=" + (1f - total));
        }
    }
}
