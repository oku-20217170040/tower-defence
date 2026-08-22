using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Ucan mermi gorseli. TAMAMEN KOZMETIKTIR.
    ///
    /// Onemli: mermi hasar TASIMAZ. Hasar TowerWeapon icinde surekli
    /// uygulanir ve olculmus denge sayilariyla kilitlidir. Mermi ucus
    /// suresi kadar gec vursaydi butun kalibrasyon (206,7 oldurme, 18,3
    /// sizinti) kayardi. Bu yuzden mermi yalnizca "kule ates etti"
    /// bilgisini gozle tasir; vardigi yerde carpma efektini dogurur.
    ///
    /// Hedefin O ANKI konumu dogumda yakalanir, mermi hedefi takip
    /// ETMEZ: takip eden mermi olen birlikte kaybolur ve atisin bittigi
    /// gorunmez. Kacirilan atis da bilgidir.
    ///
    /// Havuzlanir; kalabalik dalgada ekranda onlarca mermi olur.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private static readonly Stack<Projectile> pool = new Stack<Projectile>();
        private static Transform root;

        private SpriteRenderer spriteRenderer;
        private Vector2 from;
        private Vector2 to;
        private float travel;      // toplam ucus suresi
        private float age;

        private Sprite impactSprite;
        private float impactSize;
        private Color impactColor;

        private static Projectile Rent()
        {
            if (pool.Count > 0)
            {
                Projectile reused = pool.Pop();
                reused.gameObject.SetActive(true);
                return reused;
            }

            if (root == null) root = new GameObject("Projectiles").transform;

            var go = new GameObject("Projectile");
            go.transform.SetParent(root, false);

            var shot = go.AddComponent<Projectile>();
            shot.spriteRenderer = go.AddComponent<SpriteRenderer>();
            return shot;
        }

        /// <summary>
        /// Namlu ucundan hedefe bir mermi yollar. Hiz hucre/saniye
        /// cinsindendir; dunya birimine cell boyutuyla cevrilir.
        /// </summary>
        public static void Fire(Sprite sprite, Vector2 origin, Vector2 target,
                                float size, float speed, Color color,
                                Sprite impact, float impactScale, Color impactTint,
                                int sortingOrder = 31)
        {
            float distance = Vector2.Distance(origin, target);
            if (speed <= 0f) return;

            // Cok kisa mesafede ucus animasyonu okunmaz; dogrudan carpma.
            if (distance < 0.01f)
            {
                FxBurst.Impact(impact, target, impactScale, impactTint);
                return;
            }

            Projectile shot = Rent();
            shot.spriteRenderer.sprite = sprite != null ? sprite : PrimitiveSprites.Circle();
            shot.spriteRenderer.color = color;
            shot.spriteRenderer.sortingOrder = sortingOrder;

            shot.from = origin;
            shot.to = target;
            shot.travel = distance / speed;
            shot.age = 0f;

            shot.impactSprite = impact;
            shot.impactSize = impactScale;
            shot.impactColor = impactTint;

            Vector2 direction = (target - origin).normalized;

            Transform t = shot.transform;
            t.position = origin;
            t.localScale = new Vector3(size, size, 1f);

            // Sprite'in burnu yerel -Y'de uretiliyor (bkz. tools/blender/fx.py),
            // yani kule ve birliklerle AYNI kural: aci + 90 derece.
            t.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = travel > 0f ? age / travel : 1f;

            if (t >= 1f)
            {
                // Vardi: carpma patlamasi burada dogar. Efektin yeri
                // birligin uzerindir, kulenin degil - oyuncu vurusun
                // NEREDE oldugunu gormeli.
                FxBurst.Impact(impactSprite, to, impactSize, impactColor);

                gameObject.SetActive(false);
                pool.Push(this);
                return;
            }

            transform.position = Vector2.Lerp(from, to, t);
        }
    }
}
