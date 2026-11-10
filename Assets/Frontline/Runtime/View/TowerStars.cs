using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kule seviyesini gosteren 1-3 yildiz rozeti, kule sprite'inin
    /// USTUNE bindirilir (referans oyundaki gibi - 3 Eylul 2026 gece
    /// planı, FAZ 1 en oncelikli madde). ArtLibrary'nin genel kuralina
    /// uyar: Blender yildiz sprite'i atanmissa o kullanilir, atanmamissa
    /// PrimitiveSprites.Star() ile uretilen daireye/yildiza duser - oyun
    /// hicbir asamada bozulmaz.
    /// </summary>
    public sealed class TowerStars : MonoBehaviour
    {
        private readonly SpriteRenderer[] slots = new SpriteRenderer[Emplacement.MaxLevel];

        [Tooltip("Yildizlar arasi bosluk (dunya birimi).")]
        public float spacing = 0.16f;

        [Tooltip("Tek yildizin boyutu (dunya birimi).")]
        public float starSize = 0.14f;

        [Tooltip("Rozet sarisi. Referans oyunda yildizlar altin sarisidir.")]
        public Color starColor = new Color(1f, 0.82f, 0.15f, 1f);

        public void Build(Sprite starSprite)
        {
            Sprite sprite = starSprite != null ? starSprite : PrimitiveSprites.Star();

            for (int i = 0; i < slots.Length; i++)
            {
                var go = new GameObject("Star" + (i + 1));
                go.transform.SetParent(transform, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = starColor;
                sr.sortingOrder = 40;

                float totalWidth = (slots.Length - 1) * spacing;
                float x = -totalWidth * 0.5f + i * spacing;
                go.transform.localPosition = new Vector3(x, 0f, 0f);
                go.transform.localScale = new Vector3(starSize, starSize, 1f);

                slots[i] = sr;
            }
        }

        /// <summary>Gorunur yildiz sayisini gunceller (1-3).</summary>
        public void SetLevel(int level)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].enabled = i < level;
            }
        }
    }
}
