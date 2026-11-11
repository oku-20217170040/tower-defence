using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Yanan krater (Topcu sv3): patlama yerinde birkac saniye kalan ates
    /// alani. Icinden gecen her birlik saniyelik hasar alir.
    ///
    /// Zaman Bozulmasi ile eslesir: yavaslayan birlik ateste daha uzun kalir.
    /// </summary>
    public sealed class BurnZone : MonoBehaviour
    {
        private Battlefield world;
        private float radius;
        private float dps;
        private float life;
        private float total;
        private SpriteRenderer flame;

        private static readonly Color FlameColor = new Color(1f, 0.55f, 0.18f, 0.55f);

        public static BurnZone Spawn(Battlefield world, Vector2 center, float radius,
                                     float dps, float seconds, Sprite sprite, int sortingOrder = 13)
        {
            var go = new GameObject("BurnZone");
            go.transform.position = center;

            var zone = go.AddComponent<BurnZone>();
            zone.world = world;
            zone.radius = radius;
            zone.dps = dps;
            zone.life = seconds;
            zone.total = Mathf.Max(0.01f, seconds);

            zone.flame = go.AddComponent<SpriteRenderer>();
            zone.flame.sprite = sprite != null ? sprite : PrimitiveSprites.Circle();
            zone.flame.sortingOrder = sortingOrder;
            zone.flame.color = FlameColor;

            float size = zone.flame.sprite.bounds.size.x;
            float scale = size > 0.0001f ? radius * 2f / size : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            return zone;
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f || world == null)
            {
                Destroy(gameObject);
                return;
            }

            TowerWeapon.DamageInRadius(world, transform.position, radius, dps * Time.deltaTime);

            // Alev titrer ve sonuna dogru soner: oyuncu alanin ne zaman
            // bitecegini gorsun.
            float fade = Mathf.Clamp01(life / total * 2f);
            float flicker = 0.78f + 0.22f * Mathf.Sin(Time.time * 19f);
            Color c = FlameColor;
            c.a *= fade * flicker;
            flame.color = c;
            transform.Rotate(0f, 0f, 35f * Time.deltaTime);
        }
    }
}
