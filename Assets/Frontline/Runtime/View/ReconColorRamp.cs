using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kesif izi ve yem degerlerini ekranda gorunur renge cevirir.
    ///
    /// Iki ton bilerek ayrilmistir (spec 9.1):
    /// - Gercek kesif izi SICAK tonda (soluk sari -> parlak turuncu)
    /// - Yem SOGUK tonda (yesil)
    ///
    /// Boylece oyuncu ordun gercekten ogrendigi izi, kendi bastigi
    /// yalandan ayirt edebilir. Bu ayrim olmadan kandirma mekanigi
    /// ekranda okunamaz hale gelir.
    ///
    /// Saf hesaptir, MonoBehaviour degildir; birim testiyle dogrulanir.
    /// </summary>
    public static class ReconColorRamp
    {
        /// <summary>
        /// En yogun izin bile tam opak olmamasi icin tavan.
        ///
        /// 0,7'den 0,42'ye indirildi (23 Agu 2026). Sebep OLCUM DEGIL, gorsel
        /// bir karar: harita Blender dosemeleri ve ortu gorselleriyle
        /// giydirilince 0,7'lik tavan butun zemini beyaza boyayip altindaki
        /// sanati siliyordu. Iz artik zeminin UZERINE surulen bir renk, onun
        /// yerine gecen bir boya degil. Izin okunabilirligi bozulursa geri
        /// alinacak dugme budur.
        /// </summary>
        public const float MaxAlpha = 0.42f;

        /// <summary>
        /// Gorunurluk egrisi. 1'den buyuk deger zayif izleri bastirir ve
        /// guclu izlerin arasindaki farki korur. Dogrusal egride iz birkac
        /// dalgada tavana dayaniyor ve her yer ayni parlakliga gelip
        /// "hangi hucre daha guclu" bilgisi kayboluyordu.
        /// </summary>
        private const float IntensityGamma = 1.8f;

        private static readonly Color WeakRecon = new Color(1f, 0.95f, 0.55f);
        private static readonly Color StrongRecon = new Color(1f, 0.45f, 0.12f);
        private static readonly Color DecoyTone = new Color(0.25f, 0.95f, 0.5f);

        /// <summary>
        /// recon01 ve decoy01 [0,1] araliginda beklenir; disari tasarsa kirpilir.
        /// Ikisi de sifirsa sonuc tamamen saydamdir.
        /// </summary>
        public static Color Evaluate(float recon01, float decoy01)
        {
            float recon = Mathf.Clamp01(recon01);
            float decoy = Mathf.Clamp01(decoy01);

            float total = recon + decoy;
            if (total <= 0f)
                return new Color(0f, 0f, 0f, 0f);

            // Gercek kesif iziun kendi ici gecisi: zayif iz soluk, guclu iz parlak.
            Color warm = Color.Lerp(WeakRecon, StrongRecon, recon);

            // Yemin payi arttikca ton soguga kayar.
            float decoyShare = decoy / total;
            Color rgb = Color.Lerp(warm, DecoyTone, decoyShare);

            // Gorunurluk hangisi baskinsa ona gore belirlenir; boylece
            // yem tek basina da gorunur kalir.
            float intensity = recon > decoy ? recon : decoy;
            rgb.a = Mathf.Pow(intensity, IntensityGamma) * MaxAlpha;

            return rgb;
        }
    }
}
