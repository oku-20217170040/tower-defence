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
    /// COZUM: isin SUREKLI degil PERIYODIK ates eder ve hat 1 hucre
    /// kalinligindadir. Artik bir koridoru kapatmaz, ORNEKLER: hizli birlik
    /// iki atis arasinda karsiya gecebilir, yavaslatilmis kalabalik tam yer.
    /// Asil denge dugmesi hasar degil, ATIS ARALIGIDIR.
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

        private const float ShotDamage = 20f;
        private const float ShotInterval = 1f;

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
        public void Beam_InAMix_IsAnEvenTradeWithAnAmbush()
        {
            // Olculen: pusu yerine isin -> sizinti 5,6 -> 5,3. Yani gercek bir
            // secim; ne bedava guc ne olu secenek. Mayin tarlasi da tam boyle
            // secilmisti (x2 yavaslatma, saf pusuyle basa bas).
            var baseline = Measure(Baseline, ShotDamage, ShotInterval, seedCount: 20);
            var withBeam = Measure(WithBeam, ShotDamage, ShotInterval, seedCount: 20);

            Assert.That(withBeam.Leaked, Is.EqualTo(baseline.Leaked).Within(4f),
                "Isin artik pusuyle basa bas degil: isinli=" + withBeam.Leaked +
                " isinsiz=" + baseline.Leaked);
        }

        [Test]
        public void BeamSpam_IsNoBetterThanBatterySpam()
        {
            // Isinin dusme noktasi burasi. Tek turden kule diken oyuncu
            // isinla, bataryayla oldugundan iyi olmamali - olursa isin
            // baskin strateji olur ve oyun karar barindirmayi birakir.
            var beamSpam = Measure(new[] { TowerKind.Beam }, ShotDamage, ShotInterval, seedCount: 20);
            var batterySpam = Measure(new[] { TowerKind.Battery }, ShotDamage, ShotInterval, seedCount: 20);

            Assert.GreaterOrEqual(beamSpam.Leaked, batterySpam.Leaked - 4f,
                "Isin spam'i batarya spam'ini geride birakti - baskin strateji " +
                "olma yolunda. isin=" + beamSpam.Leaked + " batarya=" + batterySpam.Leaked);
        }

        [Test]
        public void ShotInterval_IsTheBalanceKnob_NotDamage()
        {
            // Olculen: karisimda hasar 10 ile 45 arasi AYNI sonucu veriyor
            // (sizinti 5,3), ama aralik 1 sn -> 0,5 sn yapilinca isin spam'i
            // sizintiyi 15,4'ten 5,5'e dusuruyor. Denge dugmesi aralik.
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
            // Isinin kimligi bu: hat 1 hucre kalinliginda oldugu icin degeri
            // "atis aninda kac birlik hatta" sorusuna bagli. Yavaslatilan
            // birlik hatta daha uzun kalir. Olculen: 21,8 -> 17,1.
            var withMinefield = Measure(
                new[] { TowerKind.Battery, TowerKind.Ambush, TowerKind.Beam,
                        TowerKind.Minefield, TowerKind.Ambush },
                ShotDamage, ShotInterval, seedCount: 20);

            var withoutMinefield = Measure(
                new[] { TowerKind.Battery, TowerKind.Ambush, TowerKind.Beam,
                        TowerKind.Ambush, TowerKind.Ambush },
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
