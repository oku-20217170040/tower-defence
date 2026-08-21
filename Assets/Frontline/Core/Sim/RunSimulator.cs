using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Tam bir run'i BASSIZ ve ESZAMANLI simule eder.
    ///
    /// Neden yazildi: onceki denge olcumleri birlikleri SIRAYLA isliyordu -
    /// bir birlik butun yolu yuruyor, sonra digeri basliyordu. O modelde tek
    /// hedefli bir kule, ayni anda gecen sekiz birligin HEPSINE tam DPS
    /// veriyor. Gercekte kule bir seferde tek birlige ates eder. Oyunda
    /// birlikler 0,35 saniye arayla dogup 0,4 saniyede bir hucre ilerliyor,
    /// yani sürü halinde gidiyorlar; sirali model bu yuzden tek hedefli
    /// kuleleri oldugundan guclu gosteriyordu.
    ///
    /// Ayrica alan hasari ve delici hasar sirali modelde HIC olculemez:
    /// ikisinin de degeri "ayni anda kac birlik menzilde" sorusuna baglidir.
    ///
    /// Saf C#'tir; Unity tipi kullanmaz.
    /// </summary>
    public sealed class RunSimulator
    {
        /// <summary>Kulenin nasil hedef sectigi.</summary>
        public enum Targeting
        {
            /// <summary>Menzildeki en yakin TEK birlige ates eder.</summary>
            Single = 0,

            /// <summary>Menzildeki TUM birliklere ayni anda vurur.</summary>
            Splash = 1,

            /// <summary>
            /// Isin. Belirli araliklarla ates eder ve ates ettigi anda
            /// HATTI UZERINDEKI herkese ayni hasari verir. Surekli degil,
            /// ORNEKLEYEN bir silahtir: hat 1 hucre kalinliginda oldugu
            /// icin hizli birlik iki atis arasinda karsiya gecebilir,
            /// yavaslatilmis ya da sira olmus bir kalabalik tam yer.
            /// Dengeleyici dugme dps degil, ATIS ARALIGIDIR.
            /// </summary>
            Beam = 2
        }

        public struct TowerSpec
        {
            public int X;
            public int Y;
            public TowerKind Kind;
            public float Dps;
            public float Radius;
            public Targeting Targeting;

            /// <summary>Mayin tarlasi icin gecis suresi carpani; digerlerinde 1.</summary>
            public float SlowFactor;

            /// <summary>Batarya icin tehdit gucu; digerlerinde 0.</summary>
            public float ThreatStrength;

            /// <summary>
            /// Isin hattinin yatay yarim uzunlugu (hucre). Hat YATAYDIR:
            /// kulenin durdugu satir boyunca uzanir ve butun koridorlari
            /// keser. Dikey kalinligi her zaman 1 hucredir - bir koridoru
            /// bastan sona kapatan her sey spec 3.5'i ("korunmayan koridor
            /// her zaman bulunur") siler; olcumde tam olarak bu oldu.
            /// </summary>
            public float Width;

            /// <summary>Isinin bir atista verdigi hasar.</summary>
            public float ShotDamage;

            /// <summary>Isinin atislari arasindaki sure (saniye).</summary>
            public float ShotInterval;
        }

        public struct Settings
        {
            public float BaseUnitHp;
            public float HpGrowthPerWave;
            public int BaseUnitsPerWave;
            public float UnitsAddedPerWave;
            public int TotalWaves;
            public int HomeBaseHp;

            /// <summary>Bocegin saniyede kac hucre ilerledigi.</summary>
            public float CellsPerSecond;

            /// <summary>Birlikler arasi dogum araligi (saniye).</summary>
            public float SpawnInterval;

            /// <summary>Simulasyon adimi (saniye). Kucuk olmasi dogrulugu artirir.</summary>
            public float TickSeconds;

            public float DepositPerCell;
            public float ReinforceOnSuccess;
            public float WeakenOnDeath;

            /// <summary>Bir dalganin en fazla kac saniye surebilecegi (kilitlenme koruma).</summary>
            public float WaveTimeout;

            public static Settings Default()
            {
                return new Settings
                {
                    BaseUnitHp = 10f,
                    HpGrowthPerWave = 0.10f,
                    BaseUnitsPerWave = 8,
                    UnitsAddedPerWave = 1f,
                    TotalWaves = 15,
                    HomeBaseHp = 30,
                    CellsPerSecond = 2.5f,
                    SpawnInterval = 0.35f,
                    TickSeconds = 0.05f,
                    DepositPerCell = 0.5f,
                    ReinforceOnSuccess = 1.5f,
                    WeakenOnDeath = 2.5f,
                    WaveTimeout = 600f
                };
            }
        }

        /// <summary>
        /// Dalga aralarina karisan oyuncu modeli. Ekonomiyi ve kule satin
        /// almayi simulatorun disinda tutar: simulator yalnizca savasi bilir.
        /// </summary>
        public interface IRunPlanner
        {
            /// <summary>Dalga baslamadan once cagrilir; kule listesi degistirilebilir.</summary>
            void OnWaveStart(int wave, List<TowerSpec> towers);

            /// <summary>Bir birlik olduruldu.</summary>
            void OnUnitKilled();

            /// <summary>Dalga bitti (us hala ayakta).</summary>
            void OnWaveEnd(int wave);
        }

        public struct RunResult
        {
            public bool Survived;
            public int LostAtWave;
            public int TotalKilled;
            public int TotalLeaked;
            public int HomeBaseHpLeft;
        }

        private sealed class Unit
        {
            public List<int> Path = new List<int>();
            public List<int> Visited = new List<int>();
            public int PathIndex;
            public float Progress;
            public float Hp;
            public bool Alive;
        }

        private readonly BattleGrid grid;
        private readonly IReadOnlyList<int> spawnCells;
        private readonly int baseCell;

        public RunSimulator(BattleGrid grid, IReadOnlyList<int> spawnCells, int baseCell)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.spawnCells = spawnCells ?? throw new ArgumentNullException(nameof(spawnCells));
            this.baseCell = baseCell;

            if (spawnCells.Count == 0)
                throw new ArgumentException("En az bir dogum hucresi gerekli.", nameof(spawnCells));
        }

        public RunResult Run(IReadOnlyList<TowerSpec> towers, Settings settings, int seed)
        {
            var copy = new List<TowerSpec>(towers);
            return Run(copy, settings, seed, null);
        }

        /// <summary>
        /// Planner verilirse kule listesi dalga aralarinda degisebilir; boylece
        /// oyuncunun kuleleri ZAMANLA kurmasi modellenir. Gercek run ile
        /// karsilastirinca sizintinin neredeyse tamaminin erken dalgalarda,
        /// kuleler azken olustugu goruldu - sabit kule listesiyle bu hic
        /// gorunmuyordu.
        /// </summary>
        public RunResult Run(List<TowerSpec> towers, Settings settings, int seed, IRunPlanner planner)
        {
            var recon = new ReconMap(grid, 100f, 0.15f);
            var decoy = new DecoyField(grid, 25f, 0.5f);
            var threat = new ThreatField(grid);
            var slow = new SlowField(grid);

            var model = new RouteCostModel(recon, decoy, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 3f,
                ExplorationNoise = 0.35f
            };
            var finder = new RoutePathfinder(grid, model);
            var random = new Random(seed);

            var result = new RunResult { LostAtWave = -1 };
            int baseHp = settings.HomeBaseHp;

            var active = new List<Unit>();
            var pool = new List<Unit>();

            // Isin atis sayaclari; kule indeksiyle hizali.
            var beamTimers = new List<float>();

            for (int wave = 1; wave <= settings.TotalWaves && baseHp > 0; wave++)
            {
                if (planner != null) planner.OnWaveStart(wave, towers);

                // Kule listesi degismis olabilir; alanlar her dalga yeniden kurulur.
                threat.Clear();
                slow.Clear();
                BuildFields(towers, threat, slow);

                float unitHp = settings.BaseUnitHp * (1f + settings.HpGrowthPerWave * (wave - 1));
                int toSpawn = (int)Math.Round(
                    settings.BaseUnitsPerWave + settings.UnitsAddedPerWave * (wave - 1));

                float spawnTimer = 0f;
                float elapsed = 0f;
                active.Clear();

                while ((toSpawn > 0 || active.Count > 0) && baseHp > 0 && elapsed < settings.WaveTimeout)
                {
                    float dt = settings.TickSeconds;
                    elapsed += dt;

                    // Dogum
                    spawnTimer -= dt;
                    if (toSpawn > 0 && spawnTimer <= 0f)
                    {
                        var unit = Spawn(finder, model, random, recon, unitHp, pool, settings);
                        if (unit != null) active.Add(unit);

                        toSpawn--;
                        spawnTimer = settings.SpawnInterval;
                    }

                    // Hareket
                    for (int i = 0; i < active.Count; i++)
                        Advance(active[i], slow, recon, settings, dt);

                    // Ates. Kuleler hedeflerini AYNI anlik durumdan secer;
                    // sirali modelde bu adim yoktu ve her birlik her kulenin
                    // tam DPS'ini aliyordu.
                    // Yeni kurulan isinlar icin sayac yeri acilir.
                    while (beamTimers.Count < towers.Count) beamTimers.Add(0f);

                    Fire(towers, active, settings, dt, beamTimers);

                    // Sonuclari isle
                    for (int i = active.Count - 1; i >= 0; i--)
                    {
                        Unit unit = active[i];

                        if (unit.Hp <= 0f)
                        {
                            unit.Alive = false;
                            recon.WeakenTrail(unit.Visited, settings.WeakenOnDeath);
                            result.TotalKilled++;
                            if (planner != null) planner.OnUnitKilled();
                            Recycle(unit, active, pool, i);
                            continue;
                        }

                        if (unit.PathIndex >= unit.Path.Count - 1)
                        {
                            unit.Alive = false;
                            recon.DepositTrail(unit.Visited, settings.ReinforceOnSuccess);
                            result.TotalLeaked++;
                            baseHp--;
                            Recycle(unit, active, pool, i);
                        }
                    }
                }

                recon.Decay();

                if (baseHp > 0 && planner != null) planner.OnWaveEnd(wave);

                if (baseHp <= 0 && result.LostAtWave < 0) result.LostAtWave = wave;
            }

            result.Survived = baseHp > 0;
            result.HomeBaseHpLeft = baseHp > 0 ? baseHp : 0;
            return result;
        }

        private void BuildFields(List<TowerSpec> towers, ThreatField threat, SlowField slow)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                TowerSpec t = towers[i];
                int cell = grid.Index(t.X, t.Y);

                // Tehdit yazan her kule; tur adiyla degil, ALANIYLA belirlenir.
                if (t.ThreatStrength > 0f)
                    threat.AddSource(cell, t.Radius, t.ThreatStrength);

                if (t.Kind == TowerKind.Minefield && t.SlowFactor > SlowField.None)
                    slow.AddSource(cell, t.Radius, t.SlowFactor);
            }
        }

        private Unit Spawn(RoutePathfinder finder, RouteCostModel model, Random random,
                          ReconMap recon, float hp, List<Unit> pool, Settings settings)
        {
            Unit unit = pool.Count > 0 ? pool[pool.Count - 1] : new Unit();
            if (pool.Count > 0) pool.RemoveAt(pool.Count - 1);

            unit.Path.Clear();
            unit.Visited.Clear();
            unit.PathIndex = 0;
            unit.Progress = 0f;
            unit.Hp = hp;
            unit.Alive = true;

            model.BeginQuery(random.Next(), grid);
            int spawnCell = spawnCells[random.Next(spawnCells.Count)];

            if (!finder.FindPath(spawnCell, baseCell, unit.Path))
            {
                pool.Add(unit);
                return null;
            }

            unit.Visited.Add(unit.Path[0]);
            recon.Deposit(unit.Path[0], settings.DepositPerCell);
            return unit;
        }

        private void Advance(Unit unit, SlowField slow, ReconMap recon, Settings settings, float dt)
        {
            if (!unit.Alive || unit.PathIndex >= unit.Path.Count - 1) return;

            float factor = slow.Get(unit.Path[unit.PathIndex]);
            if (factor < SlowField.None) factor = SlowField.None;

            // Yavaslatma gecis suminefieldi carpar, yani hizi boler.
            unit.Progress += settings.CellsPerSecond / factor * dt;

            while (unit.Progress >= 1f && unit.PathIndex < unit.Path.Count - 1)
            {
                unit.Progress -= 1f;
                unit.PathIndex++;

                int cell = unit.Path[unit.PathIndex];
                unit.Visited.Add(cell);
                recon.Deposit(cell, settings.DepositPerCell);
            }
        }

        private void Fire(List<TowerSpec> towers, List<Unit> active, Settings settings,
                          float dt, List<float> beamTimers)
        {
            for (int t = 0; t < towers.Count; t++)
            {
                TowerSpec tower = towers[t];
                if (tower.Dps <= 0f) continue;

                float radiusSq = tower.Radius * tower.Radius;

                if (tower.Targeting == Targeting.Beam)
                {
                    // Ates zamani gelmediyse hicbir sey yapmaz.
                    beamTimers[t] -= dt;
                    if (beamTimers[t] > 0f) continue;
                    beamTimers[t] += tower.ShotInterval > 0f ? tower.ShotInterval : 1f;

                    // Hat uzerindeki HERKES ayni hasari alir - kac birlik
                    // oldugundan bagimsiz. Degeri "atis aninda kac birlik
                    // hatta" sorusuna baglidir; sirali modelde olculemezdi.
                    for (int i = 0; i < active.Count; i++)
                    {
                        Unit unit = active[i];
                        if (!unit.Alive || unit.Hp <= 0f) continue;

                        int cell = unit.Path[unit.PathIndex];
                        if (grid.Y(cell) != tower.Y) continue;

                        float dx = grid.X(cell) - tower.X;
                        if (dx < 0f) dx = -dx;
                        if (dx <= tower.Width) unit.Hp -= tower.ShotDamage;
                    }
                    continue;
                }

                if (tower.Targeting == Targeting.Splash)
                {
                    for (int i = 0; i < active.Count; i++)
                    {
                        Unit unit = active[i];
                        if (!unit.Alive || unit.Hp <= 0f) continue;
                        if (DistanceSq(unit, tower) <= radiusSq)
                            unit.Hp -= tower.Dps * dt;
                    }
                    continue;
                }

                // Tek hedef: menzildeki EN YAKIN birlik. Bir kule bir anda
                // yalnizca bir birlige ates eder - modelin tum mesele ettigi
                // sey budur.
                Unit best = null;
                float bestSq = radiusSq;

                for (int i = 0; i < active.Count; i++)
                {
                    Unit unit = active[i];
                    if (!unit.Alive || unit.Hp <= 0f) continue;

                    float sq = DistanceSq(unit, tower);
                    if (sq <= bestSq)
                    {
                        bestSq = sq;
                        best = unit;
                    }
                }

                if (best != null) best.Hp -= tower.Dps * dt;
            }
        }

        private float DistanceSq(Unit unit, TowerSpec tower)
        {
            int cell = unit.Path[unit.PathIndex];
            float dx = grid.X(cell) - tower.X;
            float dy = grid.Y(cell) - tower.Y;
            return dx * dx + dy * dy;
        }

        private static void Recycle(Unit unit, List<Unit> active, List<Unit> pool, int index)
        {
            active.RemoveAt(index);
            pool.Add(unit);
        }
    }
}
