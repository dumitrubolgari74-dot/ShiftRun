using UnityEngine;

public class UmbrellaMove : MonoBehaviour
{
    public Rigidbody player;
    public float force = 20f;

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            // împinge în fața umbrelei
            player.AddForce(transform.forward * force);
        }
    }
}
