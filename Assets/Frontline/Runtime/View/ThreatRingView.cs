using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Batarya kulenin yaydigi titresim halkasi.
    ///
    /// Bu dekorasyon DEGILDIR: halkanin yaricapi tehdit yaricapinin ta kendisidir.
    /// Oyuncu batarya ile pusu mevzisiyi isimlerinden degil ekrandan ayirt
    /// etmelidir (spec 9.1). Pusu mevziside bu bilesen hic bulunmaz - halkasizlik
    /// "ordu beni goremiyor" demenin gorsel yoludur.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ThreatRingView : MonoBehaviour
    {
        [Tooltip("Bir halkanin disari acilma suresi (saniye).")]
        public float period = 1.6f;

        [Tooltip("Halkanin en parlak oldugu andaki saydamsizligi.")]
        [Range(0f, 1f)]
        public float peakAlpha = 0.35f;

        public Color ringColor = new Color(1f, 0.35f, 0.25f);

        private SpriteRenderer spriteRenderer;
        private float worldRadius = 1f;
        private float timer;

        public void Bind(float radiusInCells, float cellSize, int sortingOrder)
        {
            worldRadius = radiusInCells * cellSize;

            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = PrimitiveSprites.Circle();
            spriteRenderer.sortingOrder = sortingOrder;
        }

        private void Update()
        {
            if (spriteRenderer == null) return;

            timer += Time.deltaTime;
            if (timer > period) timer -= period;

            float t = timer / period;

            // Merkezden disari acilir ve acildikca soner.
            float diameter = Mathf.Lerp(0f, worldRadius * 2f, t);
            transform.localScale = new Vector3(diameter, diameter, 1f);

            Color c = ringColor;
            c.a = peakAlpha * (1f - t);
            spriteRenderer.color = c;
        }
    }
}
