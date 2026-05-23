using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 2D GOI: ЛКМ — захват tip (сразу на земле или при касании, если нажали в воздухе); без ЛКМ — прицеливание.
/// </summary>
[DefaultExecutionOrder(0)]
public class NewGoiController : MonoBehaviour
{
    [Header("Refs")]
    [FormerlySerializedAs("hammerhead")]
    public Transform hammerhead;
    [FormerlySerializedAs("body")]
    public Transform body;
    [FormerlySerializedAs("bodyPivot")]
    public Transform bodyPivot;
    [FormerlySerializedAs("hammerPivot")]
    public Transform hammerPivot;
    [FormerlySerializedAs("hammerTip")]
    public Transform hammerTip;
    public Vector2 bodyOffset;

    [Header("Movement")]
    [Tooltip("0 = длина древка из hammerPivot → hammerTip.")]
    public float hammerLength;

    [Tooltip("Макс. радиус мыши от pivot/tip. 0 = только длина древка.")]
    [FormerlySerializedAs("maxPushSpeed")]
    public float maxRange;

    [Tooltip("Ограничить maxRange длиной древка.")]
    public bool maxRangeFromShaft = true;

    [Range(0.01f, 1f)]
    public float pushVelocityBlend = 0.15f;

    public float maxBodySpeed = 8f;
    public float pushDeadzone = 0.08f;
    public float hammerAngleOffset;

    [Header("Ground")]
    public LayerMask groundLayers;
    [FormerlySerializedAs("tipCollider")]
    public Collider2D tipCollider;
    public bool ignoreBodyCollider = true;

    [Header("Collision")]
    public LayerMask obstructionLayers;
    public float castSkin = 0.04f;
    public float groundProbePadding = 0.06f;

    [Header("Grab")]
    [Tooltip("Захват и push тела — левая кнопка мыши (в воздухе — как только tip коснётся земли).")]
    public int grabMouseButton = 0;

    [Tooltip("Макс. скорость тела при зажатой ЛКМ. 0 = использовать maxBodySpeed.")]
    public float grabMaxBodySpeed = 14f;

    [Range(0.1f, 4f)]
    [Tooltip("Сила толчка при захвате (множитель к целевой скорости).")]
    public float grabPushStrength = 1.5f;

    [Range(0.01f, 1f)]
    [Tooltip("Плавность push при зажатой ЛКМ.")]
    public float grabPushBlend = 0.5f;

    [Tooltip("Мёртвая зона мыши от точки захвата при ЛКМ. 0 = как pushDeadzone.")]
    public float grabDeadzone;

    [Tooltip("Радиус мыши от hammerTip при зажатой ЛКМ. 0 = длина древка (или maxRange, если задан).")]
    public float maxRangeOnPress;

    [Tooltip("Ограничить maxRangeOnPress длиной древка.")]
    public bool maxRangeOnPressFromShaft = true;

    [Tooltip("Кольцо радиуса на hammerTip в Scene/Game view.")]
    public bool showMaxRangeOnPressVisual = true;

    public Color maxRangeOnPressColor = new Color(1f, 0.45f, 0.1f, 0.9f);

    [Header("Grab trail")]
    [Tooltip("След на hammerTip, пока зажата кнопка захвата.")]
    public bool showGrabTrail = true;

    public Color grabTrailColor = new Color(1f, 0.65f, 0.15f, 0.9f);

    [Range(0.05f, 2f)]
    public float grabTrailTime = 0.35f;

    [Range(0.01f, 0.5f)]
    public float grabTrailWidth = 0.1f;

    [Tooltip("На земле при захвате не давать гравитации тянуть вниз (vel.y < 0).")]
    public bool cancelFallWhileGrabbing = true;

    [Header("Behaviour")]
    public bool detachHammerAtStart = true;

    [Header("Debug")]
    public bool debugLog;
    [Range(0.05f, 2f)] public float debugLogInterval = 0.25f;

    // Private
    private Rigidbody2D _bodyRb;
    private Collider2D _bodyCol;
    private Camera _cam;
    private float _shaftLength;
    private float _shaftLocalAngleDeg;
    private Vector3 _shaftLocalVector;
    private float _bindPivotToTipDist;
    private Vector3 _hammerBaseScale;
    private ContactFilter2D _castFilter;
    private readonly Collider2D[] _overlapBuffer = new Collider2D[8];
    private readonly RaycastHit2D[] _castHits = new RaycastHit2D[8];

    private Vector2 _mouseWorld;
    private Vector2 _lastAimDir = Vector2.right;
    private Vector2 _anchoredTipWorld;
    private float _dbgNextLogTime;
    private bool _isGrounded;
    private bool _isGrabbing;
    private bool _grabButtonHeld;
    private bool _grabPendingGround;
    private Transform _hammerParent;
    private LineRenderer _pressRangeLine;
    private TrailRenderer _grabTrail;
    private const int PressRangeSegments = 48;
    private const string GrabTrailChildName = "GrabTrail";

    private Vector2 Pivot => bodyPivot != null ? (Vector2)bodyPivot.position : body.TransformPoint(bodyOffset);
    private Vector2 TipPos => hammerTip != null ? (Vector2)hammerTip.position : Pivot;
    /// <summary>Длина древка hammerPivot → hammerTip (меш).</summary>
    private float ShaftLength => _shaftLength > 0.01f ? _shaftLength : (hammerLength > 0.01f ? hammerLength : 2f);

    /// <summary>Радиус орбиты tip от bodyPivot (для err и полного вылета).</summary>
    private float OrbitRadius => ShaftLength;

    private float EffectiveMaxRange
    {
        get
        {
            float shaft = ShaftLength;
            if (maxRange <= 0.01f)
                return shaft;
            if (!maxRangeFromShaft)
                return maxRange;
            return Mathf.Min(maxRange, shaft);
        }
    }

    private float EffectiveMaxRangeOnPress
    {
        get
        {
            float shaft = ShaftLength;
            if (maxRangeOnPress > 0.01f)
            {
                if (!maxRangeOnPressFromShaft)
                    return maxRangeOnPress;
                return Mathf.Min(maxRangeOnPress, shaft);
            }

            if (maxRange > 0.01f)
                return EffectiveMaxRange;
            return shaft;
        }
    }

    private float GrabDeadzone =>
        grabDeadzone > 0.001f ? grabDeadzone : pushDeadzone;

    private float GrabMaxSpeed =>
        grabMaxBodySpeed > 0.01f ? grabMaxBodySpeed : maxBodySpeed;

    private int ObstructionMask => obstructionLayers.value != 0 ? obstructionLayers.value : groundLayers.value;

    void Awake()
    {
        if (groundLayers.value == 0) groundLayers = LayerMask.GetMask("Default", "Ground");
        _castFilter.useLayerMask = true;
        _castFilter.layerMask = ObstructionMask;
        _castFilter.useTriggers = false;
    }

    void Start()
    {
        _cam = Camera.main;
        if (body == null)
        {
            Debug.LogError("[NewGoiController] body не назначен.", this);
            enabled = false;
            return;
        }

        _bodyRb = body.GetComponent<Rigidbody2D>();
        _bodyCol = body.GetComponent<Collider2D>();

        if (body == null || _bodyRb == null || hammerhead == null || hammerTip == null)
        {
            Debug.LogError("[NewGoiController] Не хватает ссылок: body (Rigidbody2D), hammerhead, hammerTip", this);
            enabled = false;
            return;
        }

        if (detachHammerAtStart) DetachHammerFromBody();

        ResolveHammerPivot();
        ResolveTipCollider();
        CacheShaft();
        SetupBodyRigidbody();
        SetupHammerRigidbodies();
        IgnoreBodyHammerCollisions();
        _hammerBaseScale = hammerhead.localScale;
        SetupPressRangeVisual();
        SetupGrabTrailVisual();
    }

    void OnDisable()
    {
        RestoreHammerParent();
        if (_pressRangeLine != null)
            _pressRangeLine.enabled = false;
        SetGrabTrailEmitting(false);
    }

    void LateUpdate()
    {
        UpdatePressRangeVisual();
        UpdateGrabTrailVisual();
    }

    void Update()
    {
        if (_cam == null || hammerTip == null)
            return;

        _mouseWorld = GetMouseWorld(Pivot);

        if (Input.GetMouseButtonDown(grabMouseButton))
        {
            _grabButtonHeld = true;
            _grabPendingGround = true;
            if (IsTipGrounded())
                BeginGrab();
        }

        if (Input.GetMouseButtonUp(grabMouseButton))
        {
            _grabButtonHeld = false;
            _grabPendingGround = false;
            _isGrabbing = false;
        }
    }

    void BeginGrab()
    {
        _isGrabbing = true;
        _grabPendingGround = false;
        _anchoredTipWorld = TipPos;
    }

    void FixedUpdate()
    {
        if (_bodyRb == null)
            return;

        Vector2 pivot = Pivot;
        Vector2 mouseVec = ComputeMouseVec(pivot);
        Vector2 aimDir = ComputeAimDir(mouseVec);

        _isGrounded = IsTipGrounded();

        if (_grabPendingGround && _grabButtonHeld && !_isGrabbing && _isGrounded)
            BeginGrab();

        if (_isGrabbing)
        {
            Vector2 targetTip = ResolveAnchoredTip(pivot, aimDir);
            PlaceHammerAtTip(targetTip, lockTipToWorld: true);

            Vector2 grabVec = ComputeGrabMouseVec();
            float grabDeadzoneSq = GrabDeadzone * GrabDeadzone;
            if (grabVec.sqrMagnitude >= grabDeadzoneSq)
                PushBody(grabVec, grabPushBlend, GrabMaxSpeed, grabPushStrength, allowVertical: true);

            if (cancelFallWhileGrabbing && _isGrounded)
                CancelFallVelocity();
        }
        else
        {
            if (!_isGrounded)
                StopBodySlide();

            Vector2 aimTip = ConstrainTipToGeometry(pivot, aimDir);
            PlaceHammerAtTip(aimTip, lockTipToWorld: false);
        }

        if (debugLog && Time.time >= _dbgNextLogTime)
            LogDebugInfo(pivot);
    }

    void PushBody(Vector2 mouseVec, float blend, float maxSpeed, float strength, bool allowVertical)
    {
        Vector2 pivot = Pivot;
        Vector2 tip = _anchoredTipWorld;
        Vector2 pivotOffset = pivot - (Vector2)body.position;

        Vector2 targetBodyPos = tip - mouseVec - pivotOffset;
        Vector2 toTarget = targetBodyPos - (Vector2)body.position;

        if (strength > 0.01f)
            toTarget *= strength;

        if (!allowVertical)
            toTarget.y = Mathf.Min(toTarget.y, 0f);

        Vector2 desired = Vector2.ClampMagnitude(toTarget, maxSpeed);
        if (!allowVertical)
        {
            desired.y = Mathf.Min(desired.y, 0f);
            _bodyRb.velocity = Vector2.Lerp(_bodyRb.velocity, desired, blend);
            _bodyRb.velocity = Vector2.ClampMagnitude(_bodyRb.velocity, maxSpeed);
            if (_bodyRb.velocity.y > 0f)
                _bodyRb.velocity = new Vector2(_bodyRb.velocity.x, 0f);
        }
        else
        {
            _bodyRb.velocity = Vector2.Lerp(_bodyRb.velocity, desired, blend);
            _bodyRb.velocity = Vector2.ClampMagnitude(_bodyRb.velocity, maxSpeed);
        }
    }

    void CancelFallVelocity()
    {
        Vector2 vel = _bodyRb.velocity;
        if (vel.y < 0f)
            vel.y = 0f;
        _bodyRb.velocity = vel;
    }

    void StopBodySlide()
    {
        Vector2 vel = _bodyRb.velocity;
        vel.x = 0f;
        _bodyRb.velocity = vel;
    }

    Vector2 ResolveAnchoredTip(Vector2 pivot, Vector2 aimDir)
    {
        Vector2 toAnchor = _anchoredTipWorld - pivot;
        float dist = toAnchor.magnitude;
        float shaft = OrbitRadius;

        if (dist > shaft + 0.02f)
            return ConstrainTipToGeometry(pivot, aimDir);
        if (dist > 1e-6f)
            return _anchoredTipWorld;
        return pivot + aimDir * shaft;
    }

    Vector2 ConstrainTipToGeometry(Vector2 pivot, Vector2 dir)
    {
        float shaft = OrbitRadius;
        int hitCount = Physics2D.CircleCast(pivot, GetTipRadius(), dir, _castFilter, _castHits, shaft);
        float maxReach = shaft;
        for (int i = 0; i < hitCount; i++)
        {
            if (IsIgnored(_castHits[i].collider)) continue;
            float hitReach = _castHits[i].distance - castSkin;
            if (hitReach < maxReach) maxReach = hitReach;
        }
        maxReach = Mathf.Clamp(maxReach, 0f, shaft);
        return pivot + dir * maxReach;
    }

    void PlaceHammerAtTip(Vector2 targetTip, bool lockTipToWorld)
    {
        Vector2 pivot = Pivot;
        Vector2 shaftWorld = targetTip - pivot;
        if (shaftWorld.sqrMagnitude < 1e-8f)
            shaftWorld = _lastAimDir * OrbitRadius;

        float worldDeg = Mathf.Atan2(shaftWorld.y, shaftWorld.x) * Mathf.Rad2Deg;
        float angle = worldDeg - _shaftLocalAngleDeg + hammerAngleOffset;
        hammerhead.rotation = Quaternion.Euler(0f, 0f, angle);
        hammerhead.localScale = _hammerBaseScale;

        float reach = shaftWorld.magnitude;
        bool usePivotAnchor = !lockTipToWorld
            && hammerPivot != null
            && reach >= OrbitRadius * 0.92f;

        if (usePivotAnchor)
        {
            Vector3 pivotWorld = pivot;
            pivotWorld.z = hammerhead.position.z;
            Vector3 pivotOnHead = hammerhead.TransformPoint(hammerPivot.localPosition) - hammerhead.position;
            hammerhead.position = pivotWorld - pivotOnHead;
        }
        else
        {
            Vector2 tipOffset = (Vector2)hammerTip.position - (Vector2)hammerhead.position;
            hammerhead.position = targetTip - tipOffset;
        }
    }

    Vector2 ComputeMouseVec(Vector2 pivot) =>
        Vector2.ClampMagnitude(_mouseWorld - pivot, EffectiveMaxRange);

    Vector2 ComputeGrabMouseVec() =>
        Vector2.ClampMagnitude(_mouseWorld - _anchoredTipWorld, EffectiveMaxRangeOnPress);

    Vector2 ComputeAimDir(Vector2 mouseVec)
    {
        if (mouseVec.sqrMagnitude > 1e-8f) _lastAimDir = mouseVec.normalized;
        return _lastAimDir;
    }

    bool IsTipGrounded()
    {
        float radius = GetTipRadius() + groundProbePadding;
        int count = Physics2D.OverlapCircleNonAlloc(TipPos, radius, _overlapBuffer, groundLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _overlapBuffer[i];
            if (col == null || IsIgnored(col)) continue;
            if (tipCollider != null && col == tipCollider) continue;
            return true;
        }
        return false;
    }

    Vector2 GetMouseWorld(Vector2 pivot)
    {
        float depth = _cam.WorldToScreenPoint(body.position).z;
        Vector3 mouse = _cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));
        return new Vector2(mouse.x, mouse.y);
    }

    bool IsIgnored(Collider2D col)
    {
        if (ignoreBodyCollider && _bodyCol != null && col == _bodyCol) return true;
        if (hammerhead != null && col.transform.IsChildOf(hammerhead)) return true;
        return false;
    }

    float GetTipRadius()
    {
        if (tipCollider is CircleCollider2D circle)
            return circle.radius * tipCollider.transform.lossyScale.x;
        return tipCollider != null ? Mathf.Max(tipCollider.bounds.extents.x, tipCollider.bounds.extents.y) : 0.08f;
    }

    void ResolveTipCollider()
    {
        if (tipCollider != null || hammerTip == null) return;
        tipCollider = hammerTip.GetComponent<Collider2D>();
    }

    void ResolveHammerPivot()
    {
        if (hammerPivot != null || hammerhead == null) return;
        hammerPivot = hammerhead.Find("UmbrellaHammerPivot")
                      ?? hammerhead.Find("HammerPivot")
                      ?? hammerhead.Find("HammerAnchor");
    }

    void CacheShaft()
    {
        if (hammerPivot != null && hammerTip != null && hammerhead != null)
        {
            _shaftLocalVector = hammerTip.localPosition - hammerPivot.localPosition;
            _shaftLength = hammerhead.TransformVector(_shaftLocalVector).magnitude;
            if (_shaftLength < 0.01f)
                _shaftLength = Vector2.Distance(hammerPivot.position, hammerTip.position);
        }
        else if (hammerLength > 0.01f)
        {
            _shaftLength = hammerLength;
            _shaftLocalVector = Vector3.right * hammerLength;
        }
        else
        {
            _shaftLength = 2f;
            _shaftLocalVector = Vector3.right * 2f;
        }

        if (_shaftLocalVector.sqrMagnitude > 1e-8f)
        {
            Vector2 localShaft = new Vector2(_shaftLocalVector.x, _shaftLocalVector.y);
            _shaftLocalAngleDeg = Mathf.Atan2(localShaft.y, localShaft.x) * Mathf.Rad2Deg;
        }
        else
        {
            _shaftLocalAngleDeg = 0f;
        }

        if (bodyPivot != null && hammerTip != null)
            _bindPivotToTipDist = Vector2.Distance(bodyPivot.position, hammerTip.position);

        if (Application.isPlaying && Mathf.Abs(_bindPivotToTipDist - ShaftLength) > 0.15f)
        {
            Debug.LogWarning(
                $"[NewGoiController] bodyPivot→tip ({_bindPivotToTipDist:F2}) ≠ hammerPivot→tip ({ShaftLength:F2}). " +
                "При полном вылете hammerPivot совпадает с bodyPivot.",
                this);
        }
    }

    void SetupBodyRigidbody()
    {
        _bodyRb.gravityScale = Mathf.Max(_bodyRb.gravityScale, 2f);
        _bodyRb.drag = 0f;
        _bodyRb.angularDrag = 0f;
        _bodyRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _bodyRb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void SetupHammerRigidbodies()
    {
        foreach (var rb in hammerhead.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void IgnoreBodyHammerCollisions()
    {
        if (_bodyCol == null) return;
        foreach (var col in hammerhead.GetComponentsInChildren<Collider2D>(true))
            Physics2D.IgnoreCollision(_bodyCol, col);
    }

    void DetachHammerFromBody()
    {
        if (hammerhead == null || body == null)
            return;

        if (hammerhead.parent != body)
            return;

        _hammerParent = body;
        Transform newParent = body.parent != null ? body.parent : transform.root;
        hammerhead.SetParent(newParent, true);
    }

    public void RestoreHammerParent()
    {
        if (!detachHammerAtStart || hammerhead == null || _hammerParent == null) return;
        if (hammerhead.parent != _hammerParent)
            hammerhead.SetParent(_hammerParent, true);
    }

    void LogDebugInfo(Vector2 pivot)
    {
        _dbgNextLogTime = Time.time + debugLogInterval;
        float tipDist = Vector2.Distance(TipPos, pivot);
        float pivotGrip = hammerPivot != null ? Vector2.Distance(pivot, hammerPivot.position) : 0f;
        Debug.Log(
            $"[GOI] grabbing={_isGrabbing} grounded={_isGrounded} shaft={ShaftLength:F2} " +
            $"tipDist={tipDist:F2} err={tipDist - OrbitRadius:F3} pivotΔ={pivotGrip:F3} vel={_bodyRb.velocity}",
            this);
    }

    void OnValidate()
    {
        ResolveHammerPivot();
        ResolveTipCollider();
        _castFilter.useLayerMask = true;
        _castFilter.layerMask = ObstructionMask;
        _castFilter.useTriggers = false;
        if (hammerhead != null && !Application.isPlaying)
        {
            _hammerBaseScale = hammerhead.localScale;
            CacheShaft();
        }

        if (showMaxRangeOnPressVisual && hammerTip != null)
            SetupPressRangeVisual();

        if (showGrabTrail && hammerTip != null)
            SetupGrabTrailVisual();
    }

    void OnDrawGizmosSelected()
    {
        if (body == null)
            return;

        Vector3 pivot = Pivot;
        pivot.z = body.position.z;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pivot, 0.06f);

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.3f);
        DrawCircleGizmo(pivot, EffectiveMaxRange, 40);

        if (hammerTip != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pivot, hammerTip.position);
            DrawMaxRangeOnPressGizmo();

            if (tipCollider is CircleCollider2D circle)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(
                    tipCollider.transform.TransformPoint(circle.offset),
                    circle.radius * tipCollider.transform.lossyScale.x);
            }
        }

        if (Application.isPlaying && _isGrabbing)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(TipPos, _mouseWorld);
        }
    }

    void OnDrawGizmos()
    {
        if (!showMaxRangeOnPressVisual || hammerTip == null)
            return;

        DrawMaxRangeOnPressGizmo();
    }

    void DrawMaxRangeOnPressGizmo()
    {
        Vector3 center = hammerTip.position;
        center.z = body != null ? body.position.z : center.z;

        Gizmos.color = maxRangeOnPressColor;
        DrawCircleGizmo(center, EffectiveMaxRangeOnPress, PressRangeSegments);

        if (Application.isPlaying && _isGrabbing)
        {
            Vector3 mouse = _mouseWorld;
            mouse.z = center.z;
            Gizmos.DrawLine(center, mouse);
        }
    }

    void SetupPressRangeVisual()
    {
        if (!showMaxRangeOnPressVisual || hammerTip == null)
            return;

        Transform ring = hammerTip.Find("MaxRangeOnPress");
        if (ring == null)
        {
            var go = new GameObject("MaxRangeOnPress");
            ring = go.transform;
            ring.SetParent(hammerTip, false);
            ring.localPosition = Vector3.zero;
            ring.localRotation = Quaternion.identity;
        }

        _pressRangeLine = ring.GetComponent<LineRenderer>();
        if (_pressRangeLine == null)
            _pressRangeLine = ring.gameObject.AddComponent<LineRenderer>();

        _pressRangeLine.useWorldSpace = true;
        _pressRangeLine.loop = true;
        _pressRangeLine.positionCount = PressRangeSegments;
        _pressRangeLine.startWidth = 0.04f;
        _pressRangeLine.endWidth = 0.04f;
        _pressRangeLine.numCapVertices = 4;
        _pressRangeLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _pressRangeLine.receiveShadows = false;

        if (_pressRangeLine.sharedMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _pressRangeLine.sharedMaterial = new Material(shader);
        }

        UpdatePressRangeVisual();
    }

    void UpdatePressRangeVisual()
    {
        if (_pressRangeLine == null)
        {
            if (showMaxRangeOnPressVisual && hammerTip != null)
                SetupPressRangeVisual();
            return;
        }

        if (!showMaxRangeOnPressVisual || hammerTip == null)
        {
            _pressRangeLine.enabled = false;
            return;
        }

        _pressRangeLine.enabled = true;
        float radius = EffectiveMaxRangeOnPress;
        Vector3 center = hammerTip.position;
        float z = body != null ? body.position.z : center.z;

        for (int i = 0; i < PressRangeSegments; i++)
        {
            float a = i / (float)PressRangeSegments * Mathf.PI * 2f;
            _pressRangeLine.SetPosition(
                i,
                new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, z));
        }

        _pressRangeLine.startColor = maxRangeOnPressColor;
        _pressRangeLine.endColor = maxRangeOnPressColor;
    }

    void SetupGrabTrailVisual()
    {
        if (!showGrabTrail || hammerTip == null)
            return;

        Transform trailT = hammerTip.Find(GrabTrailChildName);
        if (trailT == null)
        {
            var go = new GameObject(GrabTrailChildName);
            trailT = go.transform;
            trailT.SetParent(hammerTip, false);
            trailT.localPosition = Vector3.zero;
            trailT.localRotation = Quaternion.identity;
        }

        _grabTrail = trailT.GetComponent<TrailRenderer>();
        if (_grabTrail == null)
            _grabTrail = trailT.gameObject.AddComponent<TrailRenderer>();

        _grabTrail.time = grabTrailTime;
        _grabTrail.startWidth = grabTrailWidth;
        _grabTrail.endWidth = grabTrailWidth * 0.15f;
        _grabTrail.minVertexDistance = 0.025f;
        _grabTrail.numCapVertices = 4;
        _grabTrail.numCornerVertices = 2;
        _grabTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _grabTrail.receiveShadows = false;
        _grabTrail.autodestruct = false;
        _grabTrail.emitting = false;
        _grabTrail.alignment = LineAlignment.TransformZ;

        ApplyGrabTrailGradient();

        if (_grabTrail.sharedMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _grabTrail.sharedMaterial = new Material(shader);
        }
    }

    void ApplyGrabTrailGradient()
    {
        if (_grabTrail == null)
            return;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(grabTrailColor, 0f),
                new GradientColorKey(grabTrailColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(grabTrailColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        _grabTrail.colorGradient = gradient;
    }

    void UpdateGrabTrailVisual()
    {
        if (_grabTrail == null)
        {
            if (showGrabTrail && hammerTip != null)
                SetupGrabTrailVisual();
            return;
        }

        if (!showGrabTrail)
        {
            SetGrabTrailEmitting(false);
            return;
        }

        _grabTrail.time = grabTrailTime;
        _grabTrail.startWidth = grabTrailWidth;
        _grabTrail.endWidth = grabTrailWidth * 0.15f;
        ApplyGrabTrailGradient();

        bool emit = Input.GetMouseButton(grabMouseButton);
        SetGrabTrailEmitting(emit);
    }

    void SetGrabTrailEmitting(bool emit)
    {
        if (_grabTrail == null)
            return;

        if (_grabTrail.emitting == emit)
            return;

        _grabTrail.emitting = emit;
        if (!emit)
            _grabTrail.Clear();
    }

    static void DrawCircleGizmo(Vector3 center, float radius, int segments)
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