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

            [Tooltip("DONEN ust yapi: namlu / anten / tabla.")]
            public Sprite sprite;

            [Tooltip("Sprite'in namlusu hangi yone cizilmis? Derece cinsinden " +
                     "duzeltme. Hazir asset paketlerinde her sayfa baska bir " +
                     "yon kullaniyor - top namluyu YUKARI, makineli SOLA " +
                     "ciziyor - ve tek bir kural hepsine uymuyor. " +
                     "0 = namlu asagi (-Y), 180 = yukari (+Y), 90 = sola (-X).")]
            public float rotationOffset;

            [Tooltip("SABIT kaide. Donmez. Bos birakilirsa kule yalnizca " +
                     "ust yapidan ibaret olur.")]
            public Sprite platform;

            [Tooltip("Sprite'in hucre boyutuna orani. 0 ise placer'in " +
                     "varsayilani kullanilir.")]
            public float scale;

            [Tooltip("Alpha 0 ise sprite kendi renkleriyle cizilir.")]
            public Color tint;
        }

        /// <summary>
        /// Kule turune gore ATIS gorselleri: ucan mermi, namlu cakmasi ve
        /// carpma patlamasi. Kule sprite'lariyla ayni kural gecerli -
        /// atanmayan her sprite daireye duser, yani fx asset'leri de
        /// parca parca gelebilir.
        /// </summary>
        [System.Serializable]
        public struct FxArt
        {
            public TowerKind kind;

            [Tooltip("Ucan mermi. Isinda bos birakilir - isinin mermisi yok.")]
            public Sprite bullet;

            [Tooltip("Namlu ucundaki cakma.")]
            public Sprite muzzle;

            [Tooltip("Hedefte patlayan carpma efekti.")]
            public Sprite impact;
        }

        [Tooltip("Tur basina birlik gorseli. Listede olmayan tur daireye duser.")]
        public UnitArt[] units;

        [Tooltip("Tur basina kule gorseli. Listede olmayan tur daireye duser.")]
        public TowerArt[] towers;

        [Tooltip("Tur basina atis gorselleri (mermi / namlu cakmasi / carpma).")]
        public FxArt[] fx;

        [Header("Us")]
        [Tooltip("Us sprite'i. Bos ise us cizilmez - oyun yine calisir.")]
        public Sprite baseSprite;

        [Tooltip("Usun hucre cinsinden genisligi. Us bir KULE degil bir " +
                 "YERDIR; 1 hucrede kule gibi durur.")]
        public float baseScale = 2.2f;

        [Tooltip("Alpha 0 ise sprite kendi renkleriyle cizilir.")]
        public Color baseTint;

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

        /// <summary>Bu kule turunun atis gorselleri. Yoksa false.</summary>
        public bool TryGetFx(TowerKind kind, out FxArt art)
        {
            if (fx != null)
            {
                for (int i = 0; i < fx.Length; i++)
                {
                    if (fx[i].kind != kind) continue;

                    art = fx[i];
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
