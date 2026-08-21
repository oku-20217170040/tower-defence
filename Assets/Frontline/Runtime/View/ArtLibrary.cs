using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Birlik ve kule gorsellerinin TEK bagli noktasi.
    ///
    /// Neden var: birlikler ve kuleler bugun kod ile uretilen dairelerle
    /// ciziliyor (PrimitiveSprites). Blender asset'leri gelince her yeri
    /// tek tek degistirmek yerine buraya baglanir; sprite atanmamis her tur
    /// eski daireye duser, yani asset'ler PARCA PARCA gelebilir ve oyun
    /// hicbir anda bozulmaz.
    ///
    /// Sahnede tek bir ornegi bulunur; UnitSpawner ve TowerPlacer onu
    /// Awake'te bulur. Bulamazlarsa hicbir sey degismez - daireler devam
    /// eder. Bu bilincli: gorsel katman oyunun calismasi icin ZORUNLU
    /// olmamali.
    /// </summary>
    public sealed class ArtLibrary : MonoBehaviour
    {
        [System.Serializable]
        public struct UnitArt
        {
            public UnitType type;
            public Sprite sprite;

            [Tooltip("Sprite'in hucre boyutuna orani. 0 ise spawner'in " +
                     "varsayilani kullanilir.")]
            public float scale;

            [Tooltip("Sprite'a uygulanacak renk. Alpha 0 ise sprite kendi " +
                     "renkleriyle cizilir (Blender render'i icin dogrusu bu).")]
            public Color tint;
        }

        [System.Serializable]
        public struct TowerArt
        {
            public TowerKind kind;
            public Sprite sprite;

            [Tooltip("Sprite'in hucre boyutuna orani. 0 ise placer'in " +
                     "varsayilani kullanilir.")]
            public float scale;

            [Tooltip("Alpha 0 ise sprite kendi renkleriyle cizilir.")]
            public Color tint;
        }

        [Tooltip("Tur basina birlik gorseli. Listede olmayan tur daireye duser.")]
        public UnitArt[] units;

        [Tooltip("Tur basina kule gorseli. Listede olmayan tur daireye duser.")]
        public TowerArt[] towers;

        private static ArtLibrary instance;

        /// <summary>Sahnedeki ornek; yoksa null (daireye dusulur).</summary>
        public static ArtLibrary Instance
        {
            get
            {
                if (instance == null) instance = FindFirstObjectByType<ArtLibrary>();
                return instance;
            }
        }

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>
        /// Bu tur icin sprite var mi. Yoksa false doner ve cagiran taraf
        /// eski daire cizimini surdurur.
        /// </summary>
        public bool TryGetUnit(UnitType type, out UnitArt art)
        {
            if (units != null)
            {
                for (int i = 0; i < units.Length; i++)
                {
                    if (units[i].type != type || units[i].sprite == null) continue;

                    art = units[i];
                    return true;
                }
            }

            art = default;
            return false;
        }

        public bool TryGetTower(TowerKind kind, out TowerArt art)
        {
            if (towers != null)
            {
                for (int i = 0; i < towers.Length; i++)
                {
                    if (towers[i].kind != kind || towers[i].sprite == null) continue;

                    art = towers[i];
                    return true;
                }
            }

            art = default;
            return false;
        }
    }
}
