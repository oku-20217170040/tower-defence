using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kulenin silahi. Mermi yok; hasar menzildeki en yakin birlige surekli
    /// uygulanir. Mermi gorselleri Plan 5'in isi.
    ///
    /// Tasarimin can alici noktasi burada gorunur: batarya kulenin hasari
    /// yuksektir ama onunden zamanla kimse gecmez, dolayisiyla GERCEK hasari
    /// dusuktur. Pusu mevzisinin hasari dusuktur ama surekli is gorur.
    /// Oyuncunun kesfetmesi gereken sey bu fark (spec 3.2).
    /// </summary>
    public sealed class TowerWeapon : MonoBehaviour
    {
        [Tooltip("Saniyede verilen hasar.")]
        public float damagePerSecond = 4f;

        [Tooltip("Menzil (hucre). Kulenin tehdit yaricapiyla ayni olmak zorunda degil.")]
        public float range = 4f;

        [Tooltip("Acikken menzildeki TUM birliklere ayni anda vurur (batarya kule). " +
                 "Olcum: alan hasari yaklasik 4 kat deger katiyor - 8 birlik gecerken " +
                 "tek hedefli kule 2, alan hasarli 8 olduruyor.")]
        public bool splash;

        [Tooltip("Acikken kule bir ISINDIR: belirli araliklarla ates eder, " +
                 "namlusunu en yakin birlige dogrultur ve o dogrultuda onune " +
                 "cikan HERKESI yarip gecer.")]
        public bool beam;

        [Tooltip("Isinin bir atista verdigi hasar. 3 olcumle secildi: nisan " +
                 "aldigi icin iskalamiyor, o yuzden dusuk tutuldu.")]
        public float beamShotDamage = 3f;

        [Tooltip("Isinin atislari arasindaki sure. ASIL denge dugmesi budur: " +
                 "0,5 sn'de isin tek basina oyunu cozuyor, 2 sn'de tek basina " +
                 "ise yaramiyor ama karisimda deger uretiyor.")]
        public float beamShotInterval = 2f;

        [Tooltip("Isin hattinin yarim KALINLIGI (hucre). Hat incedir.")]
        public float beamHalfWidth = 0.6f;

        [Tooltip("Bir atisin gorsel olarak ne kadar sure gorunecegi.")]
        public float beamFlashSeconds = 0.12f;

        [Tooltip("Isin cizgisinin GORSEL kalinligi (hucre orani). Hasar " +
                 "kalinligindan (beamHalfWidth) AYRIDIR: biri denge sayisi, " +
                 "digeri sanat karari. Once ikisi ayni sayidan turetiliyordu " +
                 "ve isin ekrani kaplayan bir bant gibi duruyordu.")]
        public float beamVisualThickness = 0.05f;

        [Header("Atis izi (KOZMETIK)")]
        [Tooltip("Iki gorsel iz arasindaki sure. Hasar SUREKLI uygulanir; " +
                 "iz yalnizca oyuncuya geri bildirimdir ve dengeye dokunmaz.")]
        public float tracerInterval = 0.18f;

        [Tooltip("Iz rengi. Pusu SOLUK ve ince, batarya PARLAK ve kalin - " +
                 "gorsel hiyerarsi mekanikle ayni kalmali (spec 9.1).")]
        public Color tracerColor = new Color(0.75f, 0.82f, 0.88f, 0.45f);

        [Tooltip("Iz kalinligi (hucre orani).")]
        public float tracerThickness = 0.05f;

        [Tooltip("Izin ne kadar sure gorunecegi.")]
        public float tracerSeconds = 0.07f;

        private float tracerTimer;

        private float beamTimer;

        /// <summary>Son atisin yonu; kule govdesi buna doner.</summary>
        public Vector2 AimDirection { get; private set; } = Vector2.down;

        /// <summary>Su an menzilde bir hedef var mi (kule ona doner).</summary>
        public bool HasTarget { get; private set; }

        private Battlefield world;
        private float worldRange;

        public void Bind(Battlefield world, float rangeInCells, float dps, bool splash)
        {
            this.world = world;
            this.splash = splash;
            range = rangeInCells;
            damagePerSecond = dps;
            worldRange = rangeInCells * world.Space.CellSize;
        }

        /// <summary>Isin baglama: menzil, atis basina hasar ve atis araligi.</summary>
        public void BindBeam(Battlefield world, float rangeInCells,
                             float shotDamage, float shotInterval, float halfWidth)
        {
            this.world = world;
            beam = true;
            range = rangeInCells;
            worldRange = rangeInCells * world.Space.CellSize;
            beamHalfWidth = halfWidth;
            beamShotDamage = shotDamage;
            beamShotInterval = shotInterval;
            beamTimer = shotInterval;
        }

        private void Update()
        {
            if (world == null) return;

            if (beam)
            {
                UpdateBeam();
                return;
            }

            if (damagePerSecond <= 0f) return;

            float damage = damagePerSecond * Time.deltaTime;

            // Namlu her karede en yakin hedefi takip eder; kule govdesi
            // buna doner (TowerTurret). Alan hasarli batarya da doner:
            // "kime bakiyor" bilgisi oyuncuya nereye vurdugunu soyler.
            UnitHealth target = FindNearestTarget();
            HasTarget = target != null;

            if (HasTarget)
            {
                Vector2 delta = (Vector2)target.transform.position - (Vector2)transform.position;
                if (delta.sqrMagnitude > 0.000001f) AimDirection = delta.normalized;
            }

            if (splash) DamageEveryoneInRange(damage);
            else if (HasTarget) target.TakeDamage(damage);

            EmitTracer(target);
        }

        /// <summary>
        /// Gorsel iz. Hasarla ILGISI YOKTUR: hasar surekli uygulanir, iz
        /// yalnizca sabit araliklarla cizilir. Ayrilmasinin sebebi, izin
        /// sikligini dengeye dokunmadan ayarlayabilmek.
        /// </summary>
        private void EmitTracer(UnitHealth target)
        {
            if (!HasTarget || target == null) return;

            tracerTimer -= Time.deltaTime;
            if (tracerTimer > 0f) return;

            tracerTimer += tracerInterval > 0f ? tracerInterval : 0.2f;

            Vector2 origin = transform.position;
            float length = Vector2.Distance(origin, target.transform.position);

            ShotFlash.Line(origin, AimDirection, length,
                           tracerThickness * world.Space.CellSize,
                           tracerColor, tracerSeconds);
        }

        /// <summary>
        /// Isin: sayac dolunca kulenin durdugu SATIR boyunca ates eder ve o
        /// satirdaki her birlige ayni hasari verir - kac birlik oldugundan
        /// bagimsiz.
        ///
        /// Neden surekli hasar degil (20 Agu 2026 olcumu): surekli hasarli
        /// isin her ayarda ya olu ya baskin cikiyordu. Bedava draft kulesi
        /// tamamen kapatilip her strateji cokerken 7 surekli isin oyunu tek
        /// basina kazaniyordu, cunku bir koridoru bastan sona kaplayan sey
        /// spec 3.5'i ("korunmayan koridor her zaman bulunur") siler.
        /// Periyodik atis bunu cozer: hat 1 hucre kalinligindadir, hizli
        /// birlik iki atis arasinda karsiya gecebilir, yavaslatilmis
        /// kalabalik tam yer. Bu yuzden isin mayin tarlasiyla eslesir.
        /// </summary>
        private void UpdateBeam()
        {
            // Nisan her karede guncellenir: kule govdesi hedefi TAKIP eder,
            // yalnizca ates aninda donmez. Ates etmeyen kule cansiz durur.
            UnitHealth nearest = FindNearestTarget();
            HasTarget = nearest != null;
            if (HasTarget)
            {
                Vector2 delta = (Vector2)nearest.transform.position - (Vector2)transform.position;
                if (delta.sqrMagnitude > 0.000001f) AimDirection = delta.normalized;
            }

            beamTimer -= Time.deltaTime;
            if (beamTimer > 0f) return;

            beamTimer += beamShotInterval > 0f ? beamShotInterval : 1f;
            if (!HasTarget) return;

            Fire(AimDirection);
        }

        /// <summary>
        /// Verilen dogrultuda onune cikan HERKESI yarip gecer - kac birlik
        /// oldugundan bagimsiz, hepsi ayni hasari alir.
        /// </summary>
        private void Fire(Vector2 direction)
        {
            var units = world.ActiveUnits;
            if (units == null || units.Count == 0) return;

            Vector2 origin = transform.position;
            float halfWidth = beamHalfWidth * world.Space.CellSize;
            int hits = 0;

            for (int i = units.Count - 1; i >= 0; i--)
            {
                UnitAgent unit = units[i];
                if (unit == null) continue;

                UnitHealth health = unit.Health;
                if (health == null || !health.IsAlive) continue;

                Vector2 v = (Vector2)unit.transform.position - origin;

                // Namlu dogrultusundaki mesafe: arkada kalan vurulmaz.
                float along = Vector2.Dot(v, direction);
                if (along < 0f || along > worldRange) continue;

                // Hatta dik uzaklik: hat ne kadar inceyse o kadar secici.
                float perp = Mathf.Abs(v.x * direction.y - v.y * direction.x);
                if (perp > halfWidth) continue;

                health.TakeDamage(beamShotDamage);
                hits++;
            }

            BeamFlash.Spawn(origin, direction,
                            world.Space.CellSize * beamVisualThickness,
                            beamFlashSeconds, hits);
        }

        /// <summary>
        /// Menzildeki her birlige ayni anda vurur. Batarya kulenin sürüye
        /// karsi degeri buradan gelir - ama ordu ondan kacmayi ogrendigi
        /// icin onunden gecen sayisi zamanla azalir (spec 3.2).
        /// </summary>
        private void DamageEveryoneInRange(float damage)
        {
            var units = world.ActiveUnits;
            if (units == null || units.Count == 0) return;

            Vector2 origin = transform.position;
            float rangeSqr = worldRange * worldRange;

            // Hasar birligi yok edebilecegi icin geriye dogru gezilir.
            for (int i = units.Count - 1; i >= 0; i--)
            {
                UnitAgent unit = units[i];
                if (unit == null) continue;

                UnitHealth health = unit.Health;
                if (health == null || !health.IsAlive) continue;

                if (((Vector2)unit.transform.position - origin).sqrMagnitude <= rangeSqr)
                    health.TakeDamage(damage);
            }
        }

        /// <summary>
        /// Menzildeki en yakin canli birlik. Sahnedeki tum birlikleri taramak
        /// yerine dunyanin tuttugu canli listesi kullanilir.
        /// </summary>
        private UnitHealth FindNearestTarget()
        {
            var units = world.ActiveUnits;
            if (units == null || units.Count == 0) return null;

            Vector2 origin = transform.position;
            float bestSqr = worldRange * worldRange;
            UnitHealth best = null;

            for (int i = 0; i < units.Count; i++)
            {
                UnitAgent unit = units[i];
                if (unit == null) continue;

                UnitHealth health = unit.Health;
                if (health == null || !health.IsAlive) continue;

                float sqr = ((Vector2)unit.transform.position - origin).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = health;
                }
            }

            return best;
        }
    }
}
