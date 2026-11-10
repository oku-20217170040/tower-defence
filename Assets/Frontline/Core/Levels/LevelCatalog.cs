using System;
using System.Collections.Generic;

namespace Frontline.Core
{
    /// <summary>
    /// Seviye tanimlarinin (LevelDefinition) hafizadaki koleksiyonu ve
    /// ekleme/duzenleme/silme kurallari. Sinav edilen tek gercek is
    /// mantigi burada durur; Editor penceresi ve disk deposu (LevelStore,
    /// Runtime katmaninda) yalnizca bunu cagirir. Boylece CRUD kurallari
    /// Unity'siz, hizli EditMode testleriyle dogrulanabilir.
    /// </summary>
    public sealed class LevelCatalog
    {
        private readonly List<LevelDefinition> levels = new List<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> Levels => levels;

        public LevelDefinition Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < levels.Count; i++)
                if (levels[i].Name == name) return levels[i];
            return null;
        }

        public bool Contains(string name) => Find(name) != null;

        /// <summary>
        /// Yeni bir seviye ekler. Isim bossa, baska bosluktan ibaretse veya
        /// zaten kullanimdaysa reddedilir - kismi/belirsiz bir katalog
        /// durumu birakmamak icin (MetaState'in "kismi islem yok" kuraliyla
        /// ayni disiplin).
        /// </summary>
        public bool Add(LevelDefinition level, out string error)
        {
            if (!Validate(level, out error)) return false;

            if (Contains(level.Name))
            {
                error = "'" + level.Name + "' adinda bir seviye zaten var.";
                return false;
            }

            levels.Add(level.Clone());
            error = null;
            return true;
        }

        /// <summary>
        /// Var olan bir seviyeyi gunceller. oldName ile bulunan kayit
        /// updated'in degerleriyle (yeni ismi dahil) degistirilir. Yeni isim
        /// baska bir kayitla cakisiyorsa reddedilir.
        /// </summary>
        public bool Update(string oldName, LevelDefinition updated, out string error)
        {
            var existing = Find(oldName);
            if (existing == null)
            {
                error = "'" + oldName + "' adinda bir seviye yok.";
                return false;
            }

            if (!Validate(updated, out error)) return false;

            if (updated.Name != oldName && Contains(updated.Name))
            {
                error = "'" + updated.Name + "' adinda bir seviye zaten var.";
                return false;
            }

            int index = levels.IndexOf(existing);
            levels[index] = updated.Clone();
            error = null;
            return true;
        }

        /// <summary>Bir seviyeyi siler. Yoksa false doner, istisna atmaz.</summary>
        public bool Remove(string name)
        {
            var existing = Find(name);
            if (existing == null) return false;
            return levels.Remove(existing);
        }

        public void Clear() => levels.Clear();

        public static bool Validate(LevelDefinition level, out string error)
        {
            if (level == null) { error = "Seviye null olamaz."; return false; }
            if (string.IsNullOrEmpty(level.Name) || level.Name.Trim().Length == 0)
            { error = "Seviye adi bos olamaz."; return false; }
            if (level.Width < 5) { error = "Genislik en az 5 olmali."; return false; }
            if (level.Height < 5) { error = "Yukseklik en az 5 olmali."; return false; }
            // MapGenerator/MapValidator en az 3 koridor gerektirir (spec 9.4) -
            // dusuk bir deger burada gecse bile Battlefield.Regenerate
            // "gecerli harita uretilemedi" istisnasiyla oyunu durdurur.
            if (level.CorridorCount < 3) { error = "En az 3 koridor olmali (spec 9.4)."; return false; }
            if (level.CorridorWidth < 1) { error = "Koridor genisligi en az 1 olmali."; return false; }
            if (level.RungCount < 0) { error = "Gecit sayisi negatif olamaz."; return false; }
            if (level.SceneryPercent < 0 || level.SceneryPercent > 90)
            { error = "Ortu yuzdesi 0-90 arasinda olmali."; return false; }
            if (level.TotalWaves < 1) { error = "En az 1 dalga olmali."; return false; }
            if (level.BaseUnitsPerWave < 1) { error = "Ilk dalgada en az 1 birlik olmali."; return false; }
            if (level.BaseUnitHp <= 0f) { error = "Birlik cani pozitif olmali."; return false; }

            error = null;
            return true;
        }
    }
}
