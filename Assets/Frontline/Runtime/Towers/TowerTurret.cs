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
    /// Taban kural: namlu YEREL -Y'de (ekranda asagi), hedef aci = yonun
    /// acisi + 90 derece. Hazir asset paketleri bu kurala uymuyor - top
    /// namluyu yukari, makineli sola ciziyor - bu yuzden her sprite kendi
    /// duzeltmesini rotationOffset ile bildirir.
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

        [Tooltip("Sprite'in namlu yonu duzeltmesi (derece). Taban kural " +
                 "namlunun YEREL -Y'de olmasidir; baska yone cizilmis " +
                 "sprite'lar bunu bildirir. Bkz. ArtLibrary.TowerArt.")]
        public float rotationOffset;

        [Tooltip("Saniyede kac derece doner. Batarya agir bir top oldugu " +
                 "icin yavas, pusu daha cevik olabilir.")]
        public float degreesPerSecond = 320f;

        [Tooltip("Hedef yokken dondugu yon (ekranda asagi = birliklerin " +
                 "geldigi yon degil, GITTIGI yon: kule usu korur).")]
        public Vector2 restDirection = Vector2.down;

        [Header("Geri tepme (recoil)")]
        [Tooltip("Namlunun her atista GERI kaydigi mesafe (dunya birimi). " +
                 "TowerWeapon.Fire cagirdiginda Recoil() bu kadar kaydirir, " +
                 "sonra ayni sureyle geri doner. Kullanici istegi (3 Eylul " +
                 "2026 gece): 'ates ederken geri tepmeler olsun'.")]
        public float recoilDistance = 0.12f;

        [Tooltip("Geri kaymanin ne kadar surede tamamlanacagi (saniye).")]
        public float recoilOutSeconds = 0.035f;

        [Tooltip("Geri kaymadan eski konuma donusun suresi (saniye). " +
                 "Donus, kaymadan daha yavastir - gercek namlu geri " +
                 "tepmesi de boyle: hizli firlar, yavas yerine oturur.")]
        public float recoilReturnSeconds = 0.11f;

        private Vector3 recoilBaseLocalPos;
        private float recoilTimer = -1f;

        private void Awake()
        {
            if (weapon == null) weapon = GetComponentInParent<TowerWeapon>();

            // Baslangicta bekleme yonune bakar; yoksa ilk karede
            // 0 dereceden savrularak doner.
            transform.rotation = Quaternion.Euler(0f, 0f, AngleFor(restDirection));
            recoilBaseLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (weapon == null) return;

            Vector2 dir = weapon.HasTarget ? weapon.AimDirection : restDirection;
            if (dir.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.Euler(0f, 0f, AngleFor(dir)),
                    degreesPerSecond * Time.deltaTime);
            }

            UpdateRecoil();
        }

        /// <summary>
        /// TowerWeapon her atista cagirir: namlu kendi bakis yonunun
        /// TERSINE (geriye) kayar, sonra yerine doner. Namlu ucu yerel
        /// -Y'de oldugu icin geri yon dunyada +transform.up'tir - bu,
        /// TowerTurret'in kendi rotasyonuna gore hesaplanir, yani kule
        /// hangi yone donuk olursa olsun kaymasi hep 'kendi arkasina'
        /// dogru olur.
        /// </summary>
        public void Recoil()
        {
            recoilTimer = 0f;
        }

        private void UpdateRecoil()
        {
            if (recoilTimer < 0f)
            {
                transform.localPosition = recoilBaseLocalPos;
                return;
            }

            recoilTimer += Time.deltaTime;
            float total = recoilOutSeconds + recoilReturnSeconds;
            float amount;

            if (recoilTimer <= recoilOutSeconds)
            {
                amount = Mathf.Lerp(0f, recoilDistance,
                    recoilOutSeconds > 0f ? recoilTimer / recoilOutSeconds : 1f);
            }
            else if (recoilTimer <= total)
            {
                float t = recoilReturnSeconds > 0f
                    ? (recoilTimer - recoilOutSeconds) / recoilReturnSeconds : 1f;
                amount = Mathf.Lerp(recoilDistance, 0f, t);
            }
            else
            {
                amount = 0f;
                recoilTimer = -1f;
            }

            transform.localPosition = recoilBaseLocalPos + (Vector3)((Vector2)transform.up * amount);
        }

        /// <summary>
        /// Sprite'in namlusu verilen yone baksin. Taban kural: namlu yerel
        /// -Y'de. rotationOffset baska yone cizilmis sprite'lari duzeltir.
        /// </summary>
        private float AngleFor(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f + rotationOffset;
        }
    }
}
