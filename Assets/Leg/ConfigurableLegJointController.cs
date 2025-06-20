using UnityEngine;

[System.Serializable]
public class LegJoints
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

public class ConfigurableLegJointController : MonoBehaviour
{
    [Header("Legs")]
    public LegJoints[] legs;

    [Header("Joint Settings")]
    public float targetVelocity = 30f;
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
        leg.hipJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.hipJoint, Quaternion.Euler(-45f, 0f, 0f));
        leg.kneeJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.kneeJoint, Quaternion.Euler(90f, 0f, 0f));
        leg.ankleJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.ankleJoint, Quaternion.identity);
    }

    void SetupJoint(ConfigurableJoint joint)
    {
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;


        // 限制 X 軸旋轉（不是本次主要使用，但防穿模保險）
        SoftJointLimit lowX = joint.lowAngularXLimit;
        lowX.limit = -45f;
        joint.lowAngularXLimit = lowX;

        SoftJointLimit highX = joint.highAngularXLimit;
        highX.limit = 45f;
        joint.highAngularXLimit = highX;

        // 限制 Y 軸旋轉（避免側翻）
        SoftJointLimit yLimit = joint.angularYLimit;
        yLimit.limit = 10f;
        joint.angularYLimit = yLimit;

        // 限制 Z 軸旋轉（主要控制方向）
        SoftJointLimit zLimit = joint.angularZLimit;
        zLimit.limit = 45f;
        joint.angularZLimit = zLimit;

        JointDrive drive = joint.angularYZDrive;
        drive.positionSpring = springForce;
        drive.positionDamper = damper;
        drive.maximumForce = Mathf.Infinity;
        joint.angularYZDrive = drive;
    }


    void ControlJoint(ConfigurableJoint joint, KeyCode forwardKey, KeyCode backwardKey)
    {
        JointDrive drive = joint.angularYZDrive;
        drive.positionSpring = springForce;
        drive.positionDamper = damper;
        drive.maximumForce = Mathf.Infinity;

        bool forward = Input.GetKey(forwardKey);
        bool backward = Input.GetKey(backwardKey);

        if (forward && !backward)
        {
            joint.targetRotation = ConvertLocalRotationToJointSpace(joint, Quaternion.Euler(0, 0, -targetVelocity));
        }
        else if (!forward && backward)
        {
            joint.targetRotation = ConvertLocalRotationToJointSpace(joint, Quaternion.Euler(0, 0, targetVelocity));
        }
        else
        {
            joint.targetRotation = ConvertLocalRotationToJointSpace(joint, Quaternion.identity);
        }

        joint.angularYZDrive = drive;
    }


    Quaternion ConvertLocalRotationToJointSpace(ConfigurableJoint joint, Quaternion localRotation)
    {
        Quaternion worldToJointSpace = Quaternion.Inverse(Quaternion.LookRotation(joint.axis, joint.secondaryAxis));
        return worldToJointSpace * localRotation;
    }
}

