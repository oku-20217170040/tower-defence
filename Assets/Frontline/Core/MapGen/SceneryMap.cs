using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>Yikilabilir ortu turu. Kaya agactan daha dayaniklidir.</summary>
    public enum SceneryKind
    {
        None = 0,
        Tree = 1,
        Rock = 2
    }

    /// <summary>
    /// Haritadaki yikilabilir ortunun cani. Kaya ve agaclar bastan engeldir;
    /// yeterince hasar alinca cukura donusur ve o hucre KULE KURULABILIR olur.
    ///
    /// Bu, haritanin oyuncu tarafindan degistirilebilmesinin tek yoludur:
    /// mevzi sayisi sabit degil, kazanilan bir seydir. Izgara ile can tablosu
    /// asla ayrisamasin diye hucreyi BuildStrip'e cevirme isini de bu sinif
    /// yapar; disaridan kimse grid'i elle degistirmemeli.
    /// </summary>
    public sealed class SceneryMap
    {
        private readonly BattleGrid grid;
        private readonly SceneryKind[] kinds;
        private readonly float[] hp;
        private readonly float[] maxHp;
        private readonly List<int> occupied = new List<int>();

        /// <summary>Bir ortu yikildiginda hucre indeksi ile tetiklenir.</summary>
        public event Action<int> Cleared;

        public SceneryMap(BattleGrid grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            this.grid = grid;
            kinds = new SceneryKind[grid.CellCount];
            hp = new float[grid.CellCount];
            maxHp = new float[grid.CellCount];
        }

        /// <summary>Uzerinde hala ayakta ortu bulunan hucreler.</summary>
        public IReadOnlyList<int> Occupied => occupied;

        public SceneryKind KindAt(int cell) => kinds[cell];
        public float HpAt(int cell) => hp[cell];
        public float MaxHpAt(int cell) => maxHp[cell];
        public bool IsStanding(int cell) => kinds[cell] != SceneryKind.None;

        /// <summary>Sag kalan can orani (0-1). Gorunum katmani catlak/cizik icin kullanir.</summary>
        public float HealthFraction(int cell)
        {
            if (maxHp[cell] <= 0f) return 0f;
            float f = hp[cell] / maxHp[cell];
            if (f < 0f) return 0f;
            return f > 1f ? 1f : f;
        }

        /// <summary>
        /// Hucreye ortu yerlestirir ve izgarada Scenery olarak isaretler.
        /// Koridor hucresine ortu konmaz: yolu tikamak haritayi cozulemez yapar.
        /// </summary>
        public void Place(int cell, SceneryKind kind, float health)
        {
            if (kind == SceneryKind.None)
                throw new ArgumentException("Ortu turu None olamaz.", nameof(kind));
            if (health <= 0f)
                throw new ArgumentOutOfRangeException(nameof(health), "Can pozitif olmali.");
            if (grid.Get(cell) == CellKind.Corridor)
                throw new InvalidOperationException("Koridor hucresine ortu konulamaz: yol tikanir.");

            if (kinds[cell] == SceneryKind.None) occupied.Add(cell);

            kinds[cell] = kind;
            hp[cell] = health;
            maxHp[cell] = health;
            grid.Set(cell, CellKind.Scenery);
        }

        /// <summary>
        /// Ortuye hasar verir. Can bitince hucre cukura (BuildStrip) doner.
        /// Ayakta ortu yoksa sessizce false doner: kule mermileri ve patlamalar
        /// her hucreye dusebilir, her isabette once kontrol etmek cagiranin
        /// isi olmamali.
        /// </summary>
        /// <returns>Bu cagri ortuyu yiktiysa true.</returns>
        public bool Damage(int cell, float amount)
        {
            if (amount <= 0f) return false;
            if (cell < 0 || cell >= kinds.Length) return false;
            if (kinds[cell] == SceneryKind.None) return false;

            hp[cell] -= amount;
            if (hp[cell] > 0f) return false;

            Clear(cell);
            return true;
        }

        /// <summary>Ortuyu tek adimda yikar (patlama / test / hile).</summary>
        public void Clear(int cell)
        {
            if (cell < 0 || cell >= kinds.Length) return;
            if (kinds[cell] == SceneryKind.None) return;

            kinds[cell] = SceneryKind.None;
            hp[cell] = 0f;
            maxHp[cell] = 0f;
            occupied.Remove(cell);

            // Cukur: artik mevzi kurulabilir. Ortuyu yikmanin tek odulu budur.
            grid.Set(cell, CellKind.BuildStrip);

            var handler = Cleared;
            if (handler != null) handler(cell);
        }
    }
}
