using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// FlyingBat sprite sheet'inden animasyon clip + controller + prefab oluşturur.
/// Menü: Tools → Create Flying Bat Prefab
/// </summary>
public class CreateFlyingBatPrefab : Editor
{
    const string SPRITE_PATH     = "Assets/Sprites/Enemies/FlyingBat/FlyingBat_Sheet.png";
    const string ANIM_CLIP_PATH  = "Assets/Sprites/Enemies/FlyingBat/FlyingBat_Fly.anim";
    const string CONTROLLER_PATH = "Assets/Sprites/Enemies/FlyingBat/FlyingBat.controller";
    const string PREFAB_PATH     = "Assets/Prefabs/Enemys/Enemy_Flying.prefab";

    [MenuItem("Tools/Create Flying Bat Prefab")]
    static void Create()
    {
        // ── 1. Sprite'ları yükle ──────────────────────────────────
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH)
            as Sprite[];

        if (sprites == null || sprites.Length == 0)
        {
            // Sprite modunda import edilmemiş olabilir, reimport et
            AssetDatabase.ImportAsset(SPRITE_PATH, ImportAssetOptions.ForceUpdate);
            sprites = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH) as Sprite[];
        }

        // Sadece Sprite tipindekileri al, ana texture'ı filtrele
        System.Collections.Generic.List<Sprite> sprList = new System.Collections.Generic.List<Sprite>();
        foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(SPRITE_PATH))
        {
            if (obj is Sprite s) sprList.Add(s);
        }
        sprList.Sort((a, b) => string.Compare(a.name, b.name));

        if (sprList.Count == 0)
        {
            EditorUtility.DisplayDialog("Hata",
                "Sprite bulunamadı. Unity'de FlyingBat_Sheet.png'i seç,\n" +
                "Sprite Mode: Multiple → Slice (Grid By Cell Size 256x256) → Apply yap.\n" +
                "Sonra bu menüyü tekrar çalıştır.", "Tamam");
            return;
        }

        Debug.Log($"[FlyingBat] {sprList.Count} sprite bulundu.");

        // ── 2. Animation Clip ────────────────────────────────────
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 8f;
        clip.wrapMode  = WrapMode.Loop;

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            "", typeof(SpriteRenderer), "m_Sprite");

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprList.Count];
        for (int i = 0; i < sprList.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time  = i / clip.frameRate,
                value = sprList[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        // Loop ayarı
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, ANIM_CLIP_PATH);

        // ── 3. Animator Controller ───────────────────────────────
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine sm       = layer.stateMachine;

        AnimatorState flyState = sm.AddState("Fly");
        flyState.motion = clip;
        sm.defaultState = flyState;

        AssetDatabase.SaveAssets();

        // ── 4. Prefab ────────────────────────────────────────────
        string prefabDir = Path.GetDirectoryName(PREFAB_PATH);
        if (!Directory.Exists(prefabDir))
            Directory.CreateDirectory(prefabDir);

        GameObject root = new GameObject("Enemy_Flying");

        // SpriteRenderer
        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite         = sprList[0];
        sr.sortingLayerName = "Default";
        sr.sortingOrder   = 5;

        // Animator
        Animator anim       = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // Collider (tower hedefleme için)
        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;

        // Rigidbody2D
        Rigidbody2D rb   = root.AddComponent<Rigidbody2D>();
        rb.isKinematic   = true;
        rb.gravityScale  = 0f;

        // EnemyHealth
        EnemyHealth health = root.AddComponent<EnemyHealth>();

        // EnemyFlying
        root.AddComponent<EnemyFlying>();

        // EnemyLifeTracker
        root.AddComponent<EnemyLifeTracker>();

        // Scale (diğer düşmanlarla uyumlu)
        root.transform.localScale = Vector3.one * 0.5f;

        // Prefab olarak kaydet
        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out success);
        DestroyImmediate(root);

        if (success)
        {
            Debug.Log($"[FlyingBat] Prefab oluşturuldu: {PREFAB_PATH}");
            EditorUtility.DisplayDialog("Tamamlandı",
                "Enemy_Flying.prefab oluşturuldu!\n\n" +
                "Sonraki adım:\n" +
                "• Prefab'ı aç → EnemyFlying bileşeninde moveSpeed, damageToBase ayarla\n" +
                "• WaveConfig'de allowedEnemies listesine ekle\n" +
                "• Level 6+ dalgalarında kullan",
                "Tamam");

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        }
        else
        {
            EditorUtility.DisplayDialog("Hata", "Prefab kaydedilemedi.", "Tamam");
        }

        AssetDatabase.Refresh();
    }
}
