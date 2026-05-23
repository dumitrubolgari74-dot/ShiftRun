using UnityEngine;

/// <summary>
/// Упрощённый GOI: захват по ЛКМ на земле, толчок тела по горизонтали мыши.
/// </summary>
public class SimpleGrabGoi : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D body;
    public Transform hammerTip;
    public LayerMask groundMask;

    [Header("Settings")]
    public float pushForce = 10f;
    public float maxSpeed = 8f;
    public float deadzone = 0.05f;

    [Header("Ground check")]
    public float tipProbeRadius = 0.1f;

    [Header("Optional")]
    public Transform hammerRotateRoot;
    public bool rotateHammerWhileGrabbing = true;

    Camera _cam;
    Vector2 _lastMouseWorld;
    Vector2 _grabPoint;
    bool _isGrabbing;
    float _tipRadius;
    Transform _hammerRoot;

    readonly Collider2D[] _overlapBuffer = new Collider2D[8];

    void Start()
    {
        _cam = Camera.main;

        if (body == null || hammerTip == null)
        {
            Debug.LogError("[SimpleGrabGoi] Назначьте body и hammerTip.", this);
            enabled = false;
            return;
        }

        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Default", "Ground");

        _hammerRoot = hammerRotateRoot != null ? hammerRotateRoot : hammerTip.parent;
        _tipRadius = tipProbeRadius;

        if (hammerTip.TryGetComponent(out CircleCollider2D circle))
            _tipRadius = circle.radius * hammerTip.lossyScale.x;
    }

    void Update()
    {
        if (_cam == null || body == null || hammerTip == null)
            return;

        Vector2 mouseWorld = GetMouseWorld();

        if (Input.GetMouseButtonDown(0) && IsTipGrounded())
        {
            _isGrabbing = true;
            _grabPoint = hammerTip.position;
            _lastMouseWorld = mouseWorld;
        }

        if (Input.GetMouseButtonUp(0))
            _isGrabbing = false;

        if (!_isGrabbing)
            return;

        float deltaX = mouseWorld.x - _lastMouseWorld.x;
        if (Mathf.Abs(deltaX) > deadzone)
        {
            Vector2 vel = body.velocity;
            vel.x += deltaX * pushForce * Time.deltaTime;
            vel.x = Mathf.Clamp(vel.x, -maxSpeed, maxSpeed);
            body.velocity = vel;
        }

        _lastMouseWorld = mouseWorld;

        if (rotateHammerWhileGrabbing && _hammerRoot != null)
        {
            Vector2 dir = mouseWorld - (Vector2)_grabPoint;
            if (dir.sqrMagnitude > 1e-6f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _hammerRoot.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    Vector2 GetMouseWorld()
    {
        float depth = _cam.WorldToScreenPoint(body.position).z;
        Vector3 mouse = _cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));
        return new Vector2(mouse.x, mouse.y);
    }

    bool IsTipGrounded()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            hammerTip.position, _tipRadius + 0.03f, _overlapBuffer, groundMask);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _overlapBuffer[i];
            if (col == null || IsIgnored(col))
                continue;
            return true;
        }

        return false;
    }

    bool IsIgnored(Collider2D col)
    {
        if (col.attachedRigidbody == body)
            return true;

        if (_hammerRoot != null && col.transform.IsChildOf(_hammerRoot))
            return true;

        if (col.transform == hammerTip)
            return true;

        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (hammerTip == null)
            return;

        float radius = tipProbeRadius;
        if (hammerTip.TryGetComponent(out CircleCollider2D circle))
            radius = circle.radius * hammerTip.lossyScale.x;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hammerTip.position, radius);

        if (Application.isPlaying && _isGrabbing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_grabPoint, radius * 0.5f);
            Gizmos.DrawLine(_grabPoint, _lastMouseWorld);
        }
    }
}
