using UnityEngine;

[System.Serializable]
public class LegJoints2
{
    public ConfigurableJoint hipJoint;
    public ConfigurableJoint kneeJoint;
    public ConfigurableJoint ankleJoint;

    public KeyCode hipForwardKey;
    public KeyCode hipBackwardKey;
    public KeyCode kneeForwardKey;
    public KeyCode kneeBackwardKey;
    public KeyCode ankleForwardKey;
    public KeyCode ankleBackwardKey;
}

public class ConfigurableLegJointController2 : MonoBehaviour
{
    [Header("Legs")]
    public LegJoints[] legs;

    [Header("Joint Settings")]
    public float targetAngle = 45f;
    public float springForce = 200f;
    public float damper = 25f;

    private void Start()
    {
        foreach (var leg in legs)
        {
            SetupJoint(leg.hipJoint);
            SetupJoint(leg.kneeJoint);
            SetupJoint(leg.ankleJoint);

            SetInitialTargetRotation(leg);
        }
    }

    private void Update()
    {
        foreach (var leg in legs)
        {
            ControlJoint(leg.hipJoint, leg.hipForwardKey, leg.hipBackwardKey);
            ControlJoint(leg.kneeJoint, leg.kneeForwardKey, leg.kneeBackwardKey);
            ControlJoint(leg.ankleJoint, leg.ankleForwardKey, leg.ankleBackwardKey);
        }
    }

    void SetInitialTargetRotation(LegJoints leg)
    {
        leg.hipJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.hipJoint, Quaternion.Euler(0f, 0f, 0f));
        leg.kneeJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.kneeJoint, Quaternion.Euler(0f, 0f, 0f));
        leg.ankleJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.ankleJoint, Quaternion.Euler(0f, 0f, 0f));
    }

    void SetupJoint(ConfigurableJoint joint)
    {
        // 限制 X 軸旋轉 ±45°
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        SoftJointLimit lowX = joint.lowAngularXLimit;
        lowX.limit = -targetAngle;
        joint.lowAngularXLimit = lowX;

        SoftJointLimit highX = joint.highAngularXLimit;
        highX.limit = targetAngle;
        joint.highAngularXLimit = highX;

        // 關閉 Y/Z 軸旋轉
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        // 設定驅動參數（只對 angularXDrive 有效）
        JointDrive drive = new JointDrive
        {
            positionSpring = springForce,
            positionDamper = damper,
            maximumForce = Mathf.Infinity
        };
        joint.angularXDrive = drive;
    }

    void ControlJoint(ConfigurableJoint joint, KeyCode forwardKey, KeyCode backwardKey)
    {
        bool forward = Input.GetKey(forwardKey);
        bool backward = Input.GetKey(backwardKey);

        Quaternion targetRot;

        if (forward && !backward)
        {
            targetRot = Quaternion.Euler(targetAngle, 0f, 0f);
        }
        else if (!forward && backward)
        {
            targetRot = Quaternion.Euler(-targetAngle, 0f, 0f);
        }
        else
        {
            targetRot = Quaternion.identity;
        }

        joint.targetRotation = ConvertLocalRotationToJointSpace(joint, targetRot);
    }

    Quaternion ConvertLocalRotationToJointSpace(ConfigurableJoint joint, Quaternion localRotation)
    {
        Quaternion worldToJointSpace = Quaternion.Inverse(Quaternion.LookRotation(joint.axis, joint.secondaryAxis));
        return worldToJointSpace * localRotation;
    }
}
