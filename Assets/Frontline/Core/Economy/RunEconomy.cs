using System;

namespace Frontline.Core
{
    /// <summary>
    /// Bir run'in kaynak hesabi. Kaynak birimi BESIN.
    ///
    /// Neden var: Plan 3'un olcumu kule sayisinin oyunun baskin degiskeni
    /// oldugunu gosterdi (spec 3.5). Koridor basina 1 kule dalga 10'da
    /// kaybettiriyor, 2 kule sifir sizintiyla kazandiriyor. Kuleler bedava
    /// oldugu surece oyunun zorluk ayari diye bir sey yoktur; bu sinif o
    /// ayarin tasiyicisidir.
    ///
    /// Saf hesaptir, MonoBehaviour degildir; birim testiyle dogrulanir.
    /// </summary>
    public sealed class RunEconomy
    {
        /// <summary>Eldeki besin.</summary>
        public int Balance { get; private set; }

        /// <summary>Run boyunca kazanilan toplam. Iade bunu artirmaz.</summary>
        public int TotalEarned { get; private set; }

        /// <summary>Run boyunca harcanan toplam. Iade bunu azaltmaz.</summary>
        public int TotalSpent { get; private set; }

        /// <summary>Bakiye degisti. Argument guncel bakiyedir.</summary>
        public event Action<int> BalanceChanged;

        public RunEconomy(int startingBalance)
        {
            if (startingBalance < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(startingBalance), "Baslangic bakiyesi negatif olamaz.");

            Balance = startingBalance;
        }

        public void Earn(int amount)
        {
            if (amount <= 0) return;

            Balance += amount;
            TotalEarned += amount;
            Raise();
        }

        public bool CanAfford(int cost)
        {
            return cost <= Balance;
        }

        /// <summary>
        /// Yeterli bakiye varsa harcar ve true doner. Yetmezse HICBIR SEY
        /// degistirmez ve false doner - kismi harcama yoktur.
        /// </summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0) return false;
            if (cost > Balance) return false;

            Balance -= cost;
            TotalSpent += cost;
            Raise();
            return true;
        }

        /// <summary>
        /// Kule satisi gibi iadeler. TotalEarned'i ARTIRMAZ; artirsaydi
        /// al-sat dongusuyle sonsuz gelir uretilebilirdi.
        /// </summary>
        public void Refund(int amount)
        {
            if (amount <= 0) return;

            Balance += amount;
            Raise();
        }

        private void Raise()
        {
            if (BalanceChanged != null) BalanceChanged(Balance);
        }
    }
}
