using UnityEngine;

public class EditorLikeCameraController : MonoBehaviour
{
    public float rotateSpeed = 5.0f;
    public float moveSpeed = 0.1f;
    public float zoomSpeed = 5.0f;

    private Vector3 lastMousePosition;

    void Update()
    {
        // ¥kÁä±ÛÂà
        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float yaw = delta.x * rotateSpeed * Time.deltaTime;
            float pitch = -delta.y * rotateSpeed * Time.deltaTime;
            transform.eulerAngles += new Vector3(pitch, yaw, 0);
        }

        // ¤¤Áä¥­²¾
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * moveSpeed * Time.deltaTime;
            transform.Translate(move, Space.Self);
        }

        // ºu½üÁY©ñ
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            transform.Translate(Vector3.forward * scroll * zoomSpeed, Space.Self);
        }

        lastMousePosition = Input.mousePosition;
    }
}
