using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Destek kulelerinin (Radar, Zaman Bozulmasi) gorsel geri bildirimi.
    /// Bu kuleler ates etmez; calistiklarini oyuncuya baska yolla soylemeleri
    /// gerekir:
    ///
    ///   * Ust yapi yavasca doner (canak tarar, yayici kollar calisir).
    ///   * Zaman Bozulmasi: etki alani zeminde LOS KIRMIZI, nabiz gibi atan
    ///     bir disk olarak gorunur - yavaslayan dusmana dusen isikla ayni renk.
    ///   * Radar: merkezden etki yaricapina dogru genisleyip sonen soluk bir
    ///     halka ("ping").
    ///
    /// Alan her karede kulenin guncel yaricapindan okunur; yukseltmede
    /// kendiliginden buyur.
    /// </summary>
    public sealed class SupportFeedback : MonoBehaviour
    {
        private Emplacement tower;
        private Transform top;
        private SpriteRenderer field;
        private float cellSize = 1f;
        private float clock;

        private static readonly Color SlowColor = new Color(0.86f, 0.12f, 0.14f, 1f);
        private static readonly Color PingColor = new Color(1f, 0.92f, 0.55f, 1f);

        public static SupportFeedback Attach(GameObject go, Emplacement tower, Transform top,
                                             float cellSize, int groundOrder)
        {
            var fb = go.AddComponent<SupportFeedback>();
            fb.tower = tower;
            fb.top = top;
            fb.cellSize = cellSize;

            var fieldGo = new GameObject("AuraField");
            fieldGo.transform.SetParent(go.transform, false);
            fb.field = fieldGo.AddComponent<SpriteRenderer>();
            fb.field.sprite = PrimitiveSprites.Circle();
            fb.field.sortingOrder = groundOrder;
            fb.field.color = Color.clear;
            return fb;
        }

        private void LateUpdate()
        {
            if (tower == null) return;

            bool radar = tower.kind == TowerKind.Decoy;
            clock += Time.deltaTime;

            if (top != null)
                top.Rotate(0f, 0f, (radar ? 45f : 22f + 9f * tower.Level) * Time.deltaTime);

            if (field == null) return;

            float radius = tower.radius * cellSize;
            float diameter;
            Color c;

            if (radar)
            {
                // Ping: 2,2 saniyede merkezden yaricapa genisler ve soner.
                float k = (clock % 2.2f) / 2.2f;
                diameter = radius * 2f * k;
                c = PingColor;
                c.a = 0.22f * (1f - k);
            }
            else
            {
                diameter = radius * 2f;
                c = SlowColor;
                c.a = 0.13f + 0.045f * Mathf.Sin(clock * 2.4f);
            }

            field.color = c;

            // Ebeveyn kule olceklidir; disk DUNYA boyutunda olmali.
            float sprite = field.sprite.bounds.size.x;
            float parent = transform.lossyScale.x;
            float scale = (sprite > 0.0001f && parent > 0.0001f) ? diameter / (sprite * parent) : 0f;
            field.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
