using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Oyuncunun/seviye ureteci penceresinin secip "bununla oyna" dedigi
    /// seviyenin adi. PlayerPrefs kullanir (static alan yerine) - Unity
    /// Play moduna girerken "domain reload" acikken static alanlar sifirlanir,
    /// PlayerPrefs hem Editor Play hem gercek build'de hayatta kalir.
    /// Secim yoksa Battlefield/RunController kendi Inspector varsayilanlarini
    /// kullanmaya devam eder - bu sinif hicbir mevcut sahneyi etkilemez.
    /// </summary>
    public static class LevelSelection
    {
        private const string Key = "Frontline.SelectedLevel";

        public static string SelectedLevelName
        {
            get => PlayerPrefs.GetString(Key, string.Empty);
            set
            {
                PlayerPrefs.SetString(Key, value ?? string.Empty);
                PlayerPrefs.Save();
            }
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
        }
    }
}
