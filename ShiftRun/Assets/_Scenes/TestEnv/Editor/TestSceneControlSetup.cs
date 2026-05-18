#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click wiring for Test.unity (ShiftRun / Test / Apply control wiring).
/// </summary>
public static class TestSceneControlSetup
{
    const string ScenePath = "Assets/_Scenes/TestEnv/Test.unity";
    const int GroundLayer = 6;

    [MenuItem("ShiftRun/Test/Apply control wiring")]
    public static void Apply()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogError($"[Test] Scene not found: {ScenePath}");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath);

        var human = GameObject.Find("Human");
        var pivot = GameObject.Find("UmbrellaPivot");
        var tip = pivot != null ? FindChildRecursive(pivot.transform, "Tip") : null;
        var cam = Camera.main;

        if (human == null || pivot == null || tip == null || cam == null)
        {
            Debug.LogError("[Test] Need Human, UmbrellaPivot, Tip, and Main Camera.");
            return;
        }

        var grip = FindChildRecursive(human.transform, "CenterOfHumman")
                   ?? FindChildRecursive(human.transform, "CenterOfMass")
                   ?? human.transform;

        var humanRb = human.GetComponent<Rigidbody>();
        if (humanRb == null)
            humanRb = human.AddComponent<Rigidbody>();
        humanRb.mass = 8f;
        humanRb.useGravity = true;
        humanRb.constraints = RigidbodyConstraints.FreezeRotation;
        if (human.GetComponent<CapsuleCollider>() == null)
            human.AddComponent<CapsuleCollider>();

        var pivotRb = pivot.GetComponent<Rigidbody>();
        if (pivotRb == null)
            pivotRb = pivot.AddComponent<Rigidbody>();
        pivotRb.mass = 1f;
        pivotRb.useGravity = false;
        pivotRb.isKinematic = true;
        pivotRb.interpolation = RigidbodyInterpolation.Interpolate;

        var joint = pivot.GetComponent<ConfigurableJoint>();
        if (joint == null)
            joint = pivot.AddComponent<ConfigurableJoint>();
        joint.connectedBody = humanRb;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.lowAngularXLimit = new SoftJointLimit { limit = -60f };
        joint.highAngularXLimit = new SoftJointLimit { limit = 60f };
        joint.angularYLimit = new SoftJointLimit { limit = 45f };

        var control = pivot.GetComponent<UmbrellaControl>();
        if (control == null)
            control = pivot.AddComponent<UmbrellaControl>();
        control.rotationCenter = grip;
        control.rotator = pivot.transform;
        control.pivot = null;
        control.cam = cam;
        control.minReach = 0.95f;
        control.maxReach = 2.66f;
        control.rotationSpeed = 10f;
        control.zAngleOffset = 0f;
        control.radiusApproachSpeed = 6f;

        var physics = pivot.GetComponent<UmbrellaPhysics>();
        if (physics == null)
            physics = pivot.AddComponent<UmbrellaPhysics>();
        physics.playerRb = humanRb;
        physics.tip = tip;
        physics.umbrellaControl = control;
        physics.forceMultiplier = 25f;
        physics.rayLength = control.maxReach;
        physics.groundLayers = 1 << GroundLayer;

        tip.localRotation = Quaternion.Euler(90f, 0f, 0f);

        AssignGroundLayerToStones();

        EditorUtility.SetDirty(pivot);
        EditorUtility.SetDirty(human);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Test] Control wiring applied and scene saved.");
    }

    static void AssignGroundLayerToStones()
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.StartsWith("piatra"))
                SetLayerRecursive(root, GroundLayer);
        }
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    static Transform FindChildRecursive(Transform root, string childName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName)
                return t;
        }

        return null;
    }
}
#endif
