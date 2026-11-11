using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontline.Core;
using Frontline.Runtime.Audio;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kulenin silahi. Uc ates bicimi var (11 Kasim 2026 kararlari,
    /// docs/specs/2026-11-11-kule-yetenek-ve-yukseltme-kararlari.md):
    ///
    ///   Stream (Makineli Tufek)  en yakin N hedefe SUREKLI hasar. Sv3'te N=2.
    ///   Shell  (Topcu)           aralikli mermi; dustugu yerde patlar,
    ///                            yaricaptaki herkese vurur. Sv3 yanan krater.
    ///   Line   (Keskin Nisanci)  aralikli atis; hat uzerindeki herkesi deler.
    ///                            Sv3 zirh delme + oldurunce ikinci atis.
    ///
    /// Sayilar buraya GOMULMEZ: hepsi Core'daki TowerStats tablosundan gelir
    /// (ApplyStats). Radar etkisi (menzil / ates hizi eki) SetAura ile gelir.
    /// </summary>
    public sealed class TowerWeapon : MonoBehaviour
    {
        public enum FireMode { Stream, Shell, Line }

        [Tooltip("Makineli Tufek: hedef basina saniyelik hasar (TowerStats'tan gelir).")]
        public float damagePerSecond = 4f;

        [Tooltip("ETKIN menzil (hucre): taban menzil + radar eki. Salt okunur gibi dusun.")]
        public float range = 4f;

        [Tooltip("Makineli Tufek: ayni anda kac hedefe ates eder (sv3 = 2).")]
        public int targets = 1;

        [Header("Topcu mermisi")]
        public float shellDamage;
        public float shellInterval = 1.2f;
        [Tooltip("Patlama yaricapi (hucre).")]
        public float blastRadius = 1.5f;
        public float burnDps;
        public float burnSeconds;

        [Header("Keskin Nisanci atisi")]
        [Tooltip("Hat uzerindeki her birlige verilen hasar.")]
        public float beamShotDamage = 3f;
        public float beamShotInterval = 2f;

        [Tooltip("Hattin yarim KALINLIGI (hucre). Hat incedir.")]
        public float beamHalfWidth = 0.6f;

        [Tooltip("Zirh delme: hattaki en yuksek canli hedefe atis hasarinin bu kati EK hasar.")]
        public float pierceBonus;

        [Tooltip("Atis birini oldururse hemen ikinci atis (zincirlenmez).")]
        public bool followUpOnKill;

        [Tooltip("Ikinci atisin gecikmesi (saniye).")]
        public float followUpDelay = 0.25f;

        [Tooltip("Bir atisin gorsel olarak ne kadar sure gorunecegi.")]
        public float beamFlashSeconds = 0.12f;

        [Tooltip("Atis cizgisinin GORSEL kalinligi (hucre orani). Hasar " +
                 "kalinligindan (beamHalfWidth) AYRIDIR.")]
        public float beamVisualThickness = 0.05f;

        [Header("Ortu Yikma")]
        [Tooltip("Atis hattinin uzerine denk gelen kaya/agaca verilen hasarin " +
                 "orani. 0 yapilirsa kuleler ortuyu hic acmaz.")]
        [Range(0f, 2f)]
        public float sceneryDamageShare = 1f;

        [Header("Atis gorseli (KOZMETIK)")]
        [Tooltip("Makineli Tufek icin iki gorsel atis arasindaki sure. Hasar " +
                 "SUREKLI uygulanir; mermi yalnizca geri bildirimdir.")]
        public float tracerInterval = 0.18f;

        [Tooltip("Mermi ve patlama rengi. Sprite atanmissa ona uygulanan tint.")]
        public Color tracerColor = new Color(0.75f, 0.82f, 0.88f, 0.45f);

        [Tooltip("Namlu cakmasinin ne kadar sure gorunecegi.")]
        public float tracerSeconds = 0.07f;

        [Header("Mermi / patlama gorselleri")]
        public TowerFx fx = TowerFx.For(TowerKind.Ambush);
        public Sprite bulletSprite;
        public Sprite muzzleSprite;
        public Sprite impactSprite;

        [Tooltip("Kule sprite'inin dunya olcegi. Namlu ucu bununla carpilir.")]
        public float visualScale = 1f;

        [Tooltip("Donen ust yapi - atis aninda Recoil() cagrilir. Bos ise atlanir.")]
        public TowerTurret turret;

        public FireMode Mode { get; private set; }

        /// <summary>Radarin verdigi menzil eki (0,2 = +%20). 0 = radar yok.</summary>
        public float RangeBonus { get; private set; }

        /// <summary>Radarin verdigi ates hizi eki.</summary>
        public float FireRateBonus { get; private set; }

        /// <summary>Bir radarin etkisinde mi (gorsel geri bildirim bunu okur).</summary>
        public bool IsBuffed { get { return RangeBonus > 0f || FireRateBonus > 0f; } }

        /// <summary>Etkin menzil (hucre): taban + radar eki.</summary>
        public float EffectiveRange { get { return baseRange * (1f + RangeBonus); } }

        /// <summary>Son atisin yonu; kule govdesi buna doner.</summary>
        public Vector2 AimDirection { get; private set; } = Vector2.down;

        /// <summary>Su an menzilde bir hedef var mi (kule ona doner).</summary>
        public bool HasTarget { get; private set; }

        /// <summary>Her gercek atista (mermi / hat atisi) tetiklenir - testler ve efektler icin.</summary>
        public event System.Action Fired;

        /// <summary>
        /// Namlunun hedefe donuk ucu. Mermi de cakma da BURADAN dogar.
        /// </summary>
        public Vector2 MuzzlePoint
        {
            get
            {
                return TowerFx.MuzzlePoint(transform.position, AimDirection,
                                           visualScale, fx.muzzleOffset);
            }
        }

        private Battlefield world;
        private TowerKind kind = TowerKind.Ambush;
        private float baseRange = 4f;
        private float damageMul = 1f;
        private float rangeMul = 1f;
        private float tracerTimer;
        private float shotTimer;
        private bool followUpUsed;
        private int manualTargetCell = -1;

        private readonly List<UnitHealth> picked = new List<UnitHealth>(4);

        private float WorldRange
        {
            get { return EffectiveRange * world.Space.CellSize; }
        }

        private float RateScale { get { return 1f + FireRateBonus; } }

        /// <summary>
        /// Kuleyi turune gore kurar. damageMul / rangeMul draft odullerinden
        /// gelen kalici carpanlardir; seviye degisse de korunurlar.
        /// </summary>
        public void Configure(Battlefield world, TowerKind kind, float damageMul, float rangeMul)
        {
            this.world = world;
            this.kind = kind;
            this.damageMul = damageMul;
            this.rangeMul = rangeMul;

            Mode = kind == TowerKind.Beam ? FireMode.Line
                 : (kind == TowerKind.Battery ? FireMode.Shell : FireMode.Stream);
        }

        /// <summary>Seviyenin sayilarini uygular (kurulumda ve her yukseltmede).</summary>
        public void ApplyStats(TowerLevelStats stats)
        {
            baseRange = stats.Range * rangeMul;
            targets = Mathf.Max(1, stats.Targets);
            damagePerSecond = stats.Dps * damageMul;

            if (Mode == FireMode.Shell)
            {
                shellDamage = stats.ShotDamage * damageMul;
                shellInterval = stats.ShotInterval;
                blastRadius = stats.BlastRadius;
                burnDps = stats.BurnDps * damageMul;
                burnSeconds = stats.BurnSeconds;
            }
            else if (Mode == FireMode.Line)
            {
                beamShotDamage = stats.ShotDamage * damageMul;
                beamShotInterval = stats.ShotInterval;
                pierceBonus = stats.PierceBonus;
                followUpOnKill = stats.FollowUpOnKill;
            }

            range = EffectiveRange;
        }

        /// <summary>Battlefield.RebuildAuras cagirir: radar etkisini yazar.</summary>
        public void SetAura(float rangeBonus, float fireRateBonus)
        {
            RangeBonus = Mathf.Max(0f, rangeBonus);
            FireRateBonus = Mathf.Max(0f, fireRateBonus);
            range = EffectiveRange;
        }

        // ------------------------------------------------------ manuel hedef

        /// <summary>Su an manuel bir ortu hedefine kilitli mi?</summary>
        public bool HasManualTarget { get { return manualTargetCell >= 0; } }

        /// <summary>
        /// Bu hucreyi menzildeyse manuel hedef yapar (agac/kaya). Manuel
        /// hedef aktifken kule BIRLIKLERE ATES ETMEZ - "odak tamamen oraya
        /// donsun" (2 Eylul 2026 kullanici talimati).
        /// </summary>
        public bool TrySetManualTarget(int cell)
        {
            if (world == null || world.Scenery == null) return false;
            if (!world.Scenery.IsStanding(cell)) return false;

            Vector2 cellWorld = world.Space.CellToWorld(cell);
            float sqr = ((Vector2)transform.position - cellWorld).sqrMagnitude;
            if (sqr > WorldRange * WorldRange) return false;

            manualTargetCell = cell;
            return true;
        }

        public void ClearManualTarget()
        {
            manualTargetCell = -1;
        }

        // ------------------------------------------------------------ dongu

        private void Update()
        {
            if (world == null) return;

            if (HasManualTarget)
            {
                UpdateManualTarget();
                return;
            }

            if (Mode == FireMode.Line) UpdateLine();
            else if (Mode == FireMode.Shell) UpdateShell();
            else UpdateStream();
        }

        private void Aim(UnitHealth target)
        {
            HasTarget = target != null;
            if (!HasTarget) return;

            Vector2 delta = (Vector2)target.transform.position - (Vector2)transform.position;
            if (delta.sqrMagnitude > 0.000001f) AimDirection = delta.normalized;
        }

        // --------------------------------------------------- Makineli Tufek

        private void UpdateStream()
        {
            if (damagePerSecond <= 0f) return;

            FindNearestTargets(picked, targets);
            Aim(picked.Count > 0 ? picked[0] : null);
            if (!HasTarget) return;

            float damage = damagePerSecond * RateScale * Time.deltaTime;
            UnitHealth first = picked[0];
            UnitHealth second = picked.Count > 1 ? picked[1] : null;

            ChipSceneryOnLine(first.transform.position, damage * sceneryDamageShare);

            // Hasar birligi yok edebilir; gorsel icin once konumlari kullan.
            EmitStreamTracer(first, second);

            for (int i = 0; i < picked.Count; i++)
                if (picked[i] != null) picked[i].TakeDamage(damage);
        }

        private void EmitStreamTracer(UnitHealth first, UnitHealth second)
        {
            tracerTimer -= Time.deltaTime * RateScale;
            if (tracerTimer > 0f) return;
            tracerTimer += tracerInterval > 0f ? tracerInterval : 0.2f;

            PlayFireFx();

            float cell = world.Space.CellSize;
            Vector2 muzzle = MuzzlePoint;

            // Hedefin KENDISI verilir: mermi ucus boyunca ona kilitlenir.
            Projectile.Fire(bulletSprite, muzzle, first,
                            fx.bulletScale * cell, fx.bulletSpeed * cell, tracerColor,
                            impactSprite, fx.impactScale * cell, tracerColor);

            // Cift namlu: ikinci hedefe de AYNI anda mermi gider - oyuncu
            // iki ayri birligin vuruldugunu gormeli.
            if (second != null)
            {
                Projectile.Fire(bulletSprite, muzzle, second,
                                fx.bulletScale * cell, fx.bulletSpeed * cell, tracerColor,
                                impactSprite, fx.impactScale * cell, tracerColor);
            }
        }

        // ------------------------------------------------------------ Topcu

        private void UpdateShell()
        {
            if (shellDamage <= 0f) return;

            UnitHealth target = FindNearestTarget();
            Aim(target);

            shotTimer -= Time.deltaTime * RateScale;
            if (shotTimer > 0f) return;
            if (!HasTarget) { shotTimer = 0f; return; }

            shotTimer += shellInterval > 0f ? shellInterval : 1f;

            ChipSceneryOnLine(target.transform.position, shellDamage * sceneryDamageShare);
            PlayFireFx();

            float cell = world.Space.CellSize;
            Vector2 muzzle = MuzzlePoint;
            float speed = Mathf.Max(0.01f, fx.bulletSpeed * cell);
            float travel = ((Vector2)target.transform.position - muzzle).magnitude / speed;

            // Mermi gorseli (kendi carpma efekti yok: patlamayi Explode cizer).
            Projectile.Fire(bulletSprite, muzzle, target,
                            fx.bulletScale * cell, fx.bulletSpeed * cell, tracerColor,
                            null, 0f, tracerColor);

            StartCoroutine(ShellFlight(target, target.transform.position, travel));
        }

        private IEnumerator ShellFlight(UnitHealth target, Vector2 lastKnown, float travel)
        {
            if (travel > 0f) yield return new WaitForSeconds(travel);

            // Hedef yoldayken olduyse mermi SON bilinen yere duser: alan
            // hasarinin anlami bu - yanindakiler yine yanar.
            Vector2 center = (target != null && target.IsAlive)
                ? (Vector2)target.transform.position
                : lastKnown;

            Explode(center);
        }

        /// <summary>Patlama: yaricaptaki herkese hasar + (sv3) yanan krater.</summary>
        public void Explode(Vector2 center)
        {
            if (world == null) return;

            float radius = blastRadius * world.Space.CellSize;
            DamageInRadius(world, center, radius, shellDamage);

            FxBurst.Impact(impactSprite, center, radius * 2f, Color.white, 0.22f);

            if (burnDps > 0f && burnSeconds > 0f)
                BurnZone.Spawn(world, center, radius * BurnRadiusShare, burnDps, burnSeconds, impactSprite);
        }

        /// <summary>Yanan krater patlamanin tamami degil, cekirdegidir.</summary>
        public const float BurnRadiusShare = 0.6f;

        /// <summary>Bir daire icindeki butun canli birliklere ayni hasari verir.</summary>
        public static int DamageInRadius(Battlefield world, Vector2 center, float radius, float damage)
        {
            var units = world.ActiveUnits;
            if (units == null || units.Count == 0 || damage <= 0f) return 0;

            float radiusSqr = radius * radius;
            int hits = 0;

            // Hasar birligi yok edebilecegi icin geriye dogru gezilir.
            for (int i = units.Count - 1; i >= 0; i--)
            {
                UnitAgent unit = units[i];
                if (unit == null) continue;

                UnitHealth health = unit.Health;
                if (health == null || !health.IsAlive) continue;

                if (((Vector2)unit.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    health.TakeDamage(damage);
                    hits++;
                }
            }

            return hits;
        }

        // --------------------------------------------------- Keskin Nisanci

        private void UpdateLine()
        {
            // Nisan her karede guncellenir: govde hedefi TAKIP eder.
            Aim(FindNearestTarget());

            shotTimer -= Time.deltaTime * RateScale;
            if (shotTimer > 0f) return;
            if (!HasTarget) { shotTimer = 0f; return; }

            shotTimer += beamShotInterval > 0f ? beamShotInterval : 1f;

            PlaySound();
            int killed = FireLine(AimDirection);

            // Oldurunce ikinci atis: zincirlenmez (ikinci atisin oldurmesi
            // ucuncuyu tetiklemez), yoksa zayif bir surude kule hic durmazdi.
            if (followUpOnKill && killed > 0 && !followUpUsed)
            {
                followUpUsed = true;
                shotTimer = Mathf.Min(shotTimer, followUpDelay);
            }
            else
            {
                followUpUsed = false;
            }
        }

        /// <summary>
        /// Verilen dogrultuda menzil boyunca onune cikan HERKESI deler.
        /// Olen birlik sayisini doner.
        /// </summary>
        private int FireLine(Vector2 direction)
        {
            if (Fired != null) Fired();

            var units = world.ActiveUnits;
            Vector2 origin = MuzzlePoint;
            float cell = world.Space.CellSize;
            float halfWidth = beamHalfWidth * cell;
            float reach = WorldRange;

            // 1. gecis: hattakileri topla, en yuksek canliyi bul (zirh delme).
            picked.Clear();
            UnitHealth toughest = null;

            if (units != null)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    UnitAgent unit = units[i];
                    if (unit == null) continue;

                    UnitHealth health = unit.Health;
                    if (health == null || !health.IsAlive) continue;

                    Vector2 v = (Vector2)unit.transform.position - origin;

                    // Namlu dogrultusundaki mesafe: arkada kalan vurulmaz.
                    float along = Vector2.Dot(v, direction);
                    if (along < 0f || along > reach) continue;

                    // Hatta dik uzaklik: hat ne kadar inceyse o kadar secici.
                    float perp = Mathf.Abs(v.x * direction.y - v.y * direction.x);
                    if (perp > halfWidth) continue;

                    picked.Add(health);
                    if (toughest == null || health.CurrentHp > toughest.CurrentHp) toughest = health;
                }
            }

            // 2. gecis: hasar. Carpma efekti VURULAN HER BIRLIGIN uzerinde.
            int killed = 0;
            for (int i = 0; i < picked.Count; i++)
            {
                UnitHealth health = picked[i];
                bool pierced = pierceBonus > 0f && health == toughest;
                float damage = beamShotDamage * (pierced ? 1f + pierceBonus : 1f);

                FxBurst.ImpactOn(impactSprite, health.transform,
                                 fx.impactScale * cell * (pierced ? 1.7f : 1f),
                                 pierced ? PierceColor : BeamFlash.color);

                health.TakeDamage(damage);
                if (!health.IsAlive) killed++;
            }

            if (turret != null) turret.Recoil();

            FxBurst.Muzzle(muzzleSprite, origin, direction,
                           fx.muzzleScale * cell, BeamFlash.color, beamFlashSeconds);

            BeamFlash.Spawn(origin, direction, cell * beamVisualThickness,
                            beamFlashSeconds, picked.Count, reach);

            return killed;
        }

        /// <summary>Zirh delen isabetin rengi: sicak beyaz-sari, normal isabetten ayrilsin.</summary>
        private static readonly Color PierceColor = new Color(1f, 0.92f, 0.55f, 1f);

        // --------------------------------------------------------- yardimci

        /// <summary>
        /// Manuel kilitli ortuye SUREKLI hasar verir. Tur farki yok: her kule
        /// kendi saniyelik hasar esdegeriyle tek hedefe vurur.
        /// </summary>
        private void UpdateManualTarget()
        {
            if (world.Scenery == null || !world.Scenery.IsStanding(manualTargetCell))
            {
                ClearManualTarget();
                HasTarget = false;
                return;
            }

            Vector2 cellWorld = world.Space.CellToWorld(manualTargetCell);
            Vector2 delta = cellWorld - (Vector2)transform.position;
            if (delta.sqrMagnitude > 0.000001f) AimDirection = delta.normalized;
            HasTarget = true;

            float damage = DpsEquivalent() * RateScale * Time.deltaTime;
            bool destroyed = world.DamageScenery(manualTargetCell, damage);

            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0f)
            {
                tracerTimer += tracerInterval > 0f ? tracerInterval : 0.2f;
                PlayFireFx();
                FxBurst.Impact(impactSprite, cellWorld,
                               fx.impactScale * world.Space.CellSize, tracerColor, tracerSeconds);
            }

            if (destroyed) ClearManualTarget();
        }

        /// <summary>Kulenin tek hedefe saniyelik hasar esdegeri.</summary>
        public float DpsEquivalent()
        {
            if (Mode == FireMode.Line) return beamShotDamage / Mathf.Max(0.01f, beamShotInterval);
            if (Mode == FireMode.Shell) return shellDamage / Mathf.Max(0.01f, shellInterval);
            return damagePerSecond;
        }

        private void PlaySound()
        {
            string clip;
            switch (kind)
            {
                case TowerKind.Ambush: clip = AudioManager.TowerFireMg; break;
                // Topcu icin ayri bir ses klibi henuz yok; keskin nisanci
                // klibi (tek, agir patlama) en yakin karsilik.
                case TowerKind.Battery: clip = AudioManager.TowerFireSniper; break;
                case TowerKind.Beam: clip = AudioManager.TowerFireSniper; break;
                default: clip = null; break;
            }

            if (clip != null) AudioManager.Instance.PlaySfx(clip);
        }

        /// <summary>Ses + geri tepme + namlu cakmasi: her gorsel atista.</summary>
        private void PlayFireFx()
        {
            if (Fired != null) Fired();
            PlaySound();
            if (turret != null) turret.Recoil();

            FxBurst.Muzzle(muzzleSprite, MuzzlePoint, AimDirection,
                           fx.muzzleScale * world.Space.CellSize, tracerColor, tracerSeconds);
        }

        /// <summary>
        /// Namlu ile hedef arasindaki hatta ilk denk gelen AYAKTA ortuye hasar
        /// verir. Kule ortuye nisan almaz; ortu yolun uzerinde oldugu icin
        /// atisi yer. Bir atis bir engeli deler.
        /// </summary>
        private void ChipSceneryOnLine(Vector2 targetPosition, float damage)
        {
            if (damage <= 0f || world == null || world.Scenery == null) return;

            Vector2 from = MuzzlePoint;
            Vector2 delta = targetPosition - from;
            float distance = delta.magnitude;
            if (distance < 0.0001f) return;

            Vector2 step = delta / distance;
            float cellSize = world.Space.CellSize;
            float walked = cellSize * 0.5f;
            int lastCell = -1;

            while (walked < distance)
            {
                Vector2 point = from + step * walked;
                walked += cellSize * 0.5f;

                int cell;
                if (!world.Space.WorldToCell(point, out cell)) continue;
                if (cell == lastCell) continue;
                lastCell = cell;

                if (!world.Scenery.IsStanding(cell)) continue;

                world.DamageScenery(cell, damage);
                return;
            }
        }

        private UnitHealth FindNearestTarget()
        {
            FindNearestTargets(picked, 1);
            return picked.Count > 0 ? picked[0] : null;
        }

        /// <summary>
        /// Menzildeki en yakin N canli birlik, yakindan uzaga. N kucuk (1-2)
        /// oldugu icin her secim icin listeyi bir kez taramak yeterli.
        /// </summary>
        private void FindNearestTargets(List<UnitHealth> into, int count)
        {
            into.Clear();

            var units = world.ActiveUnits;
            if (units == null || units.Count == 0) return;

            Vector2 origin = transform.position;
            float rangeSqr = WorldRange * WorldRange;

            for (int n = 0; n < count; n++)
            {
                float bestSqr = rangeSqr;
                UnitHealth best = null;

                for (int i = 0; i < units.Count; i++)
                {
                    UnitAgent unit = units[i];
                    if (unit == null) continue;

                    UnitHealth health = unit.Health;
                    if (health == null || !health.IsAlive || into.Contains(health)) continue;

                    float sqr = ((Vector2)unit.transform.position - origin).sqrMagnitude;
                    if (sqr <= bestSqr)
                    {
                        bestSqr = sqr;
                        best = health;
                    }
                }

                if (best == null) return;
                into.Add(best);
            }
        }
    }
}
