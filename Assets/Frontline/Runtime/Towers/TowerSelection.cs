using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kule secimi: oyuncu var olan bir kulenin uzerine dokununca o kulenin
    /// menzili gorunur, digerlerininki gizli kalir (kullanici geri bildirimi,
    /// 2 Eylul 2026: "ilgili kulenin uzerine tiklasam o kulenin kapsama
    /// alanini gormek istiyorum, onun disinda gizli olsun").
    ///
    /// TowerPlacer'in KURMA dokunusuyla CAKISMAZ: TowerPlacer yalnizca BOS
    /// kurulum seridi hucresine tepki verir (IsOccupied kontroluyle dolu
    /// hucreyi reddeder), bu bilesen ise yalnizca DOLU hucreye (uzerinde
    /// kule olan) tepki verir. Ayni Update() deseniyle paralel calisirlar.
    /// </summary>
    public sealed class TowerSelection : MonoBehaviour
    {
        public Battlefield world;
        public Camera targetCamera;

        [Tooltip("Bos birakilirsa sahnede aranir. Secili kuleyi yukseltmek " +
                 "icin kullanilir (bkz. TryUpgradeSelected). 3 Eylul 2026: " +
                 "gercek bir dokunmatik yukselt/sat butonu paneli henuz yok - " +
                 "bu, sistemin CALISAN cekirdegi (Emplacement.Upgrade + " +
                 "TowerPlacer.TryUpgrade), sandbox'ta klavye 'U' ile tetiklenir.")]
        public TowerPlacer placer;

        [Tooltip("Menzil diskinin cizim sirasi. Kulenin uzerinde ama " +
                 "arayuzun altinda durmali.")]
        public int sortingOrder = 12;

        [Tooltip("Menzil disk gorseli (fx_range_ring). Bos ise placeholder daire.")]
        public Sprite ringSprite;

        /// <summary>Su an secili kule. Yoksa null. Disaridan salt okunur -
        /// ileride hedef kilitleme gibi baska sistemler buradan okuyabilir.</summary>
        public Emplacement Selected { get; private set; }

        private RangeRingView ringView;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (placer == null) placer = FindFirstObjectByType<TowerPlacer>();

            var ringGo = new GameObject("RangeRingView");
            ringGo.transform.SetParent(transform, false);
            var renderer = ringGo.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            ringView = ringGo.AddComponent<RangeRingView>();
            ringView.ringSprite = ringSprite;
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

            if (Input.GetKeyDown(KeyCode.U)) TryUpgradeSelected();
        }

        /// <summary>
        /// Secili kuleyi bir kademe yukseltmeyi dener (bkz. TowerPlacer.
        /// TryUpgrade). Secim yoksa ya da placer baglanamadiysa hicbir sey
        /// yapmaz. Menzil halkasi yeniden baglanir: kule buyudugunde/hasar
        /// degistiginde disk ayni kalsa da secim gorseli guncel kalir.
        /// </summary>
        public bool TryUpgradeSelected()
        {
            if (Selected == null || placer == null) return false;

            bool upgraded = placer.TryUpgrade(Selected);
            if (upgraded) ApplySelection(Selected);
            return upgraded;
        }

        /// <summary>
        /// Secili kuleyi satmayi dener (bkz. TowerPlacer.TrySell). Kule
        /// GameObject'i yok edildigi icin secim de kalkmali - aksi halde
        /// Selected sahnede olmayan bir nesneyi gosterirdi.
        /// </summary>
        public bool TrySellSelected()
        {
            if (Selected == null || placer == null) return false;

            bool sold = placer.TrySell(Selected);
            if (sold) ApplySelection(null);
            return sold;
        }

        private void HandleTap(Vector3 screenPosition)
        {
            if (targetCamera == null) return;

            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(screenPosition);

            int cell;
            if (!world.Space.WorldToCell(worldPoint, out cell))
            {
                // Izgara disina dokunmak da bos yere dokunmak gibi sayilir:
                // secim varsa kalksin.
                ApplySelection(ResolveSelection(Selected, null));
                return;
            }

            Emplacement tapped = FindTowerAt(cell);

            // Bos hucreye dokunuldu ve secim de yoktu: hicbir sey yapma -
            // TowerPlacer'in kurma dokunusuyla karismasin, gereksiz is olmasin.
            if (tapped == null && Selected == null) return;

            ApplySelection(ResolveSelection(Selected, tapped));
        }

        private Emplacement FindTowerAt(int cell)
        {
            var towers = world.Towers;
            for (int i = 0; i < towers.Count; i++)
                if (towers[i].Cell == cell) return towers[i];
            return null;
        }

        /// <summary>
        /// Saf secim mantigi - Unity yasam dongusunden bagimsiz, birim
        /// testiyle dogrudan sinanabilir.
        ///
        /// - tapped null (bos hucre / izgara disi): secim kalkar.
        /// - tapped == current (ayni kuleye tekrar dokunuldu): secim kalkar.
        /// - tapped != current (baska/ilk kule): secim ona kayar.
        /// </summary>
        public static Emplacement ResolveSelection(Emplacement current, Emplacement tapped)
        {
            if (tapped == null) return null;
            if (tapped == current) return null;
            return tapped;
        }

        private void ApplySelection(Emplacement next)
        {
            Selected = next;

            if (Selected == null)
            {
                ringView.Hide();
                return;
            }

            ringView.transform.position = world.Space.CellToWorld(Selected.Cell);
            ringView.Bind(Selected.DisplayRange, world.Space.CellSize, sortingOrder);
            ringView.Show();
        }
    }
}
