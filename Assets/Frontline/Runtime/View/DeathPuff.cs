using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Tek seferlik sonme efekti. Solarak buyur ve kendini yok eder.
    ///
    /// Gerekli olmasinin sebebi: birlik yok oldugunda oyuncunun onun OLDUGUNU mu
    /// yoksa usya mi ULASTIGINI ayirt etmesi gerekir. Ikisi de "birlik ekrandan
    /// kayboldu" diye gorunurse tuzagin ise yarayip yaramadigi okunamaz.
    /// Iki olay farkli renkte sonme uretir.
    /// </summary>
    public sealed class DeathPuff : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color startColor;
        private float startScale;
        private float endScale;
        private float duration;
        private float timer;

        /// <summary>Belirtilen konumda tek seferlik sonme uretir.</summary>
        public static void Spawn(Vector2 position, Color color, float size, int sortingOrder)
        {
            var go = new GameObject("DeathPuff");
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprites.Circle();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var puff = go.AddComponent<DeathPuff>();
            puff.spriteRenderer = renderer;
            puff.startColor = color;
            puff.startScale = size;
            puff.endScale = size * 2.2f;
            puff.duration = 0.35f;

            go.transform.localScale = new Vector3(size, size, 1f);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            float scale = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            Color c = startColor;
            c.a = startColor.a * (1f - t);
            spriteRenderer.color = c;

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
