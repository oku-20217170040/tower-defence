using NUnit.Framework;
using Frontline.Core;

namespace Frontline.Tests
{
    public class RunEconomyTests
    {
        [Test]
        public void NewEconomy_StartsWithGivenBalance()
        {
            var economy = new RunEconomy(startingBalance: 40);

            Assert.AreEqual(40, economy.Balance);
            Assert.AreEqual(0, economy.TotalEarned);
            Assert.AreEqual(0, economy.TotalSpent);
        }

        [Test]
        public void Earn_IncreasesBalanceAndTotal()
        {
            var economy = new RunEconomy(0);

            economy.Earn(12);
            economy.Earn(8);

            Assert.AreEqual(20, economy.Balance);
            Assert.AreEqual(20, economy.TotalEarned);
        }

        [Test]
        public void Earn_IgnoresNonPositiveAmounts()
        {
            var economy = new RunEconomy(10);

            economy.Earn(0);
            economy.Earn(-5);

            Assert.AreEqual(10, economy.Balance);
            Assert.AreEqual(0, economy.TotalEarned);
        }

        [Test]
        public void TrySpend_SucceedsWhenAffordable()
        {
            var economy = new RunEconomy(30);

            Assert.IsTrue(economy.TrySpend(25));
            Assert.AreEqual(5, economy.Balance);
            Assert.AreEqual(25, economy.TotalSpent);
        }

        [Test]
        public void TrySpend_FailsWhenTooExpensive_AndLeavesBalanceUntouched()
        {
            var economy = new RunEconomy(30);

            Assert.IsFalse(economy.TrySpend(31));
            Assert.AreEqual(30, economy.Balance);
            Assert.AreEqual(0, economy.TotalSpent);
        }

        [Test]
        public void Balance_NeverGoesNegative()
        {
            var economy = new RunEconomy(10);

            economy.TrySpend(5);
            economy.TrySpend(5);
            economy.TrySpend(5);

            Assert.AreEqual(0, economy.Balance);
        }

        [Test]
        public void CanAfford_MatchesTrySpend()
        {
            var economy = new RunEconomy(20);

            Assert.IsTrue(economy.CanAfford(20));
            Assert.IsFalse(economy.CanAfford(21));

            // Bedava seyler her zaman alinabilir.
            Assert.IsTrue(economy.CanAfford(0));
        }

        [Test]
        public void Refund_ReturnsResourcesWithoutCountingAsEarnings()
        {
            // Iade kazanc degildir; aksi halde al-sat yaparak sonsuz gelir
            // uretilebilirdi.
            var economy = new RunEconomy(50);
            economy.TrySpend(30);

            economy.Refund(20);

            Assert.AreEqual(40, economy.Balance);
            Assert.AreEqual(0, economy.TotalEarned);
            Assert.AreEqual(30, economy.TotalSpent);
        }

        [Test]
        public void Constructor_RejectsNegativeStartingBalance()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new RunEconomy(-1));
        }
    }
}
