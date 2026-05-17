using UnityEngine;

public class UmbrellaPhysics : MonoBehaviour
{
    public Rigidbody playerRb;
    public Transform tip;
    public float forceMultiplier = 50f;

    void FixedUpdate()
    {
        RaycastHit hit;

        if (Physics.Raycast(tip.position, -tip.forward, out hit, 0.5f))
        {
            Vector3 forceDir = tip.forward;
            playerRb.AddForce(forceDir * forceMultiplier);
        }
    }
}