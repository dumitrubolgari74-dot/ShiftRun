using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks stable Ground-layer contacts for the hammer head (multi-collider safe).
/// </summary>
public class HammerHeadContact : MonoBehaviour
{
    [SerializeField] LayerMask groundLayers;

    readonly HashSet<Collider2D> _groundColliders = new HashSet<Collider2D>();
    Vector2 _lastPoint;
    Vector2 _lastNormal;
    bool _hasContactSample;

    public bool IsGrounded => _groundColliders.Count > 0;
    public Vector2 LastContactPoint => _lastPoint;
    public Vector2 LastNormal => _lastNormal;

    public void Configure(LayerMask mask)
    {
        groundLayers = mask;
    }

    bool IsGroundLayer(GameObject go) =>
        go != null && ((1 << go.layer) & groundLayers.value) != 0;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsGroundLayer(collision.gameObject))
            return;
        _groundColliders.Add(collision.collider);
        SampleBestContact(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsGroundLayer(collision.gameObject))
            return;
        _groundColliders.Add(collision.collider);
        SampleBestContact(collision);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        _groundColliders.Remove(collision.collider);
        if (_groundColliders.Count == 0)
            _hasContactSample = false;
    }

    void SampleBestContact(Collision2D collision)
    {
        if (collision.contactCount <= 0)
            return;
        int best = 0;
        float bestDot = -2f;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            float d = Vector2.Dot(c.normal, Vector2.up);
            if (d > bestDot)
            {
                bestDot = d;
                best = i;
            }
        }

        var w = collision.GetContact(best);
        _lastPoint = w.point;
        _lastNormal = w.normal;
        _hasContactSample = true;
    }

    public Vector2 GetApplyPoint(Transform hammerHead)
    {
        if (_hasContactSample)
            return _lastPoint;
        return hammerHead != null ? (Vector2)hammerHead.position : Vector2.zero;
    }
}
