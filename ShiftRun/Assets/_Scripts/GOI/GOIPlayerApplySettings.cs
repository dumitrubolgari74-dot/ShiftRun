using UnityEngine;

/// <summary>
/// Applies <see cref="GameSettings"/> mass/drag/gravity to the player Rigidbody2D at runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GOIPlayerApplySettings : MonoBehaviour
{
    [SerializeField] GameSettings settings;

    public void Configure(GameSettings s) => settings = s;

    void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            Apply();
    }
#endif

    public void Apply()
    {
        if (settings == null) return;
        var rb = GetComponent<Rigidbody2D>();
        rb.mass = settings.bodyMass;
        rb.drag = settings.linearDrag;
        rb.angularDrag = settings.angularDrag;
        rb.gravityScale = settings.gravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
}
