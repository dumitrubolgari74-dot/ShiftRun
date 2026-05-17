using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "ShiftRun/GOI/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Input")]
    public float mouseSensitivity = 1.5f;
    public float gamepadSensitivity = 2f;
    public float pointerPlaneZ = 0f;

    [Header("Hammer force")]
    [Tooltip("Multiplier for AddForceAtPosition from hammer head motion.")]
    public float torqueMultiplier = 8f;
    [Tooltip("Caps |hammer velocity| contribution per FixedUpdate (0 = no cap).")]
    public float maxHammerSpeedForForce = 80f;

    [Header("Rigidbody defaults (applied by GOIPlayerApplySettings)")]
    public float bodyMass = 4f;
    public float linearDrag = 0.5f;
    public float angularDrag = 2f;
    public float gravityScale = 1f;

    [Header("Arm IK")]
    public float upperArmLength = 0.6f;
    public float lowerArmLength = 0.5f;
    [Tooltip("1 = elbow bends down in +Y world.")]
    public float elbowBendSign = -1f;

    [Header("Camera")]
    public float cameraSmoothSpeed = 4f;
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, -10f);

    [Header("Prototype level physics (GOISceneBootstrap)")]
    public PhysicsMaterial2D prototypeLevelStone;
    public PhysicsMaterial2D prototypeLevelIce;

    [Header("Materials (optional references)")]
    public PhysicsMaterial2D hammerPhysicsMaterial;

    [Header("Audio polish")]
    public float scrapeMinSpeed = 0.5f;
    public float impactMinSpeed = 2f;
}
