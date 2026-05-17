using UnityEngine;

/// <summary>
/// Legacy mouse / gamepad → world target for ArmIK (2D XY gameplay, ortho camera).
/// </summary>
public class InputHandler : MonoBehaviour
{
    [SerializeField] GameSettings settings;
    [SerializeField] Camera targetCamera;
    [SerializeField] Transform shoulderReference;
    [SerializeField] ArmIK armIK;

    public void Configure(GameSettings s, Camera cam, Transform shoulder, ArmIK ik)
    {
        settings = s;
        targetCamera = cam;
        shoulderReference = shoulder;
        armIK = ik;
    }

    float _armAngle;
    bool _useGamepad;

    public Vector2 CurrentWorldTarget { get; private set; }

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (settings == null || armIK == null || targetCamera == null)
            return;

        Vector2 target;

        float rx = Input.GetAxisRaw("Horizontal");
        float ry = Input.GetAxisRaw("Vertical");
        float mag = Mathf.Sqrt(rx * rx + ry * ry);
        _useGamepad = mag > 0.2f;

        if (_useGamepad && shoulderReference != null)
        {
            _armAngle += rx * settings.gamepadSensitivity * Time.deltaTime * 3f;
            float pitch = ry * settings.gamepadSensitivity * Time.deltaTime * 2f;
            float reach = settings.upperArmLength + settings.lowerArmLength - 0.05f;
            Vector2 shoulder = shoulderReference.position;
            Vector2 dir = new Vector2(Mathf.Cos(_armAngle), Mathf.Sin(_armAngle));
            target = shoulder + dir * reach + Vector2.up * pitch * 0.5f;
        }
        else
        {
            Vector3 mp = Input.mousePosition;
            mp.z = Mathf.Abs(targetCamera.transform.position.z - settings.pointerPlaneZ);
            Vector3 wp = targetCamera.ScreenToWorldPoint(mp);
            target = new Vector2(wp.x, wp.y);
            if (shoulderReference != null && settings.mouseSensitivity > 0f)
            {
                Vector2 s = shoulderReference.position;
                target = s + (target - s) * settings.mouseSensitivity;
            }
        }

        CurrentWorldTarget = target;
        armIK.SetTarget(target);
    }
}
