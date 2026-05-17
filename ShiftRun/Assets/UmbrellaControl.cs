using UnityEngine;

public class UmbrellaControl : MonoBehaviour
{
    public Transform pivot;
    public Camera cam;
    public float rotationSpeed = 10f;

    void Update()
    {

        /*
        Vector3 mouse = Input.mousePosition;
        mouse.z = 10f;

        Vector3 worldMouse = cam.ScreenToWorldPoint(mouse);

        Vector3 dir = worldMouse - pivot.position;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        Debug.Log(targetRot);
        
        pivot.rotation = Quaternion.Slerp(
            pivot.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );*/

        pivot.rotation *= Quaternion.Euler( 100 * Time.deltaTime, 0,0);
        

        Vector3 angles = pivot.localEulerAngles;
        angles.z = 0;
        pivot.localEulerAngles = angles;
    }
}