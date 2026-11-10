using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Secili kulenin menzilini gosteren SABIT, ANIMASYONSUZ disk.
    ///
    /// ThreatRingView'dan bilerek basitlestirilmis: o titreyen bir tehdit
    /// uyarisiydi, bu ise oyuncunun kendi istegiyle actigi bir bilgi
    /// katmani ("bu kuleyi sectim, alani bu"). Titremek yanlis mesaj
    /// verirdi - tehlike degil, secim gosteriyoruz.
    ///
    /// Renk NOTRDUR: hicbir kule turunun rengini taklit etmez, cunku bu
    /// katman "hangi kule" degil "ne kadar alan" sorusuna cevap verir.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RangeRingView : MonoBehaviour
    {
        [Tooltip("Gercek disk gorseli (fx_range_ring, Blender'dan). Bos " +
                 "ise PrimitiveSprites.Circle() placeholder'ina duser.")]
        public Sprite ringSprite;

        [Tooltip("Diskin rengi. Notr, soluk mavi-beyaz - hicbir kule turunun " +
                 "rengiyle karismasin diye kasitli olarak notr.")]
        public Color ringColor = new Color(0.75f, 0.85f, 1f);

        [Tooltip("Diskin saydamsizligi. Silik olmali: alani gosterir ama " +
                 "altindaki sahneyi bogmaz.")]
        [Range(0f, 1f)]
        public float alpha = 0.20f;

        private SpriteRenderer spriteRenderer;

        /// <summary>
        /// Diski verilen yaricapa (hucre cinsinden) gore olceklendirip
        /// baglar. ThreatRingView.Bind ile AYNI imza - cagiran kod
        /// tanidik kalsin diye.
        /// </summary>
        public void Bind(float radiusInCells, float cellSize, int sortingOrder)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            float worldDiameter = radiusInCells * cellSize * 2f;
            transform.localScale = new Vector3(worldDiameter, worldDiameter, 1f);

            spriteRenderer.sprite = ringSprite != null ? ringSprite : PrimitiveSprites.Circle();
            spriteRenderer.sortingOrder = sortingOrder;

            Color c = ringColor;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        public void Show()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = true;
        }

        public void Hide()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
        }
    }
}
