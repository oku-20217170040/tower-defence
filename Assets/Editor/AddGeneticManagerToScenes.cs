using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Tüm Level sahnelerine GeneticManager prefabını ekler.
/// Menü: Tools → Add GeneticManager To All Levels
/// </summary>
public class AddGeneticManagerToScenes : Editor
{
    [MenuItem("Tools/Add GeneticManager To All Levels")]
    static void AddToAllLevels()
    {
        // Prefabı yükle
        string prefabPath = "Assets/Prefabs/GeneticManager.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[GA Tool] Prefab bulunamadı: {prefabPath}");
            EditorUtility.DisplayDialog("Hata", $"Prefab bulunamadı:\n{prefabPath}", "Tamam");
            return;
        }

        // Level sahnelerini bul
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/Level_1.unity",
            "Assets/Scenes/Level_2.unity",
            "Assets/Scenes/Level_3.unity",
            "Assets/Scenes/Level_4.unity",
            "Assets/Scenes/Level_5.unity",
            "Assets/Scenes/Level_6.unity",
            "Assets/Scenes/Level_7.unity",
            "Assets/Scenes/Level_8.unity",
            "Assets/Scenes/Level_9.unity",
            "Assets/Scenes/Level_10.unity",
        };

        int added   = 0;
        int skipped = 0;

        // Mevcut sahneyi kaydet
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        foreach (string path in scenePaths)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[GA Tool] Sahne bulunamadı, atlandı: {path}");
                skipped++;
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            // Zaten var mı?
            GeneticManager existing = Object.FindObjectOfType<GeneticManager>();
            if (existing != null)
            {
                Debug.Log($"[GA Tool] Zaten mevcut, atlandı: {path}");
                skipped++;
                continue;
            }

            // Prefabı sahneye ekle
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.name = "GeneticManager";

            // GridManager'ı otomatik bağla
            GridManager grid = Object.FindObjectOfType<GridManager>();
            if (grid != null)
            {
                GeneticManager gm = instance.GetComponent<GeneticManager>();
                if (gm != null)
                {
                    gm.grid = grid;
                    EditorUtility.SetDirty(gm);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[GA Tool] Eklendi: {path}");
            added++;
        }

        EditorUtility.DisplayDialog(
            "Tamamlandı",
            $"GeneticManager eklendi: {added} sahne\nAtlandı: {skipped} sahne",
            "Tamam"
        );
    }
}
