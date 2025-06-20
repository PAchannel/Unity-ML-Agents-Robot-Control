using UnityEngine;

[System.Serializable]
public class LegJoints3
{
    public ConfigurableJoint hipJoint;
    public ConfigurableJoint kneeJoint;

    public KeyCode hipXForwardKey;
    public KeyCode hipXBackwardKey;
    public KeyCode hipZOutwardKey;
    public KeyCode hipZInwardKey;

    public KeyCode kneeForwardKey;
    public KeyCode kneeBackwardKey;
}

public class ConfigurableLegJointController3 : MonoBehaviour
{
    [Header("Legs")]
    public LegJoints3[] legs;

    [Header("Hip Joint Limits")]
    public float hipXLimitNegative = -45f; // �e�\
    public float hipXLimitPositive = 45f;  // ���\
    public float hipZLimitNegative = -30f; // ����
    public float hipZLimitPositive = 30f;  // �~�i

    [Header("Knee Joint Limits")]
    public float kneeXLimitNegative = -90f; // �s��
    public float kneeXLimitPositive = 0f;   // ����

    [Header("Joint Drive Settings")]
    public float springForce = 200f;
    public float damper = 25f;

    private float[] hipXAngles;
    private float[] hipZAngles;
    private float[] kneeXAngles;

    private void Start()
    {
        hipXAngles = new float[legs.Length];
        hipZAngles = new float[legs.Length];
        kneeXAngles = new float[legs.Length];

        for (int i = 0; i < legs.Length; i++)
        {
            SetupHipJoint(legs[i].hipJoint);
            SetupKneeJoint(legs[i].kneeJoint);
        }
    }

    private void Update()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            var leg = legs[i];

            // Hip X �b����
            if (Input.GetKey(leg.hipXForwardKey))
                hipXAngles[i] = hipXLimitNegative;
            else if (Input.GetKey(leg.hipXBackwardKey))
                hipXAngles[i] = hipXLimitPositive;
            else
                hipXAngles[i] = 0f;

            // Hip Z �b����
            if (Input.GetKey(leg.hipZOutwardKey))
                hipZAngles[i] = hipZLimitPositive;
            else if (Input.GetKey(leg.hipZInwardKey))
                hipZAngles[i] = hipZLimitNegative;
            else
                hipZAngles[i] = 0f;

            Quaternion hipRot = Quaternion.Euler(hipXAngles[i], 0f, hipZAngles[i]);
            leg.hipJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.hipJoint, hipRot);

            // Knee ����
            if (Input.GetKey(leg.kneeForwardKey))
                kneeXAngles[i] = kneeXLimitNegative;
            else if (Input.GetKey(leg.kneeBackwardKey))
                kneeXAngles[i] = kneeXLimitPositive;
            else
                kneeXAngles[i] = 0f;

            Quaternion kneeRot = Quaternion.Euler(kneeXAngles[i], 0f, 0f);
            leg.kneeJoint.targetRotation = ConvertLocalRotationToJointSpace(leg.kneeJoint, kneeRot);
        }
    }

    void SetupHipJoint(ConfigurableJoint joint)
    {
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        joint.lowAngularXLimit = new SoftJointLimit { limit = hipXLimitNegative };
        joint.highAngularXLimit = new SoftJointLimit { limit = hipXLimitPositive };
        joint.angularZLimit = new SoftJointLimit { limit = Mathf.Max(Mathf.Abs(hipZLimitNegative), Mathf.Abs(hipZLimitPositive)) };

        JointDrive drive = new JointDrive
        {
            positionSpring = springForce,
            positionDamper = damper,
            maximumForce = Mathf.Infinity
        };
        joint.angularXDrive = drive;
        joint.angularYZDrive = drive;
    }

    void SetupKneeJoint(ConfigurableJoint joint)
    {
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        joint.lowAngularXLimit = new SoftJointLimit { limit = kneeXLimitNegative };
        joint.highAngularXLimit = new SoftJointLimit { limit = kneeXLimitPositive };

        JointDrive drive = new JointDrive
        {
            positionSpring = springForce,
            positionDamper = damper,
            maximumForce = Mathf.Infinity
        };
        joint.angularXDrive = drive;
    }

    Quaternion ConvertLocalRotationToJointSpace(ConfigurableJoint joint, Quaternion localRotation)
    {
        Quaternion worldToJointSpace = Quaternion.Inverse(Quaternion.LookRotation(joint.axis, joint.secondaryAxis));
        return worldToJointSpace * localRotation;
    }
}
