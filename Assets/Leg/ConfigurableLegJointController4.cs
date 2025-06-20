using UnityEngine;

public class ConfigurableLegJointController4 : MonoBehaviour
{
    [Header("Configurable Joints")]
    public ConfigurableJoint hipJoint;
    public ConfigurableJoint kneeJoint;
    public ConfigurableJoint ankleJoint;

    [Header("Input Keys")]
    public KeyCode hipForwardKey = KeyCode.Q;
    public KeyCode hipBackwardKey = KeyCode.A;
    public KeyCode kneeForwardKey = KeyCode.W;
    public KeyCode kneeBackwardKey = KeyCode.S;
    public KeyCode ankleForwardKey = KeyCode.E;
    public KeyCode ankleBackwardKey = KeyCode.D;

    [Header("Joint Settings")]
    public float targetVelocity = 30f;
    public float springForce = 200f;
    public float damper = 25f;

    [System.Serializable]
    public class LegJoints
    {
        public ConfigurableJoint hip;
        public ConfigurableJoint knee;
        public ConfigurableJoint ankle;
    }

    [Header("Legs")]
    public LegJoints[] legs;

    private void Start()
    {
        foreach (var leg in legs)
        {
            SetupJoint(leg.hip);
            SetupJoint(leg.knee);
            SetupJoint(leg.ankle);

            SetInitialTargetRotation(leg.hip, leg.knee, leg.ankle);
        }
    }

    void SetInitialTargetRotation(ConfigurableJoint hip, ConfigurableJoint knee, ConfigurableJoint ankle)
    {
        hip.targetRotation = ConvertLocalRotationToJointSpace(hip, Quaternion.Euler(-45f, 0f, 0f));
        knee.targetRotation = ConvertLocalRotationToJointSpace(knee, Quaternion.Euler(90f, 0f, 0f));
        ankle.targetRotation = ConvertLocalRotationToJointSpace(ankle, Quaternion.identity);
    }



    private void Update()
    {
        ControlJoint(hipJoint, hipForwardKey, hipBackwardKey);
        ControlJoint(kneeJoint, kneeForwardKey, kneeBackwardKey);
        ControlJoint(ankleJoint, ankleForwardKey, ankleBackwardKey);
    }
    // 將世界角度轉為關節可理解的本地空間旋轉
    Quaternion ConvertLocalRotationToJointSpace(ConfigurableJoint joint, Quaternion localRotation)
    {
        Quaternion worldToJointSpace = Quaternion.Inverse(Quaternion.LookRotation(joint.axis, joint.secondaryAxis));
        return worldToJointSpace * localRotation;
    }

    void ControlJoint(ConfigurableJoint joint, KeyCode forwardKey, KeyCode backwardKey)
    {
        JointDrive drive = joint.angularXDrive;

        if (Input.GetKey(forwardKey))
        {
            drive.positionSpring = springForce;
            drive.positionDamper = damper;
            drive.maximumForce = Mathf.Infinity;
            joint.targetRotation = ConvertLocalRotationToJointSpace(joint, Quaternion.Euler(-targetVelocity, 0, 0));
        }
        else if (Input.GetKey(backwardKey))
        {
            drive.positionSpring = springForce;
            drive.positionDamper = damper;
            drive.maximumForce = Mathf.Infinity;
            joint.targetRotation = ConvertLocalRotationToJointSpace(joint, Quaternion.Euler(targetVelocity, 0, 0));
        }
        else
        {
            drive.positionSpring = springForce;
            drive.positionDamper = damper;
            drive.maximumForce = Mathf.Infinity;
            joint.targetRotation = ConvertLocalRotationToJointSpace(joint, Quaternion.identity);
        }

        joint.angularXDrive = drive;

    }



    void SetupJoint(ConfigurableJoint joint)
    {
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        SoftJointLimit lowLimit = joint.lowAngularXLimit;
        lowLimit.limit = -45;
        joint.lowAngularXLimit = lowLimit;

        SoftJointLimit highLimit = joint.highAngularXLimit;
        highLimit.limit = 45;
        joint.highAngularXLimit = highLimit;


        JointDrive drive = joint.angularYZDrive;
        drive.positionSpring = springForce;
        drive.positionDamper = damper;
        drive.maximumForce = Mathf.Infinity;
        joint.angularYZDrive = drive;
    }



}
