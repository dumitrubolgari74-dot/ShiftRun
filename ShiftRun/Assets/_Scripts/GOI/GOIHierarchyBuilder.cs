using UnityEngine;

/// <summary>
/// Builds GOI player + simple test platforms at runtime (used by bootstrap + editor menu).
/// </summary>
public static class GOIHierarchyBuilder
{
    public static GameObject BuildPlayer(GameSettings settings, Vector3 position, Camera worldCamera)
    {
        var root = new GameObject("PlayerRoot");
        root.transform.position = position;
        root.layer = 0;
        root.AddComponent<GOIPlayerRootMarker>();

        var rb = root.AddComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var apply = root.AddComponent<GOIPlayerApplySettings>();
        apply.Configure(settings);

        // Pot body
        var pot = new GameObject("GorshokMesh");
        pot.transform.SetParent(root.transform, false);
        pot.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        pot.transform.localScale = Vector3.one;
        var cap = pot.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(0.85f, 1.05f);
        var spr = pot.AddComponent<SpriteRenderer>();
        spr.color = new Color(0.65f, 0.35f, 0.2f);
        spr.sprite = CreateSquareSprite();

        var human = new GameObject("HumanMesh");
        human.transform.SetParent(root.transform, false);
        human.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        human.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        var hs = human.AddComponent<SpriteRenderer>();
        hs.color = new Color(0.95f, 0.75f, 0.6f);
        hs.sprite = CreateSquareSprite();

        // Arm
        var armRoot = new GameObject("ArmIK");
        armRoot.transform.SetParent(root.transform, false);
        armRoot.transform.localPosition = new Vector3(0.1f, 0.25f, 0f);

        var shoulder = new GameObject("ShoulderPivot").transform;
        shoulder.SetParent(armRoot.transform, false);
        shoulder.localPosition = Vector3.zero;

        var elbow = new GameObject("ElbowJoint").transform;
        elbow.SetParent(armRoot.transform, false);

        var hand = new GameObject("HandTarget").transform;
        hand.SetParent(armRoot.transform, false);

        var armIK = armRoot.AddComponent<ArmIK>();
        armIK.Configure(settings, shoulder, elbow, hand);

        // Hammer
        var hammer = new GameObject("Hammer");
        hammer.transform.SetParent(hand, false);
        hammer.transform.localPosition = new Vector3(0.15f, -0.05f, 0f);

        var hammerBody = new GameObject("HammerMesh");
        hammerBody.transform.SetParent(hammer.transform, false);
        hammerBody.transform.localPosition = new Vector3(0.2f, 0.05f, 0f);
        hammerBody.transform.localScale = new Vector3(0.5f, 0.12f, 1f);
        var hammerSr = hammerBody.AddComponent<SpriteRenderer>();
        hammerSr.color = new Color(0.25f, 0.25f, 0.3f);
        hammerSr.sprite = CreateSquareSprite();

        var headGo = new GameObject("HammerHead");
        headGo.transform.SetParent(hammer.transform, false);
        headGo.transform.localPosition = new Vector3(0.45f, 0.02f, 0f);
        headGo.layer = 0;
        var circ = headGo.AddComponent<CircleCollider2D>();
        circ.radius = 0.12f;
        if (settings != null && settings.hammerPhysicsMaterial != null)
            circ.sharedMaterial = settings.hammerPhysicsMaterial;

        var contact = headGo.AddComponent<HammerHeadContact>();
        int groundMask = LayerMask.GetMask("Ground");
        if (groundMask == 0)
            groundMask = ~0;
        contact.Configure(groundMask);

        var hammerPhy = hammer.AddComponent<HammerPhysics>();
        hammerPhy.Configure(settings, rb, headGo.transform, contact);

        var input = root.AddComponent<InputHandler>();
        input.Configure(settings, worldCamera, shoulder, armIK);

        if (settings != null)
        {
            var p0 = (Vector2)shoulder.position;
            Vector2 dir = Vector2.right;
            elbow.position = p0 + dir * settings.upperArmLength;
            hand.position = elbow.position + dir * settings.lowerArmLength;
        }

        apply.Apply();
        return root;
    }

    public static void BuildPrototypePlatforms(PhysicsMaterial2D stoneMaterial, PhysicsMaterial2D iceMaterial = null)
    {
        var parent = new GameObject("GOI_LevelBlocks");
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
            groundLayer = 0;

        CreatePlatform(parent.transform, new Vector3(0f, -2f, 0f), new Vector2(16f, 1f), groundLayer, stoneMaterial);
        CreatePlatform(parent.transform, new Vector3(4f, 0.5f, 0f), new Vector2(3f, 0.5f), groundLayer, stoneMaterial);
        CreatePlatform(parent.transform, new Vector3(8f, 2.2f, 0f), new Vector2(2.5f, 0.45f), groundLayer, stoneMaterial);
        CreatePlatform(parent.transform, new Vector3(12f, 3.8f, 0f), new Vector2(2f, 0.4f), groundLayer, stoneMaterial);

        var iceMat = iceMaterial != null ? iceMaterial : stoneMaterial;
        var iceGo = CreatePlatform(parent.transform, new Vector3(6f, -0.8f, 0f), new Vector2(2.5f, 0.35f), groundLayer, iceMat);
        iceGo.name = "IceStrip";
        iceGo.transform.localEulerAngles = new Vector3(0f, 0f, 12f);
    }

    static GameObject CreatePlatform(Transform parent, Vector3 pos, Vector2 size, int layer, PhysicsMaterial2D mat)
    {
        var go = new GameObject("Platform");
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        if (mat != null)
            box.sharedMaterial = mat;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.45f, 0.42f, 0.4f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        return go;
    }

    static Sprite _square;

    static Sprite CreateSquareSprite()
    {
        if (_square != null) return _square;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);
        return _square;
    }
}
