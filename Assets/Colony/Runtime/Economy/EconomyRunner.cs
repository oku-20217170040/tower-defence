using UnityEngine;
using Colony.Core;

namespace Colony.Runtime
{
    /// <summary>
    /// Ekonomiyi sahneye baglar: gelirleri toplar, kule fiyatlarini hesaplar.
    ///
    /// GUNCELLEME 2 (20 Agu 2026, oyuncu geri bildirimi): baslangic besini
    /// 50 -> 90, dalga geliri 10 -> 6, oldurme geliri 2 -> 1. Erken oyun
    /// aciliyor (ilk dalgada 1 yerine 2 kule) ama toplam zorluk AYNI
    /// kaliyor (sizinti 22,1 -> 21,8) cunku gec oyun geliri kisildi.
    /// Yan fayda: artan besin 120 -> 92.
    ///
    /// GUNCELLEME (20 Agu 2026): bocek cani buyumesi %15'ten %10'a
    /// dusuruldugu icin fiyatlar yukseltildi (sessiz 24 -> 40, gurultucu
    /// 12 -> 20, yem/recine 16 -> 26). Olcum: bu ikili birlikte kule
    /// sayisini 14'ten 12'ye indiriyor ve zorlugu koruyor (sizinti 20,4 ->
    /// 22,8). Tek basina can dusurulseydi oyun KOLAYLASIRDI.
    ///
    /// Tum sayilar 162 kombinasyon x 20 seed taramasiyla secildi
    /// (ColonyEconomyTests egriyi koruyor). Secilen ayar: optimal oynayan
    /// oyuncu run'larin %85'ini kazaniyor, 8. dalgada ortalama 5,6 kuleye
    /// ulasiyor ve yuva caninin 30'da 22'sini kaybediyor.
    ///
    /// Taramada bulunan iki sey:
    /// 1. Ayni turde +10 fiyat artisi run'i olduruyordu (kazanma %10).
    ///    +5'te ekonomi nefes aliyor ama spam yine pahali.
    /// 2. Dalga sonu geliri bir TABAN GELIRDIR: oldurmekten bagimsiz oldugu
    ///    icin tuzagi calismayan oyuncunun kaybetme sarmalini kiriyor.
    /// </summary>
    public sealed class EconomyRunner : MonoBehaviour
    {
        [Header("Gelir")]
        [Tooltip("Run basindaki besin. 90 olcumle secildi. 50 iken ilk dalgada " +
                 "YALNIZCA BIR kule kurulabiliyordu - uc koridorlu bir haritada " +
                 "bu, oyuncunun sistemi hic kuramamasi demek (oyuncu geri bildirimi).")]
        public int startingBalance = 90;

        [Tooltip("Oldurulen bocek basina besin.")]
        public int foodPerKill = 1;

        [Tooltip("Dalga tamamlaninca kazanilan besin. Bu bir TABAN GELIRDIR: " +
                 "oldurmekten bagimsiz oldugu icin kaybetme sarmalini kirar.")]
        public int foodPerWave = 6;

        [Header("Kule Fiyatlari")]
        [Tooltip("Gurultucu kule ucuzdur: tek basina ise yaramadigi icin (spec 3.4) " +
                 "oyuncunun onu denemesi ucuza gelmeli.")]
        public int noisyCost = 20;

        [Tooltip("Sessiz kule pahalidir: gercek katliami o yapar.")]
        public int silentCost = 40;

        [Tooltip("Yem kulesi orta fiyatlidir.")]
        public int lureCost = 26;

        [Tooltip("Recine kulesi. Hasar vermedigi icin sessizden ucuz; degeri " +
                 "yalnizca bir sessiz kuleyle eslestiginde ortaya cikiyor.")]
        public int resinCost = 26;

        [Tooltip("Ayni turden her yeni kulenin ek maliyeti. 10'da run olcumde " +
                 "olduruyordu (kazanma %10); 5 spam'i yine pahali tutuyor.")]
        public int costStepPerSameKind = 5;

        public RunEconomy Economy { get; private set; }

        [Header("Draft Odulleri")]
        [Tooltip("Indirim toplaminin ust siniri (yuzde). Sinirsiz indirim " +
                 "ekonomiyi anlamsizlastirir.")]
        public int maxDiscountPercent = 60;

        // Kule turu basina birikmis indirim ve bedava kule hakki.
        private readonly int[] discountPercent = new int[4];
        private readonly int[] freeVouchers = new int[4];

        private BugSpawner spawner;
        private RunController run;
        private ColonyWorld world;
        private int lastKillCount;

        private void Awake()
        {
            Economy = new RunEconomy(startingBalance);

            spawner = FindFirstObjectByType<BugSpawner>();
            run = FindFirstObjectByType<RunController>();
            world = FindFirstObjectByType<ColonyWorld>();
        }

        private void OnEnable()
        {
            if (run != null) run.WaveChanged += OnWaveChanged;
        }

        private void OnDisable()
        {
            if (run != null) run.WaveChanged -= OnWaveChanged;
        }

        private void Update()
        {
            if (spawner == null) return;

            // Oldurulen sayisindaki artis kadar gelir yaz. Olay yerine sayac
            // farki kullanmak, bocek yok edilirken abonelik sirasina bagimli
            // olmayi engelliyor.
            int killed = spawner.TotalKilled;
            if (killed > lastKillCount)
            {
                Economy.Earn((killed - lastKillCount) * foodPerKill);
                lastKillCount = killed;
            }
        }

        private void OnWaveChanged(int wave, int total)
        {
            // Ilk dalga icin gelir yok; baslangic sermayesi onun yerine geciyor.
            if (wave > 1) Economy.Earn(foodPerWave);
        }

        /// <summary>Bu turden bir sonraki kulenin fiyati.</summary>
        public int CostFor(TowerKind kind)
        {
            if (freeVouchers[(int)kind] > 0) return 0;

            int raw = BaseCostFor(kind) + costStepPerSameKind * CountOf(kind);
            int off = discountPercent[(int)kind];
            if (off <= 0) return raw;

            return raw * (100 - off) / 100;
        }

        /// <summary>Draft odulu: bu turden bir kule bedava.</summary>
        public void GrantFreeTower(TowerKind kind)
        {
            freeVouchers[(int)kind]++;
        }

        /// <summary>Draft odulu: bu turun fiyatinda kalici indirim.</summary>
        public void AddDiscount(TowerKind kind, int percent)
        {
            int total = discountPercent[(int)kind] + percent;
            if (total > maxDiscountPercent) total = maxDiscountPercent;
            discountPercent[(int)kind] = total;
        }

        public int DiscountFor(TowerKind kind) { return discountPercent[(int)kind]; }

        public int FreeTowersFor(TowerKind kind) { return freeVouchers[(int)kind]; }

        public bool CanAfford(TowerKind kind)
        {
            return Economy.CanAfford(CostFor(kind));
        }

        /// <summary>Satin almayi dener. Basarisizsa hicbir sey degismez.</summary>
        public bool TryBuy(TowerKind kind)
        {
            if (freeVouchers[(int)kind] > 0)
            {
                freeVouchers[(int)kind]--;
                return true;
            }

            return Economy.TrySpend(CostFor(kind));
        }

        private int BaseCostFor(TowerKind kind)
        {
            if (kind == TowerKind.Noisy) return noisyCost;
            if (kind == TowerKind.Lure) return lureCost;
            if (kind == TowerKind.Resin) return resinCost;
            return silentCost;
        }

        private int CountOf(TowerKind kind)
        {
            if (world == null) return 0;

            int count = 0;
            var towers = world.Towers;
            for (int i = 0; i < towers.Count; i++)
                if (towers[i] != null && towers[i].kind == kind) count++;

            return count;
        }
    }
}
