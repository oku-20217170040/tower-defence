using System;

namespace Frontline.Core
{
    /// <summary>
    /// Bir seviyenin haritasini renklendirmek icin secilen tema. Gorsel
    /// karsiligi (ArtLibrary.SceneryTheme) Runtime katmaninda cozulur -
    /// Core motora Unity tipi sizdirmaz (bkz. Battlefield yorumu).
    /// </summary>
    public enum LevelTheme
    {
        Default = 0,
        Snow = 1
    }

    /// <summary>
    /// Seviye ureteci (Editor penceresi) ile duzenlenen, diske kaydedilen tek
    /// bir seviye tanimi. Iki grup alan tasir: harita uretim parametreleri
    /// (Battlefield'in zaten sahip oldugu MapTemplate alanlariyla birebir
    /// ayni anlam) ve dalga/zorluk parametreleri (RunController'in zaten
    /// sahip oldugu alanlarla birebir ayni anlam). Yeni bir kavram icat
    /// etmek yerine var olan iki bilesenin Inspector alanlarini disariya
    /// tasiyoruz.
    ///
    /// JsonUtility'nin sindirebilecegi sekilde yazildi: yalnizca public
    /// alanlar, ilkel tipler. [Serializable] mscorlib'tendir, Core'un
    /// noEngineReferences kuralini bozmaz (MetaSaveData ile ayni gerekce).
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public string Name = "Yeni Seviye";

        // ── Harita (Battlefield/MapTemplate ile birebir ayni anlam) ──────
        public int Width = 13;
        public int Height = 30;
        public int CorridorCount = 3;
        public int CorridorWidth = 2;
        public int RungCount = 4;
        public int SceneryPercent = 30;
        public float TreeHealth = 12f;
        public float RockHealth = 30f;
        public int Seed = 0;
        public LevelTheme Theme = LevelTheme.Default;

        // ── Dalgalar (RunController ile birebir ayni anlam) ──────────────
        public int TotalWaves = 15;
        public int BaseUnitsPerWave = 8;
        public float UnitsAddedPerWave = 1f;
        public float BaseUnitHp = 10f;
        public float HpGrowthPerWave = 0.10f;

        public LevelDefinition Clone()
        {
            return new LevelDefinition
            {
                Name = Name,
                Width = Width,
                Height = Height,
                CorridorCount = CorridorCount,
                CorridorWidth = CorridorWidth,
                RungCount = RungCount,
                SceneryPercent = SceneryPercent,
                TreeHealth = TreeHealth,
                RockHealth = RockHealth,
                Seed = Seed,
                Theme = Theme,
                TotalWaves = TotalWaves,
                BaseUnitsPerWave = BaseUnitsPerWave,
                UnitsAddedPerWave = UnitsAddedPerWave,
                BaseUnitHp = BaseUnitHp,
                HpGrowthPerWave = HpGrowthPerWave
            };
        }
    }
}
