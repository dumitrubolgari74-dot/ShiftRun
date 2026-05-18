using UnityEngine;

/// <summary>
/// 2D: rotator движется по окружности вокруг rotationCenter (на кольце min–max), не внутри неё.
/// </summary>
public class UmbrellaControl : MonoBehaviour
{
    [Header("Pivot")]
    [Tooltip("Центр окружности (рука / хват).")]
    public Transform rotationCenter;

    [Tooltip("Зонт — всегда ставится на окружность вокруг центра.")]
    public Transform rotator;

    [Tooltip("Устаревшее: если задано, используется как rotator.")]
    public Transform pivot;

    public Camera cam;
    public float rotationSpeed = 10f;

    [Header("Orbit ring (XY, axis Z)")]
    public float minReach = 0.4f;
    public float maxReach = 3f;
    [Tooltip("Скорость отдаления от центра (приближение — вместе с мышью).")]
    public float radiusApproachSpeed = 6f;

    [Tooltip("Доп. угол Z, если модель смотрит не вдоль +X.")]
    public float zAngleOffset = 0f;

    Rigidbody _rotatorRb;
    Vector3 _lastAimPoint;
    float _lastOrbitRadius;
    float _currentOrbitRadius;

    void Awake()
    {
        if (rotator == null)
            rotator = pivot != null ? pivot : transform;
        if (rotationCenter == null)
            rotationCenter = rotator;

        _rotatorRb = rotator.GetComponent<Rigidbody>();
        if (cam == null)
            cam = Camera.main;

        minReach = Mathf.Max(0.01f, minReach);
        maxReach = Mathf.Max(minReach, maxReach);
    }

    void Start()
    {
        _currentOrbitRadius = GetCurrentRadiusXY();
        if (_currentOrbitRadius < minReach)
            SnapRotatorOntoOrbit(minReach);
        _currentOrbitRadius = Mathf.Clamp(_currentOrbitRadius, minReach, maxReach);
    }

    float GetCurrentRadiusXY()
    {
        if (rotationCenter == null || rotator == null)
            return minReach;
        Vector3 center = rotationCenter.position;
        return new Vector2(rotator.position.x - center.x, rotator.position.y - center.y).magnitude;
    }

    void FixedUpdate()
    {
        if (rotationCenter == null || rotator == null || cam == null)
            return;

        if (!TryGetOrbitTarget(out float targetAngleZ, out float targetRadius))
            return;

        // Ближе к центру — радиус сразу как у мыши (до minReach); дальше — плавное отдаление
        if (targetRadius <= _currentOrbitRadius + 0.001f)
            _currentOrbitRadius = targetRadius;
        else
            _currentOrbitRadius = Mathf.MoveTowards(_currentOrbitRadius, targetRadius, radiusApproachSpeed * Time.fixedDeltaTime);

        _lastOrbitRadius = _currentOrbitRadius;

        float step = rotationSpeed * Time.fixedDeltaTime * 60f;
        float newZ = Mathf.MoveTowardsAngle(rotator.eulerAngles.z, targetAngleZ, step);
        float angleRad = (newZ - zAngleOffset) * Mathf.Deg2Rad;
        Vector3 center = rotationCenter.position;
        Vector3 onRing = center + new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * _currentOrbitRadius;
        _lastAimPoint = onRing;
        onRing.z = rotator.position.z;

        Quaternion targetRot = Quaternion.Euler(0f, 0f, newZ);

        if (_rotatorRb != null)
        {
            _rotatorRb.MovePosition(onRing);
            _rotatorRb.MoveRotation(targetRot);
        }
        else
        {
            rotator.SetPositionAndRotation(onRing, targetRot);
        }
    }

    /// <summary>Угол и радиус от позиции мыши (радиус = дистанция мыши, но не меньше minReach).</summary>
    bool TryGetOrbitTarget(out float angleZ, out float targetRadius)
    {
        angleZ = rotator.eulerAngles.z;
        targetRadius = minReach;

        Vector3 center = rotationCenter.position;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.forward, center);

        if (!plane.Raycast(ray, out float enter))
            return false;

        Vector3 hit = ray.GetPoint(enter);
        Vector2 toMouse = new Vector2(hit.x - center.x, hit.y - center.y);
        float mouseDist = toMouse.magnitude;

        if (mouseDist < 1e-6f)
            toMouse = Vector2.right;
        else
            toMouse /= mouseDist;

        // Радиус = дистанция мыши (1:1), не ближе minReach
        targetRadius = Mathf.Clamp(mouseDist, minReach, maxReach);
        angleZ = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg + zAngleOffset;
        return true;
    }

    void SnapRotatorOntoOrbit(float radius)
    {
        if (rotationCenter == null || rotator == null)
            return;

        Vector3 center = rotationCenter.position;
        Vector2 offset = new Vector2(rotator.position.x - center.x, rotator.position.y - center.y);
        if (offset.sqrMagnitude < 1e-6f)
            offset = Vector2.right;

        offset = offset.normalized * Mathf.Clamp(offset.magnitude, minReach, maxReach);
        if (offset.magnitude < minReach)
            offset = offset.normalized * radius;

        Vector3 pos = new Vector3(center.x + offset.x, center.y + offset.y, rotator.position.z);
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg + zAngleOffset;
        rotator.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, angle));
        _currentOrbitRadius = radius;
    }

    void OnDrawGizmos()
    {
        Transform center = rotationCenter != null ? rotationCenter : rotator;
        if (center == null)
            return;

        Vector3 c = center.position;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireSphere(c, 0.12f);

        DrawReachRingXY(c, minReach, new Color(0.3f, 1f, 0.5f, 0.35f));
        DrawReachRingXY(c, maxReach, new Color(1f, 0.35f, 0.2f, 0.5f));

        if (rotator != null && rotator != center)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(rotator.position, 0.07f);
            Gizmos.DrawLine(c, rotator.position);
        }

        if (Application.isPlaying && _lastAimPoint != default)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lastAimPoint, 0.06f);
        }

        if (Application.isPlaying && _lastOrbitRadius > 0f)
            DrawReachRingXY(c, _lastOrbitRadius, new Color(1f, 1f, 0.2f, 0.6f));
    }

    static void DrawReachRingXY(Vector3 center, float radius, Color color)
    {
        if (radius <= 0f)
            return;

        const int segments = 32;
        float z = center.z;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        Gizmos.color = color;
        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 p = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, z);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    void OnValidate()
    {
        minReach = Mathf.Max(0.01f, minReach);
        maxReach = Mathf.Max(minReach, maxReach);
    }
}
