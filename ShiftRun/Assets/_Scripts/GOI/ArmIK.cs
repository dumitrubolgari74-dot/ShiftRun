using UnityEngine;

/// <summary>
/// Two-bone IK in 2D (FABRIK, one full cycle). Drives elbow and hand transforms in LateUpdate.
/// </summary>
public class ArmIK : MonoBehaviour
{
    [SerializeField] GameSettings settings;
    [SerializeField] Transform shoulderAnchor;
    [SerializeField] Transform elbowTransform;
    [SerializeField] Transform handTransform;

    Vector2 _target;

    public void Configure(GameSettings s, Transform shoulder, Transform elbow, Transform hand)
    {
        settings = s;
        shoulderAnchor = shoulder;
        elbowTransform = elbow;
        handTransform = hand;
    }

    public void SetTarget(Vector2 worldTarget)
    {
        _target = worldTarget;
    }

    public Transform HandTransform => handTransform;

    void LateUpdate()
    {
        if (settings == null || shoulderAnchor == null || elbowTransform == null || handTransform == null)
            return;

        float a = settings.upperArmLength;
        float b = settings.lowerArmLength;
        float bendSign = Mathf.Sign(settings.elbowBendSign);
        if (Mathf.Approximately(bendSign, 0f))
            bendSign = -1f;

        Vector2 p0 = shoulderAnchor.position;
        Vector2 p1 = elbowTransform.position;
        Vector2 p2 = handTransform.position;
        Vector2 t = _target;

        float maxReach = Mathf.Max(0.01f, a + b - 0.01f);
        Vector2 toT = t - p0;
        float dist = toT.magnitude;
        if (dist > maxReach)
            t = p0 + toT.normalized * maxReach;

        // Backward: fix hand on target, pull elbow toward hand
        p2 = t;
        Vector2 dir21 = (p1 - p2).normalized;
        p1 = p2 + dir21 * b;

        // Pull shoulder side — shoulder is fixed at p0
        Vector2 dir10 = (p0 - p1).normalized;
        p1 = p0 + dir10 * a;

        // Forward: extend from shoulder
        Vector2 dir01 = (p1 - p0).normalized;
        p1 = p0 + dir01 * a;
        Vector2 dir12 = (t - p1).normalized;
        p2 = p1 + dir12 * b;

        // Choose elbow bend side (stay on one side of the shoulder–target line)
        Vector2 st = (t - p0);
        if (st.sqrMagnitude > 1e-6f)
        {
            float perp = (st.x * dir01.y - st.y * dir01.x);
            if (Mathf.Sign(perp) != bendSign && perp != 0f)
            {
                dir01 = Rotate2D(dir01, bendSign * Mathf.Sign(perp) * 0.02f);
                p1 = p0 + dir01.normalized * a;
                dir12 = (t - p1).normalized;
                p2 = p1 + dir12 * b;
            }
        }

        elbowTransform.position = p1;
        handTransform.position = p2;
    }

    static Vector2 Rotate2D(Vector2 v, float radians)
    {
        float c = Mathf.Cos(radians);
        float s = Mathf.Sin(radians);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
