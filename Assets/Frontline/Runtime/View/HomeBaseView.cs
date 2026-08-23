using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Usun gorseli ve VURULMA animasyonu.
    ///
    /// Neden gerekli (20 Agu dersi): "usya varan birlik can eksiltir"
    /// zinciri 104 test yesilken sessizce kopuktu, cunku ekranda usun
    /// vurulduguna dair HICBIR sey yoktu. Sayi arayuzde kucuk bir
    /// yazidir; oyuncu ona bakmiyorsa sizinti hic olmamis gibi gecer.
    /// Bu bilesen o geri bildirimi ekrana koyar - hem oyun hissi hem de
    /// runtime zincirinin canli oldugunun gozle dogrulanmasi.
    ///
    /// Uc kanaldan birden anlatir:
    ///   1. SARSINTI - kisa, sert, hemen soner (carpmanin kendisi)
    ///   2. KIRMIZI CAKMA - tek karelik degil, 0,25 sn sonen (fark edilir)
    ///   3. KALICI RENK - kalan cana gore us yesilden kirmiziya kayar
    ///      (oyuncu HUD'a bakmadan durumu bilir)
    ///
    /// Sprite atanmamissa hicbir sey cizilmez ve oyun aynen calisir -
    /// ArtLibrary kurali burada da gecerli.
    /// </summary>
    public sealed class HomeBaseView : MonoBehaviour
    {
        public Battlefield world;
        public HomeBaseHealth homeBase;

        [Tooltip("Us sprite'i. Bos ise ArtLibrary'den alinir.")]
        public Sprite sprite;

        [Tooltip("Usun hucre cinsinden genisligi. 0 ise ArtLibrary'deki deger.")]
        public float scale;

        [Tooltip("Cizim sirasi. BIRLIKLERIN USTUNDE olmali (birlik sirasi 20): " +
                 "usse yaklasan birlik binanin ARKASINA gecmeli ki iceri " +
                 "giriyor gibi gorunsun. 8 iken birlikler kalenin catisinda " +
                 "yuruyup usten kayboluyordu.")]
        public int sortingOrder = 24;

        [Tooltip("Us hucresinden YUKARI kaydirma (hucre). Us hucresi en alt " +
                 "sirada; sprite 2 hucreden genis oldugu icin ortalanirsa " +
                 "yarisi haritanin disinda kalir.")]
        public float verticalOffset = 0.6f;

        [Tooltip("Vurulunca sarsinti genligi (hucre orani).")]
        public float shakeAmplitude = 0.12f;

        [Tooltip("Sarsintinin suresi. Kisa olmali; uzun sarsinti oyunu " +
                 "sarhos gosteriyor.")]
        public float shakeSeconds = 0.18f;

        [Tooltip("Vurulma cakmasinin suresi.")]
        public float flashSeconds = 0.25f;

        private SpriteRenderer spriteRenderer;
        private Sprite impactSprite;
        private Vector2 anchor;
        private Color baseColor = Color.white;
        private float flashTimer;
        private float shakeTimer;
        private int lastHp = int.MinValue;
        private float worldScale = 1f;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (homeBase == null) homeBase = FindFirstObjectByType<HomeBaseHealth>();

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            ArtLibrary art = ArtLibrary.Instance;
            if (art != null)
            {
                if (sprite == null) sprite = art.baseSprite;
                if (scale <= 0f) scale = art.baseScale;
                if (art.baseTint.a > 0f) baseColor = art.baseTint;

                // Usun vurulma patlamasi bataryanin carpma efektidir:
                // ikisi de "agir bir sey carpti" der, ikinci bir asset
                // uretmek gereksiz.
                ArtLibrary.FxArt fxArt;
                if (art.TryGetFx(TowerKind.Battery, out fxArt)) impactSprite = fxArt.impact;
            }

            if (scale <= 0f) scale = 2.2f;

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = baseColor;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.enabled = sprite != null;

            if (world != null)
            {
                world.WorldRebuilt += Place;
                if (world.Grid != null) Place();
            }

            if (homeBase != null)
            {
                homeBase.Changed += OnHealthChanged;
                homeBase.Emptied += OnEmptied;
                lastHp = homeBase.CurrentHp;
            }
        }

        private void OnDestroy()
        {
            if (world != null) world.WorldRebuilt -= Place;

            if (homeBase != null)
            {
                homeBase.Changed -= OnHealthChanged;
                homeBase.Emptied -= OnEmptied;
            }
        }

        /// <summary>Us, haritanin us hucresine oturur.</summary>
        private void Place()
        {
            if (world == null || world.Grid == null) return;

            anchor = world.Space.CellToWorld(world.BaseCell)
                     + Vector2.up * (verticalOffset * world.Space.CellSize);
            transform.position = anchor;

            worldScale = world.Space.CellSize * scale;
            transform.localScale = new Vector3(worldScale, worldScale, 1f);
        }

        private void OnHealthChanged(int hp)
        {
            bool damaged = lastHp > int.MinValue && hp < lastHp;
            lastHp = hp;

            if (!damaged) return;

            flashTimer = flashSeconds;
            shakeTimer = shakeSeconds;

            // Patlama usun KENARINDA dogar, tam ortasinda degil: birlik
            // cepheden carpar ve efekt cekirdegi ortmemeli.
            Vector2 hitPoint = anchor + new Vector2(
                Random.Range(-0.35f, 0.35f), Random.Range(0.15f, 0.45f)) * worldScale;

            FxBurst.Impact(impactSprite, hitPoint, worldScale * 0.45f,
                           new Color(1f, 0.55f, 0.25f, 0.95f), 0.28f, sortingOrder + 6);
        }

        /// <summary>Us dustu: buyuk, yavas, tek bir patlama.</summary>
        private void OnEmptied()
        {
            FxBurst.Impact(impactSprite, anchor, worldScale * 1.4f,
                           new Color(1f, 0.35f, 0.15f, 1f), 0.9f, sortingOrder + 6);
        }

        private void Update()
        {
            if (spriteRenderer == null || sprite == null) return;
            if (world == null || world.Grid == null) return;

            // Kalan cana gore kalici renk: tam candayken kendi rengi,
            // bitmeye yakinken kirmizi. Oyuncu HUD'a bakmadan bilir.
            float healthy = 1f;
            if (homeBase != null && homeBase.maxHp > 0)
                healthy = Mathf.Clamp01(homeBase.CurrentHp / (float)homeBase.maxHp);

            Color color = Color.Lerp(new Color(0.85f, 0.25f, 0.20f), baseColor,
                                     Mathf.Sqrt(healthy));

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(flashTimer / flashSeconds);
                color = Color.Lerp(color, new Color(1f, 0.75f, 0.6f), k);
            }

            spriteRenderer.color = color;

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(shakeTimer / shakeSeconds);
                float amp = shakeAmplitude * worldScale * k;

                transform.position = anchor + new Vector2(
                    Random.Range(-amp, amp), Random.Range(-amp, amp));
            }
            else
            {
                transform.position = anchor;
            }
        }
    }
}
