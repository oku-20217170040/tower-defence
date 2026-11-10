using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Bir sprite'in zemine dusen golgesi: ayni silueti koyu ve yari saydam
    /// olarak, DUNYA uzayinda sabit bir kayma ile cizer.
    ///
    /// Neden ayri bilesen: taret doner. Golge taretin cocugu olsaydi kayma
    /// da onunla donerdi ve isik her kulede baska yonden geliyormus gibi
    /// gorunurdu. Burada golge kaynagin donusunu izler ama kaymasi hep
    /// ayni yone bakar (isik sol ustten - tools/blender/library/fl_lib.py
    /// ile ayni yon).
    ///
    /// Kaynagin sprite'i degisirse (seviye yukseltmesi) golge de degisir.
    /// </summary>
    public sealed class SpriteShadow : MonoBehaviour
    {
        public SpriteRenderer source;

        [Tooltip("Dunya uzayinda kayma (hucre orani ile carpilmis). " +
                 "Isik sol ustten geldigi icin golge sag alta duser.")]
        public Vector2 worldOffset = new Vector2(0.06f, -0.06f);

        public Color color = new Color(0f, 0f, 0f, 0.28f);

        private SpriteRenderer own;

        public static SpriteShadow Attach(SpriteRenderer source, Transform parent,
                                          Vector2 worldOffset, int sortingOrder)
        {
            var go = new GameObject(source.gameObject.name + "Shadow");
            go.transform.SetParent(parent, false);

            var shadow = go.AddComponent<SpriteShadow>();
            shadow.source = source;
            shadow.worldOffset = worldOffset;
            shadow.own = go.AddComponent<SpriteRenderer>();
            shadow.own.sortingOrder = sortingOrder;
            shadow.Sync();
            return shadow;
        }

        private void LateUpdate()
        {
            Sync();
        }

        private void Sync()
        {
            if (source == null || own == null) return;

            own.sprite = source.sprite;
            own.color = color;
            own.enabled = source.enabled;

            Transform s = source.transform;
            transform.SetPositionAndRotation(
                s.position + new Vector3(worldOffset.x, worldOffset.y, 0f), s.rotation);
        }
    }
}
