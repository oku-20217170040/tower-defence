using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Ordun ogrenilmis rota hafizasi. Birlikler gectikleri hucrelere kesif izi
    /// birakir; usya ulasan rota guclenir, olumle biten rota sonumlenir.
    /// Yuksek kesif izi = tercih edilen rota (ceker, itmez).
    /// </summary>
    public sealed class ReconMap
    {
        private readonly float[] values;

        /// <summary>Bir hucrenin tasiyabilecegi en yuksek kesif izi degeri.</summary>
        public float Max { get; }

        /// <summary>Her Decay() cagrisinda kaybedilen oran (0-1).</summary>
        public float DecayRate { get; }

        public ReconMap(BattleGrid grid, float max, float decayRate)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (max <= 0f)
                throw new ArgumentOutOfRangeException(nameof(max), "Max pozitif olmali.");
            if (decayRate < 0f || decayRate > 1f)
                throw new ArgumentOutOfRangeException(nameof(decayRate), "DecayRate 0-1 araliginda olmali.");

            values = new float[grid.CellCount];
            Max = max;
            DecayRate = decayRate;
        }

        public float Get(int index) => values[index];

        public void Deposit(int index, float amount)
        {
            float v = values[index] + amount;
            values[index] = v > Max ? Max : v;
        }

        public void Weaken(int index, float amount)
        {
            float v = values[index] - amount;
            values[index] = v < 0f ? 0f : v;
        }

        public void DepositTrail(IReadOnlyList<int> path, float amountPerCell)
        {
            if (path == null) return;
            for (int i = 0; i < path.Count; i++)
                Deposit(path[i], amountPerCell);
        }

        public void WeakenTrail(IReadOnlyList<int> path, float amountPerCell)
        {
            if (path == null) return;
            for (int i = 0; i < path.Count; i++)
                Weaken(path[i], amountPerCell);
        }

        /// <summary>Dalga sonunda cagrilir: tum izler orantili olarak sonumlenir.</summary>
        public void Decay()
        {
            float keep = 1f - DecayRate;
            for (int i = 0; i < values.Length; i++)
                values[i] *= keep;
        }

        public void Clear() => Array.Clear(values, 0, values.Length);
    }
}
