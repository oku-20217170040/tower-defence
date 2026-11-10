using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Frontline.Runtime;

namespace Frontline.Tests
{
    /// <summary>
    /// TowerSelection bir MonoBehaviour + sahne etkilesimi oldugu icin tam
    /// birim testi zor (dokunus, kamera, Battlefield gerektiriyor). Bunun
    /// yerine secim mantigi saf statik bir metoda (ResolveSelection)
    /// cikarildi ve burada Unity yasam dongusunden bagimsiz test edildi.
    /// RangeRingView ise gercek bir MonoBehaviour oldugu ama sahne/kamera
    /// gerektirmedigi icin dogrudan sinandi.
    /// </summary>
    public class TowerSelectionTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private Emplacement NewTower(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go.AddComponent<Emplacement>();
        }

        // --- ResolveSelection: saf secim mantigi ---

        [Test]
        public void ResolveSelection_NoCurrent_TappedNull_StaysNull()
        {
            Emplacement result = TowerSelection.ResolveSelection(null, null);
            Assert.IsNull(result);
        }

        [Test]
        public void ResolveSelection_NoCurrent_TappedTower_SelectsIt()
        {
            Emplacement tower = NewTower("Tower_A");

            Emplacement result = TowerSelection.ResolveSelection(null, tower);

            Assert.AreEqual(tower, result);
        }

        [Test]
        public void ResolveSelection_TappedSameAsCurrent_TogglesOff()
        {
            Emplacement tower = NewTower("Tower_A");

            Emplacement result = TowerSelection.ResolveSelection(tower, tower);

            Assert.IsNull(result);
        }

        [Test]
        public void ResolveSelection_TappedDifferentTower_SwitchesSelection()
        {
            Emplacement current = NewTower("Tower_A");
            Emplacement other = NewTower("Tower_B");

            Emplacement result = TowerSelection.ResolveSelection(current, other);

            Assert.AreEqual(other, result);
            Assert.AreNotEqual(current, result);
        }

        [Test]
        public void ResolveSelection_TappedEmptyCell_ClearsSelection()
        {
            Emplacement current = NewTower("Tower_A");

            // Bos hucreye (tapped == null) dokunuldugunda mevcut secim
            // her zaman kalkmali - kule bulunmadigi anlamina gelir.
            Emplacement result = TowerSelection.ResolveSelection(current, null);

            Assert.IsNull(result);
        }

        [Test]
        public void ResolveSelection_IsPure_DoesNotMutateInputs()
        {
            Emplacement current = NewTower("Tower_A");
            Emplacement other = NewTower("Tower_B");

            TowerSelection.ResolveSelection(current, other);

            // Statik metot yalnizca deger dondurur, kuleler uzerinde
            // yan etki yaratmaz.
            Assert.AreEqual("Tower_A", current.name);
            Assert.AreEqual("Tower_B", other.name);
        }

        // --- RangeRingView: sabit, animasyonsuz disk ---

        private RangeRingView NewRingView()
        {
            var go = new GameObject("RangeRingView");
            spawned.Add(go);
            go.AddComponent<SpriteRenderer>();
            return go.AddComponent<RangeRingView>();
        }

        [Test]
        public void RangeRingView_Bind_ScalesToRadiusTimesCellSizeTimesTwo()
        {
            RangeRingView view = NewRingView();

            view.Bind(radiusInCells: 4f, cellSize: 1.5f, sortingOrder: 12);

            float expectedDiameter = 4f * 1.5f * 2f;
            Assert.AreEqual(expectedDiameter, view.transform.localScale.x, 0.0001f);
            Assert.AreEqual(expectedDiameter, view.transform.localScale.y, 0.0001f);
        }

        [Test]
        public void RangeRingView_Bind_AppliesConfiguredAlpha()
        {
            RangeRingView view = NewRingView();
            view.alpha = 0.2f;

            view.Bind(radiusInCells: 2f, cellSize: 1f, sortingOrder: 1);

            var renderer = view.GetComponent<SpriteRenderer>();
            Assert.AreEqual(0.2f, renderer.color.a, 0.0001f);
        }

        [Test]
        public void RangeRingView_ShowHide_TogglesRendererEnabled()
        {
            RangeRingView view = NewRingView();
            var renderer = view.GetComponent<SpriteRenderer>();

            view.Show();
            Assert.IsTrue(renderer.enabled);

            view.Hide();
            Assert.IsFalse(renderer.enabled);
        }
    }
}
