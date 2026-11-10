using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Bir ortu yikilinca oyuncuya besin verir (kullanici geri bildirimi,
    /// 2 Eylul 2026: "obje patladiktan sonra buyukluk durumuna gore coin
    /// kazansin"). Hesap Core/Economy/SceneryReward.cs'de; burasi yalnizca
    /// Battlefield.SceneryClearedDetailed'i EconomyRunner'a baglayan
    /// kablodur.
    /// </summary>
    public sealed class SceneryRewardRunner : MonoBehaviour
    {
        public Battlefield world;
        public EconomyRunner economy;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (economy == null) economy = FindFirstObjectByType<EconomyRunner>();
        }

        private void OnEnable()
        {
            if (world != null) world.SceneryClearedDetailed += OnSceneryCleared;
        }

        private void OnDisable()
        {
            if (world != null) world.SceneryClearedDetailed -= OnSceneryCleared;
        }

        private void OnSceneryCleared(int cell, SceneryKind kind, float maxHp)
        {
            if (economy == null) return;

            int coins = SceneryReward.CoinsFor(maxHp);
            if (coins > 0) economy.Economy.Earn(coins);
        }
    }
}
