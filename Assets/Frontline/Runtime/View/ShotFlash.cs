using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Atis izleri ve isin carpmasi. TAMAMEN KOZMETIKTIR: hasar bugunku
    /// gibi TowerWeapon icinde uygulanir, buradaki hicbir sey dengeye
    /// dokunmaz. Denge sayilari olculmus ve testlerle kilitli; gorsel
    /// katman onlari degistiremez.
    ///
    /// GORSEL DIL (spec 9.1, 22 Agu karari): pusu mevzisi SOLUK ve KISA
    /// iz atar, batarya KALIN ve PARLAK. Oyuncu pusunun calistigini gorur
    /// - "asil katil pusu" kesfine ancak boyle varabilir - ama ekranda
    /// gurultu yapan hep batarya olur. Gorsel hiyerarsi mekanikle ayni
    /// kalir.
    ///
    /// Havuzlanir: mobilde kalabalik dalgada onlarca iz ayni anda
    /// dogabilir, her biri icin GameObject yaratmak cop toplayiciyi
    /// bogar.
    /// </summary>
    public sealed class ShotFlash : MonoBehaviour
    {
        private static readonly Stack<ShotFlash> pool = new Stack<ShotFlash>();
        private static Transform root;

        private SpriteRenderer spriteRenderer;
        private float life;
        private float age;
        private Color startColor;

        private static ShotFlash Rent()
        {
            if (pool.Count > 0)
            {
                ShotFlash reused = pool.Pop();
                reused.gameObject.SetActive(true);
                return reused;
            }

            if (root == null)
            {
                var holder = new GameObject("ShotFlashes");
                root = holder.transform;
            }

            var go = new GameObject("ShotFlash");
            go.transform.SetParent(root, false);

            var flash = go.AddComponent<ShotFlash>();
            flash.spriteRenderer = go.AddComponent<SpriteRenderer>();
            flash.spriteRenderer.sprite = PrimitiveSprites.Square();
            return flash;
        }

        /// <summary>
        /// Kaynaktan verilen yonde uzanan bir cizgi cizer.
        /// Uzunluk ve kalinlik DUNYA birimindedir.
        /// </summary>
        public static void Line(Vector2 origin, Vector2 direction, float length,
                                float thickness, Color color, float seconds,
                                int sortingOrder = 30)
        {
            if (length <= 0f || seconds <= 0f) return;

            ShotFlash flash = Rent();
            Transform t = flash.transform;

            // Cizgi ORTASINDAN konumlanir: sprite merkezden olceklenir.
            t.position = origin + direction * (length * 0.5f);
            t.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(length, thickness, 1f);

            flash.spriteRenderer.color = color;
            flash.spriteRenderer.sortingOrder = sortingOrder;
            flash.startColor = color;
            flash.life = seconds;
            flash.age = 0f;
        }

        private void Update()
        {
            age += Time.deltaTime;

            if (age >= life)
            {
                gameObject.SetActive(false);
                pool.Push(this);
                return;
            }

            // Sonerek kaybolur; sabit sure gorunup aniden yok olmak
            // kalabalik dalgada goz tirmaliyordu.
            float k = 1f - age / life;
            Color c = startColor;
            c.a = startColor.a * k;
            spriteRenderer.color = c;
        }
    }

    /// <summary>
    /// Isinin delici carpmasi: INCE ve UCU GORUNMEYEN tek cizgi.
    ///
    /// Iki karar (22 Agu, kullanici):
    ///
    /// 1. INCE. Ilk surum kalinligi hasar yariciapindan turetiyordu
    ///    (1,2 hucre) ve ekrani kaplayan bir bant gibi duruyordu.
    ///    Gorsel kalinlik artik hasar kalinligindan AYRI: hasar hattinin
    ///    ne kadar genis oldugu bir denge sayisi, cizginin ne kadar
    ///    kalin gorundugu bir sanat karari.
    ///
    /// 2. UCU GORUNMEZ. Cizgi kameranin disinda biter, boylece oyuncu
    ///    isinin nerede durdugunu gormez. Bunun icin MENZIL DE
    ///    haritayi asacak sekilde acildi (40 hucre) - yoksa gorsel
    ///    menzil hakkinda yalan soylerdi. Bedeli olculdu: karisimda
    ///    sizinti 15,7 -> 11,9, tek basina hala ise yaramiyor (30,0).
    /// </summary>
    public static class BeamFlash
    {
        /// <summary>Isin rengi. Kule sprite'indaki mercekle ayni aile.</summary>
        public static Color color = new Color(0.55f, 0.88f, 1f, 0.95f);

        public static void Spawn(Vector2 origin, Vector2 direction,
                                 float thickness, float seconds, int hits)
        {
            // Vurdugu birlik sayisi arttikca isin PARLAR - kalinlasmaz.
            // Kalinlasma inceligi bozuyordu; parlaklik ayni bilgiyi
            // cizgiyi kalinlastirmadan verir.
            Color c = color;
            c.a = Mathf.Min(1f, color.a * (0.75f + Mathf.Min(hits, 6) * 0.08f));

            // Duz cizgi ELEKTRIK YAYINA cevrildi (22 Agu, kullanici):
            // ayni kalinlik, ayni "boydan boya kesme", ama yol kirikli ve
            // her karede yeniden kiriliyor. Kalinlik ve uzunluk kurallari
            // degismedi - degisen yalnizca cizginin nasil cizildigi.
            ElectricBeam.Spawn(origin, direction,
                               OffscreenLength(origin, direction),
                               thickness, c, seconds);
        }

        /// <summary>
        /// Kameranin disina cikmayi GARANTI eden uzunluk. Sabit buyuk bir
        /// sayi yerine kameradan hesaplanir; yoksa yakinlastirma degisince
        /// isinin ucu ekranda gorunur hale gelir.
        /// </summary>
        private static float OffscreenLength(Vector2 origin, Vector2 direction)
        {
            Camera cam = Camera.main;
            if (cam == null) return 100f;

            float halfHeight = cam.orthographic
                ? cam.orthographicSize
                : Mathf.Abs(cam.transform.position.z) * Mathf.Tan(
                      cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

            float halfWidth = halfHeight * cam.aspect;
            float halfDiagonal = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);

            // Kule ekranin neresinde olursa olsun karsi kenari asar.
            float fromCenter = Vector2.Distance(origin, (Vector2)cam.transform.position);
            return (halfDiagonal + fromCenter) * 1.2f;
        }
    }
}
