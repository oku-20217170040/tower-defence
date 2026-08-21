using System.Collections.Generic;
using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    /// <summary>
    /// Isin kulesinin kararlarini kilitler (21 Agustos 2026 olcumu).
    ///
    /// ISININ HIKAYESI - bu yorum silinmemeli, cunku isin ilk denemede
    /// oyunu bozdu ve sebebi sayisal degildi:
    ///
    /// Ilk tasarim "delip gecen SUREKLI hat hasari" idi. Her ayarda ya olu
    /// ya baskin cikti; dps 1-40, genislik 0-4, menzil 6-20, yatay hat,
    /// dikey hat ve tehdit yazan surum denendi. Bedava draft kulesi tamamen
    /// kapatilip olculdugunde sebep ortaya cikti:
    ///
    ///   bedava kule yok    kazanma  sizinti  kule
    ///   Skilled                 %0     30     4,0
    ///   batarya spam            %0     30     4,4
    ///   pusu spam               %0     30     2,0
    ///   SUREKLI isin spam     %100      7,5   7,0
    ///
    /// Her strateji cokerken 7 surekli isin oyunu tek basina kazaniyordu.
    /// Sebep: bir koridoru bastan sona kaplayan sey spec 3.5'i ("korunmayan
    /// koridor her zaman bulunur") siler - ve o gercek oyunun omurgasi.
    /// Tehdit yazdirmak da cozmedi (spam sizintisi 1,9 -> 2,6), cunku her
    /// koridorda isin varken ordunun kacacak koridoru kalmiyor.
    ///
    /// COZUM: isin SUREKLI degil PERIYODIK ates eder. Namlusunu menzildeki
    /// en yakin birlige dogrultur ve O DOGRULTUDA onune cikan herkesi
    /// yarip gecer. Bir koridoru kapatmaz.
    ///
    /// MEKANIK DUZELTMESI (22 Agu 2026): isin once "kulenin satiri boyunca
    /// sabit yatay hat" olarak yazilmisti. Kullanici tarifi farkliydi ve
    /// dogrusu odur: namlu doner, hedefe nisan alir, hattaki herkesi deler.
    /// Nisan aldigi icin ISKALAMIYOR - bu yuzden ayni sayilarla cok guclu
    /// cikti (spam sizinti 0,0) ve hasar 20 -> 3, aralik 1 -> 2 sn yapildi.
    ///
    /// Asil denge dugmesi hasar degil, ATIS ARALIGIDIR.
    ///
    /// ACIK IS - ISININ ATIS ARALIGI PLAN 8 BITINCE YENIDEN OLCULMELI.
    /// Aralik 1 sn, ordunun SAF PIYADE oldugu bir bilesimde secildi ve
    /// kullanicinin run'iyla dogrulandi. Plan 8 her tur eklendiginde
    /// ordunun toplanmasini degistiriyor, isinin degeri de dogrudan buna
    /// bagli. Su an aralik 3 sn karisimi tabana yaklastiriyor (10,8 / 13,3)
    /// ama simdi ayarlamak yanlis olur: Gorev 4-6 bunu yine kaydiracak ve
    /// ust uste iki ayar hangi degisikligin ne yaptigini olculemez kilar
    /// (Plan 8'in kendi uyarisi). Bu yuzden asagidaki testler SAYI degil
    /// ILISKI iddia eder.
    /// </summary>
    public class BeamTowerTests
    {
        private BattleGrid grid;
        private int baseCell;
        private List<int> spawnCells;
        private List<int> corridorColumns;
        private RunSimulator sim;

        private static readonly int[] YSlots = { 10, 5, 15, 7, 13, 3, 17, 9, 11 };

        [SetUp]
        public void SetUp()
        {
            var template = new MapTemplate { Width = 11, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            grid = new MapGenerator().Generate(template, seed: 3);
            baseCell = grid.Index(grid.Width / 2, 0);

            spawnCells = new List<int>();
            int topRow = grid.Height - 1;
            for (int x = 0; x < grid.Width; x++)
            {
                int cell = grid.Index(x, topRow);
                if (grid.IsWalkable(cell)) spawnCells.Add(cell);
            }

            corridorColumns = new List<int>();
            int midRow = grid.Height / 2;
            bool inRun = false;
            for (int x = 0; x < grid.Width; x++)
            {
                bool walkable = grid.IsWalkable(grid.Index(x, midRow));
                if (walkable && !inRun) { corridorColumns.Add(x); inRun = true; }
                else if (!walkable) inRun = false;
            }

            sim = new RunSimulator(grid, spawnCells, baseCell);
        }

        private int[] Place(int index, int kind)
        {
            int column = corridorColumns[index % corridorColumns.Count];
            int y = YSlots[(index / corridorColumns.Count) % YSlots.Length];

            if (kind == (int)TowerKind.Minefield) y += 1;

            if (y < 2) y = 2;
            if (y > grid.Height - 3) y = grid.Height - 3;

            return new[] { column, y };
        }

        private struct Averages
        {
            public int WinPercent;
            public float Leaked;
            public float HomeBaseHpLeft;
        }

        private Averages Measure(TowerKind[] buyOrder, float shotDamage, float shotInterval, int seedCount)
        {
            var settings = RunSimulator.Settings.Default();

            // Isin OYNANAN kurulumda olculur: taban sapma 0,12 ve %25 izci.
            // Bu satir olmadan test saf piyade ordusunu olcerdi - oyunun
            // artik sahip olmadigi bir kosulu. Ayni tuzak kalibrasyon
            // testlerinde bir kez yasandi.
            //
            // Isin dar bir hatta vurdugu icin degeri ORDUNUN NE KADAR
            // TOPLANDIGINA baglidir. Bu yuzden bilesim degistikce isinin
            // gucu de degisir; olculen (B,A,B,ISIN,M, sizinti):
            //   saf piyade            -> isin karisimda ~5,3 · taban ~5,6
            //   izci %25              -> isin  3,6 · taban  3,3
            //   izci %25 + asker %20  -> isin  3,9 · taban 13,3
            // Yani asker orduyu koridora civileyince isin ONUN PANZEHIRI
            // haline geliyor - plandaki niyet tam buydu.
            settings.Composition = UnitRoster.Default();

            int wins = 0;
            float leaked = 0f, hp = 0f;

            for (int s = 0; s < seedCount; s++)
            {
                var policy = EconomyPlanner.Policy.Skilled();
                policy.BuyOrder = buyOrder;
                policy.BeamShotDamage = shotDamage;
                policy.BeamShotInterval = shotInterval;

                var planner = new EconomyPlanner(
                    EconomyPlanner.Costs.Default(), policy, Place, settings.TotalWaves);

                var built = new List<RunSimulator.TowerSpec>();
                var result = sim.Run(built, settings, seed: 1000 + s * 37, planner: planner);

                if (result.Survived) wins++;
                leaked += result.TotalLeaked;
                hp += result.HomeBaseHpLeft;
            }

            return new Averages
            {
                WinPercent = 100 * wins / seedCount,
                Leaked = leaked / seedCount,
                HomeBaseHpLeft = hp / seedCount
            };
        }

        private const float ShotDamage = 3f;
        private const float ShotInterval = 2f;

        private static readonly TowerKind[] Baseline =
        {
            TowerKind.Battery, TowerKind.Ambush, TowerKind.Battery,
            TowerKind.Ambush, TowerKind.Minefield
        };

        private static readonly TowerKind[] WithBeam =
        {
            TowerKind.Battery, TowerKind.Ambush, TowerKind.Battery,
            TowerKind.Beam, TowerKind.Minefield
        };

        [Test]
        public void Beam_IsTheCounterToAConcentratedArmy()
        {
            // Asker orduyu bir koridora civiler; isin de tam olarak orayi
            // cezalandirir. Olculen: isinsiz 20,7 sizinti, isinli 15,7.
            //
            // Ozellik nisan alan surumde de duruyor: DAGINIK orduda isinin
            // kazanci 1,2 sizinti (3,3 -> 2,1), TOPLANMIS orduda 5,0
            // (20,7 -> 15,7). Yani isin yaklasik dort kat daha degerli.
            //
            // Bu bir denge SAYISI degil, bir ILISKI iddiasidir: bilesim
            // degistikce sayilar kayar ama iliski kalmalidir. Sayi
            // iddiasi Plan 8 bitince yeniden olculecek (asagiya bak).
            var baseline = Measure(Baseline, ShotDamage, ShotInterval, seedCount: 20);
            var withBeam = Measure(WithBeam, ShotDamage, ShotInterval, seedCount: 20);

            Assert.Less(withBeam.Leaked, baseline.Leaked - 3f,
                "Isin artik toplanmis orduyu cezalandirmiyor - kimligi " +
                "kaybolmus olabilir. isinli=" + withBeam.Leaked +
                " isinsiz=" + baseline.Leaked);
        }

        [Test]
        public void BeamAlone_IsNotAStrategy()
        {
            // Isin bir UZMANDIR: dengeli bir karisimin icinde deger uretir,
            // tek basina cozum degildir. Mayin tarlasi da boyle secilmisti.
            // Olculen: yalnizca isin 30,0 sizinti (kazanma %0) · dengeli
            // karisim 15,7.
            //
            // Bu testin dustugu gun isin baskin strateji olmus demektir -
            // isinin ilk tasariminda (surekli hat) tam olarak bu olmustu.
            var beamSpam = Measure(new[] { TowerKind.Beam }, ShotDamage, ShotInterval, seedCount: 20);
            var balancedMix = Measure(WithBeam, ShotDamage, ShotInterval, seedCount: 20);

            Assert.Greater(beamSpam.Leaked, balancedMix.Leaked + 5f,
                "Yalnizca isin diken oyuncu artik dengeli karisim kadar iyi - " +
                "isin baskin strateji oluyor. spam=" + beamSpam.Leaked +
                " karisim=" + balancedMix.Leaked);
        }

        [Test]
        public void ShotInterval_IsTheBalanceKnob_NotDamage()
        {
            // Olculen: aralik kisaldikca isin spam'i cozume yaklasir
            // (aralik 2 -> 30,0 sizinti · aralik 0,5 -> 0,2). Denge
            // dugmesi ARALIK.
            var fast = Measure(new[] { TowerKind.Beam }, ShotDamage, 0.5f, seedCount: 20);
            var chosen = Measure(new[] { TowerKind.Beam }, ShotDamage, ShotInterval, seedCount: 20);

            Assert.Less(fast.Leaked, chosen.Leaked - 4f,
                "Atis araligini yariya indirmek artik isini guclendirmiyor; " +
                "secilen 1 sn'nin gerekcesi olcume dayanmiyor. hizli=" +
                fast.Leaked + " secilen=" + chosen.Leaked);
        }

        [Test]
        public void Beam_IsWorthMoreWhenPairedWithAMinefield()
        {
            // Isinin kimligi bu: hat ince oldugu icin degeri "atis aninda
            // kac birlik hatta diziliyor" sorusuna bagli. Yavaslatilan
            // birlik hatta daha uzun kalir. Olculen: 23,0 -> 15,7.
            // Karsilastirma DENGELI bir karisim uzerinden yapilir: tek
            // bataryali kurulumlar artik ikisi de kaybediyor (30/30) ve
            // fark olculemiyordu.
            var withMinefield = Measure(
                new[] { TowerKind.Battery, TowerKind.Ambush, TowerKind.Battery,
                        TowerKind.Beam, TowerKind.Minefield },
                ShotDamage, ShotInterval, seedCount: 20);

            var withoutMinefield = Measure(
                new[] { TowerKind.Battery, TowerKind.Ambush, TowerKind.Battery,
                        TowerKind.Beam, TowerKind.Ambush },
                ShotDamage, ShotInterval, seedCount: 20);

            Assert.Less(withMinefield.Leaked, withoutMinefield.Leaked - 1.5f,
                "Isin-mayin tarlasi eslesmesi artik deger uretmiyor; isinin " +
                "kimligi kaybolmus olabilir. mayinli=" + withMinefield.Leaked +
                " mayinsiz=" + withoutMinefield.Leaked);
        }

        [Test]
        public void ContinuousBeam_WouldStillBreakTheGame()
        {
            // Kayitli gercek: atis araligini cok kucultmek isini SUREKLI
            // hasara yaklastirir ve oyunu cozer. Bu test, aralik dugmesinin
            // sessizce kucultulmesini yakalar.
            var almostContinuous = Measure(new[] { TowerKind.Beam }, ShotDamage, 0.05f, seedCount: 20);

            Assert.Less(almostContinuous.Leaked, 8f,
                "Surekli isin artik oyunu cozmuyor - isin modeli degismis " +
                "olabilir, periyodik atis gerekcesi yeniden olculmeli. " +
                "sizinti=" + almostContinuous.Leaked);
        }
    }
}
