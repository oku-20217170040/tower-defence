using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>Draft uretilirken kullanilan run durumu.</summary>
    public struct DraftContext
    {
        /// <summary>Yeni baslayacak dalganin numarasi (1 tabanli).</summary>
        public int Wave;

        /// <summary>
        /// Koridor basina kesif izi payi; toplami 1. CorridorPressure.Measure
        /// uretir.
        /// </summary>
        public float[] CorridorShares;
    }

    /// <summary>
    /// Dalga sonu draft'ini uretir: 3 secenek, tekrarsiz, seed ile
    /// belirlenimci.
    ///
    /// Secenekler rastgele DEGILDIR (spec 2.2). Ordunin ezberledigi
    /// koridora yonelik bedava kule secenekleri daha agir tartilir. Bu,
    /// adaptif sistemin oynanisa baglandigi ikinci kanaldir: ordu nereyi
    /// ogrendiyse oyuncuya orayi isleme firsati sunulur.
    /// </summary>
    public static class DraftGenerator
    {
        /// <summary>Sunulan secenek sayisi.</summary>
        public const int OptionCount = 3;

        /// <summary>
        /// Koridor tercihinin secenek agirligini ne kadar buktugu. 0 iken
        /// draft orduden bagimsizdir; cok yuksekte ezberlenmemis koridorlar
        /// hic sunulmaz ve oyuncunun secenegi kalmaz.
        /// </summary>
        public const float CorridorBias = 2f;

        public static IReadOnlyList<DraftOption> Generate(DraftContext context, int seed)
        {
            var pool = new List<DraftOption>();
            var weights = new List<float>();

            AddCorridorOptions(context, pool, weights);
            AddGlobalOptions(context, pool, weights);

            var rng = new Random(seed);
            var picked = new List<DraftOption>(OptionCount);
            var usedIds = new HashSet<string>();

            while (picked.Count < OptionCount && pool.Count > 0)
            {
                int index = PickWeighted(rng, weights);
                var option = pool[index];

                pool.RemoveAt(index);
                weights.RemoveAt(index);

                // Ayni secenek iki kez sunulmaz; oyuncuya sahte secim olur.
                if (usedIds.Add(option.Id)) picked.Add(option);
            }

            return picked;
        }

        private static void AddCorridorOptions(
            DraftContext context, List<DraftOption> pool, List<float> weights)
        {
            float[] shares = context.CorridorShares;
            if (shares == null || shares.Length == 0) shares = new[] { 1f };

            var kinds = new[] { TowerKind.Battery, TowerKind.Ambush, TowerKind.Decoy, TowerKind.Minefield };

            for (int c = 0; c < shares.Length; c++)
            {
                // Esit dagilimda oran 1 olsun diye koridor sayisiyla carpiliyor;
                // boylece bias yalnizca SAPMAYI odullendirir.
                float relative = shares[c] * shares.Length;
                if (relative < 0f) relative = 0f;

                float weight = 1f + CorridorBias * relative;

                for (int k = 0; k < kinds.Length; k++)
                {
                    pool.Add(new DraftOption
                    {
                        Kind = DraftOptionKind.FreeTower,
                        Tower = kinds[k],
                        Passive = PassiveKind.None,
                        Corridor = c,
                        Amount = 1
                    });
                    weights.Add(weight);
                }
            }
        }

        private static void AddGlobalOptions(
            DraftContext context, List<DraftOption> pool, List<float> weights)
        {
            var kinds = new[] { TowerKind.Battery, TowerKind.Ambush, TowerKind.Decoy, TowerKind.Minefield };
            for (int k = 0; k < kinds.Length; k++)
            {
                pool.Add(new DraftOption
                {
                    Kind = DraftOptionKind.Discount,
                    Tower = kinds[k],
                    Passive = PassiveKind.None,
                    Corridor = -1,
                    Amount = 25
                });
                weights.Add(2f);
            }

            // Besin gec dalgalarda buyur; yoksa ilerledikce degersizlesir.
            int wave = context.Wave < 1 ? 1 : context.Wave;
            pool.Add(new DraftOption
            {
                Kind = DraftOptionKind.Food,
                Tower = TowerKind.Battery,
                Passive = PassiveKind.None,
                Corridor = -1,
                Amount = 10 + 2 * wave
            });
            weights.Add(3f);

            pool.Add(new DraftOption
            {
                Kind = DraftOptionKind.Passive,
                Tower = TowerKind.Battery,
                Passive = PassiveKind.Damage,
                Corridor = -1,
                Amount = 10
            });
            weights.Add(2f);

            pool.Add(new DraftOption
            {
                Kind = DraftOptionKind.Passive,
                Tower = TowerKind.Battery,
                Passive = PassiveKind.Range,
                Corridor = -1,
                Amount = 10
            });
            weights.Add(2f);
        }

        private static int PickWeighted(Random rng, List<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += weights[i];

            if (total <= 0f) return rng.Next(weights.Count);

            double roll = rng.NextDouble() * total;
            float running = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                running += weights[i];
                if (roll < running) return i;
            }

            return weights.Count - 1;
        }
    }
}
