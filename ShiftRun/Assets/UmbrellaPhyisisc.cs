using UnityEngine;

public class UmbrellaPhysics : MonoBehaviour
{
    public Rigidbody playerRb;
    public Transform tip;
    public float forceMultiplier = 50f;
    public float rayLength = 1.5f;
    public LayerMask groundLayers = ~0;
    [Tooltip("Если задан — rayLength подстраивается под maxReach зонта.")]
    public UmbrellaControl umbrellaControl;

    RaycastHit _lastHit;
    bool _hasHit;

    void Awake()
    {
        if (umbrellaControl == null)
            umbrellaControl = GetComponent<UmbrellaControl>();
    }

    void FixedUpdate()
    {
        _hasHit = false;
        if (playerRb == null || tip == null)
            return;

        float castLen = rayLength;
        if (umbrellaControl != null)
            castLen = Mathf.Max(rayLength, umbrellaControl.maxReach);

        Vector3 origin = tip.position;
        Vector3 castDir = -tip.forward.normalized;

        if (Physics.Raycast(origin, castDir, out _lastHit, castLen, groundLayers))
        {
            _hasHit = true;
            playerRb.AddForce(tip.forward * forceMultiplier);
        }
    }

    void OnDrawGizmos()
    {
        if (tip == null)
            return;

        float castLen = rayLength;
        if (umbrellaControl != null)
            castLen = Mathf.Max(rayLength, umbrellaControl.maxReach);

        Vector3 origin = tip.position;
        Vector3 castDir = -tip.forward.normalized;
        Vector3 end = origin + castDir * castLen;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(origin, 0.04f);

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
        Gizmos.DrawRay(origin, tip.forward * 0.35f);

        if (Application.isPlaying && _hasHit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_lastHit.point, 0.06f);
            Gizmos.DrawLine(origin, _lastHit.point);
        }
        else if (!Application.isPlaying)
        {
            if (Physics.Raycast(origin, castDir, out RaycastHit hit, castLen, groundLayers))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(hit.point, 0.06f);
                Gizmos.DrawLine(origin, hit.point);
            }
        }
    }
}
