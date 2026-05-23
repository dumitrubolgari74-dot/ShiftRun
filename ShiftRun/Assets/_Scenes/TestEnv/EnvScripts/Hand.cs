using UnityEngine;

[DefaultExecutionOrder(50)]
public class Hand : MonoBehaviour
{
    public Transform hammerHandle;
    public Sprite[] sprites;
    public bool rightHand;

    [Tooltip("Множитель длины: distance * scalePerUnit → stretch.")]
    public float scalePerUnit = 0.5f;

    public float minStretch = 0.3f;
    public float maxStretch = 1.2f;

    const float MeshHalfHeight = 1f;

    SpriteRenderer _sprite;
    Vector3 _baseLocalScale;
    Transform _body;
    Vector3 _shoulderLocal;

    void Start()
    {
        _baseLocalScale = transform.localScale;
        _body = transform.parent;
        _shoulderLocal = transform.localPosition;

        ResolveHammerHandle();
        _sprite = GetComponent<SpriteRenderer>();
    }

    void ResolveHammerHandle()
    {
        if (hammerHandle != null)
            return;

        var control = GetComponentInParent<PlayerControl>();
        if (control != null)
        {
            hammerHandle = control.hammerHandle;
            return;
        }

        var goi = GetComponentInParent<NewGoiController>();
        if (goi != null && goi.hammerhead != null)
            hammerHandle = goi.hammerhead.Find("UmrellaHammerHandler")
                           ?? goi.hammerhead.Find("HammerHandler");
    }

    void FixedUpdate()
    {
        if (hammerHandle == null)
        {
            ResolveHammerHandle();
            if (hammerHandle == null)
                return;
        }

        Vector3 shoulder = _body != null ? _body.TransformPoint(_shoulderLocal) : transform.position;
        Vector3 handle = hammerHandle.position;
        handle.z = shoulder.z;

        Vector3 handDir = handle - shoulder;
        float dist = handDir.magnitude;
        if (dist < 1e-8f)
            return;

        Quaternion rot = Quaternion.FromToRotation(Vector3.down, handDir);
        transform.rotation = rot;

        float stretch = Mathf.Clamp(dist * scalePerUnit, minStretch, maxStretch);
        transform.localScale = new Vector3(
            _baseLocalScale.x,
            _baseLocalScale.y * stretch,
            _baseLocalScale.z);

        float halfLen = _baseLocalScale.y * stretch * MeshHalfHeight;
        transform.position = handle + rot * Vector3.up * halfLen;

        if (_sprite == null)
            return;

        _sprite.flipX = rightHand ^ handDir.y > 0;

        if (sprites == null || sprites.Length == 0)
            return;

        int spriteIndex = Mathf.Clamp((int)(dist * 8f), 0, sprites.Length - 1);
        _sprite.sprite = sprites[spriteIndex];
    }
}
