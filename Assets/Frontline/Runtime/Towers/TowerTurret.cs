using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kulenin NAMLUSUNU hedefe dondurur.
    ///
    /// Yalnizca turret cocuguna takilir; kaide (Platform) sabit kalir.
    /// Once kule kokune takiliyordu ve kaide de donuyordu - tepeden bakista
    /// donen bir beton kaide, kulenin yerden sokulup cevrildigi izlenimi
    /// veriyor. Gercek top kulelerinde donen sey yalnizca ust yapidir.
    ///
    /// Sprite'lar tepeden bakisla ve namlu YEREL -Y'de (ekranda asagi)
    /// uretiliyor (bkz. tools/blender/towers.py). Birliklerdeki kuralin
    /// aynisi, tek yerden: hedef aci = yonun acisi + 90 derece.
    ///
    /// Yalnizca HASAR VEREN kulelere takilir. Yem ve mayin tarlasi
    /// donmez - donen bir sey oyuncuya "bu beni hedefliyor" der ve
    /// hasarsiz kule bunu soylememeli.
    /// </summary>
    public sealed class TowerTurret : MonoBehaviour
    {
        [Tooltip("Nisan yonunu okudugu silah. Bos birakilirsa ust nesnelerde " +
                 "aranir - turret cocugunun silahi kule kokundedir.")]
        public TowerWeapon weapon;

        [Tooltip("Saniyede kac derece doner. Batarya agir bir top oldugu " +
                 "icin yavas, pusu daha cevik olabilir.")]
        public float degreesPerSecond = 320f;

        [Tooltip("Hedef yokken dondugu yon (ekranda asagi = birliklerin " +
                 "geldigi yon degil, GITTIGI yon: kule usu korur).")]
        public Vector2 restDirection = Vector2.down;

        private void Awake()
        {
            if (weapon == null) weapon = GetComponentInParent<TowerWeapon>();

            // Baslangicta bekleme yonune bakar; yoksa ilk karede
            // 0 dereceden savrularak doner.
            transform.rotation = Quaternion.Euler(0f, 0f, AngleFor(restDirection));
        }

        private void Update()
        {
            if (weapon == null) return;

            Vector2 dir = weapon.HasTarget ? weapon.AimDirection : restDirection;
            if (dir.sqrMagnitude < 0.000001f) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0f, 0f, AngleFor(dir)),
                degreesPerSecond * Time.deltaTime);
        }

        /// <summary>Sprite'in yerel -Y ekseni verilen yone baksin.</summary>
        private static float AngleFor(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        }
    }
}
