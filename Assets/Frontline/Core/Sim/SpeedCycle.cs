using System;

namespace Frontline.Core
{
    /// <summary>
    /// Oyun hizi kademesi. Saf C# - Time.timeScale'i bilmez, yalnizca
    /// hangi carpanda oldugumuzu ve bir sonrakinin ne oldugunu soyler.
    ///
    /// Neden var: bir run 15 dalga ve dalga arasi bekleme iceriyor. Tek
    /// hizda oyuncu ekrana bakip bekliyor; karar verecek bir sey yokken
    /// gecen sure oyunun en zayif kismi. Referans mobil TD'lerin hepsinde
    /// bu dugme var ve olmadigi surumler "yavas" diye birakiliyor.
    ///
    /// Kademeler kasten 1-2-3: 4 ve ustunde birlikler kare basina bir
    /// hucreden fazla ilerleyebiliyor ve pusu mevzisi menzilini isabetsiz
    /// atliyor. Ust sinir bir denge karari, gorsel tercih degil.
    /// </summary>
    public sealed class SpeedCycle
    {
        /// <summary>Izin verilen carpanlar, artan sirada.</summary>
        public static readonly float[] Steps = { 1f, 2f, 3f };

        private int index;

        /// <summary>Su anki carpan.</summary>
        public float Current
        {
            get { return Steps[index]; }
        }

        /// <summary>Kacinci kademe (0 tabanli).</summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>"x2" gibi arayuzde gosterilecek etiket.</summary>
        public string Label
        {
            get { return "x" + ((int)Current); }
        }

        /// <summary>
        /// Bir sonraki kademeye gecer; sondaysa basa doner ve yeni carpani
        /// dondurur. Dongusel olmasi kasti: tek dugme, tek parmak.
        /// </summary>
        public float Next()
        {
            index = (index + 1) % Steps.Length;
            return Current;
        }

        /// <summary>x1'e doner. Run bittiginde ya da duraklatmada kullanilir.</summary>
        public void Reset()
        {
            index = 0;
        }

        /// <summary>
        /// Verilen carpana en yakin kademeye oturur. Kayitli ayari geri
        /// yuklerken listede olmayan bir deger gelirse patlamamasi icin.
        /// </summary>
        public void SetClosest(float multiplier)
        {
            int best = 0;
            float bestGap = Math.Abs(Steps[0] - multiplier);

            for (int i = 1; i < Steps.Length; i++)
            {
                float gap = Math.Abs(Steps[i] - multiplier);
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }

            index = best;
        }
    }
}
