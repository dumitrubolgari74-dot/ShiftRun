#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Убирает GOI-скрипты с PlayerBlackOut и настраивает 2D физику (EnvScripts/PlayerControl).
/// </summary>
public static class BlackOutTo2DSetup
{
    const string PlayerRootName = "PlayerBlackOut";

    [MenuItem("GOI/Convert PlayerBlackOut to 2D (EnvScripts)")]
    public static void ConvertFromMenu()
    {
        var root = GameObject.Find(PlayerRootName);
        if (root == null)
        {
            Debug.LogError($"[2D] В сцене нет объекта '{PlayerRootName}'.");
            return;
        }

        Convert(root);
        MarkDirty(root);
        Debug.Log("[2D] PlayerBlackOut переведён на PlayerControl + Rigidbody2D.", root);
    }

    [MenuItem("GOI/Layout Hammer Visual (PlayerControl)")]
    public static void LayoutHammerVisualFromMenu()
    {
        var control = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<PlayerControl>()
            : null;
        if (control == null)
        {
            Debug.LogWarning("[2D] Выбери объект с PlayerControl.");
            return;
        }

        LayoutHammerVisual(control);
        MarkDirty(control.gameObject);
        Debug.Log("[2D] Hammer visual layout применён.", control);
    }

    public static void LayoutHammerVisual(PlayerControl control)
    {
        if (control == null || control.hammerHead == null)
            return;

        Transform visual = control.hammerHandle ?? control.hammerHead.Find("HammerHandler");
        if (visual == null)
            return;

        Transform hammerPivot = control.hammerPivot;
        if (hammerPivot == null)
        {
            hammerPivot = control.hammerHead.Find("HammerPivot")
                          ?? control.hammerHead.Find("HammerAnchor");
            if (hammerPivot == null)
            {
                var go = new GameObject("HammerPivot");
                hammerPivot = go.transform;
                hammerPivot.SetParent(control.hammerHead, false);
            }

            control.hammerPivot = hammerPivot;
        }

        float halfLen = visual.localScale.x * 0.5f;
        float startX = visual.localPosition.x - halfLen;
        const float tipX = 0f;

        hammerPivot.localPosition = new Vector3(startX, visual.localPosition.y, 0f);

        float length = tipX - startX;
        if (length < 0.01f)
            length = 0.01f;

        visual.localPosition = new Vector3(
            (startX + tipX) * 0.5f,
            visual.localPosition.y,
            0f);
        visual.localScale = new Vector3(length, visual.localScale.y, visual.localScale.z);
    }

    public static void Convert(GameObject root)
    {
        StripGoiComponents(root);

        Transform bodyT = root.transform.Find("Body");
        if (bodyT == null)
        {
            Debug.LogError("[2D] Нет дочернего Body.", root);
            return;
        }

        Transform shoulder = bodyT.Find("Shoulder") ?? bodyT.Find("HammerAnchor");
        Transform hammerT = bodyT.Find("HammerHead");
        if (hammerT == null && shoulder != null)
            hammerT = shoulder.Find("HammerHead");

        if (hammerT == null)
        {
            Debug.LogError("[2D] Нет HammerHead.", root);
            return;
        }

        if (shoulder != null)
        {
            hammerT.SetParent(bodyT, true);
            Object.DestroyImmediate(shoulder.gameObject);
        }

        ConfigureBody(bodyT.gameObject);
        ConfigureHammer(hammerT.gameObject);
        StripGoiFromChildren(bodyT);

        Transform bodyPivot = bodyT.Find("BodyPivot") ?? CreateBodyPivot(bodyT);
        Transform hammerPivot = hammerT.Find("HammerPivot") ?? hammerT.Find("HammerAnchor");
        Transform hammerHandle = hammerT.Find("HammerHandler") ?? hammerT.Find("HammerHandle");

        var control = root.GetComponent<PlayerControl>() ?? root.AddComponent<PlayerControl>();
        control.body = bodyT;
        control.hammerHead = hammerT;
        control.bodyPivot = bodyPivot;
        control.hammerPivot = hammerPivot;
        control.hammerHandle = hammerHandle;
        if (control.maxRange < 2.5f)
            control.maxRange = 3.19f;

        ConfigureHands(bodyT, control);
        ConfigureCamera(bodyT);
        ConfigureGround();
    }

    static void ConfigureHands(Transform bodyT, PlayerControl control)
    {
        ConfigureHand(bodyT, "LeftHand", false, control);
        ConfigureHand(bodyT, "RightHand", true, control);
    }

    static void ConfigureHand(
        Transform bodyT,
        string handName,
        bool rightHand,
        PlayerControl control)
    {
        Transform handT = bodyT.Find(handName);
        if (handT == null)
            return;

        var hand = handT.GetComponent<Hand>() ?? handT.gameObject.AddComponent<Hand>();
        hand.hammerHandle = control.hammerHandle;
        hand.rightHand = rightHand;
    }

    static void StripGoiComponents(GameObject go)
    {
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null)
                continue;
            string name = mb.GetType().Name;
            if (name.StartsWith("Goi"))
                Object.DestroyImmediate(mb);
        }
    }

    static void StripGoiFromChildren(Transform body)
    {
        foreach (var mb in body.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null)
                continue;
            string name = mb.GetType().Name;
            if (name.StartsWith("Goi"))
                Object.DestroyImmediate(mb);
        }
    }

    static void ConfigureBody(GameObject body)
    {
        Strip3DPhysics(body);

        var rb = body.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = body.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 5f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.8f;
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(
            "Assets/_Scenes/TestEnv/EnvScripts/BodyPhysicsMaterial.physicsMaterial2D");
        if (mat != null)
            rb.sharedMaterial = mat;

        var col = body.GetComponent<CircleCollider2D>();
        if (col == null)
            col = body.AddComponent<CircleCollider2D>();

        col.radius = 0.55f;
        col.offset = new Vector2(0f, 0.23f);
        if (mat != null)
            col.sharedMaterial = mat;
    }

    static void ConfigureHammer(GameObject hammer)
    {
        Strip3DPhysics(hammer);

        var rb = hammer.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = hammer.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 0.05f;
        rb.gravityScale = 0f;
        rb.drag = 0.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(
            "Assets/_Scenes/TestEnv/EnvScripts/HammerMaterial.physicsMaterial2D");
        if (mat != null)
            rb.sharedMaterial = mat;

        foreach (var c3d in hammer.GetComponents<Collider>())
            Object.DestroyImmediate(c3d);

        var col = hammer.GetComponent<CircleCollider2D>();
        if (col == null)
            col = hammer.AddComponent<CircleCollider2D>();

        col.radius = 0.35f;
        if (mat != null)
            col.sharedMaterial = mat;
    }

    static void Strip3DPhysics(GameObject go)
    {
        foreach (var rb in go.GetComponents<Rigidbody>())
            Object.DestroyImmediate(rb);
        foreach (var ab in go.GetComponents<ArticulationBody>())
            Object.DestroyImmediate(ab);
        foreach (var j in go.GetComponents<Joint>())
            Object.DestroyImmediate(j);
        foreach (var c in go.GetComponents<Collider>())
            Object.DestroyImmediate(c);
    }

    static void ConfigureCamera(Transform body)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        foreach (var mb in cam.GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name.StartsWith("Goi"))
                Object.DestroyImmediate(mb);
        }

        var follow = cam.GetComponent<CameraFollowObject>() ?? cam.gameObject.AddComponent<CameraFollowObject>();
        follow.target = body;
    }

    static Transform CreateBodyPivot(Transform body)
    {
        var go = new GameObject("BodyPivot");
        go.transform.SetParent(body, false);
        go.transform.localPosition = new Vector3(-0.45f, 0.12f, 0f);
        return go.transform;
    }

    static void ConfigureGround()
    {
        var ground = GameObject.Find("Ground");
        if (ground == null)
            return;

        if (ground.GetComponent<BoxCollider2D>() == null)
        {
            var box = ground.AddComponent<BoxCollider2D>();
            box.size = new Vector2(11.35f, 1f);
        }

        var rb = ground.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = ground.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    static void MarkDirty(GameObject root)
    {
        EditorUtility.SetDirty(root);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
