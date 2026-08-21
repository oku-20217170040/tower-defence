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
        /// Tehdit alanini siler - ordunun korkusunu sifirlar, ogrenmeyi
        /// resetler. Oyuncu yonlendirmeyi yeniden kurmak zorunda kalir.
        /// </summary>
        Queen = 5
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
        /// Taban sapma 0,35 olculmus bir sabittir; izci onu birlik basina
        /// YUKSELTIR, tabani dusurmez.
        /// </summary>
        public float ExplorationNoiseOverride;

        /// <summary>Olunce kac yavru birakir (kulucka). 0 = bolunmez.</summary>
        public int SplitCount;

        /// <summary>Yavrularin ana birlige gore can carpani.</summary>
        public float SplitHpMultiplier;

        /// <summary>Tehdit alanini siler (kralice).</summary>
        public bool ClearsThreat;

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
        /// Verilen tur icin taban profil. SAYILAR HENUZ OLCULMEDI - Plan 8
        /// Gorev 2-6 her turu tek tek tarayacak. Buradaki degerler yalnizca
        /// baslangic noktasidir; olcum yapilmadan koda gecmis sayilmazlar.
        /// </summary>
        public static UnitProfile For(UnitType type)
        {
            UnitProfile profile = Infantry();
            profile.Type = type;

            if (type == UnitType.Scout)
            {
                profile.HpMultiplier = 0.5f;
                profile.SpeedMultiplier = 1.6f;
                profile.ExplorationNoiseOverride = 0.5f;
            }
            else if (type == UnitType.Soldier)
            {
                profile.HpMultiplier = 3f;
                profile.SpeedMultiplier = 0.6f;
                profile.ReinforceMultiplier = 3f;
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
            else if (type == UnitType.Queen)
            {
                profile.HpMultiplier = 8f;
                profile.SpeedMultiplier = 0.7f;
                profile.ClearsThreat = true;
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
