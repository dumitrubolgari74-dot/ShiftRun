using UnityEngine;

public class SetCenterOfMass : MonoBehaviour
{
    public Rigidbody rb;
    public Transform centerOfMass;

    void Start()
    {
        rb.centerOfMass = rb.transform.InverseTransformPoint(centerOfMass.position);
    }
}