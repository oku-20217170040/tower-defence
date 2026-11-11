using System;
using System.Collections.Generic;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Tek bir birlik. Usya dogru yurur ve gectigi hucrelere kesif izi birakir.
    ///
    /// Rota SPAWN ANINDA BIR KEZ hesaplanir, her karede degil. Bu bir performans
    /// tercihi degil, tasarim geregidir: ordu kararini verip yola cikmalidir ki
    /// oyuncunun yaniltmasi anlamli olsun. Her an rotasini duzelten bir surunun
    /// kandirilmasi mumkun olmaz.
    /// </summary>
    public sealed class UnitAgent : MonoBehaviour
    {
        [Tooltip("Saniyede kac hucre ilerlesin.")]
        public float cellsPerSecond = 2.5f;

        [Tooltip("Gecerken her hucreye birakilan kesif izi.")]
        public float depositPerCell = 0.5f;

        [Tooltip("Usya ulasinca tum rotaya eklenen pekistirme. Basarili rota guclenir.")]
        public float reinforceOnSuccess = 1.5f;

        [Tooltip("Olurse o ana kadarki rotadan silinen miktar. Olumle biten rota sonumlenir.")]
        public float weakenOnDeath = 2.5f;

        [Tooltip("Birlik gittigi yone donerken saniyede kac derece doner. " +
                 "Aninda donmek tirtikli gorunur; koridor donuslerinde " +
                 "yumusak bir yalpalama istiyoruz.")]
        public float turnDegreesPerSecond = 720f;

        /// <summary>
        /// Bu birligin turu ve carpanlari. Varsayilan piyadedir; spawner
        /// dalga bilesimine gore degistirir.
        /// </summary>
        public UnitProfile profile = UnitProfile.Infantry();

        private Battlefield world;
        private HomeBaseHealth homeBase;
        private readonly List<int> path = new List<int>();
        private readonly List<int> visited = new List<int>();
        private int pathIndex;
        private bool active;

        /// <summary>Usya ulasti ya da oldu; her iki durumda da spawner haberdar olur.</summary>
        public event Action<UnitAgent> Finished;

        public bool ReachedBase { get; private set; }

        /// <summary>Bu birligin cani. Kuleler hedef secerken kullanir.</summary>
        public UnitHealth Health { get; private set; }

        /// <summary>Usya ulasirsa bu kadar can eksiltir.</summary>
        public int damageToBase = 1;

        /// <summary>
        /// Ucan birlik zemindeki alanlardan (yavaslatma, kirilganlik)
        /// ETKILENMEZ - 11 Kasim 2026 karari: ucani digerlerinden ayiran,
        /// "yavaslatmayla cozulemeyen" tehdit olmasidir.
        /// </summary>
        public bool IsFlyer { get { return profile.Type == UnitType.Flyer; } }

        /// <summary>Su an durdugu hucredeki gecis suresi carpani (1 = normal hiz).</summary>
        public float SlowFactorHere
        {
            get
            {
                if (!active || world == null || world.Slow == null || IsFlyer) return SlowField.None;
                if (pathIndex < 0 || pathIndex >= path.Count) return SlowField.None;
                return Mathf.Max(SlowField.None, world.Slow.Get(path[pathIndex]));
            }
        }

        /// <summary>Su an aldigi hasarin carpani (kirilganlik alani; 1 = normal).</summary>
        public float DamageTakenMultiplier
        {
            get
            {
                if (!active || world == null || world.Fragile == null || IsFlyer) return 1f;
                if (pathIndex < 0 || pathIndex >= path.Count) return 1f;
                return Mathf.Max(1f, world.Fragile.Get(path[pathIndex]));
            }
        }

        /// <summary>
        /// Bocegi baslatir. Rota bulunamazsa false doner ve birlik hemen biter -
        /// gecerli haritada bu olmamali, olursa dogrulamada bir sorun var demektir.
        /// </summary>
        public bool Initialize(Battlefield world, int spawnCell, HomeBaseHealth homeBase)
        {
            this.world = world;
            this.homeBase = homeBase;

            visited.Clear();
            pathIndex = 0;
            ReachedBase = false;

            // UCAN: yol bulucu HIC calistirilmaz. Ucan araziyi tanimaz,
            // dolayisiyla maliyet modelinin (iz, yem, tehdit, kesif sapmasi)
            // hicbiri onun icin tanimli degildir. Ortak maliyet modeline
            // ucan icin ozel bir ayar sokmak yururyen ordunun dengesini
            // kirletirdi; ucan kendi aptal duz cizgisini kullanir.
            if (profile.IgnoresTerrain)
            {
                FlightPath.Build(world.Grid, spawnCell, world.BaseCell, path);
                return BeginRoute(spawnCell);
            }

            // Rota algisi TURE ozeldir: kor birlik ize ve yeme bakmaz, izci
            // daha yuksek sapmayla yola cikar. Bunlar sorgu basina verilir,
            // cunku maliyet modeli butun ordunun ortak nesnesidir.
            world.CostModel.IgnoresRecon = profile.IgnoresRecon;
            world.CostModel.IgnoresDecoy = profile.IgnoresDecoy;
            world.CostModel.ExplorationNoise = profile.ExplorationNoiseOverride > 0f
                ? profile.ExplorationNoiseOverride
                : world.explorationNoise;

            // Her birlik kendi yon tercihiyle yola cikar. Bu olmadan tum birlikler
            // ayni koridoru secer ve ordu hicbir zaman alternatif denemez.
            world.CostModel.BeginQuery(UnityEngine.Random.Range(int.MinValue, int.MaxValue), world.Grid);

            if (!world.Pathfinder.FindPath(spawnCell, world.BaseCell, path))
            {
                Debug.LogWarning("[UnitAgent] " + spawnCell + " hucminefieldden usya rota yok.");
                active = false;
                return false;
            }

            return BeginRoute(spawnCell);
        }

        /// <summary>
        /// Rota hazir olduktan sonraki ortak baslangic. Yururyen ve ucan
        /// birlik burada AYNI kodu kullanir; ayrilan tek sey rotanin nasil
        /// uretildigidir.
        /// </summary>
        private bool BeginRoute(int spawnCell)
        {
            if (path.Count == 0)
            {
                active = false;
                return false;
            }

            transform.position = world.Space.CellToWorld(spawnCell);
            EnterCell(path[0]);

            // Dogum aninda yon ANINDA kurulur; yoksa her birlik yola
            // yanlis bakarak cikip ilk karede donuyor gibi gorunur.
            if (path.Count > 1)
                FaceInstantly((Vector2)world.Space.CellToWorld(path[1]) - (Vector2)transform.position);

            Health = GetComponent<UnitHealth>();
            world.RegisterUnit(this);

            UnitStatusFx.Ensure(this);
            active = true;
            return true;
        }

        private void Update()
        {
            if (!active) return;

            int nextIndex = pathIndex + 1;
            if (nextIndex >= path.Count)
            {
                Arrive();
                return;
            }

            Vector2 target = world.Space.CellToWorld(path[nextIndex]);

            // Gittigi yone bakar. Sprite'in burnu YEREL -Y'dedir
            // (bkz. tools/blender/units.py: kama burnu -Y'de uretiliyor),
            // o yuzden hedef aci yonun acisi + 90 derecedir.
            TurnToward(target - (Vector2)transform.position, Time.deltaTime);

            // Mayin tarlasi alanindaki hucrede gecis suresi uzar. Birlik bunu
            // ALGILAMAZ - rota degismez, yalnizca yavaslar.
            float slow = SlowFactorHere;
            float step = cellsPerSecond * profile.SpeedMultiplier / slow
                         * world.Space.CellSize * Time.deltaTime;

            transform.position = Vector2.MoveTowards(transform.position, target, step);

            if (((Vector2)transform.position - target).sqrMagnitude < 0.0001f)
            {
                pathIndex = nextIndex;
                EnterCell(path[pathIndex]);
            }
        }

        /// <summary>Yon acisi: sprite'in yerel -Y ekseni verilen yone bakar.</summary>
        private static float AngleFor(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        }

        private void FaceInstantly(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.000001f) return;

            transform.rotation = Quaternion.Euler(0f, 0f, AngleFor(direction));
        }

        private void TurnToward(Vector2 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < 0.000001f) return;

            Quaternion wanted = Quaternion.Euler(0f, 0f, AngleFor(direction));
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, wanted, turnDegreesPerSecond * deltaTime);
        }

        private void EnterCell(int cell)
        {
            visited.Add(cell);

            // Ucan iz BIRAKMAZ. Sebep mekanik degil, tasarimsal degil -
            // ikisi de: ucan duvarlarin uzerinden geciyor, iz biraksaydi
            // YURUNMEYEN hucrelere kesif izi yazardi ve yururyen ordu o
            // ize gore rota kurmaya calisirdi. Ogrenme katmani cop olurdu.
            if (profile.LeavesNoRecon) return;

            world.Recon.Deposit(cell, depositPerCell * profile.DepositMultiplier);
        }

        private void Arrive()
        {
            active = false;
            ReachedBase = true;

            // Basarili rota guclenir - ordun ogrenmesi budur.
            // Ucan bu kanali da kullanmaz: ordu ucandan HICBIR SEY ogrenmez.
            if (!profile.LeavesNoRecon)
                world.Recon.DepositTrail(visited, reinforceOnSuccess * profile.ReinforceMultiplier);

            // Kralice usya varirsa ordu ogrendigi her seyi unutur.
            // Tehdit alani DEGIL kesif izi silinir: ordunun hafizasi odur
            // (tehdit her dalga kulelerden yeniden kurulur).
            if (profile.ClearsRecon) world.Recon.Clear();
            if (profile.ClearsThreat && world.Threat != null) world.Threat.Clear();

            // Varan birlik de listeden cikmali. Cikmazsa kuleler yok edilmis
            // hedeflere ates etmeye devam eder.
            world.UnregisterUnit(this);

            // Sizinti oyunun en pahali olayi; gorunmeden gecmemeli.
            // Birligin bir kopyasi kapidan iceri suzulur (usun ALTINDA
            // cizilir). Oyun mantigi beklemez - sayac hemen isler.
            LeaveEntryVisual();

            Finished?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Usse giren birligin gorsel kopyasini birakir. Us gorunumu
        /// sahnede yoksa sessizce atlanir - gorsel katman oyunun
        /// calismasi icin zorunlu olmamali.
        /// </summary>
        private void LeaveEntryVisual()
        {
            var sprite = GetComponent<SpriteRenderer>();
            if (sprite == null) return;

            var view = FindFirstObjectByType<HomeBaseView>();
            if (view == null) return;

            BaseEntry.Spawn(sprite, world.Space.CellToWorld(world.BaseCell),
                            view.sortingOrder);
        }

        /// <summary>
        /// Birlik olur. Rotasi zayiflar; boylece olumle biten yol sonumlenir.
        /// Plan 2'de kule hasari yok, bu elle tetiklenir.
        /// </summary>
        public void Kill()
        {
            if (!active) return;
            active = false;

            // Ucanin olumu de orduya bir sey ogretmez; duz cizgisi
            // yururyen birliklerin koridoruyla ilgisiz oldugu icin oradan
            // silinen iz yanlis koridoru sonumlendirirdi.
            if (!profile.LeavesNoRecon)
                world.Recon.WeakenTrail(visited, weakenOnDeath * profile.WeakenMultiplier);
            world.UnregisterUnit(this);

            // Olum ACIK renkte soner.
            DeathPuff.Spawn(transform.position, new Color(1f, 0.95f, 0.8f, 0.9f),
                            world.Space.CellSize * 0.7f, 25);

            Finished?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
