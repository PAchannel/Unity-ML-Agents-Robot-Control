using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleQuadrupedController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotateSpeed = 60f;
    public float jumpForce = 5f;
    private Rigidbody rb;

    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // ��V����
        float moveZ = 0f;
        float rotateY = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) moveZ += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) moveZ -= 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) rotateY -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) rotateY += 1f;

        // �����P����
        Vector3 forwardMove = transform.forward * moveZ * moveSpeed * Time.deltaTime;
        transform.position += forwardMove;
        transform.Rotate(Vector3.up, rotateY * rotateSpeed * Time.deltaTime);

        // ���D�]�Ȧb�a���^
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // ��P�a�O�I���ɤ��\���D
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }
}


