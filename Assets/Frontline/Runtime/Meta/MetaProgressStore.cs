using System;
using System.IO;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// MetaState'i diske yazar ve okur. MonoBehaviour degildir; sahnede
    /// bir nesneye ihtiyac duymaz.
    ///
    /// Kural: kayit dosyasi YOKSA veya BOZUKSA burasi PATLAMAZ. Kalici
    /// ilerlemenin bir istisnayla oyunu acilmaz hale getirmesi kabul
    /// edilemez; her hatada temiz varsayilan durum donulur.
    /// </summary>
    public static class MetaProgressStore
    {
        public const string FileName = "meta.json";

        /// <summary>Varsayilan kayit yolu.</summary>
        public static string DefaultPath
        {
            get { return Application.persistentDataPath + "/" + FileName; }
        }

        /// <summary>Son islemde bir hata olustu mu (tani icin).</summary>
        public static string LastError { get; private set; }

        public static MetaState Load()
        {
            return LoadFrom(DefaultPath);
        }

        /// <summary>
        /// Verilen yoldan yukler. Dosya yoksa, bossa veya bozuksa temiz
        /// bir MetaState doner.
        /// </summary>
        public static MetaState LoadFrom(string path)
        {
            LastError = null;

            try
            {
                if (string.IsNullOrEmpty(path)) return new MetaState();
                if (!File.Exists(path)) return new MetaState();

                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json) || json.Trim().Length == 0)
                    return new MetaState();

                var data = JsonUtility.FromJson<MetaSaveData>(json);
                if (data == null) return new MetaState();

                return data.ToState();
            }
            catch (Exception e)
            {
                // Bozuk kayit oyunu acilmaz yapmamali; sifirdan baslanir.
                LastError = e.Message;
                Debug.LogWarning("[MetaProgressStore] Kayit okunamadi, temiz " +
                                 "durumla baslaniyor: " + e.Message);
                return new MetaState();
            }
        }

        public static bool Save(MetaState state)
        {
            return SaveTo(state, DefaultPath);
        }

        /// <summary>
        /// Verilen yola yazar. Basarisizlikta false doner ve loglar;
        /// istisna disari sizmaz.
        /// </summary>
        public static bool SaveTo(MetaState state, string path)
        {
            LastError = null;

            if (state == null) return false;
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                var data = MetaSaveData.From(state);
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
                Debug.LogWarning("[MetaProgressStore] Kayit yazilamadi: " + e.Message);
                return false;
            }
        }

        /// <summary>Kaydi siler. Dosya yoksa da true doner.</summary>
        public static bool Clear()
        {
            return ClearAt(DefaultPath);
        }

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
