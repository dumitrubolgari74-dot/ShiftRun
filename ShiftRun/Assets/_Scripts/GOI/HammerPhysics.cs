using UnityEngine;

/// <summary>
/// Transfers motion of the hammer head into forces on the player root Rigidbody2D.
/// </summary>
public class HammerPhysics : MonoBehaviour
{
    [SerializeField] GameSettings settings;
    [SerializeField] Rigidbody2D bodyRb;
    [SerializeField] Transform hammerHead;
    [SerializeField] HammerHeadContact headContact;
    [SerializeField] AudioSource optionalAudio;
    [SerializeField] AudioClip scrapeLoop;
    [SerializeField] AudioClip softImpact;

    Vector3 _prevHeadPos;
    float _lastImpactTime;

    public void Configure(GameSettings s, Rigidbody2D rb, Transform head, HammerHeadContact contact)
    {
        settings = s;
        bodyRb = rb;
        hammerHead = head;
        headContact = contact;
    }

    void Awake()
    {
        if (bodyRb == null)
            bodyRb = GetComponentInParent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (hammerHead != null)
            _prevHeadPos = hammerHead.position;
    }

    void FixedUpdate()
    {
        if (settings == null || bodyRb == null || hammerHead == null || headContact == null)
            return;

        Vector3 headNow = hammerHead.position;
        Vector3 delta = headNow - _prevHeadPos;
        _prevHeadPos = headNow;

        Vector2 v = (Vector2)(delta / Time.fixedDeltaTime);
        float speed = v.magnitude;

        if (headContact.IsGrounded && speed > 0.01f)
        {
            Vector2 force = v * settings.torqueMultiplier;
            if (settings.maxHammerSpeedForForce > 0f && speed > settings.maxHammerSpeedForForce)
                force *= settings.maxHammerSpeedForForce / speed;

            // Favor tangential shove along the surface
            Vector2 n = headContact.LastNormal.sqrMagnitude > 0.0001f ? headContact.LastNormal : Vector2.up;
            Vector2 tangent = v - Vector2.Dot(v, n) * n;
            if (tangent.sqrMagnitude > 0.0001f)
                force = Vector2.Lerp(force, tangent * settings.torqueMultiplier, 0.35f);

            Vector2 applyPos = headContact.GetApplyPoint(hammerHead);
            bodyRb.AddForceAtPosition(force, applyPos, ForceMode2D.Force);
        }

        UpdateAudio(speed, headContact.IsGrounded);
    }

    void UpdateAudio(float hammerSpeed, bool grounded)
    {
        if (optionalAudio == null)
            return;

        if (scrapeLoop != null && grounded && hammerSpeed > settings.scrapeMinSpeed)
        {
            if (optionalAudio.clip != scrapeLoop || !optionalAudio.isPlaying)
            {
                optionalAudio.clip = scrapeLoop;
                optionalAudio.loop = true;
                optionalAudio.Play();
            }
            optionalAudio.volume = Mathf.Clamp01((hammerSpeed - settings.scrapeMinSpeed) * 0.05f);
        }
        else if (optionalAudio.isPlaying && optionalAudio.clip == scrapeLoop)
        {
            optionalAudio.Stop();
            optionalAudio.loop = false;
        }

        if (softImpact != null && grounded && hammerSpeed > settings.impactMinSpeed)
        {
            if (Time.time - _lastImpactTime > 0.35f)
            {
                optionalAudio.PlayOneShot(softImpact, 0.28f);
                _lastImpactTime = Time.time;
            }
        }
    }
}
