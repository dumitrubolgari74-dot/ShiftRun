using UnityEngine;

/// <summary>
/// 2D GOI: velocity + Transform rotation. HammerPivot (anchor) → BodyPivot, tip (collider) → maxRange.
/// </summary>
[DefaultExecutionOrder(0)]
public class PlayerControl : MonoBehaviour
{
    public Transform hammerHead;
    public Transform body;

    [Tooltip("Центр окружности мыши и хват (BodyPivot).")]
    public Transform bodyPivot;

    [Tooltip("Anchor — начало древка (смотрит на BodyPivot). HammerPivot.")]
    public Transform hammerPivot;

    [Tooltip("Визуал древка (HammerHandler). Layout только через меню GOI.")]
    public Transform hammerHandle;

    [Tooltip("Доп. лимит радиуса tip от BodyPivot. 0 = только длина древка.")]
    public float maxRange;

    [Tooltip("Радиус орбиты = длина древка (HammerPivot → tip).")]
    public bool maxRangeFromShaft = true;

    [Tooltip("mouseVec от BodyPivot, не от центра экрана.")]
    public bool mouseFromBodyPivot = true;

    [Tooltip("Отвязать HammerHead от Body при старте (два Dynamic RB2D без jitter).")]
    public bool detachHammerAtStart = true;

    public float hammerMaxSpeed = 18f;

    [Range(0.01f, 1f)]
    public float hammerVelocityBlend = 0.2f;

    [Tooltip("Подгонка меша, градусы.")]
    public float hammerAngleOffset;

    public float maxBodySpeed = 8f;

    [Range(0.01f, 1f)]
    public float pushVelocityBlend = 0.15f;

    public float pushDeadzone = 0.08f;

    [Tooltip("Кадры grounded после отрыва от земли.")]
    public int coyoteFrames = 2;

    public LayerMask groundLayers;

    public bool IsGrounded => _isGrounded;

    Rigidbody2D _bodyRb;
    Rigidbody2D _hammerRb;
    Collider2D _hammerCol;

    bool _isGrounded;
    int _coyoteCounter;

    Vector2 _pivotOffsetFromBody;
    float _shaftWorldLength;
    Vector3 _hammerHeadBaseScale;

    void Awake()
    {
        if (groundLayers.value == 0)
            groundLayers = LayerMask.GetMask("Ground");
    }

    void Start()
    {
        _bodyRb = body != null ? body.GetComponent<Rigidbody2D>() : null;
        _hammerRb = hammerHead != null ? hammerHead.GetComponent<Rigidbody2D>() : null;

        if (_bodyRb == null || _hammerRb == null)
        {
            Debug.LogError("[PlayerControl] Нужны Rigidbody2D на Body и HammerHead.", this);
            return;
        }

        if (detachHammerAtStart)
            DetachHammerFromBody();

        _hammerCol = hammerHead.GetComponent<Collider2D>();
        if (hammerHead != null)
            _hammerHeadBaseScale = hammerHead.localScale;

        CacheHammerAxis();
        CachePivotOffset();

        var bodyCol = body.GetComponent<Collider2D>();
        if (bodyCol != null && _hammerCol != null)
            Physics2D.IgnoreCollision(bodyCol, _hammerCol);
    }

    void DetachHammerFromBody()
    {
        if (hammerHead == null || body == null)
            return;

        if (hammerHead.parent == body)
            hammerHead.SetParent(transform, true);
    }

    void CacheHammerAxis()
    {
        if (hammerHead == null || hammerPivot == null)
        {
            _shaftWorldLength = 1f;
            return;
        }

        Vector2 anchorToTip = -(Vector2)hammerPivot.localPosition;
        if (anchorToTip.sqrMagnitude < 1e-8f)
            anchorToTip = Vector2.right;

        _shaftWorldLength = anchorToTip.magnitude * hammerHead.lossyScale.x;
        if (_shaftWorldLength < 0.01f)
            _shaftWorldLength = Vector2.Distance(hammerPivot.position, hammerHead.position);
    }

    void CachePivotOffset()
    {
        if (body == null || bodyPivot == null)
        {
            _pivotOffsetFromBody = Vector2.zero;
            return;
        }

        _pivotOffsetFromBody = (Vector2)(bodyPivot.position - body.position);
    }

    Vector2 PivotPosition => bodyPivot != null ? (Vector2)bodyPivot.position : (Vector2)body.position;

    float EffectiveMaxRange
    {
        get
        {
            float shaft = _shaftWorldLength > 0.01f ? _shaftWorldLength : 2f;
            if (!maxRangeFromShaft)
                return maxRange > 0.01f ? maxRange : shaft;
            if (maxRange > 0.01f)
                return Mathf.Min(maxRange, shaft);
            return shaft;
        }
    }

    void FixedUpdate()
    {
        if (_bodyRb == null || _hammerRb == null || Camera.main == null)
            return;

        CachePivotOffset();

        Vector3 mouseVec = ComputeMouseVec();
        Vector2 idealTip = ComputeIdealTip((Vector2)mouseVec);

        ApplyHammerVelocity(idealTip);
        ConstrainHammerToBody();
        UpdateGrounded();
        ApplyHammerRotation(idealTip);

        if (_isGrounded)
            ApplyBodyPush(mouseVec, idealTip);
        else
            StopBodySlide();
    }

    Vector2 ComputeIdealTip(Vector2 mouseVec)
    {
        float range = EffectiveMaxRange;
        float mag = mouseVec.magnitude;
        if (mag < 1e-8f)
            return PivotPosition + Vector2.right * range;

        Vector2 dir = mouseVec / mag;
        float reach = Mathf.Clamp(mag, 0f, range);
        return PivotPosition + dir * reach;
    }

    void ApplyHammerVelocity(Vector2 idealTip)
    {
        Vector2 error = idealTip - _hammerRb.position;
        float range = EffectiveMaxRange;
        float gain = hammerMaxSpeed / Mathf.Max(range, 0.01f);
        Vector2 desiredVel = Vector2.ClampMagnitude(error * gain, hammerMaxSpeed);
        _hammerRb.velocity = Vector2.Lerp(_hammerRb.velocity, desiredVel, hammerVelocityBlend);
    }

    void ConstrainHammerToBody()
    {
        float range = EffectiveMaxRange;
        Vector2 pivot = PivotPosition;
        Vector2 delta = _hammerRb.position - pivot;
        float dist = delta.magnitude;
        if (dist <= range)
            return;

        Vector2 radial = delta / dist;
        _hammerRb.position = pivot + radial * range;

        Vector2 vel = _hammerRb.velocity;
        vel -= Vector2.Dot(vel, radial) * radial;
        _hammerRb.velocity = vel;
        Physics2D.SyncTransforms();
    }

    void ApplyHammerRotation(Vector2 idealTip)
    {
        Vector3 aim = (Vector3)(idealTip - PivotPosition);
        if (aim.sqrMagnitude < 1e-8f)
            return;

        hammerHead.rotation = Quaternion.FromToRotation(Vector3.right, aim);
        if (hammerAngleOffset != 0f)
            hammerHead.rotation *= Quaternion.Euler(0f, 0f, hammerAngleOffset);

        hammerHead.localScale = _hammerHeadBaseScale;
        Physics2D.SyncTransforms();
    }

    void UpdateGrounded()
    {
        bool touching = _hammerCol != null && _hammerCol.IsTouchingLayers(groundLayers);
        if (touching)
            _coyoteCounter = coyoteFrames;
        else if (_coyoteCounter > 0)
            _coyoteCounter--;

        _isGrounded = touching || _coyoteCounter > 0;
    }

    void StopBodySlide()
    {
        Vector2 vel = _bodyRb.velocity;
        vel.x = 0f;
        _bodyRb.velocity = vel;
    }

    void ApplyBodyPush(Vector3 mouseVec, Vector2 idealTip)
    {
        Vector2 hammerPos = _hammerRb.position;
        Vector2 targetBodyPos = hammerPos - (Vector2)mouseVec - _pivotOffsetFromBody;

        Vector2 toTarget = targetBodyPos - _bodyRb.position;

        Vector2 blocked = idealTip - hammerPos;
        if (blocked.y > 0.04f)
            toTarget.y = Mathf.Min(toTarget.y, 0f);

        if (toTarget.sqrMagnitude < pushDeadzone * pushDeadzone)
        {
            Vector2 vel = _bodyRb.velocity;
            vel.x = 0f;
            vel.y = Mathf.Min(vel.y, 0f);
            _bodyRb.velocity = vel;
            return;
        }

        Vector2 desired = Vector2.ClampMagnitude(toTarget, maxBodySpeed);
        _bodyRb.velocity = Vector2.Lerp(_bodyRb.velocity, desired, pushVelocityBlend);
        _bodyRb.velocity = Vector2.ClampMagnitude(_bodyRb.velocity, maxBodySpeed);
    }

    Vector3 ComputeMouseVec()
    {
        float depth = Mathf.Abs(Camera.main.transform.position.z - body.position.z);
        Vector3 mouse = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));
        mouse.z = 0f;

        Vector3 origin;
        if (mouseFromBodyPivot)
        {
            origin = PivotPosition;
            origin.z = 0f;
        }
        else
        {
            origin = Camera.main.ScreenToWorldPoint(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, depth));
            origin.z = 0f;
        }

        return Vector3.ClampMagnitude(mouse - origin, EffectiveMaxRange);
    }

    void OnValidate()
    {
        if (hammerHead != null)
            _hammerHeadBaseScale = hammerHead.localScale;
        CacheHammerAxis();
    }

    void OnDrawGizmosSelected()
    {
        if (body == null)
            return;

        Transform pivot = bodyPivot != null ? bodyPivot : body;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        DrawCircleXY(pivot.position, EffectiveMaxRange, 48);

        if (hammerHead != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hammerHead.position, 0.05f);
            Gizmos.DrawLine(pivot.position, hammerHead.position);
        }

        if (hammerPivot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(hammerPivot.position, 0.04f);
            if (hammerHead != null)
                Gizmos.DrawLine(hammerPivot.position, hammerHead.position);
            Gizmos.DrawLine(hammerPivot.position, pivot.position);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pivot.position, 0.06f);
    }

    static void DrawCircleXY(Vector3 center, float radius, int segments)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
