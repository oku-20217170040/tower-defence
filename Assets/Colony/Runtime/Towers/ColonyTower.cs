using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Sahnedeki bir kule. Plan 2'de kuleler ATES ETMEZ - yalnizca koloninin
    /// rota kararini etkilerler. Hasar, menzil ve hedefleme Plan 3'un isidir.
    /// Buradaki amac tek bir soruyu cevaplayabilmek: oyuncu suruyu
    /// yonlendirebiliyor mu?
    /// </summary>
    public sealed class ColonyTower : MonoBehaviour
    {
        public TowerKind kind = TowerKind.Noisy;

        [Tooltip("Etki yaricapi (hucre).")]
        public float radius = 4f;

        [Tooltip("Gurultucu kulenin tehdit gucu. 4 olcumle secildi: doymus bir izi " +
                 "5-6 dalgada kirar. Daha yuksegi suruyu aninda kacirir ve ogrenmeyi " +
                 "anlamsiz kilar, daha dusugu hic etki etmez.")]
        public float threatStrength = 4f;

        private ColonyWorld world;

        /// <summary>Kulenin durdugu izgara hucresi.</summary>
        public int Cell { get; private set; }

        public void Place(ColonyWorld world, int cell)
        {
            this.world = world;
            Cell = cell;
            transform.position = world.Space.CellToWorld(cell);

            world.RegisterTower(this);
        }

        private void OnDestroy()
        {
            if (world != null)
                world.UnregisterTower(this);
        }
    }
}
