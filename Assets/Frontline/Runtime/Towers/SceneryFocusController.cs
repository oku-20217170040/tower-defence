using System.Collections.Generic;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Oyuncu bir agac/kayaya dokununca, o hucreyi menziline alan TUM
    /// kuleleri oraya kilitler (kullanici geri bildirimi, 2 Eylul 2026):
    /// "ilgili kulenin menzil alani icerisindeki agac ve taslara
    /// bastigimda kuleler buraya ates etsin, odak tamamen oraya donsun,
    /// ta ki patlayana kadar ya da tekrar oraya basip odagi alana kadar."
    ///
    /// TowerPlacer'in kurma dokunusuyla ve TowerSelection'in kule secme
    /// dokunusuyla CAKISMAZ: bu bilesen yalnizca AYAKTA ORTU olan
    /// hucrelere tepki verir (agac/kaya), digerleri bos hucreye ya da
    /// kuleye tepki verir - hicbiri ayni hucre durumunu paylasmaz.
    /// </summary>
    public sealed class SceneryFocusController : MonoBehaviour
    {
        public Battlefield world;
        public Camera targetCamera;

        [Tooltip("Kilit isareti (fx_focus_marker). Bos ise daire cizilir.")]
        public Sprite markerSprite;

        [Tooltip("Isaretin cizim sirasi. Ortunun ustunde durmali.")]
        public int markerSortingOrder = 6;

        [Tooltip("Isaretin hucreye orani.")]
        public float markerScale = 1.1f;

        /// <summary>Su an kilitli hucre. -1 = kilit yok.</summary>
        public int FocusedCell { get; private set; } = -1;

        private readonly List<TowerWeapon> lockedWeapons = new List<TowerWeapon>();
        private SpriteRenderer markerRenderer;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (targetCamera == null) targetCamera = Camera.main;

            var markerGo = new GameObject("SceneryFocusMarker");
            markerGo.transform.SetParent(transform, false);
            markerRenderer = markerGo.AddComponent<SpriteRenderer>();
            markerRenderer.enabled = false;
        }

        private void OnEnable()
        {
            if (world != null) world.SceneryCleared += OnSceneryCleared;
        }

        private void OnDisable()
        {
            if (world != null) world.SceneryCleared -= OnSceneryCleared;
        }

        private void Update()
        {
            if (world == null || world.Grid == null) return;

            if (Input.GetMouseButtonUp(0))
            {
                if (Frontline.Runtime.UI.UiTouchGuard.IsPointerOverUi()) return;

                var menu = Frontline.Runtime.UI.RadialBuildMenu.Instance;
                if (menu != null && menu.IsOpen) return;

                bool panned = MapCamera.Instance != null && MapCamera.Instance.DraggedSincePress;
                if (panned) return;

                HandleTap(Input.mousePosition);
            }
        }

        private void HandleTap(Vector3 screenPosition)
        {
            if (targetCamera == null || world.Scenery == null) return;

            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(screenPosition);

            int cell;
            if (!world.Space.WorldToCell(worldPoint, out cell)) return;

            // Ayakta ortu olmayan hucreye dokunus bu bilesenin isi degil -
            // TowerPlacer (kurma) ya da TowerSelection (kule secme) hallediyor.
            if (!world.Scenery.IsStanding(cell))
            {
                if (cell == FocusedCell) ClearFocus();
                return;
            }

            if (cell == FocusedCell)
            {
                // Ayni hucreye tekrar dokunuldu: odagi birak.
                ClearFocus();
                return;
            }

            SetFocus(cell);
        }

        /// <summary>Hucreyi menziline alan her kuleyi manuel hedefe kilitler.</summary>
        private void SetFocus(int cell)
        {
            ReleaseLockedWeapons();

            FocusedCell = cell;

            var towers = world.Towers;
            for (int i = 0; i < towers.Count; i++)
            {
                Emplacement tower = towers[i];
                if (tower == null) continue;

                TowerWeapon weapon = tower.GetComponent<TowerWeapon>();
                if (weapon == null) continue;

                if (weapon.TrySetManualTarget(cell)) lockedWeapons.Add(weapon);
            }

            markerRenderer.sprite = markerSprite != null ? markerSprite : PrimitiveSprites.Circle();
            markerRenderer.sortingOrder = markerSortingOrder;
            markerRenderer.transform.position = world.Space.CellToWorld(cell);

            float target = world.Space.CellSize * markerScale;
            Vector2 size = markerRenderer.sprite.bounds.size;
            if (size.x > 0.0001f && size.y > 0.0001f)
                markerRenderer.transform.localScale = new Vector3(target / size.x, target / size.y, 1f);

            markerRenderer.enabled = true;
        }

        private void ClearFocus()
        {
            FocusedCell = -1;
            ReleaseLockedWeapons();
            markerRenderer.enabled = false;
        }

        private void ReleaseLockedWeapons()
        {
            for (int i = 0; i < lockedWeapons.Count; i++)
                if (lockedWeapons[i] != null) lockedWeapons[i].ClearManualTarget();

            lockedWeapons.Clear();
        }

        /// <summary>
        /// Kilitli hucre baskasi tarafindan (baska bir kule, ya da
        /// TowerWeapon'un kendi hasari) yikildiginda isareti kapat.
        /// TowerWeapon zaten kendi manuel hedefini otomatik biraktigi icin
        /// burasi yalnizca GORSEL isareti temizler.
        /// </summary>
        private void OnSceneryCleared(int cell)
        {
            if (cell == FocusedCell) ClearFocus();
        }
    }
}
