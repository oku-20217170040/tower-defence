using System;
using System.Collections.Generic;

namespace Colony.Core
{
    /// <summary>
    /// Tam bir run'i BASSIZ ve ESZAMANLI simule eder.
    ///
    /// Neden yazildi: onceki denge olcumleri bocekleri SIRAYLA isliyordu -
    /// bir bocek butun yolu yuruyor, sonra digeri basliyordu. O modelde tek
    /// hedefli bir kule, ayni anda gecen sekiz bocegin HEPSINE tam DPS
    /// veriyor. Gercekte kule bir seferde tek bocege ates eder. Oyunda
    /// bocekler 0,35 saniye arayla dogup 0,4 saniyede bir hucre ilerliyor,
    /// yani sürü halinde gidiyorlar; sirali model bu yuzden tek hedefli
    /// kuleleri oldugundan guclu gosteriyordu.
    ///
    /// Ayrica alan hasari ve delici hasar sirali modelde HIC olculemez:
    /// ikisinin de degeri "ayni anda kac bocek menzilde" sorusuna baglidir.
    ///
    /// Saf C#'tir; Unity tipi kullanmaz.
    /// </summary>
    public sealed class RunSimulator
    {
        /// <summary>Kulenin nasil hedef sectigi.</summary>
        public enum Targeting
        {
            /// <summary>Menzildeki en yakin TEK bocege ates eder.</summary>
            Single = 0,

            /// <summary>Menzildeki TUM boceklere ayni anda vurur.</summary>
            Splash = 1
        }

        public struct TowerSpec
        {
            public int X;
            public int Y;
            public TowerKind Kind;
            public float Dps;
            public float Radius;
            public Targeting Targeting;

            /// <summary>Recine icin gecis suresi carpani; digerlerinde 1.</summary>
            public float SlowFactor;

            /// <summary>Gurultucu icin tehdit gucu; digerlerinde 0.</summary>
            public float ThreatStrength;
        }

        public struct Settings
        {
            public float BaseBugHp;
            public float HpGrowthPerWave;
            public int BaseBugsPerWave;
            public float BugsAddedPerWave;
            public int TotalWaves;
            public int NestHp;

            /// <summary>Bocegin saniyede kac hucre ilerledigi.</summary>
            public float CellsPerSecond;

            /// <summary>Bocekler arasi dogum araligi (saniye).</summary>
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
                    BaseBugHp = 10f,
                    HpGrowthPerWave = 0.10f,
                    BaseBugsPerWave = 8,
                    BugsAddedPerWave = 1f,
                    TotalWaves = 15,
                    NestHp = 30,
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

            /// <summary>Bir bocek olduruldu.</summary>
            void OnBugKilled();

            /// <summary>Dalga bitti (yuva hala ayakta).</summary>
            void OnWaveEnd(int wave);
        }

        public struct RunResult
        {
            public bool Survived;
            public int LostAtWave;
            public int TotalKilled;
            public int TotalLeaked;
            public int NestHpLeft;
        }

        private sealed class Bug
        {
            public List<int> Path = new List<int>();
            public List<int> Visited = new List<int>();
            public int PathIndex;
            public float Progress;
            public float Hp;
            public bool Alive;
        }

        private readonly ColonyGrid grid;
        private readonly IReadOnlyList<int> spawnCells;
        private readonly int nestCell;

        public RunSimulator(ColonyGrid grid, IReadOnlyList<int> spawnCells, int nestCell)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.spawnCells = spawnCells ?? throw new ArgumentNullException(nameof(spawnCells));
            this.nestCell = nestCell;

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
            var pheromone = new PheromoneMap(grid, 100f, 0.15f);
            var lure = new LureField(grid, 25f, 0.5f);
            var threat = new ThreatField(grid);
            var slow = new SlowField(grid);

            var model = new RouteCostModel(pheromone, lure, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 3f,
                ExplorationNoise = 0.35f
            };
            var finder = new ColonyPathfinder(grid, model);
            var random = new Random(seed);

            var result = new RunResult { LostAtWave = -1 };
            int nestHp = settings.NestHp;

            var active = new List<Bug>();
            var pool = new List<Bug>();

            for (int wave = 1; wave <= settings.TotalWaves && nestHp > 0; wave++)
            {
                if (planner != null) planner.OnWaveStart(wave, towers);

                // Kule listesi degismis olabilir; alanlar her dalga yeniden kurulur.
                threat.Clear();
                slow.Clear();
                BuildFields(towers, threat, slow);

                float bugHp = settings.BaseBugHp * (1f + settings.HpGrowthPerWave * (wave - 1));
                int toSpawn = (int)Math.Round(
                    settings.BaseBugsPerWave + settings.BugsAddedPerWave * (wave - 1));

                float spawnTimer = 0f;
                float elapsed = 0f;
                active.Clear();

                while ((toSpawn > 0 || active.Count > 0) && nestHp > 0 && elapsed < settings.WaveTimeout)
                {
                    float dt = settings.TickSeconds;
                    elapsed += dt;

                    // Dogum
                    spawnTimer -= dt;
                    if (toSpawn > 0 && spawnTimer <= 0f)
                    {
                        var bug = Spawn(finder, model, random, pheromone, bugHp, pool, settings);
                        if (bug != null) active.Add(bug);

                        toSpawn--;
                        spawnTimer = settings.SpawnInterval;
                    }

                    // Hareket
                    for (int i = 0; i < active.Count; i++)
                        Advance(active[i], slow, pheromone, settings, dt);

                    // Ates. Kuleler hedeflerini AYNI anlik durumdan secer;
                    // sirali modelde bu adim yoktu ve her bocek her kulenin
                    // tam DPS'ini aliyordu.
                    Fire(towers, active, settings, dt);

                    // Sonuclari isle
                    for (int i = active.Count - 1; i >= 0; i--)
                    {
                        Bug bug = active[i];

                        if (bug.Hp <= 0f)
                        {
                            bug.Alive = false;
                            pheromone.WeakenTrail(bug.Visited, settings.WeakenOnDeath);
                            result.TotalKilled++;
                            if (planner != null) planner.OnBugKilled();
                            Recycle(bug, active, pool, i);
                            continue;
                        }

                        if (bug.PathIndex >= bug.Path.Count - 1)
                        {
                            bug.Alive = false;
                            pheromone.DepositTrail(bug.Visited, settings.ReinforceOnSuccess);
                            result.TotalLeaked++;
                            nestHp--;
                            Recycle(bug, active, pool, i);
                        }
                    }
                }

                pheromone.Decay();

                if (nestHp > 0 && planner != null) planner.OnWaveEnd(wave);

                if (nestHp <= 0 && result.LostAtWave < 0) result.LostAtWave = wave;
            }

            result.Survived = nestHp > 0;
            result.NestHpLeft = nestHp > 0 ? nestHp : 0;
            return result;
        }

        private void BuildFields(List<TowerSpec> towers, ThreatField threat, SlowField slow)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                TowerSpec t = towers[i];
                int cell = grid.Index(t.X, t.Y);

                if (t.Kind == TowerKind.Noisy && t.ThreatStrength > 0f)
                    threat.AddSource(cell, t.Radius, t.ThreatStrength);

                if (t.Kind == TowerKind.Resin && t.SlowFactor > SlowField.None)
                    slow.AddSource(cell, t.Radius, t.SlowFactor);
            }
        }

        private Bug Spawn(ColonyPathfinder finder, RouteCostModel model, Random random,
                          PheromoneMap pheromone, float hp, List<Bug> pool, Settings settings)
        {
            Bug bug = pool.Count > 0 ? pool[pool.Count - 1] : new Bug();
            if (pool.Count > 0) pool.RemoveAt(pool.Count - 1);

            bug.Path.Clear();
            bug.Visited.Clear();
            bug.PathIndex = 0;
            bug.Progress = 0f;
            bug.Hp = hp;
            bug.Alive = true;

            model.BeginQuery(random.Next(), grid);
            int spawnCell = spawnCells[random.Next(spawnCells.Count)];

            if (!finder.FindPath(spawnCell, nestCell, bug.Path))
            {
                pool.Add(bug);
                return null;
            }

            bug.Visited.Add(bug.Path[0]);
            pheromone.Deposit(bug.Path[0], settings.DepositPerCell);
            return bug;
        }

        private void Advance(Bug bug, SlowField slow, PheromoneMap pheromone, Settings settings, float dt)
        {
            if (!bug.Alive || bug.PathIndex >= bug.Path.Count - 1) return;

            float factor = slow.Get(bug.Path[bug.PathIndex]);
            if (factor < SlowField.None) factor = SlowField.None;

            // Yavaslatma gecis suresini carpar, yani hizi boler.
            bug.Progress += settings.CellsPerSecond / factor * dt;

            while (bug.Progress >= 1f && bug.PathIndex < bug.Path.Count - 1)
            {
                bug.Progress -= 1f;
                bug.PathIndex++;

                int cell = bug.Path[bug.PathIndex];
                bug.Visited.Add(cell);
                pheromone.Deposit(cell, settings.DepositPerCell);
            }
        }

        private void Fire(List<TowerSpec> towers, List<Bug> active, Settings settings, float dt)
        {
            for (int t = 0; t < towers.Count; t++)
            {
                TowerSpec tower = towers[t];
                if (tower.Dps <= 0f) continue;

                float radiusSq = tower.Radius * tower.Radius;

                if (tower.Targeting == Targeting.Splash)
                {
                    for (int i = 0; i < active.Count; i++)
                    {
                        Bug bug = active[i];
                        if (!bug.Alive || bug.Hp <= 0f) continue;
                        if (DistanceSq(bug, tower) <= radiusSq)
                            bug.Hp -= tower.Dps * dt;
                    }
                    continue;
                }

                // Tek hedef: menzildeki EN YAKIN bocek. Bir kule bir anda
                // yalnizca bir bocege ates eder - modelin tum mesele ettigi
                // sey budur.
                Bug best = null;
                float bestSq = radiusSq;

                for (int i = 0; i < active.Count; i++)
                {
                    Bug bug = active[i];
                    if (!bug.Alive || bug.Hp <= 0f) continue;

                    float sq = DistanceSq(bug, tower);
                    if (sq <= bestSq)
                    {
                        bestSq = sq;
                        best = bug;
                    }
                }

                if (best != null) best.Hp -= tower.Dps * dt;
            }
        }

        private float DistanceSq(Bug bug, TowerSpec tower)
        {
            int cell = bug.Path[bug.PathIndex];
            float dx = grid.X(cell) - tower.X;
            float dy = grid.Y(cell) - tower.Y;
            return dx * dx + dy * dy;
        }

        private static void Recycle(Bug bug, List<Bug> active, List<Bug> pool, int index)
        {
            active.RemoveAt(index);
            pool.Add(bug);
        }
    }
}
