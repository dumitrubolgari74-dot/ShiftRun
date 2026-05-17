using UnityEngine;

public class SmoothCamera : MonoBehaviour
{
    [SerializeField] GameSettings settings;
    [SerializeField] Transform target;

    public void Configure(GameSettings s, Transform t)
    {
        settings = s;
        target = t;
    }

    Vector3 _velocity;

    void LateUpdate()
    {
        if (target == null)
            return;

        float smooth = settings != null ? settings.cameraSmoothSpeed : 4f;
        Vector3 off = settings != null ? settings.cameraOffset : new Vector3(0f, 1.5f, -10f);
        Vector3 desired = target.position + off;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 1f / Mathf.Max(0.01f, smooth));
        transform.rotation = Quaternion.identity;
    }

    public void SetTarget(Transform t) => target = t;
}
