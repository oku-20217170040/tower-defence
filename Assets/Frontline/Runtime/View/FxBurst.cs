using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Tek karelik gorsel patlama: namlu cakmasi ve carpma efekti.
    /// TAMAMEN KOZMETIKTIR.
    ///
    /// Animasyon KOD ile yapilir - sprite tek karedir. Kare kare sheet
    /// yerine olcek + donme + sonme secildi: mobilde bellek ve import
    /// maliyeti dusuk, sure ve buyukluk tek yerden ayarlanabiliyor ve
    /// ayni sprite hem cakma hem carpma olarak kullanilabiliyor.
    ///
    /// Iki egri var ve ikisi ayri isler:
    ///   * namlu cakmasi  -> ANINDA buyuk baslar, kucullerek soner
    ///     (patlama namludan disari itilir gibi durur)
    ///   * carpma         -> kucuk baslar, hizla buyuyup soner
    ///     (enerji disari yayilir)
    ///
    /// Havuzlanir: kalabalik dalgada saniyede onlarca efekt doguyor.
    /// </summary>
    public sealed class FxBurst : MonoBehaviour
    {
        private static readonly Stack<FxBurst> pool = new Stack<FxBurst>();
        private static Transform root;

        private SpriteRenderer spriteRenderer;
        private Color startColor;
        private float startSize;
        private float endSize;
        private float spinDegrees;
        private float startAngle;
        private float life;
        private float age;

        private static FxBurst Rent()
        {
            if (pool.Count > 0)
            {
                FxBurst reused = pool.Pop();
                reused.gameObject.SetActive(true);
                return reused;
            }

            if (root == null) root = new GameObject("FxBursts").transform;

            var go = new GameObject("FxBurst");
            go.transform.SetParent(root, false);

            var burst = go.AddComponent<FxBurst>();
            burst.spriteRenderer = go.AddComponent<SpriteRenderer>();
            return burst;
        }

        /// <summary>
        /// Namlu cakmasi: verilen yone bakar, buyuk baslar ve kucullerek
        /// soner. Sprite'in burnu yerel -Y'de uretildigi icin aci
        /// TowerTurret ile AYNI kuralla hesaplanir (+90 derece).
        /// </summary>
        public static void Muzzle(Sprite sprite, Vector2 position, Vector2 direction,
                                  float size, Color color, float seconds = 0.07f,
                                  int sortingOrder = 33)
        {
            Spawn(sprite, position, AngleFor(direction), size, size * 0.65f,
                  color, seconds, 0f, sortingOrder);
        }

        /// <summary>
        /// Carpma / patlama: kucuk baslar, buyuyerek soner ve hafifce
        /// doner. Donme her patlamayi tekilllestirir - ayni sprite ust
        /// uste ciktiginda goz "kopyala yapistir" demesin diye baslangic
        /// acisi da rastgeledir.
        /// </summary>
        public static void Impact(Sprite sprite, Vector2 position, float size,
                                  Color color, float seconds = 0.22f,
                                  int sortingOrder = 34)
        {
            Spawn(sprite, position, Random.Range(0f, 360f), size * 0.45f, size,
                  color, seconds, Random.Range(-70f, 70f), sortingOrder);
        }

        private static void Spawn(Sprite sprite, Vector2 position, float angle,
                                  float from, float to, Color color, float seconds,
                                  float spin, int sortingOrder)
        {
            if (seconds <= 0f || to <= 0f) return;

            FxBurst burst = Rent();

            // Sprite yoksa daireye duselir - asset'ler parca parca
            // gelebilmeli, oyun hicbir anda bozulmamali (ArtLibrary kurali).
            burst.spriteRenderer.sprite = sprite != null ? sprite : PrimitiveSprites.Circle();
            burst.spriteRenderer.color = color;
            burst.spriteRenderer.sortingOrder = sortingOrder;

            burst.transform.position = position;
            burst.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            burst.transform.localScale = new Vector3(from, from, 1f);

            burst.startColor = color;
            burst.startSize = from;
            burst.endSize = to;
            burst.startAngle = angle;
            burst.spinDegrees = spin;
            burst.life = seconds;
            burst.age = 0f;
        }

        private static float AngleFor(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.000001f) return 0f;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);

            float size = Mathf.Lerp(startSize, endSize, t);
            transform.localScale = new Vector3(size, size, 1f);

            if (spinDegrees != 0f)
                transform.rotation = Quaternion.Euler(0f, 0f, startAngle + spinDegrees * t);

            // Sonme egrisi karesel: patlama once parlak durur, sonuna
            // dogru hizla kaybolur. Dogrusal sonme "yavas yavas silinen
            // bir resim" gibi duruyordu.
            Color c = startColor;
            c.a = startColor.a * (1f - t) * (1f - t);
            spriteRenderer.color = c;

            if (t >= 1f)
            {
                gameObject.SetActive(false);
                pool.Push(this);
            }
        }
    }
}
