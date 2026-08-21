using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tüm Level sahnelerindeki WaveSpawner'a Enemy_Flying prefabını ekler.
/// Menü: Tools → Add Flying Enemy To All Waves
/// </summary>
public class AddFlyingEnemyToWaves : Editor
{
    const string PREFAB_PATH = "Assets/Prefabs/Enemys/Enemy_Flying.prefab";

    static readonly string[] SCENE_PATHS =
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

    [MenuItem("Tools/Add Flying Enemy To All Waves")]
    static void AddToAllWaves()
    {
        GameObject flyingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (flyingPrefab == null)
        {
            EditorUtility.DisplayDialog("Hata",
                $"Prefab bulunamadı:\n{PREFAB_PATH}\n\nÖnce 'Tools → Create Flying Bat Prefab' çalıştır.",
                "Tamam");
            return;
        }

        // EnemyFlying, EnemyMover'dan türediği için GetComponent<EnemyMover>() çalışır
        EnemyMover flyingMover = flyingPrefab.GetComponent<EnemyMover>();
        if (flyingMover == null)
        {
            EditorUtility.DisplayDialog("Hata",
                "Enemy_Flying prefabında EnemyMover (veya EnemyFlying) componenti yok.\n" +
                "Prefabı kontrol et.", "Tamam");
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        int totalWaves = 0;
        int skipped    = 0;

        foreach (string scenePath in SCENE_PATHS)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[FlyingEnemy] Sahne yok, atlandı: {scenePath}");
                skipped++;
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            WaveSpawner spawner = Object.FindObjectOfType<WaveSpawner>();
            if (spawner == null || spawner.waves == null || spawner.waves.Length == 0)
            {
                Debug.LogWarning($"[FlyingEnemy] WaveSpawner bulunamadı: {scenePath}");
                skipped++;
                continue;
            }

            bool sceneChanged = false;

            foreach (WaveConfig wave in spawner.waves)
            {
                if (wave == null) continue;

                // Zaten ekli mi?
                bool alreadyAdded = false;
                if (wave.allowedEnemies != null)
                {
                    foreach (var e in wave.allowedEnemies)
                        if (e != null && e.gameObject == flyingPrefab)
                        { alreadyAdded = true; break; }
                }

                if (alreadyAdded) continue;

                // allowedEnemies dizisini genişlet
                int oldLen = wave.allowedEnemies != null ? wave.allowedEnemies.Length : 0;
                EnemyMover[] newArr = new EnemyMover[oldLen + 1];
                if (oldLen > 0)
                    wave.allowedEnemies.CopyTo(newArr, 0);
                newArr[oldLen] = flyingMover;
                wave.allowedEnemies = newArr;

                totalWaves++;
                sceneChanged = true;
            }

            if (sceneChanged)
            {
                EditorUtility.SetDirty(spawner);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[FlyingEnemy] Güncellendi: {scenePath}");
            }
        }

        EditorUtility.DisplayDialog("Tamamlandı",
            $"Flying enemy {totalWaves} wave'e eklendi.\nAtlanan sahne: {skipped}",
            "Tamam");
    }
}
