using NUnit.Framework;
using UnityEngine;
using Frontline.Core;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// Atis gorselinin TEK olculebilir parcasi: merminin nereden doguru.
    ///
    /// Gorsellerin geri kalani (renk, sure, patlama egrisi) kullaniciya
    /// aittir - oyunun disiplini bu: olculebilir olan teste, gorsel olan
    /// oynayan goze. Ama "mermi namlunun UCUNDAN cikmali" bir sanat
    /// karari degil, bir konum hesabidir ve sessizce bozulabilir.
    /// </summary>
    public sealed class TowerFxTests
    {
        [Test]
        public void MuzzlePoint_NisanYonundeVeOfsetKadarUzakta()
        {
            Vector2 tower = new Vector2(3f, -2f);
            Vector2 aim = new Vector2(0f, -1f);

            Vector2 muzzle = TowerFx.MuzzlePoint(tower, aim, 1f, 0.42f);

            Assert.AreEqual(3f, muzzle.x, 0.0001f);
            Assert.AreEqual(-2.42f, muzzle.y, 0.0001f);
        }

        [Test]
        public void MuzzlePoint_KuleOlcegiyleBuyur()
        {
            Vector2 muzzle = TowerFx.MuzzlePoint(Vector2.zero, Vector2.right, 2f, 0.5f);

            Assert.AreEqual(1f, muzzle.x, 0.0001f);
        }

        [Test]
        public void MuzzlePoint_NormalizeEdilmemisYonuNormalizeEder()
        {
            // Nisan vektoru hedefe olan HAM fark olabilir; uzunlugu
            // namlu ucunu haritanin disina firlatmamali.
            Vector2 muzzle = TowerFx.MuzzlePoint(Vector2.zero, new Vector2(0f, -9f),
                                                 1f, 0.4f);

            Assert.AreEqual(-0.4f, muzzle.y, 0.0001f);
        }

        [Test]
        public void MuzzlePoint_YonYoksaKuleMerkezindeKalir()
        {
            Vector2 muzzle = TowerFx.MuzzlePoint(new Vector2(1f, 1f), Vector2.zero,
                                                 1f, 0.4f);

            Assert.AreEqual(new Vector2(1f, 1f), muzzle);
        }

        [Test]
        public void Isinin_Mermisi_Yoktur()
        {
            // Isin bir hat cizer; mermi ucursa iki gorsel ayni anda
            // ayni seyi anlatir ve hat okunmaz olur.
            Assert.AreEqual(0f, TowerFx.For(TowerKind.Beam).bulletScale);
        }

        [Test]
        public void GorselHiyerarsi_BataryaGurultulu_PusuSessiz()
        {
            TowerFx battery = TowerFx.For(TowerKind.Battery);
            TowerFx ambush = TowerFx.For(TowerKind.Ambush);

            // spec 9.1: batarya ekranda gurultu yapan tek kule.
            Assert.Greater(battery.impactScale, ambush.impactScale);
            Assert.Greater(battery.muzzleScale, ambush.muzzleScale);
            Assert.Greater(battery.bulletScale, ambush.bulletScale);

            // Pusunun mermisi kucuk ama HIZLI: hafif silah.
            Assert.Greater(ambush.bulletSpeed, battery.bulletSpeed);
        }
    }
}
