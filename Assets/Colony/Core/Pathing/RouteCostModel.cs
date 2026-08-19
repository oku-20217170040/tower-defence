using System;

namespace Colony.Core
{
    /// <summary>
    /// Uc alani tek bir adim maliyetinde birlestirir.
    ///
    ///   stepCost = 1
    ///            + ThreatWeight * threat
    ///            + AttractionWeight * (1 - normalizedAttraction)
    ///
    /// Cekim (feromon + yem) maliyeti DUSURUR ama bunu negatif deger ureterek
    /// degil, sabit bir cezayi azaltarak yapar. Maliyet asla 1'in altina inmez;
    /// bu, A* sezgiselinin kabul edilebilir (admissible) kalmasi icin zorunludur.
    /// </summary>
    public sealed class RouteCostModel
    {
        private readonly PheromoneMap pheromone;
        private readonly LureField lure;
        private readonly ThreatField threat;

        /// <summary>Cekimin doygunluga ulastigi toplam deger. Feromon tavani ile aynidir.</summary>
        private readonly float attractionSaturation;

        public float ThreatWeight { get; set; } = 1f;
        public float AttractionWeight { get; set; } = 1f;

        public RouteCostModel(PheromoneMap pheromone, LureField lure, ThreatField threat)
        {
            this.pheromone = pheromone ?? throw new ArgumentNullException(nameof(pheromone));
            this.lure = lure ?? throw new ArgumentNullException(nameof(lure));
            this.threat = threat ?? throw new ArgumentNullException(nameof(threat));
            attractionSaturation = pheromone.Max;
        }

        /// <summary>Bir hucreye adim atmanin maliyeti. Her zaman 1 veya uzeri.</summary>
        public float StepCost(int index)
        {
            float attraction = pheromone.Get(index) + lure.Get(index);
            float normalized = attraction / attractionSaturation;
            if (normalized > 1f) normalized = 1f;

            float attractionPenalty = AttractionWeight * (1f - normalized);
            float threatPenalty = ThreatWeight * threat.Get(index);

            return 1f + threatPenalty + attractionPenalty;
        }
    }
}
