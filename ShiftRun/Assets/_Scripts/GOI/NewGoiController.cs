using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

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
    [Tooltip("Слои, на которых нельзя начать захват наконечником (например Wall).")]
    public LayerMask nonGrabbableLayers;
    [FormerlySerializedAs("tipCollider")]
    public Collider tipCollider;
    public bool ignoreBodyCollider = true;

    [Header("Collision")]
    public LayerMask obstructionLayers;
    [Tooltip("Слои, которые молот никогда не должен проходить (например Wall).")]
    public LayerMask hammerBlockingLayers;
    public float castSkin = 0.04f;
    public float groundProbePadding = 0.06f;
    [Tooltip("Если включено, столкновения учитываются только наконечником (tip). Древко/меш проходят сквозь препятствия.")]
    public bool tipOnlyCollision = true;
    [Tooltip("Шаг отката tip от целевой точки при поиске позиции без пересечения.")]
    [Range(0.01f, 0.25f)]
    public float tipBackoffStep = 0.05f;
    [Tooltip("Не позволять молоту проходить через коллайдеры целиком (наконечник + древко/меш).")]
    public bool hammerMustNotPassThrough = true;

    [Header("Physics")]
    [Tooltip("Автоматически добавлять Rigidbody/CapsuleCollider (3D), если их нет на body.")]
    public bool autoAdd3DPhysics = true;
    [Tooltip("Фиксировать body в плоскости XY (Freeze Z + Freeze Rotation X/Y).")]
    public bool lockBodyToXYPlane = true;
    [Tooltip("Полностью заморозить вращение body (включая Z).")]
    public bool freezeBodyRotation = true;

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

    [Header("Smoothing")]
    [Tooltip("Сглаживать движение/поворот молота, чтобы убрать резкие телепорты.")]
    public bool smoothHammerMotion = true;
    [Range(1f, 80f)]
    [Tooltip("Скорость подтягивания tip к целевой точке (без захвата).")]
    public float hammerFollowSpeed = 28f;
    [Range(1f, 80f)]
    [Tooltip("Скорость подтягивания tip к целевой точке при захвате.")]
    public float hammerFollowSpeedWhileGrabbing = 36f;
    [Range(0.01f, 1f)]
    [Tooltip("Вкл. pivot-якоря при reach >= OrbitRadius * threshold.")]
    public float pivotAnchorEnterThreshold = 0.96f;
    [Range(0.01f, 1f)]
    [Tooltip("Выкл. pivot-якоря при reach <= OrbitRadius * threshold.")]
    public float pivotAnchorExitThreshold = 0.9f;
    
    [Header("Visual")]
    [Tooltip("Смещение молота по Z относительно body. Подберите знак под вашу камеру.")]
    public float hammerDepthOffset = -0.05f;

    [Header("Debug")]
    public bool debugLog;
    [Range(0.05f, 2f)] public float debugLogInterval = 0.25f;

    private Rigidbody _bodyRb;
    private Collider _bodyCol;
    private Camera _cam;
    private float _shaftLength;
    private float _shaftLocalAngleDeg;
    private Vector3 _shaftLocalVector;
    private float _bindPivotToTipDist;
    private Vector3 _hammerBaseScale;
    private float _hammerCollisionRadius;
    private readonly Collider[] _overlapBuffer = new Collider[8];
    private readonly RaycastHit[] _castHits = new RaycastHit[8];

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
    private bool _smoothedTipInitialized;
    private Vector2 _smoothedTipWorld;
    private bool _usePivotAnchor;
    private readonly List<ColliderState> _hammerColliderStates = new List<ColliderState>(8);
    private const int PressRangeSegments = 48;
    private const string GrabTrailChildName = "GrabTrail";
    private struct ColliderState
    {
        public Collider collider;
        public bool initialEnabled;
    }

    private Vector2 Pivot => bodyPivot != null ? (Vector2)bodyPivot.position : body.TransformPoint(bodyOffset);
    private Vector2 TipPos => hammerTip != null ? (Vector2)hammerTip.position : Pivot;
    private float PlaneZ => body != null ? body.position.z : 0f;
    private float ShaftLength => _shaftLength > 0.01f ? _shaftLength : (hammerLength > 0.01f ? hammerLength : 2f);

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
    private int HammerBlockMask
    {
        get
        {
            int baseMask = ObstructionMask;
            if (hammerBlockingLayers.value != 0)
                baseMask |= hammerBlockingLayers.value;
            return baseMask;
        }
    }

    void Awake()
    {
        if (groundLayers.value == 0) groundLayers = LayerMask.GetMask("Default", "Ground");
        if (nonGrabbableLayers.value == 0) nonGrabbableLayers = LayerMask.GetMask("Wall");
        if (hammerBlockingLayers.value == 0) hammerBlockingLayers = LayerMask.GetMask("Wall");
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

        if (autoAdd3DPhysics)
            EnsureBody3DPhysics();
        _bodyRb = body.GetComponent<Rigidbody>();
        _bodyCol = body.GetComponent<Collider>();

        if (_bodyRb == null || _bodyCol == null || hammerhead == null || hammerTip == null)
        {
            Debug.LogError(
                "[NewGoiController] Не хватает ссылок: body (Rigidbody + Collider), hammerhead, hammerTip. " +
                "Либо включите autoAdd3DPhysics, либо добавьте 3D-компоненты вручную.",
                this);
            enabled = false;
            return;
        }

        if (detachHammerAtStart) DetachHammerFromBody();

        ResolveHammerPivot();
        ResolveTipCollider();
        SetupTipOnlyColliders();
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
        _smoothedTipInitialized = false;
        _usePivotAnchor = false;
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
            if (CanStartGrabHere())
                BeginGrab();
        }

        if (Input.GetMouseButtonUp(grabMouseButton))
        {
            _grabButtonHeld = false;
            _grabPendingGround = false;
            _isGrabbing = false;
        }

        if (_grabPendingGround && _grabButtonHeld && !_isGrabbing && CanStartGrabHere())
            BeginGrab();
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

        if (_isGrabbing)
        {
            Vector2 targetTip = ResolveAnchoredTip(pivot, aimDir);
            Vector2 smoothedTip = GetSmoothedTip(targetTip, grabbing: true);
            PlaceHammerAtTip(smoothedTip, lockTipToWorld: true);

            Vector2 grabVec = ComputeGrabMouseVec();
            float grabDeadzoneSq = GrabDeadzone * GrabDeadzone;
            if (grabVec.sqrMagnitude >= grabDeadzoneSq)
                PushBody(grabVec, grabPushBlend, GrabMaxSpeed, grabPushStrength, allowVertical: true);

            bool hasIntentionalDownInput = grabVec.y < -GrabDeadzone;
            if (cancelFallWhileGrabbing && _isGrounded && !hasIntentionalDownInput)
                CancelFallVelocity();
        }
        else
        {
            if (!_isGrounded)
                StopBodySlide();

            Vector2 aimTip = ConstrainTipToGeometry(pivot, aimDir);
            Vector2 smoothedTip = GetSmoothedTip(aimTip, grabbing: false);
            PlaceHammerAtTip(smoothedTip, lockTipToWorld: false);
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
            Vector2 next = Vector2.Lerp(ToVec2(_bodyRb.velocity), desired, blend);
            next = Vector2.ClampMagnitude(next, maxSpeed);
            if (next.y > 0f)
                next = new Vector2(next.x, 0f);
            SetBodyVelocity(next);
        }
        else
        {
            Vector2 next = Vector2.Lerp(ToVec2(_bodyRb.velocity), desired, blend);
            next = Vector2.ClampMagnitude(next, maxSpeed);
            SetBodyVelocity(next);
        }
    }

    void CancelFallVelocity()
    {
        Vector2 vel = ToVec2(_bodyRb.velocity);
        if (vel.y < 0f)
            vel.y = 0f;
        SetBodyVelocity(vel);
    }

    void StopBodySlide()
    {
        Vector2 vel = ToVec2(_bodyRb.velocity);
        vel.x = 0f;
        SetBodyVelocity(vel);
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
        if (tipOnlyCollision && !hammerMustNotPassThrough)
            return ConstrainTipByOverlapOnly(pivot, dir, shaft);

        Vector3 pivot3 = ToPlaneVec3(pivot, PlaneZ);
        Vector3 dir3 = new Vector3(dir.x, dir.y, 0f);
        float castRadius = hammerMustNotPassThrough ? GetHammerCollisionRadius() : GetTipRadius();
        int hitCount = Physics.SphereCastNonAlloc(pivot3, castRadius, dir3, _castHits, shaft, HammerBlockMask, QueryTriggerInteraction.Ignore);
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

    Vector2 ConstrainTipByOverlapOnly(Vector2 pivot, Vector2 dir, float shaft)
    {
        Vector2 target = pivot + dir * shaft;
        float radius = Mathf.Max(0.01f, GetTipRadius() - castSkin);

        if (!IsTipBlockedAt(target, radius))
            return target;

        float step = Mathf.Max(0.01f, tipBackoffStep);
        int steps = Mathf.CeilToInt(shaft / step);
        for (int i = 1; i <= steps; i++)
        {
            float reach = Mathf.Max(0f, shaft - i * step);
            Vector2 candidate = pivot + dir * reach;
            if (!IsTipBlockedAt(candidate, radius))
                return candidate;
        }

        return pivot;
    }

    bool IsTipBlockedAt(Vector2 tipWorld, float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(
            ToPlaneVec3(tipWorld, PlaneZ),
            radius,
            _overlapBuffer,
            HammerBlockMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null || IsIgnored(col))
                continue;
            if (tipCollider != null && col == tipCollider)
                continue;
            return true;
        }

        return false;
    }

    void PlaceHammerAtTip(Vector2 targetTip, bool lockTipToWorld)
    {
        Vector2 pivot = Pivot;
        Vector2 shaftWorld = targetTip - pivot;
        if (shaftWorld.sqrMagnitude < 1e-8f)
            shaftWorld = _lastAimDir * OrbitRadius;

        float worldDeg = Mathf.Atan2(shaftWorld.y, shaftWorld.x) * Mathf.Rad2Deg;
        float angle = worldDeg - _shaftLocalAngleDeg + hammerAngleOffset;

        if (smoothHammerMotion)
        {
            float rotLerp = Mathf.Clamp01(Time.fixedDeltaTime * 22f);
            float z = Mathf.LerpAngle(hammerhead.eulerAngles.z, angle, rotLerp);
            hammerhead.rotation = Quaternion.Euler(0f, 0f, z);
        }
        else
        {
            hammerhead.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        hammerhead.localScale = _hammerBaseScale;

        float reach = shaftWorld.magnitude;
        bool usePivotAnchor = ShouldUsePivotAnchor(lockTipToWorld, reach);
        float hammerZ = GetHammerDepthZ();

        if (usePivotAnchor)
        {
            Vector3 pivotWorld = pivot;
            pivotWorld.z = hammerZ;
            Vector3 pivotOnHead = hammerhead.TransformPoint(hammerPivot.localPosition) - hammerhead.position;
            hammerhead.position = pivotWorld - pivotOnHead;
            Vector3 pos = hammerhead.position;
            pos.z = hammerZ;
            hammerhead.position = pos;
        }
        else
        {
            Vector2 tipOffset = (Vector2)hammerTip.position - (Vector2)hammerhead.position;
            Vector2 pos2 = targetTip - tipOffset;
            hammerhead.position = new Vector3(pos2.x, pos2.y, hammerZ);
        }
    }

    Vector2 GetSmoothedTip(Vector2 targetTip, bool grabbing)
    {
        if (!smoothHammerMotion)
        {
            _smoothedTipInitialized = false;
            return targetTip;
        }

        float speed = grabbing ? hammerFollowSpeedWhileGrabbing : hammerFollowSpeed;
        if (speed <= 0.01f)
            return targetTip;

        if (!_smoothedTipInitialized)
        {
            _smoothedTipWorld = TipPos;
            _smoothedTipInitialized = true;
        }

        float maxDelta = speed * Time.fixedDeltaTime;
        _smoothedTipWorld = Vector2.MoveTowards(_smoothedTipWorld, targetTip, maxDelta);
        return _smoothedTipWorld;
    }

    bool ShouldUsePivotAnchor(bool lockTipToWorld, float reach)
    {
        if (lockTipToWorld || hammerPivot == null)
        {
            _usePivotAnchor = false;
            return false;
        }

        float enter = Mathf.Max(pivotAnchorEnterThreshold, pivotAnchorExitThreshold + 0.01f);
        float exit = Mathf.Min(pivotAnchorExitThreshold, enter - 0.01f);

        if (_usePivotAnchor)
        {
            if (reach <= OrbitRadius * exit)
                _usePivotAnchor = false;
        }
        else
        {
            if (reach >= OrbitRadius * enter)
                _usePivotAnchor = true;
        }

        return _usePivotAnchor;
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
        int count = Physics.OverlapSphereNonAlloc(ToPlaneVec3(TipPos, PlaneZ), radius, _overlapBuffer, groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null || IsIgnored(col)) continue;
            if (tipCollider != null && col == tipCollider) continue;
            return true;
        }
        return false;
    }

    bool CanStartGrabHere()
    {
        if (!IsTipGrounded())
            return false;
        if (nonGrabbableLayers.value == 0)
            return true;

        float radius = GetTipRadius() + groundProbePadding;
        int count = Physics.OverlapSphereNonAlloc(
            ToPlaneVec3(TipPos, PlaneZ),
            radius,
            _overlapBuffer,
            nonGrabbableLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null || IsIgnored(col))
                continue;
            if (tipCollider != null && col == tipCollider)
                continue;
            return false;
        }

        return true;
    }

    Vector2 GetMouseWorld(Vector2 pivot)
    {
        float depth = _cam.WorldToScreenPoint(body.position).z;
        Vector3 mouse = _cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));
        return new Vector2(mouse.x, mouse.y);
    }

    bool IsIgnored(Collider col)
    {
        if (ignoreBodyCollider && _bodyCol != null && col == _bodyCol) return true;
        if (hammerhead != null && col.transform.IsChildOf(hammerhead)) return true;
        return false;
    }

    float GetTipRadius()
    {
        EnsureTipColliderReferenceValid();

        if (tipCollider is SphereCollider sphere)
            return sphere.radius * MaxAbsAxis(tipCollider.transform.lossyScale);
        if (tipCollider != null)
            return Mathf.Max(tipCollider.bounds.extents.x, tipCollider.bounds.extents.y);
        return 0.08f;
    }

    void ResolveTipCollider()
    {
        if (EnsureTipColliderReferenceValid())
        {
            EnsureTipColliderAlwaysEnabled();
            return;
        }
        if (hammerTip == null) return;
        tipCollider = hammerTip.GetComponent<Collider>();
        if (tipCollider != null)
        {
            EnsureTipColliderAlwaysEnabled();
            return;
        }

        CircleCollider2D legacyCircle = hammerTip.GetComponent<CircleCollider2D>();
        if (legacyCircle != null)
        {
            SphereCollider sphere = hammerTip.gameObject.AddComponent<SphereCollider>();
            sphere.center = new Vector3(legacyCircle.offset.x, legacyCircle.offset.y, 0f);
            sphere.radius = Mathf.Max(legacyCircle.radius, 0.02f);
            tipCollider = sphere;
        }

        EnsureTipColliderAlwaysEnabled();
    }

    void SetupTipOnlyColliders()
    {
        if (hammerhead == null)
            return;

        EnsureTipColliderReferenceValid();
        EnsureTipColliderAlwaysEnabled();
        CacheHammerColliderStates();
        bool applyTipOnly = tipOnlyCollision && !hammerMustNotPassThrough;
        for (int i = 0; i < _hammerColliderStates.Count; i++)
        {
            ColliderState state = _hammerColliderStates[i];
            Collider col = state.collider;
            if (col == null)
                continue;

            col.enabled = applyTipOnly ? false : state.initialEnabled;
        }
    }

    void EnsureTipColliderAlwaysEnabled()
    {
        if (!EnsureTipColliderReferenceValid())
            return;

        try
        {
            if (!tipCollider.enabled)
                tipCollider.enabled = true;
        }
        catch (MissingReferenceException)
        {
            tipCollider = null;
        }
    }

    void CacheHammerColliderStates()
    {
        EnsureTipColliderReferenceValid();
        _hammerColliderStates.RemoveAll(state => state.collider == null);
        Collider[] hammerColliders = hammerhead.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < hammerColliders.Length; i++)
        {
            Collider col = hammerColliders[i];
            if (col == null)
                continue;
            if (tipCollider != null && col == tipCollider)
                continue;

            bool alreadyTracked = false;
            for (int j = 0; j < _hammerColliderStates.Count; j++)
            {
                if (_hammerColliderStates[j].collider == col)
                {
                    alreadyTracked = true;
                    break;
                }
            }

            if (!alreadyTracked)
            {
                _hammerColliderStates.Add(new ColliderState
                {
                    collider = col,
                    initialEnabled = col.enabled
                });
            }
        }
    }

    bool EnsureTipColliderReferenceValid()
    {
        if (tipCollider == null)
            return false;

        try
        {
            _ = tipCollider.gameObject;
            return true;
        }
        catch (MissingReferenceException)
        {
            tipCollider = null;
            return false;
        }
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

        CacheHammerCollisionRadius();
    }

    void CacheHammerCollisionRadius()
    {
        _hammerCollisionRadius = Mathf.Max(GetTipRadius(), 0.08f);
        if (hammerhead == null)
            return;

        Collider[] hammerColliders = hammerhead.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < hammerColliders.Length; i++)
        {
            Collider col = hammerColliders[i];
            if (col == null)
                continue;

            Bounds b = col.bounds;
            float extent = Mathf.Max(b.extents.x, b.extents.y);
            if (extent > _hammerCollisionRadius)
                _hammerCollisionRadius = extent;
        }
    }

    void SetupBodyRigidbody()
    {
        _bodyRb.useGravity = true;
        _bodyRb.drag = 0f;
        _bodyRb.angularDrag = 0f;
        _bodyRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _bodyRb.interpolation = RigidbodyInterpolation.Interpolate;

        if (lockBodyToXYPlane)
            _bodyRb.constraints |= RigidbodyConstraints.FreezePositionZ
                                | RigidbodyConstraints.FreezeRotationX
                                | RigidbodyConstraints.FreezeRotationY;

        if (freezeBodyRotation)
            _bodyRb.constraints |= RigidbodyConstraints.FreezeRotationZ;
    }

    void SetupHammerRigidbodies()
    {
        foreach (var rb in hammerhead.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void IgnoreBodyHammerCollisions()
    {
        if (_bodyCol == null) return;
        foreach (var col in hammerhead.GetComponentsInChildren<Collider>(true))
            Physics.IgnoreCollision(_bodyCol, col);
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
            $"tipDist={tipDist:F2} err={tipDist - OrbitRadius:F3} pivotΔ={pivotGrip:F3} vel={ToVec2(_bodyRb.velocity)}",
            this);
    }

    void OnValidate()
    {
        ResolveHammerPivot();
        ResolveTipCollider();
        SetupTipOnlyColliders();
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

            if (tipCollider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(tipCollider.bounds.center, GetTipRadius());
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

    static Vector3 ToPlaneVec3(Vector2 xy, float z) => new Vector3(xy.x, xy.y, z);

    static Vector2 ToVec2(Vector3 vec) => new Vector2(vec.x, vec.y);

    void SetBodyVelocity(Vector2 xyVelocity)
    {
        Vector3 velocity = _bodyRb.velocity;
        velocity.x = xyVelocity.x;
        velocity.y = xyVelocity.y;
        _bodyRb.velocity = velocity;
    }

    static float MaxAbsAxis(Vector3 v) =>
        Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    float GetHammerCollisionRadius()
    {
        if (_hammerCollisionRadius <= 0.001f)
            CacheHammerCollisionRadius();
        return Mathf.Max(_hammerCollisionRadius, GetTipRadius());
    }

    float GetHammerDepthZ()
    {
        if (body == null)
            return hammerhead != null ? hammerhead.position.z : 0f;
        return body.position.z + hammerDepthOffset;
    }

    void EnsureBody3DPhysics()
    {
        Rigidbody rb = body.GetComponent<Rigidbody>();
        if (rb == null)
            rb = body.gameObject.AddComponent<Rigidbody>();

        Collider bodyCollider = body.GetComponent<Collider>();
        if (bodyCollider != null)
            return;

        CapsuleCollider capsule = body.gameObject.AddComponent<CapsuleCollider>();
        capsule.direction = 1;
        capsule.center = Vector3.zero;
        capsule.radius = 0.35f;
        capsule.height = 1.8f;

        CapsuleCollider2D legacyCapsule = body.GetComponent<CapsuleCollider2D>();
        if (legacyCapsule != null)
        {
            capsule.center = new Vector3(legacyCapsule.offset.x, legacyCapsule.offset.y, 0f);
            capsule.radius = Mathf.Max(legacyCapsule.size.x * 0.5f, 0.05f);
            capsule.height = Mathf.Max(legacyCapsule.size.y, capsule.radius * 2f);
        }
    }
}