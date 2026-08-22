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
    /// Mermi hedefe KILITLENIR: ucus boyunca hedefin o anki konumuna
    /// yonelir, boylece patlama her zaman birligin UZERINDE olur.
    ///
    /// Once dogumda yakalanan SABIT bir noktaya uculuyordu; hizli birlikte
    /// patlama gorunur sekilde ARKADA kaliyordu (22 Agu kullanici geri
    /// bildirimi). Hasar zaten TowerWeapon icinde surekli uygulaniyor -
    /// yani gorselin hedefi kacirmasinin bilgi degeri yoktu, yalnizca
    /// oyuncuya yanlis sey soyluyordu.
    ///
    /// Hedef ucus sirasinda olur ya da havuza donerse mermi son bilinen
    /// konumuna gidip orada patlar: atisin bittigi yine gorunur.
    ///
    /// Havuzlanir; kalabalik dalgada ekranda onlarca mermi olur.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private static readonly Stack<Projectile> pool = new Stack<Projectile>();
        private static Transform root;

        private SpriteRenderer spriteRenderer;
        private Vector2 to;
        private float speed;       // dunya birimi / saniye
        private UnitHealth target;

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
        public static void Fire(Sprite sprite, Vector2 origin, UnitHealth target,
                                float size, float speed, Color color,
                                Sprite impact, float impactScale, Color impactTint,
                                int sortingOrder = 31)
        {
            if (speed <= 0f || target == null) return;

            Vector2 destination = target.transform.position;

            // Cok kisa mesafede ucus animasyonu okunmaz; dogrudan carpma.
            if (Vector2.Distance(origin, destination) < 0.01f)
            {
                FxBurst.Impact(impact, destination, impactScale, impactTint);
                return;
            }

            Projectile shot = Rent();
            shot.spriteRenderer.sprite = sprite != null ? sprite : PrimitiveSprites.Circle();
            shot.spriteRenderer.color = color;
            shot.spriteRenderer.sortingOrder = sortingOrder;

            shot.to = destination;
            shot.speed = speed;
            shot.target = target;

            shot.impactSprite = impact;
            shot.impactSize = impactScale;
            shot.impactColor = impactTint;

            Transform t = shot.transform;
            t.position = origin;
            t.localScale = new Vector3(size, size, 1f);
            shot.FaceDestination();
        }

        /// <summary>
        /// Burnu gidilen yone cevirir. Hedef kacarken mermi de donmeli;
        /// yalnizca dogumda dondurulseydi kilitlenen mermi yan yan
        /// ucuyormus gibi gorunurdu. Sprite'in burnu yerel -Y'de
        /// uretiliyor (bkz. tools/blender/fx.py), yani kule ve
        /// birliklerle AYNI kural: aci + 90 derece.
        /// </summary>
        private void FaceDestination()
        {
            Vector2 direction = to - (Vector2)transform.position;
            if (direction.sqrMagnitude < 0.000001f) return;

            transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
        }

        private void Update()
        {
            // KILITLENME: hedef hala ayakta ve sahnedeyse varis noktasi
            // her karede guncellenir. Havuza donmus bir birligin
            // transform'u baska bir birlige ait olabilecegi icin
            // aktiflik de kontrol edilir - yoksa mermi yanlis birligi
            // kovalar.
            if (target != null && target.IsAlive && target.gameObject.activeInHierarchy)
                to = target.transform.position;
            else
                target = null;

            Vector2 position = transform.position;
            Vector2 delta = to - position;
            float step = speed * Time.deltaTime;

            if (delta.magnitude <= step)
            {
                // Vardi: carpma patlamasi HEDEFIN uzerinde dogar ve
                // sonene kadar onunla birlikte yol alir.
                if (target != null)
                    FxBurst.ImpactOn(impactSprite, target.transform, impactSize, impactColor);
                else
                    FxBurst.Impact(impactSprite, to, impactSize, impactColor);

                target = null;
                gameObject.SetActive(false);
                pool.Push(this);
                return;
            }

            transform.position = position + delta.normalized * step;
            FaceDestination();
        }
    }
}
