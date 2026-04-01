using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    Vector3 velocity;
    bool isGrounded;

    // Start nu este necesar momentan, așa că îl putem lăsa gol sau șterge
    void Start()
    {
    }

    void Update()
    {
        // 1. Verificăm dacă suntem pe pământ
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Mișcare pe A și D (Axa Orizontală)
        float x = Input.GetAxis("Horizontal");
        Vector3 move = transform.right * x;

        controller.Move(move * speed * Time.deltaTime);

        // 3. Săritura pe SPACE
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Aplicăm gravitația (căderea)
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}