using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Feromon ve yem degerlerini ekranda gorunur renge cevirir.
    ///
    /// Iki ton bilerek ayrilmistir (spec 9.1):
    /// - Gercek feromon SICAK tonda (soluk sari -> parlak turuncu)
    /// - Yem SOGUK tonda (yesil)
    ///
    /// Boylece oyuncu kolonin gercekten ogrendigi izi, kendi bastigi
    /// yalandan ayirt edebilir. Bu ayrim olmadan kandirma mekanigi
    /// ekranda okunamaz hale gelir.
    ///
    /// Saf hesaptir, MonoBehaviour degildir; birim testiyle dogrulanir.
    /// </summary>
    public static class PheromoneColorRamp
    {
        /// <summary>En yogun izin bile tam opak olmamasi icin tavan.</summary>
        public const float MaxAlpha = 0.85f;

        private static readonly Color WeakPheromone = new Color(1f, 0.95f, 0.55f);
        private static readonly Color StrongPheromone = new Color(1f, 0.45f, 0.12f);
        private static readonly Color LureTone = new Color(0.25f, 0.95f, 0.5f);

        /// <summary>
        /// pheromone01 ve lure01 [0,1] araliginda beklenir; disari tasarsa kirpilir.
        /// Ikisi de sifirsa sonuc tamamen saydamdir.
        /// </summary>
        public static Color Evaluate(float pheromone01, float lure01)
        {
            float pheromone = Mathf.Clamp01(pheromone01);
            float lure = Mathf.Clamp01(lure01);

            float total = pheromone + lure;
            if (total <= 0f)
                return new Color(0f, 0f, 0f, 0f);

            // Gercek feromonun kendi ici gecisi: zayif iz soluk, guclu iz parlak.
            Color warm = Color.Lerp(WeakPheromone, StrongPheromone, pheromone);

            // Yemin payi arttikca ton soguga kayar.
            float lureShare = lure / total;
            Color rgb = Color.Lerp(warm, LureTone, lureShare);

            // Gorunurluk hangisi baskinsa ona gore belirlenir; boylece
            // yem tek basina da gorunur kalir.
            float intensity = pheromone > lure ? pheromone : lure;
            rgb.a = intensity * MaxAlpha;

            return rgb;
        }
    }
}
