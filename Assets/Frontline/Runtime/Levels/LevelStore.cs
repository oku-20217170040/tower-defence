using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// JsonUtility'nin sindirebilecegi duz sarici. Dizi JsonUtility'nin
    /// serilestirebildigi tek koleksiyon tipi (Dictionary/List kok nesne
    /// olarak olmaz) - MetaSaveData'daki ayni kisitla ayni cozum.
    /// </summary>
    [Serializable]
    internal sealed class LevelCatalogSaveData
    {
        public LevelDefinition[] levels = Array.Empty<LevelDefinition>();
    }

    /// <summary>
    /// LevelCatalog'u diske yazar ve okur. MetaProgressStore ile ayni
    /// disiplin: kayit dosyasi yoksa/bozuksa PATLAMAZ, bos bir katalogla
    /// devam eder - seviye ureteci hicbir kosulda oyunu acilmaz yapmamali.
    /// </summary>
    public static class LevelStore
    {
        public const string FileName = "levels.json";

        public static string DefaultPath =>
            Application.persistentDataPath + "/" + FileName;

        public static string LastError { get; private set; }

        public static LevelCatalog Load() => LoadFrom(DefaultPath);

        public static LevelCatalog LoadFrom(string path)
        {
            LastError = null;
            var catalog = new LevelCatalog();

            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return catalog;

                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json) || json.Trim().Length == 0)
                    return catalog;

                var data = JsonUtility.FromJson<LevelCatalogSaveData>(json);
                if (data?.levels == null) return catalog;

                foreach (var level in data.levels)
                {
                    if (level == null) continue;
                    string error;
                    // Bozuk/gecersiz bir kayit tum katalogu iptal etmesin -
                    // yalnizca o kayit atlanir.
                    if (LevelCatalog.Validate(level, out error))
                        catalog.Add(level, out error);
                }

                return catalog;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogWarning("[LevelStore] Kayit okunamadi, bos katalogla " +
                                  "devam ediliyor: " + e.Message);
                return new LevelCatalog();
            }
        }

        public static bool Save(LevelCatalog catalog) => SaveTo(catalog, DefaultPath);

        public static bool SaveTo(LevelCatalog catalog, string path)
        {
            LastError = null;
            if (catalog == null || string.IsNullOrEmpty(path)) return false;

            try
            {
                var list = new List<LevelDefinition>(catalog.Levels);
                var data = new LevelCatalogSaveData { levels = list.ToArray() };
                string json = JsonUtility.ToJson(data, true);

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogWarning("[LevelStore] Kayit yazilamadi: " + e.Message);
                return false;
            }
        }

        public static bool Clear() => ClearAt(DefaultPath);

        public static bool ClearAt(string path)
        {
            LastError = null;
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }
    }
}
