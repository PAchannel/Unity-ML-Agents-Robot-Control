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
        // 方向移動
        float moveZ = 0f;
        float rotateY = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) moveZ += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) moveZ -= 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) rotateY -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) rotateY += 1f;

        // 平移與旋轉
        Vector3 forwardMove = transform.forward * moveZ * moveSpeed * Time.deltaTime;
        transform.position += forwardMove;
        transform.Rotate(Vector3.up, rotateY * rotateSpeed * Time.deltaTime);

        // 跳躍（僅在地面）
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 當與地板碰撞時允許跳躍
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }
}


