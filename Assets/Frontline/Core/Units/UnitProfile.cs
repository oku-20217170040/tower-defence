namespace Frontline.Core
{
    /// <summary>
    /// Birlik turu. Roster klasik TD'den (hizli/tank/ucan/bolunen/boss)
    /// OLDUGU GIBI alinmadi; her tur bir OGRENME KANALINA baglandi.
    ///
    /// Kural (Plan 8): bir birlik turu kesif izi/ogrenme ile etkilesmiyorsa
    /// yalnizca bir can rakamidir ve eklenmez. Ayni kural Plan 7'de kuleler
    /// icin iki tasarimi eledi (Saat kulesi, surekli Isin).
    /// </summary>
    public enum UnitType
    {
        /// <summary>Taban birlik. Iz birakir, tehdit ve yem gorur.</summary>
        Infantry = 0,

        /// <summary>
        /// Hizli ve kirilgan. Yuksek sapmayla YENI koridor dener; ordu onun
        /// sayesinde ogrenir. Oyuncu onu oldurerek ogrenmeyi yavaslatabilir.
        /// </summary>
        Scout = 1,

        /// <summary>
        /// Yavas, cok canli. Vardiginda rotayi COK GUCLU pekistirir - gectigi
        /// koridoru orduya civiler. Doymus iz duvardir.
        /// </summary>
        Soldier = 2,

        /// <summary>
        /// Kesif izine ve yeme HIC bakmaz; en kisa yolu tutar. Gercek ucan
        /// dusmanin yerine gecer (20 Agu karari): ucan, yalan soyleme
        /// kanalini tumuyle kapatirdi; kor birlik yalnizca YEM kanalini
        /// kapatir, tehdit kanali calismaya devam eder.
        /// </summary>
        Blind = 3,

        /// <summary>
        /// Olunce ikiye ayrilir ve oldugu yere guclu olum-izi basar.
        /// Oyuncunun kendi tuzagi orduya "buradan gitme" diye isaretlenir.
        /// </summary>
        Brood = 4,

        /// <summary>
        /// Ordunun HAFIZASINI siler: usya varirsa kesif izi sifirlanir ve
        /// ordu ogrendigi her seyi unutur.
        ///
        /// Tasarim belgesi "tehdit alanini temizler" diyordu; olcum bunun
        /// yanlis oldugunu gosterdi. Tehdit alani ordunun hafizasi degil,
        /// kulelerin STATIK haritasidir ve her dalga kulelerden yeniden
        /// kurulur - silmenin hicbir etkisi olmuyor (sizinti 26,4 -> 26,4).
        /// Ordunun gercek hafizasi kesif izidir.
        /// </summary>
        Queen = 5,

        /// <summary>
        /// Yaban Arisi: UCAR. Koridor tanimaz, duz cizgide usya gider
        /// (spec 5.2).
        ///
        /// TASARIM UYARISI - bu tur oyunun merkezini delme riskini tasir.
        /// Oyunun tamami koridor + kesif izi + kandirma uzerine kurulu
        /// (spec 3). Koridor tanimayan bir birlik, oyuncunun butun tuzak
        /// kurgusunu atlar. 20 Agustos'ta bu yuzden ucan birlik yerine
        /// KOR birlik (Blind) konmustu. Ucan yine de eklendi, ama uc
        /// kisitla - ucu de bu riskin dogrudan panzehiridir:
        ///
        ///   1) SEYREK GELIR. Pay kucuk ve FirstWave gec
        ///      (bkz. UnitRoster.FlyerShare / FlyerFirstWave). Ucan bir
        ///      tehdit cesnisidir, dalganin govdesi degildir.
        ///   2) IZ BIRAKMAZ. Deposit/Reinforce/Weaken carpanlari 0 ve
        ///      LeavesNoRecon acik. Sebep mekanik: ucan duvarlarin
        ///      uzerinden gecer; iz biraksaydi YURUNMEYEN hucrelere
        ///      kesif izi yazardi ve ogrenme katmani yalnizca kirlenmis
        ///      olmazdi, anlamsizlasirdi. Ucan orduyu ogretmez, yalnizca
        ///      oyuncuyu yoklar.
        ///   3) CEVABI VARDIR. Duz cizgi tam olarak koridorlarin
        ///      ARASINDAN gecer - spec 3.8'e gore kurulum seritleri
        ///      oradadir. Yani menzili yeten kule ona ulasir; cani da
        ///      dusuktur (x0,6). Ucan "cevapsiz" degil, "farkli cevap
        ///      isteyen" tehdittir.
        ///
        /// OLCULMEDI: bu paragraftaki butun sayilar TAHMINDIR.
        /// </summary>
        Flyer = 6
    }

    /// <summary>
    /// Bir birlik turunun sayilari. Hepsi TABAN degerin CARPANIDIR; boylece
    /// olculmus sabitler (birlik cani 10, hiz 2,5 hucre/sn, iz 0,5+1,5)
    /// tek yerde kalir ve turler onlara gore konumlanir.
    ///
    /// Saf C#'tir; Unity tipi kullanmaz - denge olcumleri bassiz kosulabilsin
    /// diye (bu projede en cok ise yarayan sey oydu).
    /// </summary>
    public struct UnitProfile
    {
        public UnitType Type;

        public float HpMultiplier;
        public float SpeedMultiplier;

        /// <summary>Yururken hucre basina birakilan iz carpani.</summary>
        public float DepositMultiplier;

        /// <summary>Usya varinca rotaya eklenen pekistirme carpani.</summary>
        public float ReinforceMultiplier;

        /// <summary>Olunce rotadan silinen miktarin carpani.</summary>
        public float WeakenMultiplier;

        /// <summary>Kesif izini yok sayar (kor birlik).</summary>
        public bool IgnoresRecon;

        /// <summary>Yemi yok sayar (kor birlik).</summary>
        public bool IgnoresDecoy;

        /// <summary>
        /// Bu turun kesif sapmasi. 0 ise taban sapma kullanilir.
        ///
        /// Taban 0,12'dir ve kesfi TASIYAN birlik izcidir (21 Agu olcumu).
        /// Onceki taban 0,35'ti; o deger butun birlikler ayni sapmayi
        /// tasirken olculmustu ve o kurulumda izci gereksiz kaliyordu
        /// (entropi 0,750 -> 0,789).
        /// </summary>
        public float ExplorationNoiseOverride;

        /// <summary>Olunce kac yavru birakir (kulucka). 0 = bolunmez.</summary>
        public int SplitCount;

        /// <summary>Yavrularin ana birlige gore can carpani.</summary>
        public float SplitHpMultiplier;

        /// <summary>
        /// Yavrular ANA BIRLIGIN rotasini surdurmek yerine YENI rota arar.
        /// Kacisma karsiligidir: kulucka olunce surusu dagilir.
        /// </summary>
        public bool SplitlingsRepath;

        /// <summary>Yavrularin rota ararken kullandigi sapma. 0 = taban.</summary>
        public float SplitlingNoise;

        /// <summary>
        /// Tehdit alanini siler (kralice). UYARI: tehdit alani ordunun
        /// HAFIZASI DEGILDIR - her dalga basinda kulelerden yeniden
        /// kurulur. Bu yuzden silmenin olculebilir bir etkisi yoktur.
        /// Ordunun gercek hafizasi kesif izidir; bkz. ClearsRecon.
        /// </summary>
        public bool ClearsThreat;

        /// <summary>
        /// Kesif izini siler: ordunun OGRENDIGI her seyi sifirlar.
        /// Kralicenin "ogrenmeyi resetler" iddiasinin gercek karsiligi.
        /// </summary>
        public bool ClearsRecon;

        /// <summary>
        /// Gectigi hucrelerdeki yemi siler (kor birlik). Yalan onun
        /// uzerinde islemez ve YALANI ORTAYA CIKARIR: arkadan gelen
        /// birlikler de o hucrede yeme kanmaz.
        /// </summary>
        public bool ErasesDecoy;

        /// <summary>
        /// Ucar: araziyi (duvarlari, koridorlari) yok sayar ve usya duz
        /// cizgide gider. Yol bulucu hic calistirilmaz.
        ///
        /// Polarite bilerek NEGATIFTIR (varsayilan false = yurur). Boylece
        /// default(UnitProfile) eski davranisi korur; mevcut 200 test bu
        /// varsayima bagli.
        /// </summary>
        public bool IgnoresTerrain;

        /// <summary>
        /// Hic kesif izi birakmaz: ne yururken deposit, ne varista
        /// pekistirme, ne olumde zayiflatma. Ucan icindir.
        ///
        /// Carpanlari 0 yapmak tek basina yeterli olabilirdi; ayri bir
        /// bayrak var cunku "iz birakmama" bir SAYI degil bir TASARIM
        /// KARARIDIR ve testin dogrudan iddia edebilecegi bir sey olmali.
        /// Yine negatif polarite: varsayilan false = iz birakir.
        /// </summary>
        public bool LeavesNoRecon;

        /// <summary>Taban birlik: butun carpanlar 1, hicbir ozel davranis yok.</summary>
        public static UnitProfile Infantry()
        {
            return new UnitProfile
            {
                Type = UnitType.Infantry,
                HpMultiplier = 1f,
                SpeedMultiplier = 1f,
                DepositMultiplier = 1f,
                ReinforceMultiplier = 1f,
                WeakenMultiplier = 1f,
                SplitHpMultiplier = 1f
            };
        }

        /// <summary>
        /// Yaban Arisi. Ucar, iz birakmaz, kirilgandir.
        ///
        /// Sayilar OLCULMEDI - tahmindir. Gerekceler:
        ///   can x0,6   : oyuncunun cevabi olabilmeli; menzili yeten tek
        ///                kule birkac atista dusurebilmeli (spec 5.2'nin
        ///                "cevap birak" kosulu).
        ///   hiz x1,3   : duz cizgi zaten kisa yoldur; ustune yuksek hiz
        ///                verilirse kule hic atis penceresi bulamaz. 1,3
        ///                "aceleci ama vurulabilir" bandi icin secildi.
        ///   iz carpanlari 0 : bkz. UnitType.Flyer aciklamasi (2. kisit).
        ///
        /// IgnoresRecon/IgnoresDecoy DE acik: ucan zaten yolu izlemiyor,
        /// dolayisiyla yem ve iz onun icin tanimsizdir. Bu, ucanin
        /// KANDIRILAMAZ oldugu anlamina gelir - tam da payinin kucuk ve
        /// giris dalgasinin gec olmasinin sebebi budur.
        /// </summary>
        public static UnitProfile Flyer()
        {
            return For(UnitType.Flyer);
        }

        /// <summary>
        /// Verilen tur icin taban profil.
        ///
        /// IZCI OLCULDU (21 Agu 2026). Digerlerinin sayilari HENUZ
        /// OLCULMEDI - Plan 8 Gorev 3-6 her turu tek tek tarayacak;
        /// buradaki degerler yalnizca baslangic noktasidir.
        /// </summary>
        public static UnitProfile For(UnitType type)
        {
            UnitProfile profile = Infantry();
            profile.Type = type;

            if (type == UnitType.Scout)
            {
                // Olculen (taban sapma 0,12, dalga basina koridor entropisi):
                //   izci yok  -> 0,428
                //   izci %25  -> 0,575   (secilen)
                //   izci %50  -> 0,694   (oyun kolaylasiyor: sizinti 4,1 -> 2,3)
                // Sapma zayif bir dugme: 0,5 ile 0,85 arasi entropiyi yalnizca
                // 0,552'den 0,584'e tasiyor. 0,7 ortadan secildi.
                profile.HpMultiplier = 0.5f;
                profile.SpeedMultiplier = 1.6f;
                profile.ExplorationNoiseOverride = 0.7f;
            }
            else if (type == UnitType.Soldier)
            {
                // Olculen (21 Agu 2026). Metrik: askerin USYA VARDIGI
                // sutunun SONRAKI dalgadaki pay artisi ("civileme").
                // Ortalama entropi bunu gormuyordu - dalgadan dalgaya
                // bakmak gerekti.
                //
                //   pekistirme x1  -> lift -0,009  (civileme YOK)
                //   pekistirme x3  -> lift +0,014
                //   pekistirme x6  -> lift +0,041  (secilen)
                //   pekistirme x20 -> lift +0,070  (kazanci azaliyor)
                //
                // Pekistirme bir ZORLUK dugmesi DEGIL: x1'den x20'ye
                // sizinti yalnizca 11,8 -> 13,6. Karakter dugmesidir,
                // tipki bataryanin alan hasari gibi.
                //
                // Can ise keskin bir zorluk esigi (bu projede kacinci kez):
                //   can x2 -> sizinti  4,0   can x3 -> 13,2   can x4 -> 22,6
                profile.HpMultiplier = 3f;
                profile.SpeedMultiplier = 0.6f;
                profile.ReinforceMultiplier = 6f;
            }
            else if (type == UnitType.Blind)
            {
                profile.IgnoresRecon = true;
                profile.IgnoresDecoy = true;
            }
            else if (type == UnitType.Brood)
            {
                profile.SplitCount = 2;
                profile.SplitHpMultiplier = 0.5f;
                profile.WeakenMultiplier = 3f;
            }
            else if (type == UnitType.Flyer)
            {
                profile.HpMultiplier = 0.6f;
                profile.SpeedMultiplier = 1.3f;

                profile.IgnoresTerrain = true;
                profile.LeavesNoRecon = true;

                // Iz kanallarinin ucu de kapali. LeavesNoRecon'u
                // uygulayan taraf bunlari zaten atlamali; carpanlarin da
                // 0 olmasi ikinci bir emniyet kemeridir.
                profile.DepositMultiplier = 0f;
                profile.ReinforceMultiplier = 0f;
                profile.WeakenMultiplier = 0f;

                // Ucan yolu izlemedigi icin iz ve yem onun icin tanimsiz.
                profile.IgnoresRecon = true;
                profile.IgnoresDecoy = true;
            }
            else if (type == UnitType.Queen)
            {
                // Olculen (21 Agu 2026, pay %8):
                //   tehdit siler   -> sizinti 26,4 · entropi 0,507
                //   tehdit silmez  -> sizinti 26,4 · entropi 0,507  (AYNI)
                //   HAFIZA siler   -> sizinti 25,2 · entropi 0,552
                //
                // Hafiza silme candan BAGIMSIZ bir kanal: zayif kralicede
                // (can x2) bile entropi 0,520 -> 0,535. Yani kralice
                // "buyuk canli birlik" degil, ordunun ogrendigini
                // unutturan birliktir.
                profile.HpMultiplier = 8f;
                profile.SpeedMultiplier = 0.7f;
                profile.ClearsThreat = false;
                profile.ClearsRecon = true;
            }

            return profile;
        }
    }

    /// <summary>
    /// Bir dalgadaki tur payi. Paylarin toplami 1'den kucukse kalan pay
    /// piyadeye gider.
    /// </summary>
    public struct UnitMix
    {
        public UnitProfile Profile;

        /// <summary>Dalgadaki payi [0, 1].</summary>
        public float Share;

        /// <summary>Bu tur ilk kez hangi dalgada cikar.</summary>
        public int FirstWave;
    }
}
