using UnityEngine;

public class BodyJumpController : MonoBehaviour
{
    public float jumpForce = 300f;  // 可調整跳躍力量
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}

