using System;

namespace Frontline.Core
{
    /// <summary>
    /// Uc alani tek bir adim maliyetinde birlestirir.
    ///
    ///   stepCost = 1
    ///            + ThreatWeight * threat
    ///            + AttractionWeight * (1 - normalizedAttraction)
    ///
    /// Cekim (kesif izi + yem) maliyeti DUSURUR ama bunu negatif deger ureterek
    /// degil, sabit bir cezayi azaltarak yapar. Maliyet asla 1'in altina inmez;
    /// bu, A* sezgiselinin kabul edilebilir (admissible) kalmasi icin zorunludur.
    /// </summary>
    public sealed class RouteCostModel
    {
        private readonly ReconMap recon;
        private readonly DecoyField decoy;
        private readonly ThreatField threat;

        /// <summary>Cekimin doygunluga ulastigi toplam deger. Kesif izi tavani ile aynidir.</summary>
        private readonly float attractionSaturation;

        public float ThreatWeight { get; set; } = 1f;
        public float AttractionWeight { get; set; } = 1f;

        /// <summary>
        /// Kesif orani [0, 1). Her birligin maliyet algisina kendine ozgu bir
        /// sapma katar.
        ///
        /// NEDEN GEREKLI: A* belirlenimcidir. Koridorlar esit maliyetteyken
        /// (ki MapValidator bunu zorunlu kilar) her birlik ayni koridoru secer
        /// ve ordu hicbir zaman alternatif denemez. Kesif olmadan ogrenme
        /// diye bir sey olmaz: ilk secim son secimdir. Gercek karincalar
        /// kesif iziu olasiliksal takip eder; bu alan onun karsiligidir.
        /// </summary>
        public float ExplorationNoise { get; set; } = 0f;

        /// <summary>
        /// Kesif izini yok sayar. Kor birlik icindir (Plan 8): yem kanalini
        /// kapatir ama tehdit kanali calismaya devam eder. Sorgu basina
        /// ayarlanir; BeginQuery'den ONCE verilmelidir.
        /// </summary>
        public bool IgnoresRecon { get; set; }

        /// <summary>Yemi yok sayar. Kor birlik icindir.</summary>
        public bool IgnoresDecoy { get; set; }

        private float[] queryNoise;
        private int gridWidth;

        public RouteCostModel(ReconMap recon, DecoyField decoy, ThreatField threat)
        {
            this.recon = recon ?? throw new ArgumentNullException(nameof(recon));
            this.decoy = decoy ?? throw new ArgumentNullException(nameof(decoy));
            this.threat = threat ?? throw new ArgumentNullException(nameof(threat));
            attractionSaturation = recon.Max;
        }

        /// <summary>
        /// Tek bir yol aramasi icin sapma alanini uretir. Her birlik kendi
        /// tohumuyla cagirir, boylece farkli birlikler farkli koridorlar dener.
        ///
        /// Sapma SUTUN BAZINDA uretilir, hucre bazinda degil. Bu kritik:
        /// hucre basina bagimsiz sapma 18 hucrelik bir yol boyunca ortalamaya
        /// gidip soner ve kesif yok olur. Sutun bazinda sapma ise bir birlige
        /// "bu koridoru sevme" gibi tutarli bir YON TERCIHI verir - gercek
        /// karincanin davranisi da budur.
        ///
        /// Sapma arama BOYUNCA SABIT kalir. Bu sart: A* ayni hucrenin
        /// maliyetini her sorguldugunda ayni cevabi almalidir.
        /// </summary>
        public void BeginQuery(int seed, BattleGrid grid)
        {
            if (ExplorationNoise <= 0f)
            {
                queryNoise = null;
                return;
            }

            gridWidth = grid.Width;

            if (queryNoise == null || queryNoise.Length != gridWidth)
                queryNoise = new float[gridWidth];

            var random = new Random(seed);
            float span = ExplorationNoise * 2f;

            for (int x = 0; x < gridWidth; x++)
                queryNoise[x] = 1f - ExplorationNoise + (float)random.NextDouble() * span;
        }

        /// <summary>
        /// Bir hucreye adim atmanin maliyeti. Sapma yokken her zaman 1 veya uzeri;
        /// sapma varken en dusuk deger 1 * (1 - ExplorationNoise) olur.
        /// </summary>
        public float StepCost(int index)
        {
            float attraction = 0f;
            if (!IgnoresRecon) attraction += recon.Get(index);
            if (!IgnoresDecoy) attraction += decoy.Get(index);
            float normalized = attraction / attractionSaturation;
            if (normalized > 1f) normalized = 1f;

            float attractionPenalty = AttractionWeight * (1f - normalized);
            float threatPenalty = ThreatWeight * threat.Get(index);

            float cost = 1f + threatPenalty + attractionPenalty;

            if (queryNoise != null)
                cost *= queryNoise[index % gridWidth];

            return cost;
        }
    }
}
