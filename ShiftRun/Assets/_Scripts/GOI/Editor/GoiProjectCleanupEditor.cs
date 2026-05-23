#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Меню GOI/* для финальной настройки сцены после реорганизации папок.
/// </summary>
public static class GoiProjectCleanupEditor
{
    const string TestScenePath = "Assets/_Scenes/Game/Test.unity";
    const string PlayerPrefabPath = "Assets/_Prefabs/Player.prefab";
    const int GroundLayer = 6;

    [MenuItem("GOI/Fix Stone Colliders (Scene + Prefabs)")]
    public static void FixStonesMenu()
    {
        FixStoneCollidersInScene();
        FixStonePrefabsInProject();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("GOI/Apply Project Cleanup (Scene + Stones)")]
    public static void ApplyProjectCleanup()
    {
        if (!System.IO.File.Exists(TestScenePath))
        {
            Debug.LogError($"[GOI Cleanup] Сцена не найдена: {TestScenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        ReplaceInlinePlayerWithPrefab();
        RemoveStrayHandIkComponents();
        FixStoneCollidersInScene();
        FixStonePrefabsInProject();
        WireMainCamera();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GOI Cleanup] Готово: Player prefab, камни 2D, камера, лишний HandIK.");
    }

    [MenuItem("GOI/Replace Player With Prefab Only")]
    public static void ReplacePlayerWithPrefabOnly()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;
        ReplaceInlinePlayerWithPrefab();
        WireMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static void ReplaceInlinePlayerWithPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GOI Cleanup] Prefab не найден: {PlayerPrefabPath}");
            return;
        }

        Vector3 spawnPos = new Vector3(63.09f, 2.51f, -0.021f);
        Transform existingPlayer = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Player")
            {
                existingPlayer = root.transform;
                spawnPos = root.position;
                break;
            }
        }

        if (existingPlayer != null)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(existingPlayer.gameObject))
            {
                Debug.Log("[GOI Cleanup] Player уже prefab instance — пропуск.");
                return;
            }

            Object.DestroyImmediate(existingPlayer.gameObject);
        }

        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = spawnPos;
        Undo.RegisterCreatedObjectUndo(player, "Create Player Prefab");
        Debug.Log("[GOI Cleanup] Player заменён на prefab instance.");
    }

    static void WireMainCamera()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        var follow = cam.GetComponent<CameraFollowObject>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollowObject>();

        var body = GameObject.Find("Body")?.transform;
        if (body == null)
        {
            var goi = Object.FindFirstObjectByType<NewGoiController>();
            if (goi != null)
                body = goi.body;
        }

        if (body != null)
        {
            follow.target = body;
            EditorUtility.SetDirty(follow);
        }
    }

    static void RemoveStrayHandIkComponents()
    {
        int removed = 0;
        foreach (var ik in Object.FindObjectsByType<HandIK>(FindObjectsSortMode.None))
        {
            if (ik.gameObject.name is "Left1" or "Right1")
                continue;

            Object.DestroyImmediate(ik);
            removed++;
        }

        if (removed > 0)
            Debug.Log($"[GOI Cleanup] Удалено лишних HandIK: {removed}");
    }

    static void FixStoneCollidersInScene()
    {
        int fixedCount = 0;
        var groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(
            "Assets/_Materials/Physics2D/GroundPhysicsMaterial.physicsMaterial2D");

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            fixedCount += FixStoneRecursive(root.transform, groundMat);

        if (fixedCount > 0)
            Debug.Log($"[GOI Cleanup] Камни в сцене с Collider2D: {fixedCount}");
    }

    static void FixStonePrefabsInProject()
    {
        var groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(
            "Assets/_Materials/Physics2D/GroundPhysicsMaterial.physicsMaterial2D");
        int count = 0;
        foreach (var path in AssetDatabase.FindAssets("piatra t:Prefab"))
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(path);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            count += FixStoneRecursive(root.transform, groundMat);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        if (count > 0)
            Debug.Log($"[GOI Cleanup] Камни в prefab: {count}");
    }

    static int FixStoneRecursive(Transform t, PhysicsMaterial2D groundMat)
    {
        int count = 0;
        if (t.name.StartsWith("piatra"))
        {
            t.gameObject.layer = GroundLayer;

            var meshCol = t.GetComponent<MeshCollider>();
            if (meshCol != null)
                Object.DestroyImmediate(meshCol);

            var col = t.GetComponent<Collider2D>();
            if (col == null)
            {
                var box = t.gameObject.AddComponent<BoxCollider2D>();
                var scale = t.lossyScale;
                box.size = new Vector2(Mathf.Max(0.5f, Mathf.Abs(scale.x)), Mathf.Max(0.5f, Mathf.Abs(scale.y)));
                col = box;
            }

            if (groundMat != null)
                col.sharedMaterial = groundMat;

            count++;
        }

        for (int i = 0; i < t.childCount; i++)
            count += FixStoneRecursive(t.GetChild(i), groundMat);

        return count;
    }
}
#endif
