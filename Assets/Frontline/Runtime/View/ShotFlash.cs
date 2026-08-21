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
    /// Isinin delici carpmasi. Ayri bir tip degil, ShotFlash uzerine
    /// kurulu bir tarif: uzun, kalin ve kisa omurlu tek cizgi.
    /// </summary>
    public static class BeamFlash
    {
        /// <summary>Isin rengi. Kule sprite'indaki mercekle ayni aile.</summary>
        public static Color color = new Color(0.45f, 0.82f, 1f, 0.9f);

        public static void Spawn(Vector2 origin, Vector2 direction, float range,
                                 float thickness, float seconds, int hits)
        {
            // Vurdugu birlik sayisi arttikca isin kalinlasir: delip gectigi
            // kalabaligi oyuncu bir bakista gorsun.
            float boost = 1f + Mathf.Min(hits, 6) * 0.12f;

            ShotFlash.Line(origin, direction, range, thickness * boost,
                           color, seconds, sortingOrder: 32);
        }
    }
}
