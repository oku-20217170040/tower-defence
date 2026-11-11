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
            /// Isin. Belirli araliklarla ates eder: namlusunu menzildeki EN
            /// YAKIN birlige dogrultur ve O DOGRULTUDA onune cikan HERKESI
            /// yarip gecer. Delici hat hasari.
            ///
            /// Surekli degil ORNEKLEYEN bir silahtir: hat incedir, hizli
            /// birlik iki atis arasinda hattan cikabilir; sira olmus ya da
            /// yavaslatilmis kalabalik ise tam yer. Bu yuzden may in
            /// tarlasiyla eslesir ve toplanmis orduyu cezalandirir.
            ///
            /// Dengeleyici dugme hasar degil, ATIS ARALIGIDIR.
            /// </summary>
            Beam = 2,

            /// <summary>
            /// Topcu (11 Kasim 2026). Menzildeki en yakin birlige mermi atar;
            /// mermi dustugu yerde patlar ve BlastRadius icindeki herkese
            /// vurur. Splash'tan farki: butun menzile degil, hedefin cevresine.
            /// </summary>
            Blast = 3
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
            /// Isin hattinin YARIM KALINLIGI (hucre). Hattin yonu sabit
            /// degildir: kule namlusunu hedefe dogrultur, hat o dogrultuda
            /// uzanir. Menzil Radius'tur.
            ///
            /// Kalinlik kucuk tutulmali. Bir koridoru bastan sona kaplayan
            /// her sey spec 3.5'i ("korunmayan koridor her zaman bulunur")
            /// siler; olcumde tam olarak bu oldu (surekli isin denemesi).
            /// </summary>
            public float Width;

            /// <summary>Isinin bir atista verdigi hasar.</summary>
            public float ShotDamage;

            /// <summary>Isinin atislari arasindaki sure (saniye).</summary>
            public float ShotInterval;

            /// <summary>
            /// Topcu (Targeting.Blast): patlama yaricapi (hucre). Mermi en
            /// yakin hedefe duser ve bu yaricaptaki herkese vurur.
            /// </summary>
            public float BlastRadius;

            /// <summary>
            /// Yem basar mi. 11 Kasim 2026'ya kadar bunu TowerKind.Decoy
            /// belirliyordu; o tur artik RADAR oldu ve yem basmiyor. Yem
            /// (ve kor birligin onu silmesi) Core'da bir yetenek olarak
            /// duruyor ama oyunda hicbir kule kullanmiyor.
            /// </summary>
            public bool LaysDecoy;

            /// <summary>Radar: Radius icindeki silahli kulelere menzil eki.</summary>
            public float RangeBonus;
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

            /// <summary>
            /// Taban kesif sapmasi. Profili kendi sapmasini vermeyen her
            /// birlik bunu kullanir.
            ///
            /// 0,12 olcumle secildi (21 Agu 2026). Onceki deger 0,35'ti ve o
            /// olcum BUTUN BIRLIKLER AYNI SAPMAYI TASIRKEN yapilmisti; tur
            /// sistemi gelince kesif izciye yuklendi. Dalga basina koridor
            /// entropisi: taban 0,35'te izcisiz 0,750 / izcili 0,789 (izci
            /// gereksiz), taban 0,12'de izcisiz 0,428 / izcili 0,630 (izci
            /// tasiyici).
            /// </summary>
            public float ExplorationNoise;

            /// <summary>
            /// Dalga bilesimi. null ya da bos ise HEPSI PIYADEDIR.
            ///
            /// Varsayilanin piyade kalmasi sarttir: RunCalibrationTests ve
            /// CurrentRunCalibrationTests gercek run'lara bagli; varsayilan
            /// bilesim degisirse ikisi de sessizce yanlis seyi olcer.
            /// </summary>
            public UnitMix[] Composition;

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
                    WaveTimeout = 600f,
                    ExplorationNoise = 0.12f
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

            /// <summary>
            /// Haritanin ORTA SATIRINDA her sutundan kac birlik gecti.
            /// Sutun indeksiyle hizali.
            ///
            /// Neden bu metrik: izcinin degeri "kac kisi oldu" degil,
            /// ORDUNUN KAC FARKLI KORIDOR DENEDIGIDIR. Sizinti sayisi
            /// izciyi hic gormuyor (olcum: %30 izci -> 5,4 sizinti,
            /// piyade 5,3). Kesif ancak dagilima bakarak olculur.
            /// </summary>
            public int[] ColumnUsage;

            /// <summary>
            /// DALGA BASINA koridor dagilimi entropisinin ortalamasi [0, 1].
            /// 0 = ordu o dalgada tek koridora saplandi, 1 = koridorlari esit
            /// deniyor. Izcinin olcum metrigi budur.
            ///
            /// Neden dalga basina: butun run'a yayilan entropi doyuyor
            /// (olcumde 0,954) cunku yogunlasma dalga ICINDE olur ve dalgalar
            /// arasinda koridor degistikce ortalama duzlesir. Toplam dagilim
            /// ordunun bir dalgada karar verip vermedigini GORMEZ.
            /// </summary>
            public float RouteEntropy;

            /// <summary>
            /// Dalga dalga koridor entropisi (1 tabanli; 0. eleman
            /// kullanilmaz). Ordunun "dagilarak baslayip yerlesmesi"
            /// ancak buna bakarak gorulur - ortalama onu duzler.
            /// </summary>
            public float[] WaveEntropy;

            /// <summary>
            /// [dalga][sutun] = o dalgada o sutundan gecmeyi SECEN birlik
            /// sayisi. Ortalama entropi bir turun etkisini duzler; turlerin
            /// birbirini etkilemesi ancak dalgadan dalgaya bakarak gorulur.
            /// </summary>
            public int[][] WaveColumnUsage;

            /// <summary>
            /// [dalga][sutun] = o dalgada o sutundan USYA VARAN asker sayisi.
            /// Askerin iddiasi ("vardigi koridoru orduya civiler") ancak
            /// bunun sonraki dalgadaki payla karsilastirilmasiyla olculur.
            /// </summary>
            public int[][] WaveSoldierArrivals;

            /// <summary>
            /// [dalga][sutun] = o dalgada o sutunda OLEN birlik sayisi.
            /// Kuluckanin iddiasi ("oyuncunun kendi tuzagi orduya
            /// 'buradan gitme' diye isaretlenir") ancak bunun sonraki
            /// dalgadaki payla karsilastirilmasiyla olculur - askerin
            /// civilemesinin tersi.
            /// </summary>
            public int[][] WaveDeathColumns;
        }

        private sealed class Unit
        {
            public List<int> Path = new List<int>();
            public List<int> Visited = new List<int>();
            public int PathIndex;
            public float Progress;
            public float Hp;
            public bool Alive;
            public UnitProfile Profile;

            /// <summary>Dogdugu andaki can. Kulucka yavrusunun cani bundan turetilir.</summary>
            public float SpawnHp;

            /// <summary>Kulucka yavrusu mu (tekrar bolunmesin diye).</summary>
            public bool IsSpawnling;
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

            float BaseExplorationNoise = settings.ExplorationNoise > 0f
                ? settings.ExplorationNoise
                : 0.35f;

            var model = new RouteCostModel(recon, decoy, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 3f,
                ExplorationNoise = BaseExplorationNoise
            };
            var finder = new RoutePathfinder(grid, model);
            var random = new Random(seed);

            var result = new RunResult
            {
                LostAtWave = -1,
                ColumnUsage = new int[grid.Width],
                WaveEntropy = new float[settings.TotalWaves + 1],
                WaveColumnUsage = new int[settings.TotalWaves + 1][],
                WaveSoldierArrivals = new int[settings.TotalWaves + 1][],
                WaveDeathColumns = new int[settings.TotalWaves + 1][]
            };

            for (int w = 0; w <= settings.TotalWaves; w++)
            {
                result.WaveColumnUsage[w] = new int[grid.Width];
                result.WaveSoldierArrivals[w] = new int[grid.Width];
                result.WaveDeathColumns[w] = new int[grid.Width];
            }
            int midRow = grid.Height / 2;

            var waveColumns = new int[grid.Width];
            double entropySum = 0d;
            int entropyWaves = 0;
            int baseHp = settings.HomeBaseHp;

            var active = new List<Unit>();
            var pool = new List<Unit>();

            // Isin atis sayaclari; kule indeksiyle hizali.
            var beamTimers = new List<float>();

            for (int wave = 1; wave <= settings.TotalWaves && baseHp > 0; wave++)
            {
                if (planner != null) planner.OnWaveStart(wave, towers);

                Array.Clear(waveColumns, 0, waveColumns.Length);

                // Kule listesi degismis olabilir; alanlar her dalga yeniden kurulur.
                threat.Clear();
                slow.Clear();
                BuildFields(towers, threat, slow);

                // Yem her dalga TAZELENIR; etkisi kalici degildir (spec 3.3).
                // Runtime'daki Battlefield.ApplyDecoys ile ayni kural.
                ApplyDecoys(towers, decoy);

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
                        UnitProfile profile = PickProfile(settings.Composition, wave, random);

                        var unit = Spawn(finder, model, random, recon, unitHp, pool,
                                         settings, profile, BaseExplorationNoise);
                        if (unit != null)
                        {
                            active.Add(unit);
                            RecordCorridorChoice(unit, midRow, result.ColumnUsage);
                            RecordCorridorChoice(unit, midRow, waveColumns);
                            if (wave < result.WaveColumnUsage.Length)
                                RecordCorridorChoice(unit, midRow, result.WaveColumnUsage[wave]);
                        }

                        toSpawn--;
                        spawnTimer = settings.SpawnInterval;
                    }

                    // Hareket
                    for (int i = 0; i < active.Count; i++)
                        Advance(active[i], slow, recon, decoy, settings, dt);

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
                            recon.WeakenTrail(unit.Visited,
                                settings.WeakenOnDeath * unit.Profile.WeakenMultiplier);
                            result.TotalKilled++;

                            // Nerede oldu: kuluckanin olum-izi iddiasi
                            // ancak bununla olculebilir.
                            if (wave < result.WaveDeathColumns.Length)
                                result.WaveDeathColumns[wave][grid.X(unit.Path[unit.PathIndex])]++;
                            if (planner != null) planner.OnUnitKilled();

                            // Kulucka: oldugu yerden yavrular ayrilir. Yavru
                            // tekrar bolunmez, yoksa tek birlik ordu uretir.
                            if (unit.Profile.SplitCount > 0 && !unit.IsSpawnling)
                                Split(unit, active, pool, finder, model, random,
                                      BaseExplorationNoise);

                            Recycle(unit, active, pool, i);
                            continue;
                        }

                        if (unit.PathIndex >= unit.Path.Count - 1)
                        {
                            unit.Alive = false;
                            recon.DepositTrail(unit.Visited,
                                settings.ReinforceOnSuccess * unit.Profile.ReinforceMultiplier);

                            // Asker hangi koridordan vardi: iz civilemesinin
                            // olculebilmesi icin gerekli.
                            if (unit.Profile.Type == UnitType.Soldier
                                && wave < result.WaveSoldierArrivals.Length)
                                RecordCorridorChoice(unit, midRow, result.WaveSoldierArrivals[wave]);

                            // Kralice usya varirsa ordunun korkusu silinir.
                            // Kapsam ve kaliciligi Plan 8 Gorev 6'da olculecek.
                            if (unit.Profile.ClearsThreat) threat.Clear();

                            // Ordunun ogrendigi her sey silinir. Oyuncunun
                            // kurdugu yonlendirme degil, ORDUNUN HAFIZASI
                            // sifirlanir - kralicenin asil sildigi sey budur.
                            if (unit.Profile.ClearsRecon) recon.Clear();

                            result.TotalLeaked++;
                            baseHp--;
                            Recycle(unit, active, pool, i);
                        }
                    }
                }

                float waveEntropy = NormalizedEntropy(waveColumns, midRow);
                if (wave < result.WaveEntropy.Length) result.WaveEntropy[wave] = waveEntropy;

                entropySum += waveEntropy;
                entropyWaves++;

                recon.Decay();

                if (baseHp > 0 && planner != null) planner.OnWaveEnd(wave);

                if (baseHp <= 0 && result.LostAtWave < 0) result.LostAtWave = wave;
            }

            result.Survived = baseHp > 0;
            result.HomeBaseHpLeft = baseHp > 0 ? baseHp : 0;
            result.RouteEntropy = entropyWaves > 0 ? (float)(entropySum / entropyWaves) : 0f;
            return result;
        }

        /// <summary>
        /// Yem kulelerini tazeler.
        ///
        /// UYARI (21 Agu 2026'da fark edildi): bu adim simulatore BUGUN
        /// eklendi. Oncesinde DecoyField olusturuluyor ama HIC
        /// doldurulmuyordu - yani bu tarihe kadarki butun denge
        /// olcumlerinde yem kulesi etkisizdi. Yem alan bir politika
        /// olculmedigi icin sonuclar bundan etkilenmedi, ama kor birlik
        /// (yemi yok sayar) ancak bu adim olmadan olculemezdi.
        /// </summary>
        private void ApplyDecoys(List<TowerSpec> towers, DecoyField decoy)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                TowerSpec tower = towers[i];
                if (!tower.LaysDecoy) continue;

                int reach = (int)tower.Radius;
                float radiusSq = tower.Radius * tower.Radius;

                for (int y = tower.Y - reach; y <= tower.Y + reach; y++)
                {
                    for (int x = tower.X - reach; x <= tower.X + reach; x++)
                    {
                        if (!grid.InBounds(x, y)) continue;

                        int cell = grid.Index(x, y);
                        if (!grid.IsWalkable(cell)) continue;

                        float dx = x - tower.X;
                        float dy = y - tower.Y;
                        if (dx * dx + dy * dy > radiusSq) continue;

                        decoy.Apply(cell);
                    }
                }
            }
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
                          ReconMap recon, float hp, List<Unit> pool, Settings settings,
                          UnitProfile profile, float baseNoise)
        {
            Unit unit = pool.Count > 0 ? pool[pool.Count - 1] : new Unit();
            if (pool.Count > 0) pool.RemoveAt(pool.Count - 1);

            unit.Path.Clear();
            unit.Visited.Clear();
            unit.PathIndex = 0;
            unit.Progress = 0f;
            unit.Hp = hp * profile.HpMultiplier;
            unit.SpawnHp = unit.Hp;
            unit.Alive = true;
            unit.Profile = profile;
            unit.IsSpawnling = false;

            // Rota algisi birlige ozeldir: kor birlik ize ve yeme bakmaz,
            // izci daha yuksek sapmayla yola cikar.
            model.IgnoresRecon = profile.IgnoresRecon;
            model.IgnoresDecoy = profile.IgnoresDecoy;
            model.ExplorationNoise = profile.ExplorationNoiseOverride > 0f
                ? profile.ExplorationNoiseOverride
                : baseNoise;

            model.BeginQuery(random.Next(), grid);
            int spawnCell = spawnCells[random.Next(spawnCells.Count)];

            if (!finder.FindPath(spawnCell, baseCell, unit.Path))
            {
                pool.Add(unit);
                return null;
            }

            unit.Visited.Add(unit.Path[0]);
            recon.Deposit(unit.Path[0], settings.DepositPerCell * profile.DepositMultiplier);
            return unit;
        }

        private void Advance(Unit unit, SlowField slow, ReconMap recon, DecoyField decoy,
                             Settings settings, float dt)
        {
            if (!unit.Alive || unit.PathIndex >= unit.Path.Count - 1) return;

            float factor = slow.Get(unit.Path[unit.PathIndex]);
            if (factor < SlowField.None) factor = SlowField.None;

            // Yavaslatma gecis suminefieldi carpar, yani hizi boler.
            unit.Progress += settings.CellsPerSecond * unit.Profile.SpeedMultiplier / factor * dt;

            while (unit.Progress >= 1f && unit.PathIndex < unit.Path.Count - 1)
            {
                unit.Progress -= 1f;
                unit.PathIndex++;

                int cell = unit.Path[unit.PathIndex];
                unit.Visited.Add(cell);
                recon.Deposit(cell, settings.DepositPerCell * unit.Profile.DepositMultiplier);

                // Kor birlik yalani ortaya cikarir: gectigi hucrede yem kalmaz.
                if (unit.Profile.ErasesDecoy) decoy.Erase(cell);
            }
        }

        /// <summary>
        /// Radar etkisi: silahli bir kule, menzilinde oldugu radarlarin EN
        /// GUCLUSUNDEN menzil eki alir (ust uste binmez). Keskin Nisanci
        /// yarim oran alir - calisma zamaniyla ayni kural (TowerStats).
        /// </summary>
        private static float RadarBonusFor(List<TowerSpec> towers, TowerSpec tower)
        {
            float best = 0f;

            for (int i = 0; i < towers.Count; i++)
            {
                TowerSpec radar = towers[i];
                if (radar.RangeBonus <= 0f) continue;

                float dx = radar.X - tower.X;
                float dy = radar.Y - tower.Y;
                if (dx * dx + dy * dy > radar.Radius * radar.Radius) continue;

                float bonus = TowerStats.RangeBonusFor(tower.Kind, radar.RangeBonus);
                if (bonus > best) best = bonus;
            }

            return best;
        }

        private void Fire(List<TowerSpec> towers, List<Unit> active, Settings settings,
                          float dt, List<float> beamTimers)
        {
            for (int t = 0; t < towers.Count; t++)
            {
                TowerSpec tower = towers[t];
                if (tower.Dps <= 0f) continue;

                float radarBonus = RadarBonusFor(towers, tower);
                if (radarBonus > 0f) tower.Radius *= 1f + radarBonus;

                float radiusSq = tower.Radius * tower.Radius;

                if (tower.Targeting == Targeting.Beam)
                {
                    // Ates zamani gelmediyse hicbir sey yapmaz.
                    beamTimers[t] -= dt;
                    if (beamTimers[t] > 0f) continue;
                    beamTimers[t] += tower.ShotInterval > 0f ? tower.ShotInterval : 1f;

                    // 1) Namluyu menzildeki en yakin birlige dogrult.
                    Unit aim = null;
                    float aimSq = tower.Radius * tower.Radius;

                    for (int i = 0; i < active.Count; i++)
                    {
                        Unit unit = active[i];
                        if (!unit.Alive || unit.Hp <= 0f) continue;

                        float sq = DistanceSq(unit, tower);
                        if (sq <= aimSq) { aimSq = sq; aim = unit; }
                    }

                    if (aim == null) continue;

                    int aimCell = aim.Path[aim.PathIndex];
                    float dirX = grid.X(aimCell) - tower.X;
                    float dirY = grid.Y(aimCell) - tower.Y;
                    float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                    if (len < 0.0001f) { dirX = 0f; dirY = -1f; }
                    else { dirX /= len; dirY /= len; }

                    // 2) O dogrultuda onune cikan HERKESI yarip gec.
                    //    Hedef sayisindan bagimsiz, hepsi ayni hasari alir.
                    for (int i = 0; i < active.Count; i++)
                    {
                        Unit unit = active[i];
                        if (!unit.Alive || unit.Hp <= 0f) continue;

                        int cell = unit.Path[unit.PathIndex];
                        float vx = grid.X(cell) - tower.X;
                        float vy = grid.Y(cell) - tower.Y;

                        // Namlu dogrultusundaki mesafe: arkada kalan vurulmaz.
                        float along = vx * dirX + vy * dirY;
                        if (along < 0f || along > tower.Radius) continue;

                        // Hatta dik uzaklik: hat ne kadar inceyse o kadar secici.
                        float perp = vx * dirY - vy * dirX;
                        if (perp < 0f) perp = -perp;
                        if (perp <= tower.Width) unit.Hp -= tower.ShotDamage;
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

                // Tek hedef ya da Topcu: menzildeki EN YAKIN birlik secilir.
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

                if (best == null) continue;

                if (tower.Targeting == Targeting.Blast)
                {
                    // Topcu: mermi hedefe duser, patlama yaricapindaki
                    // HERKES ayni hasari alir. Calisma zamaninda bu aralikli
                    // mermidir; burada ayni hasarin surekli esdegeri.
                    int center = best.Path[best.PathIndex];
                    float cx = grid.X(center);
                    float cy = grid.Y(center);
                    float blastSq = tower.BlastRadius * tower.BlastRadius;

                    for (int i = 0; i < active.Count; i++)
                    {
                        Unit unit = active[i];
                        if (!unit.Alive || unit.Hp <= 0f) continue;

                        int cell = unit.Path[unit.PathIndex];
                        float dx = grid.X(cell) - cx;
                        float dy = grid.Y(cell) - cy;
                        if (dx * dx + dy * dy <= blastSq) unit.Hp -= tower.Dps * dt;
                    }
                    continue;
                }

                best.Hp -= tower.Dps * dt;
            }
        }

        private float DistanceSq(Unit unit, TowerSpec tower)
        {
            int cell = unit.Path[unit.PathIndex];
            float dx = grid.X(cell) - tower.X;
            float dy = grid.Y(cell) - tower.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Birligin rotasinin orta satirda hangi sutundan gectigini isaretler.
        /// Rota spawn aninda bir kez hesaplandigi icin bu, birligin KARARIDIR.
        /// </summary>
        private void RecordCorridorChoice(Unit unit, int midRow, int[] columnUsage)
        {
            for (int i = 0; i < unit.Path.Count; i++)
            {
                int cell = unit.Path[i];
                if (grid.Y(cell) != midRow) continue;

                columnUsage[grid.X(cell)]++;
                return;
            }
        }

        /// <summary>
        /// Shannon entropisi, MEVCUT sutun sayisina gore normalize edilir -
        /// kullanilan sutun sayisina gore DEGIL.
        ///
        /// Fark onemli: kullanilana gore normalize edilseydi "iki koridoru
        /// esit kullanan ordu" ile "alti koridoru esit kullanan ordu" ayni
        /// puani alirdi ve metrik tam da olcmek istedigi seyi (kesif
        /// genisligini) gormezdi.
        /// </summary>
        private float NormalizedEntropy(int[] counts, int midRow)
        {
            int available = 0;
            for (int x = 0; x < grid.Width; x++)
                if (grid.IsWalkable(grid.Index(x, midRow))) available++;

            if (available <= 1) return 0f;

            int total = 0;
            for (int i = 0; i < counts.Length; i++) total += counts[i];
            if (total == 0) return 0f;

            double entropy = 0d;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0) continue;

                double p = (double)counts[i] / total;
                entropy -= p * Math.Log(p);
            }

            return (float)(entropy / Math.Log(available));
        }

        /// <summary>
        /// Dalga bilesiminden bir tur secer. Paylarin toplami 1'den kucukse
        /// kalan pay piyadeye gider; bilesim bos ise hepsi piyadedir.
        /// </summary>
        private static UnitProfile PickProfile(UnitMix[] composition, int wave, Random random)
        {
            if (composition == null || composition.Length == 0)
                return UnitProfile.Infantry();

            float roll = (float)random.NextDouble();
            float cursor = 0f;

            for (int i = 0; i < composition.Length; i++)
            {
                UnitMix mix = composition[i];
                if (wave < mix.FirstWave) continue;
                if (mix.Share <= 0f) continue;

                cursor += mix.Share;
                if (roll < cursor) return mix.Profile;
            }

            return UnitProfile.Infantry();
        }

        /// <summary>
        /// Kulucka olunce yavrular ayni yol uzerinde, oldugu noktada belirir.
        /// Yavrular ana birligin rotasini paylasir - yeni rota aramazlar,
        /// cunku ayrilma bir KARAR degil, bir kazadir.
        /// </summary>
        private void Split(Unit parent, List<Unit> active, List<Unit> pool,
                           RoutePathfinder finder, RouteCostModel model, Random random,
                           float baseNoise)
        {
            for (int n = 0; n < parent.Profile.SplitCount; n++)
            {
                Unit child = pool.Count > 0 ? pool[pool.Count - 1] : new Unit();
                if (pool.Count > 0) pool.RemoveAt(pool.Count - 1);

                child.Path.Clear();
                child.Path.AddRange(parent.Path);
                child.Visited.Clear();
                child.Visited.AddRange(parent.Visited);
                child.PathIndex = parent.PathIndex;
                child.Progress = parent.Progress;
                child.Profile = parent.Profile;
                child.IsSpawnling = true;
                child.Hp = BaseHpOf(parent) * parent.Profile.SplitHpMultiplier;
                child.SpawnHp = child.Hp;
                child.Alive = true;

                // Yavru yeni rota arayabilir: kulucka olunce surusu dagilir.
                if (parent.Profile.SplitlingsRepath)
                {
                    model.IgnoresRecon = parent.Profile.IgnoresRecon;
                    model.IgnoresDecoy = parent.Profile.IgnoresDecoy;
                    model.ExplorationNoise = parent.Profile.SplitlingNoise > 0f
                        ? parent.Profile.SplitlingNoise
                        : baseNoise;
                    model.BeginQuery(random.Next(), grid);

                    var fresh = new List<int>();
                    if (finder.FindPath(parent.Path[parent.PathIndex], baseCell, fresh))
                    {
                        child.Path.Clear();
                        child.Path.AddRange(fresh);
                        child.PathIndex = 0;
                        child.Progress = 0f;
                    }
                }

                active.Add(child);
            }
        }

        /// <summary>
        /// Yavrunun cani ana birligin BASLANGIC canindan turetilir; olum
        /// anindaki cani 0 oldugu icin ondan turetilemez.
        /// </summary>
        private static float BaseHpOf(Unit parent)
        {
            return parent.SpawnHp;
        }

        private static void Recycle(Unit unit, List<Unit> active, List<Unit> pool, int index)
        {
            active.RemoveAt(index);
            pool.Add(unit);
        }
    }
}
