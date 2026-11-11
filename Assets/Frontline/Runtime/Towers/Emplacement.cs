using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Sahnedeki bir kule. Plan 2'de kuleler ATES ETMEZ - yalnizca ordunin
    /// rota kararini etkilerler. Hasar, menzil ve hedefleme Plan 3'un isidir.
    /// Buradaki amac tek bir soruyu cevaplayabilmek: oyuncu suruyu
    /// yonlendirebiliyor mu?
    /// </summary>
    public sealed class Emplacement : MonoBehaviour
    {
        public TowerKind kind = TowerKind.Battery;

        [Tooltip("Etki yaricapi (hucre).")]
        public float radius = 4f;

        [Tooltip("Batarya kulenin tehdit gucu. 4 olcumle secildi: doymus bir izi " +
                 "5-6 dalgada kirar. Daha yuksegi suruyu aninda kacirir ve ogrenmeyi " +
                 "anlamsiz kilar, daha dusugu hic etki etmez.")]
        public float threatStrength = 4f;

        [Tooltip("Mayin tarlasi kulesinin gecis suresi carpani. 2 olcumle secildi ve " +
                 "esik keskin: 1,5'te mayin tarlasi olu mekanik (6 kule butcesinde " +
                 "kazanma %20), 3'te sizintiyi sifira indirip baskin strateji " +
                 "oluyor. 2'de saf pusu mevzisiyle basa bas - yani gercek bir secim.")]
        public float slowFactor = 2f;

        [Tooltip("Zaman Bozulmasi sv3: alandaki dusmanin aldigi hasar carpani. 1 = yok.")]
        public float fragility = 1f;

        [Tooltip("Radar: etkiledigi kulelere menzil eki (0,2 = +%20).")]
        public float rangeBonus;

        [Tooltip("Radar sv3: etkiledigi kulelere ates hizi eki.")]
        public float fireRateBonus;

        private Battlefield world;

        /// <summary>Kulenin durdugu izgara hucresi.</summary>
        public int Cell { get; private set; }

        /// <summary>
        /// Gorsel/guc kademesi, 1-3. 3 Eylul 2026 eklendi (kullanici
        /// talimati: "yukselt butonu artik gorsel/stat kademesini
        /// ilerletsin"). Referans oyunda oldugu gibi 3 sabit seviye -
        /// sonsuz yukseltme yok, govde ailesi degismez, sadece detay/
        /// parlaklik/hasar artar (bkz. tools/blender/generate_tower_variety_set.py).
        /// </summary>
        public int Level { get; private set; } = 1;

        public const int MaxLevel = 3;

        /// <summary>
        /// Bu kule icin harcanan TOPLAM besin (kurulum + her yukseltme).
        /// SellValue() bunun bir orani. TowerPlacer.Place/TryUpgrade
        /// gercek harcanan tutari RecordSpend ile bildirir - burada yeni
        /// bir fiyat tablosu YOK, tek kaynak gercekten odenen besindir.
        /// </summary>
        public int SpentTotal { get; private set; }

        /// <summary>
        /// Satista geri odenen oran. 3 Eylul 2026 (dokunmatik Sat butonu):
        /// kullanici talimati "kesin sayi uydurma, mevcut ekonomi
        /// biriminden turet" - en basit haliyle harcananin bir kismi.
        /// %60 secildi: satis bir ceza tasimali (aksi halde kule kur-sat
        /// dongusuyle bedava yeniden konumlandirma olurdu) ama oyuncuyu
        /// hatali bir yerlestirmede tamamen cezalandirmamali.
        /// </summary>
        public const float SellRefundRatio = 0.6f;

        private SpriteRenderer turretRenderer;
        private TowerWeapon weapon;
        private TowerStars stars;

        /// <summary>Bu kulenin silahi; destek kulelerinde null.</summary>
        public TowerWeapon Weapon { get { return weapon; } }

        /// <summary>Su anki seviyenin sayilari (TowerStats).</summary>
        public TowerLevelStats Stats { get { return TowerStats.For(kind, Level); } }

        /// <summary>
        /// Oyuncuya gosterilecek menzil (hucre): silahli kulede ETKIN ates
        /// menzili (radar eki dahil), destek kulesinde etki yaricapi.
        /// </summary>
        public float DisplayRange
        {
            get { return weapon != null ? weapon.EffectiveRange : radius; }
        }

        /// <summary>
        /// Seviyenin sayilarini kuleye yazar ve alanlari (tehdit, yavaslatma,
        /// kirilganlik, radar) yeniden kurar. Kurulumda ve her yukseltmede.
        /// </summary>
        public void ApplyStats()
        {
            TowerLevelStats stats = Stats;

            if (TowerStats.IsWeapon(kind))
            {
                // Tehdit yaricapi da budur (yalnizca Topcu tehdit yazar).
                radius = stats.Range;
            }
            else
            {
                radius = stats.AuraRadius;
                slowFactor = stats.SlowFactor;
                fragility = stats.Fragility;
                rangeBonus = stats.RangeBonus;
                fireRateBonus = stats.FireRateBonus;
            }

            if (weapon != null) weapon.ApplyStats(stats);
            if (world != null) world.RebuildThreat();
        }

        public void Place(Battlefield world, int cell)
        {
            this.world = world;
            Cell = cell;
            transform.position = world.Space.CellToWorld(cell);

            world.RegisterTower(this);
        }

        /// <summary>
        /// TowerPlacer.Place cagirir: yukseltmenin gorseli (turret sprite)
        /// ve istatistigi (weapon hasari) degistirebilmesi icin referanslari
        /// burada birakir. Ikisi de opsiyoneldir (namlusuz destek kulede
        /// weapon null olabilir).
        /// </summary>
        public void BindVisual(SpriteRenderer turretRenderer, TowerWeapon weapon)
        {
            this.turretRenderer = turretRenderer;
            this.weapon = weapon;
            ApplyStats();
        }

        /// <summary>
        /// TowerPlacer.Place cagirir: yildiz rozetini bu kuleye baglar ve
        /// mevcut seviyeyi (yerlesirken hep 1) hemen gosterir.
        /// </summary>
        public void BindStars(TowerStars stars)
        {
            this.stars = stars;
            if (stars != null) stars.SetLevel(Level);
        }

        /// <summary>
        /// Bir kademe yukseltir (3'te durur). Sprite'i ArtLibrary'den
        /// yeni seviyeye gore degistirir ve varsa silahin hasarini
        /// StatMultiplier ile yeniden olceklendirir. ArtLibrary'de bu
        /// seviye icin sprite yoksa gorsel oldugu gibi kalir - eksik
        /// asset oyunu bozmamali (ArtLibrary'nin genel kurali).
        /// </summary>
        public bool Upgrade()
        {
            if (Level >= MaxLevel) return false;

            Level++;

            ApplyStats();

            if (turretRenderer != null)
            {
                ArtLibrary art = ArtLibrary.Instance;
                ArtLibrary.TowerArt entry;
                if (art != null && art.TryGetTower(kind, out entry) &&
                    entry.levelTurrets != null &&
                    entry.levelTurrets.Length >= Level &&
                    entry.levelTurrets[Level - 1] != null)
                {
                    turretRenderer.sprite = entry.levelTurrets[Level - 1];
                }
            }

            if (stars != null) stars.SetLevel(Level);

            return true;
        }

        /// <summary>
        /// Bu kule icin gercekten odenen bir tutari kaydeder (kurulum
        /// fiyati ya da yukseltme maliyeti). Negatif/sifir yoksayilir -
        /// bedava draft odulu kuleleri SpentTotal'e katkida bulunmaz,
        /// yani satista geri odemesi de olmaz (dogru: hic besin
        /// harcanmadi).
        /// </summary>
        public void RecordSpend(int amount)
        {
            if (amount > 0) SpentTotal += amount;
        }

        /// <summary>
        /// Bu kuleyi simdi satarsa geri kazanilacak besin. Saf hesap -
        /// TowerPlacer.TrySell ekonomiye/sahneye uygulayan taraftir.
        /// </summary>
        public int SellValue()
        {
            return Mathf.RoundToInt(SpentTotal * SellRefundRatio);
        }

        /// <summary>
        /// Seviye basina yukseltme maliyeti. Sabit carpan: her seviye bir
        /// oncekinden %50 daha pahali (basit, uydurma bir ekonomi egrisi
        /// DEGIL - kullanici talimati: "yeni rastgele sayi uydurma, en
        /// basit haliyle ekle"). fromLevel = yukseltme oncesi seviye
        /// (1 -> 2 gecisi icin 1 verilir).
        /// </summary>
        public static int UpgradeCost(int baseBuildCost, int fromLevel)
        {
            return Mathf.RoundToInt(baseBuildCost * Mathf.Pow(1.5f, fromLevel));
        }

        /// <summary>
        /// 11 Kasim 2026: ARTIK KULLANILMIYOR - seviye sayilari kule turune
        /// gore TowerStats tablosundan gelir (her kulenin kendi yetenegi var,
        /// duz +%20 hasar degil). Yontem eski testler icin yerinde duruyor.
        ///
        /// Seviye basina hasar/guc carpani: seviye basina +%20. Saf ve
        /// test edilebilir - TowerWeapon'un mevcut damagePerSecond/
        /// beamShotDamage alanlarina carpilir, yeni bir dengeye DOKUNMAZ
        /// (seviye 1 carpani daima 1, yani mevcut ekonomi testleri
        /// degismeden gecer).
        /// </summary>
        public static float StatMultiplier(int level)
        {
            return 1f + 0.2f * (level - 1);
        }

        private void OnDestroy()
        {
            if (world != null)
                world.UnregisterTower(this);
        }
    }
}
