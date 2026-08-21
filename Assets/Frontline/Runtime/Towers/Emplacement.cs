using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Sahnedeki bir kule. Plan 2'de kuleler ATES ETMEZ - yalnizca ordunin
    /// rota kararini etkilerler. Hasar, menzil ve hedefleme Plan 3'un isidir.
    /// Buradaki amac tek bir soruyu cevaplayabilmek: oyuncu suruyu
    /// yonlendirebiliyor mu?
    /// </summary>
    public sealed class Emplacement : MonoBehaviour
    {
        public TowerKind kind = TowerKind.Battery;

        [Tooltip("Etki yaricapi (hucre).")]
        public float radius = 4f;

        [Tooltip("Batarya kulenin tehdit gucu. 4 olcumle secildi: doymus bir izi " +
                 "5-6 dalgada kirar. Daha yuksegi suruyu aninda kacirir ve ogrenmeyi " +
                 "anlamsiz kilar, daha dusugu hic etki etmez.")]
        public float threatStrength = 4f;

        [Tooltip("Mayin tarlasi kulesinin gecis suresi carpani. 2 olcumle secildi ve " +
                 "esik keskin: 1,5'te mayin tarlasi olu mekanik (6 kule butcesinde " +
                 "kazanma %20), 3'te sizintiyi sifira indirip baskin strateji " +
                 "oluyor. 2'de saf pusu mevzisiyle basa bas - yani gercek bir secim.")]
        public float slowFactor = 2f;

        private Battlefield world;

        /// <summary>Kulenin durdugu izgara hucresi.</summary>
        public int Cell { get; private set; }

        public void Place(Battlefield world, int cell)
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
