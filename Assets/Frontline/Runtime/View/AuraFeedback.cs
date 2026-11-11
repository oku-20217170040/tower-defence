using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Radar etkisindeki kulenin geri bildirimi (11 Kasim 2026, Serhat):
    /// "ara ara yanip sonen animasyonla radarin etkisinde oldugu hissi".
    ///
    /// Iki isaret birlikte calisir:
    ///   * kulenin kosesinde yanip sonen sari sinyal isigi,
    ///   * ayni anda taretin kisa bir an soguk maviye calmasi.
    /// Radar yoksa ikisi de kapali; kule normal gorunur.
    /// </summary>
    public sealed class AuraFeedback : MonoBehaviour
    {
        [Tooltip("Iki parlama arasindaki sure (saniye).")]
        public float period = 1.3f;

        [Tooltip("Parlamanin suresi (saniye).")]
        public float flash = 0.16f;

        private TowerWeapon weapon;
        private SpriteRenderer turret;
        private SpriteRenderer lamp;
        private Color baseColor;
        private float clock;
        private bool tinted;

        private static readonly Color LampColor = new Color(1f, 0.86f, 0.25f, 1f);
        private static readonly Color FlashTint = new Color(0.62f, 0.86f, 1f, 1f);

        public static AuraFeedback Attach(GameObject tower, TowerWeapon weapon, SpriteRenderer turret)
        {
            var fb = tower.AddComponent<AuraFeedback>();
            fb.weapon = weapon;
            fb.turret = turret;
            fb.baseColor = turret != null ? turret.color : Color.white;

            var lampGo = new GameObject("RadarLamp");
            lampGo.transform.SetParent(tower.transform, false);
            lampGo.transform.localPosition = new Vector3(0.30f, 0.30f, 0f);

            fb.lamp = lampGo.AddComponent<SpriteRenderer>();
            fb.lamp.sprite = PrimitiveSprites.Circle();
            fb.lamp.color = LampColor;
            fb.lamp.sortingOrder = turret != null ? turret.sortingOrder + 1 : 20;

            float size = fb.lamp.sprite.bounds.size.x;
            float scale = size > 0.0001f ? 0.11f / size : 0.11f;
            lampGo.transform.localScale = new Vector3(scale, scale, 1f);
            fb.lamp.enabled = false;

            // Kuleler ayni anda yanip sonmesin: her biri kendi fazinda.
            fb.clock = Random.value * fb.period;
            return fb;
        }

        private void LateUpdate()
        {
            bool buffed = weapon != null && weapon.IsBuffed;

            if (!buffed)
            {
                if (lamp != null) lamp.enabled = false;
                Restore();
                return;
            }

            clock += Time.deltaTime;
            if (clock >= period) clock -= period;

            bool on = clock < flash;
            if (lamp != null) lamp.enabled = on;

            if (turret == null) return;

            if (on)
            {
                turret.color = baseColor * FlashTint;
                tinted = true;
            }
            else
            {
                Restore();
            }
        }

        private void Restore()
        {
            if (!tinted || turret == null) return;
            turret.color = baseColor;
            tinted = false;
        }
    }
}
