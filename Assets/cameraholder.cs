using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public Transform target;
    public Vector3 positionOffset = new Vector3(0, 0.5f, 0.2f);
    public float positionSmoothSpeed = 5f;

    public float rotationSensitivity = 2f;   // 控制轉動靈敏度
    public float rotationThreshold = 1f;     // 幾度以內忽略（防抖）
    public float rotationSmoothSpeed = 5f;   // 平滑旋轉速度

    private Quaternion lastStableRotation;

    void Start()
    {
        if (target != null)
        {
            lastStableRotation = target.rotation;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 位置平滑追蹤
        Vector3 desiredPosition = target.TransformPoint(positionOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * positionSmoothSpeed);

        // 計算與上一次穩定角度的差距
        float angleDifference = Quaternion.Angle(lastStableRotation, target.rotation);

        if (angleDifference > rotationThreshold)
        {
            // 如果超過閾值就更新角度
            lastStableRotation = Quaternion.Slerp(lastStableRotation, target.rotation, Time.deltaTime * rotationSmoothSpeed);
        }

        transform.rotation = lastStableRotation;
    }
}
