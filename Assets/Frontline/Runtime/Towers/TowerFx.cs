using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kule turune gore atis gorseli: namlu ucu, mermi, cakma ve carpma.
    /// TAMAMEN KOZMETIKTIR - hasar TowerWeapon icinde uygulanir, buradaki
    /// hicbir sayi dengeye dokunmaz.
    ///
    /// Neden ayri bir sinif: "namlunun ucu nerede" sorusunun tek bir dogru
    /// cevabi var ve o cevap SPRITE'tan gelir (bkz. tools/blender/towers.py).
    /// TowerPlacer, TowerWeapon ve namlu cakmasi ayni sayiyi kullanmazsa
    /// mermi govdeden, cakma namludan cikar ve ikisi ayrisir.
    ///
    /// Namlu uzakligi olculdu: sprite'lar 1 dunya birimi genisliginde
    /// uretiliyor, yani asagidaki degerler dogrudan sprite koordinatidir.
    ///   batarya -> Muzzle kutusu y = -0,42
    ///   pusu    -> Muzzle kutusu y = -0,46 (23 Agu: mazgal gercek
    ///              namluya cevrildi, ofis 0,28'den buyudu)
    ///   isin    -> Muzzle agzi y = -0,48 (23 Agu: yatay ray one
    ///              bakan yayiciya cevrildi)
    /// </summary>
    public struct TowerFx
    {
        /// <summary>Namlu ucunun kule merkezine uzakligi (sprite birimi).</summary>
        public float muzzleOffset;

        /// <summary>Merminin hucre boyutuna orani.</summary>
        public float bulletScale;

        /// <summary>Merminin hizi (hucre / saniye).</summary>
        public float bulletSpeed;

        /// <summary>Namlu cakmasinin hucre boyutuna orani.</summary>
        public float muzzleScale;

        /// <summary>Carpma patlamasinin hucre boyutuna orani.</summary>
        public float impactScale;

        /// <summary>Sprite yoksa kullanilacak renk (daireye dusus).</summary>
        public Color fallbackColor;

        /// <summary>
        /// Tur basina olculmus varsayilanlar. Gorsel hiyerarsi mekanikle
        /// AYNI kalir (spec 9.1): batarya buyuk ve yavas mermi + genis
        /// patlama, pusu kucuk hizli dart + ufak kivilcim.
        /// </summary>
        public static TowerFx For(TowerKind kind)
        {
            if (kind == TowerKind.Battery)
            {
                return new TowerFx
                {
                    muzzleOffset = 0.42f,
                    bulletScale = 0.34f,
                    bulletSpeed = 11f,
                    muzzleScale = 0.55f,
                    impactScale = 0.75f,
                    fallbackColor = new Color(1f, 0.62f, 0.25f, 0.9f)
                };
            }

            if (kind == TowerKind.Beam)
            {
                return new TowerFx
                {
                    muzzleOffset = 0.50f,
                    bulletScale = 0f,      // isinin mermisi YOK, hat cizilir
                    bulletSpeed = 0f,
                    muzzleScale = 0.45f,
                    impactScale = 0.5f,
                    fallbackColor = new Color(0.55f, 0.88f, 1f, 0.9f)
                };
            }

            // Pusu ve hasarsiz kuleler ayni sessiz bantta.
            return new TowerFx
            {
                muzzleOffset = 0.48f,
                bulletScale = 0.22f,
                bulletSpeed = 20f,
                muzzleScale = 0.30f,
                impactScale = 0.36f,
                fallbackColor = new Color(0.80f, 0.88f, 0.94f, 0.75f)
            };
        }

        /// <summary>
        /// Namlunun ucu. Mermi ve cakma kule MERKEZINDEN degil buradan
        /// dogar; merkezden dogan mermi kulenin govdesinin icinden cikip
        /// namluyu yalanci yapar.
        /// </summary>
        public static Vector2 MuzzlePoint(Vector2 towerPosition, Vector2 aim,
                                          float worldScale, float offset)
        {
            if (aim.sqrMagnitude < 0.000001f) return towerPosition;
            return towerPosition + aim.normalized * (offset * worldScale);
        }
    }
}
