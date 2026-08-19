using System;
using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Yuvanin cani. Yuvaya ulasan her bocek can eksiltir; sifirlaninca run kaybedilir.
    /// </summary>
    public sealed class NestHealth : MonoBehaviour
    {
        [Tooltip("Run basindaki can. Olcumde zayif bir dugme oldugu gorildu: " +
                 "30'dan 50'ye cikarmak kayip dalgasini yalnizca 10'dan 11'e tasidi. " +
                 "Asil belirleyici kule sayisi.")]
        public int maxHp = 30;

        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0;

        /// <summary>Can degisti. Argument guncel candir.</summary>
        public event Action<int> Changed;

        /// <summary>Can bitti; run kaybedildi.</summary>
        public event Action Emptied;

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHp = maxHp;
            if (Changed != null) Changed(CurrentHp);
        }

        public void Damage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;

            CurrentHp -= amount;
            if (CurrentHp < 0) CurrentHp = 0;

            if (Changed != null) Changed(CurrentHp);

            if (CurrentHp == 0 && Emptied != null) Emptied();
        }
    }
}
