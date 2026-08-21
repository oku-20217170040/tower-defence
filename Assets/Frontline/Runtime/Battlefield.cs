using System;
using System.Collections.Generic;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Cekirdegi (Frontline.Core) sahneye baglayan tek nokta.
    /// Haritayi uretir, dogrular, alanlari ve yol bulucuyu kurar.
    ///
    /// Frontline.Core'a Unity tipi sizmamasi icin tum kopru burada durur:
    /// MonoBehaviour olan tek sey bu sinif ve gorunum katmanidir.
    /// </summary>
    public sealed class Battlefield : MonoBehaviour
    {
        [Header("Harita Sablonu")]
        [Tooltip("Dar eksen. Dikey tutusta ekranin genisligi.")]
        public int width = 11;

        [Tooltip("Uzun eksen. Dikey tutusta ekranin yuksekligi.")]
        public int height = 20;

        [Tooltip("Kac dikey koridor uretilecek. Spec 9.4: en az 3.")]
        public int corridorCount = 3;

        [Tooltip("Her koridorun hucre cinsinden genisligi.")]
        public int corridorWidth = 2;

        [Tooltip("0 birakilirsa her calistirmada rastgele harita uretilir.")]
        public int seed = 0;

        [Header("Dogrulama")]
        [Tooltip("En ucuz iki koridor arasinda izin verilen azami maliyet farki. " +
                 "Buyuk fark bir koridoru bastan galip yapar ve secimi yanilsamaya cevirir.")]
        public float maxCostGap = 10f;

        [Header("Alanlar")]
        [Tooltip("Bir hucrenin tasiyabilecegi azami kesif izi.")]
        public float reconMax = 100f;

        [Tooltip("Her dalga sonunda izlerin kaybettigi oran.")]
        [Range(0f, 1f)]
        public float reconDecay = 0.15f;

        [Tooltip("Yemin ilk uygulamada ekledigi miktar. Tum yem dengesi bu sayiya bagli (spec 9.2).")]
        public float decoyStrength = 25f;

        [Tooltip("Ayni hucreye tekrar yem basmanin azalan getiri carpani.")]
        [Range(0f, 0.99f)]
        public float decoyDiminish = 0.5f;

        [Header("Rota Agirliklari")]
        public float threatWeight = 3f;

        [Tooltip("Ogrenilmis izin cekim gucu. 6 iken doymus iz bir DUVAR olur ve " +
                 "hicbir kule surusu yerinden oynatamaz; 2 iken suru cok cabuk pes " +
                 "eder. 3 olcumle secildi: suru direnir ama 5-6 dalgada teslim olur.")]
        public float attractionWeight = 3f;

        [Tooltip("Kesif orani. Her birlige yon tercihi katar. 0 = tum birlikler ayni " +
                 "koridoru secer ve ordu hicbir zaman alternatif denemez, yani " +
                 "ogrenme olmaz. 0.35 olcumle secildi: ordu dagilarak baslar ve " +
                 "6-8 dalgada tek koridora yerlesir.")]
        [Range(0f, 0.9f)]
        public float explorationNoise = 0.35f;

        [Header("Gorunum")]
        [Tooltip("Bir hucrenin dunya birimi cinsinden kenar uzunlugu.")]
        public float cellSize = 1f;

        /// <summary>Dogrulamadan gecen harita bulunamazsa vazgecilecek deneme sayisi.</summary>
        private const int MaxGenerationAttempts = 50;

        public BattleGrid Grid { get; private set; }
        public ReconMap Recon { get; private set; }
        public DecoyField Decoy { get; private set; }
        public ThreatField Threat { get; private set; }
        public SlowField Slow { get; private set; }
        public RouteCostModel CostModel { get; private set; }
        public RoutePathfinder Pathfinder { get; private set; }
        public GridSpace Space { get; private set; }

        /// <summary>Birliklerin hedefi: alt bandin ortasi.</summary>
        public int BaseCell { get; private set; }

        /// <summary>Birliklerin girebilecegi hucreler: ust banttaki koridor hucreleri.</summary>
        public IReadOnlyList<int> SpawnCells => spawnCells;

        private readonly List<int> spawnCells = new List<int>();

        /// <summary>Dunya yeniden uretildiginde tetiklenir; gorunum katmanlari buna baglanir.</summary>
        public event Action WorldRebuilt;

        private readonly List<Emplacement> towers = new List<Emplacement>();
        private readonly List<UnitAgent> activeUnits = new List<UnitAgent>();

        /// <summary>
        /// Sahnedeki canli birlikler. Kuleler hedef ararken bunu kullanir;
        /// her karede tum sahneyi taramak kabul edilemez.
        /// </summary>
        public IReadOnlyList<UnitAgent> ActiveUnits => activeUnits;

        public void RegisterUnit(UnitAgent unit)
        {
            if (unit != null && !activeUnits.Contains(unit)) activeUnits.Add(unit);
        }

        public void UnregisterUnit(UnitAgent unit)
        {
            if (unit != null) activeUnits.Remove(unit);
        }

        /// <summary>Sahnedeki tum kuleler. Yalnizca okuma icin.</summary>
        public IReadOnlyList<Emplacement> Towers => towers;

        /// <summary>
        /// Kule kayit olur. Batarya kuleler tehdit alanini degistirdigi icin
        /// alan bastan hesaplanir; pusu mevzisiler icin bu bedava bir islemdir.
        /// </summary>
        public void RegisterTower(Emplacement tower)
        {
            if (tower == null || towers.Contains(tower)) return;
            towers.Add(tower);
            RebuildThreat();
        }

        public void UnregisterTower(Emplacement tower)
        {
            if (tower == null) return;
            if (towers.Remove(tower)) RebuildThreat();
        }

        /// <summary>
        /// Tehdit alanini sifirdan kurar. YALNIZCA batarya kuleler yazar -
        /// pusu mevzisiler ordunin algisinda hic var olmaz (spec 3.1).
        /// </summary>
        public void RebuildThreat()
        {
            if (Threat == null) return;

            Threat.Clear();
            for (int i = 0; i < towers.Count; i++)
            {
                Emplacement tower = towers[i];
                if (tower == null || tower.kind != TowerKind.Battery) continue;

                Threat.AddSource(tower.Cell, tower.radius, tower.threatStrength);
            }

            RebuildSlow();
        }

        /// <summary>
        /// Yavaslatma alanini sifirdan kurar. YALNIZCA mayin tarlasi kuleleri yazar
        /// ve tehdit alanina HIC dokunmazlar - ordu yavaslatmayi algilamaz.
        /// Algilasaydi kacar ve mayin tarlasi olu mekanige donusurdu (olculdu).
        /// </summary>
        public void RebuildSlow()
        {
            if (Slow == null) return;

            Slow.Clear();
            for (int i = 0; i < towers.Count; i++)
            {
                Emplacement tower = towers[i];
                if (tower == null || tower.kind != TowerKind.Minefield) continue;

                Slow.AddSource(tower.Cell, tower.radius, tower.slowFactor);
            }
        }

        /// <summary>
        /// Yem kulelerini tazeler. Her dalga basinda cagrilir cunku yemin
        /// etkisi kalici degildir: basarili tuzak kendini yer ve yem surekli
        /// beslenmek ister (spec 3.3).
        /// </summary>
        public void ApplyDecoys()
        {
            if (Decoy == null) return;

            for (int i = 0; i < towers.Count; i++)
            {
                Emplacement tower = towers[i];
                if (tower == null || tower.kind != TowerKind.Decoy) continue;

                ApplyDecoyAround(tower.Cell, tower.radius);
            }
        }

        private void ApplyDecoyAround(int centerCell, float radius)
        {
            int cx = Grid.X(centerCell);
            int cy = Grid.Y(centerCell);
            int reach = (int)radius;

            for (int y = cy - reach; y <= cy + reach; y++)
            {
                for (int x = cx - reach; x <= cx + reach; x++)
                {
                    if (!Grid.InBounds(x, y)) continue;

                    int cell = Grid.Index(x, y);
                    if (!Grid.IsWalkable(cell)) continue;

                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy > radius * radius) continue;

                    Decoy.Apply(cell);
                }
            }
        }

        private void Awake()
        {
            Regenerate(seed);
        }

        /// <summary>
        /// Haritayi uretir ve dogrular. Gecemezse yeni seed ile yeniden dener.
        /// Sessizce bozuk harita ile devam etmek en kotu sonuctur (spec 9.4),
        /// bu yuzden deneme hakki bitince istisna firlatilir.
        /// </summary>
        public void Regenerate(int requestedSeed)
        {
            var template = new MapTemplate
            {
                Width = width,
                Height = height,
                CorridorCount = corridorCount,
                CorridorWidth = corridorWidth
            };

            var generator = new MapGenerator();
            var validator = new MapValidator();

            int activeSeed = requestedSeed != 0
                ? requestedSeed
                : UnityEngine.Random.Range(1, int.MaxValue);

            BattleGrid candidate = null;
            MapValidationResult result = default;
            int attempt = 0;

            while (attempt < MaxGenerationAttempts)
            {
                candidate = generator.Generate(template, activeSeed + attempt);
                result = validator.Validate(candidate, maxCostGap);

                if (result.IsValid) break;
                attempt++;
            }

            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    "[Battlefield] " + MaxGenerationAttempts +
                    " denemede gecerli harita uretilemedi. Son sebep: " + result.FaidecoyReason);
            }

            Grid = candidate;
            BuildFields();
            BuildEndpoints();

            Debug.Log("[Battlefield] Harita hazir. Seed=" + (activeSeed + attempt) +
                      " deneme=" + (attempt + 1) +
                      " koridor=" + result.CorridorCount +
                      " maliyetFarki=" + result.CostGap.ToString("F2"));

            WorldRebuilt?.Invoke();
        }

        private void BuildFields()
        {
            Recon = new ReconMap(Grid, reconMax, reconDecay);
            Decoy = new DecoyField(Grid, decoyStrength, decoyDiminish);
            Threat = new ThreatField(Grid);
            Slow = new SlowField(Grid);

            CostModel = new RouteCostModel(Recon, Decoy, Threat)
            {
                ThreatWeight = threatWeight,
                AttractionWeight = attractionWeight,
                ExplorationNoise = explorationNoise
            };

            Pathfinder = new RoutePathfinder(Grid, CostModel);
            Space = new GridSpace(Grid, cellSize);
        }

        private void BuildEndpoints()
        {
            BaseCell = Grid.Index(Grid.Width / 2, 0);

            spawnCells.Clear();
            int topRow = Grid.Height - 1;
            for (int x = 0; x < Grid.Width; x++)
            {
                int cell = Grid.Index(x, topRow);
                if (Grid.IsWalkable(cell))
                    spawnCells.Add(cell);
            }
        }

        /// <summary>Rastgele bir giris hucresi secer.</summary>
        public int RandomSpawnCell()
        {
            if (spawnCells.Count == 0) return BaseCell;
            return spawnCells[UnityEngine.Random.Range(0, spawnCells.Count)];
        }
    }
}
