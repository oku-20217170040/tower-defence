namespace Frontline.Core
{
    /// <summary>Draft seceneginin turu.</summary>
    public enum DraftOptionKind
    {
        /// <summary>Bedava kule. Belirli bir koridora onerilir.</summary>
        FreeTower = 0,

        /// <summary>Bir kule turunun fiyatinda kalici indirim.</summary>
        Discount = 1,

        /// <summary>Anlik besin.</summary>
        Food = 2,

        /// <summary>Kalici pasif iyilestirme.</summary>
        Passive = 3
    }

    /// <summary>Pasif iyilestirmenin hangi ozelligi artirdigi.</summary>
    public enum PassiveKind
    {
        None = 0,
        Damage = 1,
        Range = 2
    }

    /// <summary>
    /// Dalga sonunda sunulan tek bir secenek. Saf veridir; nasil cizilecegi
    /// Runtime'in isidir.
    ///
    /// Corridor alani seceneklerin neden rastgele olmadigini tasir (spec 2.2):
    /// ordu bir koridoru ezberlemisse o koridora yonelik secenekler daha
    /// sik cikar. Draft'i siradan bir kart seciminden ayiran sey budur.
    /// </summary>
    public struct DraftOption
    {
        public DraftOptionKind Kind;

        /// <summary>FreeTower ve Discount icin anlamli.</summary>
        public TowerKind Tower;

        /// <summary>Passive icin anlamli.</summary>
        public PassiveKind Passive;

        /// <summary>FreeTower icin hedef koridor; digerlerinde -1.</summary>
        public int Corridor;

        /// <summary>Besin miktari, indirim yuzdesi veya pasif yuzdesi.</summary>
        public int Amount;

        /// <summary>Ayni secenegin iki kez sunulmasini engelleyen kimlik.</summary>
        public string Id
        {
            get { return Kind + ":" + Tower + ":" + Passive + ":" + Corridor; }
        }
    }
}
