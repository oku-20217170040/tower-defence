using System;
using System.Collections.Generic;
using UnityEngine;
using Colony.Core;

namespace Colony.Runtime
{
    /// <summary>
    /// Cekirdegi (Colony.Core) sahneye baglayan tek nokta.
    /// Haritayi uretir, dogrular, alanlari ve yol bulucuyu kurar.
    ///
    /// Colony.Core'a Unity tipi sizmamasi icin tum kopru burada durur:
    /// MonoBehaviour olan tek sey bu sinif ve gorunum katmanidir.
    /// </summary>
    public sealed class ColonyWorld : MonoBehaviour
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
        [Tooltip("Bir hucrenin tasiyabilecegi azami feromon.")]
        public float pheromoneMax = 100f;

        [Tooltip("Her dalga sonunda izlerin kaybettigi oran.")]
        [Range(0f, 1f)]
        public float pheromoneDecay = 0.15f;

        [Tooltip("Yemin ilk uygulamada ekledigi miktar. Tum yem dengesi bu sayiya bagli (spec 9.2).")]
        public float lureStrength = 25f;

        [Tooltip("Ayni hucreye tekrar yem basmanin azalan getiri carpani.")]
        [Range(0f, 0.99f)]
        public float lureDiminish = 0.5f;

        [Header("Rota Agirliklari")]
        public float threatWeight = 3f;
        public float attractionWeight = 6f;

        [Tooltip("Kesif orani. Her bocege yon tercihi katar. 0 = tum bocekler ayni " +
                 "koridoru secer ve koloni hicbir zaman alternatif denemez, yani " +
                 "ogrenme olmaz. 0.35 olcumle secildi: koloni dagilarak baslar ve " +
                 "6-8 dalgada tek koridora yerlesir.")]
        [Range(0f, 0.9f)]
        public float explorationNoise = 0.35f;

        [Header("Gorunum")]
        [Tooltip("Bir hucrenin dunya birimi cinsinden kenar uzunlugu.")]
        public float cellSize = 1f;

        /// <summary>Dogrulamadan gecen harita bulunamazsa vazgecilecek deneme sayisi.</summary>
        private const int MaxGenerationAttempts = 50;

        public ColonyGrid Grid { get; private set; }
        public PheromoneMap Pheromone { get; private set; }
        public LureField Lure { get; private set; }
        public ThreatField Threat { get; private set; }
        public RouteCostModel CostModel { get; private set; }
        public ColonyPathfinder Pathfinder { get; private set; }
        public GridSpace Space { get; private set; }

        /// <summary>Boceklerin hedefi: alt bandin ortasi.</summary>
        public int NestCell { get; private set; }

        /// <summary>Boceklerin girebilecegi hucreler: ust banttaki koridor hucreleri.</summary>
        public IReadOnlyList<int> SpawnCells => spawnCells;

        private readonly List<int> spawnCells = new List<int>();

        /// <summary>Dunya yeniden uretildiginde tetiklenir; gorunum katmanlari buna baglanir.</summary>
        public event Action WorldRebuilt;

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

            ColonyGrid candidate = null;
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
                    "[ColonyWorld] " + MaxGenerationAttempts +
                    " denemede gecerli harita uretilemedi. Son sebep: " + result.FailureReason);
            }

            Grid = candidate;
            BuildFields();
            BuildEndpoints();

            Debug.Log("[ColonyWorld] Harita hazir. Seed=" + (activeSeed + attempt) +
                      " deneme=" + (attempt + 1) +
                      " koridor=" + result.CorridorCount +
                      " maliyetFarki=" + result.CostGap.ToString("F2"));

            WorldRebuilt?.Invoke();
        }

        private void BuildFields()
        {
            Pheromone = new PheromoneMap(Grid, pheromoneMax, pheromoneDecay);
            Lure = new LureField(Grid, lureStrength, lureDiminish);
            Threat = new ThreatField(Grid);

            CostModel = new RouteCostModel(Pheromone, Lure, Threat)
            {
                ThreatWeight = threatWeight,
                AttractionWeight = attractionWeight,
                ExplorationNoise = explorationNoise
            };

            Pathfinder = new ColonyPathfinder(Grid, CostModel);
            Space = new GridSpace(Grid, cellSize);
        }

        private void BuildEndpoints()
        {
            NestCell = Grid.Index(Grid.Width / 2, 0);

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
            if (spawnCells.Count == 0) return NestCell;
            return spawnCells[UnityEngine.Random.Range(0, spawnCells.Count)];
        }
    }
}
