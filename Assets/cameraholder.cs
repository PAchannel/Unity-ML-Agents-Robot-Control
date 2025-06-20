using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public Transform target;
    public Vector3 positionOffset = new Vector3(0, 0.5f, 0.2f);
    public float positionSmoothSpeed = 5f;

    public float rotationSensitivity = 2f;   // ��������F�ӫ�
    public float rotationThreshold = 1f;     // �X�ץH�������]���ݡ^
    public float rotationSmoothSpeed = 5f;   // ���Ʊ���t��

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

        // ��m���ưl��
        Vector3 desiredPosition = target.TransformPoint(positionOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * positionSmoothSpeed);

        // �p��P�W�@��í�w���ת��t�Z
        float angleDifference = Quaternion.Angle(lastStableRotation, target.rotation);

        if (angleDifference > rotationThreshold)
        {
            // �p�G�W�L�H�ȴN��s����
            lastStableRotation = Quaternion.Slerp(lastStableRotation, target.rotation, Time.deltaTime * rotationSmoothSpeed);
        }

        transform.rotation = lastStableRotation;
    }
}
