namespace Frontline.Core
{
    /// <summary>
    /// Genetik katmanin BUTUN sabitleri. Tek dosyada toplandi ki ileride
    /// bassiz simulator taramasiyla (RunSimulator) tek tek olculebilsin -
    /// bu projede her sabit once tahmin edildi, sonra olculdu.
    ///
    /// Tasarim kisiti (spec bolum 6, "beceri tavani korunmalidir"):
    /// ordu run'lar boyunca GUCLENMEZ, yalnizca DAGILIMI kayar. Toplam
    /// guc butcesi sabittir; oyuncunun baskin stratejisine karsi direnc
    /// artarken baska bir direnc ayni miktarda azalir. Nemesis islevi
    /// budur: favori strateji asinir, oyun adaletsizlesmez.
    ///
    /// Saf C#'tir; Unity tipi kullanmaz.
    /// </summary>
    public static class EvolutionPressure
    {
        /// <summary>TowerKind uye sayisi (Battery, Ambush, Decoy, Minefield, Beam).</summary>
        public const int TowerKindCount = 5;

        /// <summary>UnitType uye sayisi (Infantry, Scout, Soldier, Blind, Brood, Queen).</summary>
        public const int UnitTypeCount = 6;

        // ---- Direnc butcesi -------------------------------------------------

        /// <summary>
        /// Notr direnc. 1 = "bu kuleye karsi ne avantaj ne dezavantaj".
        /// </summary>
        public const float BaselineResistance = 1f;

        /// <summary>
        /// Direnclerin TOPLAMI. Evrim bu toplami degistiremez; yalnizca
        /// kuleler arasinda pay dagitir. Guc butcesinin kendisidir.
        /// </summary>
        public const float ResistanceBudget = TowerKindCount * BaselineResistance;

        /// <summary>
        /// Alt sinir. Bir kuleye karsi direnc bunun altina inemez; yoksa
        /// oyuncu tek kuleyi spamlayip kalan dorde karsi orduyu kagit
        /// haline getirirdi (butce korunsa bile somurulur).
        /// </summary>
        public const float MinResistance = 0.55f;

        /// <summary>
        /// Ust sinir. Ordu HIC yenilmez olamaz. Denge noktasi zaten
        /// ~1,32'de olusur (LearnRate/DecayRate orani); bu deger yalnizca
        /// guvenlik kilididir.
        /// </summary>
        public const float MaxResistance = 1.60f;

        /// <summary>
        /// Ogrenme hizi. Oyuncunun kule dagilimindaki baskinlik ile notr
        /// pay (1/5) arasindaki fark bu katsayiyla direnc olur.
        ///
        /// Secim gerekcesi: tek kule spamlanirsa (pay 1, notr 0,2) run
        /// basina artis 0,06 * 0,8 = 0,048. Yani ordu bir run'da
        /// tanınmaz hale gelmez - oyuncunun ogrendigi hala gecerlidir -
        /// ama 5-6 run'da favori strateji hissedilir sekilde asinir.
        /// </summary>
        public const float LearnRate = 0.06f;

        /// <summary>
        /// Sonme hizi. Her run direncler tabana dogru bu oranda geri
        /// cekilir. Kullanilmayan stratejiye karsi direnc boylece soner
        /// ve oyuncu eski taktigine donebilir (cesitlendirme, ceza degil).
        ///
        /// LearnRate ile birlikte denge noktasini belirler:
        /// 0,06*0,8 = 0,15*(r-1) -> r = 1,32. Ust sinirin altinda kalir,
        /// yani sinir normalde hic devreye girmez.
        /// </summary>
        public const float DecayRate = 0.15f;

        /// <summary>
        /// Oyuncu run'i KAYBETTIYSE baski bu katsayiyla carpilir. Kaybeden
        /// oyuncunun uzerine gitmek beceri tavanini degil moral tabanini
        /// vurur; sonme yine tam hizda calisir, yani kaybedilen run'lar
        /// orduyu tabana dogru geri getirir.
        /// </summary>
        public const float LossPressureScale = 0.35f;

        /// <summary>
        /// Rastgele mutasyon genligi. Cok kucuk: evrimin yonu OLCUMDEN
        /// gelmeli, zardan degil. Amaci yalnizca simetrik durumlarda
        /// (butun kuleler esit kullanilmis) kilitlenmeyi kirmaktir.
        /// </summary>
        public const float MutationAmplitude = 0.004f;

        /// <summary>
        /// Bir run'da tek bir direncin degisebilecegi EN BUYUK miktar.
        /// Sert kilit: hicbir girdi kombinasyonu orduyu tek run'da
        /// tanınmaz hale getiremesin. LearnRate+DecayRate+mutasyon en
        /// kotu halde ~0,10 verir; bu deger onun hemen ustundedir.
        /// </summary>
        public const float MaxResistanceStepPerRun = 0.12f;

        // ---- Birlik payi butcesi -------------------------------------------

        /// <summary>
        /// Bir birlik turunun "guc agirligi". UnitProfile.For'daki can
        /// carpanlaridir - guc butcesi oradaki olculmus sayilara baglanir,
        /// ayri bir tablo uydurulmaz.
        /// </summary>
        public static float PowerWeight(UnitType type)
        {
            if (type == UnitType.Scout) return 0.5f;
            if (type == UnitType.Soldier) return 3f;
            if (type == UnitType.Queen) return 8f;
            if (type == UnitType.Blind) return 1f;
            if (type == UnitType.Brood) return 1f;
            return 1f; // Infantry
        }

        /// <summary>
        /// Tek bir turun alabilecegi en buyuk pay. Ordu tek turden
        /// olusamaz: bu hem gorsel olarak okunmaz olurdu hem de spec
        /// 5.2'nin ucgenini (izci DAGITIR, asker TOPLAR, kralice
        /// UNUTTURUR) yok ederdi.
        /// </summary>
        public const float MaxUnitShare = 0.35f;

        /// <summary>
        /// Piyade DISI turlerin toplam payi. Piyade her zaman ordunun en
        /// az %40'idir; taban birlik olmadan iz/tehdit/yem kanallari
        /// okunmaz hale gelir.
        /// </summary>
        public const float MaxNonInfantryShare = 0.60f;

        /// <summary>
        /// Bilesimin toplam guc butcesi: bugunku varsayilan bilesimin
        /// (UnitRoster.Default) agirlikli gucu. Evrim paylari kaydirir,
        /// bu sayiyi degistiremez.
        ///
        /// 0,25*0,5 + 0,20*3 + 0,05*8 + 0,50*1 = 1,625
        /// </summary>
        public static float DefaultCompositionPower()
        {
            var genome = ArmyGenome.Default();
            return genome.CompositionPower();
        }

        /// <summary>Pay mutasyonunun run basina genligi. Direnclerden de kucuk.</summary>
        public const float ShareMutationAmplitude = 0.006f;

        /// <summary>
        /// Pay baskisi: oyuncu run'i kazandiysa ordu bilesimi bu kadar
        /// kayar. Kaybettiyse ters yone AYNI kadar kaymaz - kaybeden
        /// oyuncuya karsi bilesim sertlestirilmez.
        /// </summary>
        public const float SharePressure = 0.010f;

        /// <summary>Guc butcesi normalizasyonunun kabul ettigi sapma.</summary>
        public const float PowerTolerance = 1e-4f;
    }
}
