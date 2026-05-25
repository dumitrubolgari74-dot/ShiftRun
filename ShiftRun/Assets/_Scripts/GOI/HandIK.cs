using UnityEngine;

[DefaultExecutionOrder(50)]
public class HandIK : MonoBehaviour
{
    public bool right;
    public Transform hand1;
    public Transform hand2;
    public Transform target;

    [Tooltip("Подтянуть hand2 по Z к target (1 = точное совпадение).")]
    [Range(0f, 1f)]
    public float matchTargetZ = 1f;

    float _lenUpper;
    float _lenLower;

    void Awake()
    {
        if (hand1 == null)
            hand1 = transform;
        if (hand2 == null && hand1 != null && hand1.childCount > 0)
            hand2 = hand1.GetChild(0);

        CacheLengths();
    }

    void OnValidate()
    {
        if (hand1 == null)
            hand1 = transform;
        if (hand2 == null && hand1 != null && hand1.childCount > 0)
            hand2 = hand1.GetChild(0);
    }

    void CacheLengths()
    {
        if (hand1 == null || hand2 == null)
            return;

        _lenUpper = Vector2.Distance(hand1.position, hand2.position);
        Transform wrist = hand2.childCount > 0 ? hand2.GetChild(0) : hand2;
        _lenLower = Vector2.Distance(hand2.position, wrist.position);
    }

    void LateUpdate()
    {
        if (target == null || hand1 == null || hand2 == null)
            return;

        if (_lenUpper < 1e-5f || _lenLower < 1e-5f)
            CacheLengths();

        if (_lenUpper < 1e-5f || _lenLower < 1e-5f)
            return;

        Vector2 shoulder = hand1.position;
        Vector2 goal = target.position;
        float bend = right ? -1f : 1f;

        SolveTwoBone(shoulder, goal, _lenUpper, _lenLower, bend, out Vector2 elbow);

        AimBone(hand1, shoulder, elbow);
        AimBone(hand2, hand2.position, goal);
        MatchHand2ZToTarget();
    }

    void MatchHand2ZToTarget()
    {
        if (matchTargetZ <= 0f)
            return;

        Vector3 p = hand2.position;
        float targetZ = target.position.z;
        p.z = Mathf.Lerp(p.z, targetZ, matchTargetZ);
        hand2.position = p;
    }

    static void SolveTwoBone(
        Vector2 shoulder,
        Vector2 goal,
        float lenUpper,
        float lenLower,
        float bendSign,
        out Vector2 elbow)
    {
        Vector2 toGoal = goal - shoulder;
        float dist = toGoal.magnitude;
        float maxReach = lenUpper + lenLower - 1e-4f;

        if (dist < 1e-6f)
        {
            elbow = shoulder + Vector2.right * lenUpper;
            return;
        }

        if (dist > maxReach)
            goal = shoulder + toGoal / dist * maxReach;

        toGoal = goal - shoulder;
        dist = toGoal.magnitude;

        float cosShoulder = (lenUpper * lenUpper + dist * dist - lenLower * lenLower)
                            / (2f * lenUpper * dist);
        float shoulderOffset = Mathf.Acos(Mathf.Clamp(cosShoulder, -1f, 1f));
        float shoulderAngle = Mathf.Atan2(toGoal.y, toGoal.x) - bendSign * shoulderOffset;
        elbow = shoulder + new Vector2(Mathf.Cos(shoulderAngle), Mathf.Sin(shoulderAngle)) * lenUpper;
    }

    static void AimBone(Transform bone, Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        if (dir.sqrMagnitude < 1e-8f)
            return;

        float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        bone.rotation = Quaternion.Euler(0f, 0f, z);
    }

    void OnDrawGizmosSelected()
    {
        if (hand1 == null || hand2 == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(hand1.position, hand2.position);
        if (hand2.childCount > 0)
            Gizmos.DrawLine(hand2.position, hand2.GetChild(0).position);

        if (target == null)
            return;

        Gizmos.color = right ? Color.green : Color.magenta;
        Gizmos.DrawWireSphere(target.position, 0.05f);
        Gizmos.DrawLine(hand2.position, target.position);
    }
}
