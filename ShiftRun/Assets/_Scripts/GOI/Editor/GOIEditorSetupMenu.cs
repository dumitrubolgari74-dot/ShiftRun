#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click setup: physics materials, GameSettings asset, prefab, and prototype scene.
/// </summary>
public static class GOIEditorSetupMenu
{
    const string Root = "Assets/_Data/GOI";
    const string PrefabPath = "Assets/_Prefabs/GOI_Player.prefab";
    const string ScenePath = "Assets/_Scenes/GOI_Prototype.unity";
    const string SettingsPath = Root + "/GOI_DefaultSettings.asset";

    [MenuItem("ShiftRun/GOI/Generate prototype (materials, settings, prefab, scene)")]
    static void GenerateAll()
    {
        EnsureFolder("Assets/_Data");
        EnsureFolder("Assets/_Data/GOI");
        EnsureFolder("Assets/_Prefabs");

        var stone = GetOrCreatePhysMat(Root + "/GOI_Mat_Stone.physicsMaterial2D", 0.6f, 0f, 0, 0);
        GetOrCreatePhysMat(Root + "/GOI_Mat_Metal.physicsMaterial2D", 0.2f, 0.1f, 1, 1);
        GetOrCreatePhysMat(Root + "/GOI_Mat_Wood.physicsMaterial2D", 0.8f, 0f, 2, 0);
        var ice = GetOrCreatePhysMat(Root + "/GOI_Mat_Ice.physicsMaterial2D", 0.05f, 0f, 1, 0);
        var hammer = GetOrCreatePhysMat(Root + "/GOI_Mat_Hammer.physicsMaterial2D", 0.9f, 0.05f, 0, 1);

        var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        settings.prototypeLevelStone = stone;
        settings.prototypeLevelIce = ice;
        settings.hammerPhysicsMaterial = hammer;
        EditorUtility.SetDirty(settings);

        var tempCam = new GameObject("__TempCam__").AddComponent<Camera>();
        tempCam.orthographic = true;
        tempCam.transform.position = new Vector3(0f, 1.5f, -10f);

        var player = GOIHierarchyBuilder.BuildPlayer(settings, Vector3.zero, tempCam);
        Object.DestroyImmediate(tempCam.gameObject);

        PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
        Object.DestroyImmediate(player);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var bootGo = new GameObject("GOI_Bootstrap");
        var boot = bootGo.AddComponent<GOISceneBootstrap>();
        var so = new SerializedObject(boot);
        so.FindProperty("settings").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0f, 1.5f, -10f);
        camGo.AddComponent<SmoothCamera>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GOI] Done. Open scene {ScenePath} and press Play. Prefab: {PrefabPath}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, name);
    }

    static PhysicsMaterial2D GetOrCreatePhysMat(string path, float friction, float bounce, int frictionCombine, int bounceCombine)
    {
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D
            {
                friction = friction,
                bounciness = bounce
            };
            mat.frictionCombine = (PhysicsMaterialCombine)frictionCombine;
            mat.bounceCombine = (PhysicsMaterialCombine)bounceCombine;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.friction = friction;
            mat.bounciness = bounce;
            mat.frictionCombine = (PhysicsMaterialCombine)frictionCombine;
            mat.bounceCombine = (PhysicsMaterialCombine)bounceCombine;
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }
}
#endif
