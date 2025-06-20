using UnityEngine;

// 定義關節的控制類型
public enum ArticulationControlType { Armpit_XZ, Knee_Z_Only }

public class JointInitializer : MonoBehaviour
{
    [Header("關節基本設定")]
    public Rigidbody connectedBody; // 此關節連接到的父剛體
    [Tooltip("選擇此關節的控制類型：\nArmpit_XZ: 允許X和Z軸旋轉，Y軸鎖定 (適用於髖部/肩部)\nKnee_Z_Only: 只允許Z軸旋轉，X和Y軸鎖定 (適用於膝蓋/肘部，假設Z是彎曲軸)")]
    public ArticulationControlType articulationType = ArticulationControlType.Knee_Z_Only;

    [Header("通用驅動設定")]
    [Tooltip("是否讓此腳本設定關節的Slerp驅動")]
    public bool configureDrive = true;
    public float driveSpringForce = 1000f;
    public float driveDamperForce = 50f;
    public float driveMaxForce = float.MaxValue;

    [Header("角度限制 (正負範圍)")]
    [Tooltip("若為 Armpit_XZ，此為X軸的活動範圍 (+/- 值)")]
    public float limitX = 45f;
    [Tooltip("若為 Armpit_XZ，此為Z軸的活動範圍 (+/- 值)。若為 Knee_Z_Only，此為Z軸的活動範圍 (+/- 值)")]
    public float limitZ = 60f;
    // Y 軸通常被鎖定，所以不單獨設定 limitY，除非 articulationType 改變

    void Start()
    {
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        if (joint == null)
        {
            Debug.LogError("錯誤：" + gameObject.name + " 上沒有找到 ConfigurableJoint 元件！JointInitializer 無法工作。", gameObject);
            return;
        }

        if (connectedBody == null)
        {
            Debug.LogWarning("警告：JointInitializer 在 " + gameObject.name + " 上的 Connected Body 未指派！關節可能無法正常工作。", gameObject);
        }
        joint.connectedBody = connectedBody;

        // 標準化 ConfigurableJoint 的內部座標系：
        // ConfigurableJoint 的旋轉是相對於一個由 axis 和 secondaryAxis 定義的座標系。
        // 將 axis 設為 (1,0,0) (本地X)，secondaryAxis 設為 (0,1,0) (本地Y)，
        // 則 ConfigurableJoint 的 X, Y, Z 旋轉就分別對應 GameObject 本地座標系的 X, Y, Z 旋轉。
        joint.axis = Vector3.right;       // ConfigurableJoint 的 X 軸
        joint.secondaryAxis = Vector3.up; // ConfigurableJoint 的 Y 軸
                                          // ConfigurableJoint 的 Z 軸將是 Cross(axis, secondaryAxis) = (0,0,1)

        // 鎖定所有線性運動
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // 根據關節類型設定角向運動和限制
        SoftJointLimit limitValX = new SoftJointLimit { limit = this.limitX };
        SoftJointLimit limitValZ = new SoftJointLimit { limit = this.limitZ };
        SoftJointLimit lockedLimit = new SoftJointLimit { limit = 0 };

        switch (articulationType)
        {
            case ArticulationControlType.Armpit_XZ:
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Locked;  // Y軸鎖定
                joint.angularZMotion = ConfigurableJointMotion.Limited;

                joint.lowAngularXLimit = new SoftJointLimit { limit = -this.limitX }; // X軸限制
                joint.highAngularXLimit = limitValX;
                joint.angularYLimit = lockedLimit;                         // Y軸鎖定
                joint.angularZLimit = limitValZ;                         // Z軸限制 (對稱)
                Debug.Log(gameObject.name + " (JointInitializer) 設定為 Armpit_XZ: X Limit +/-" + this.limitX + ", Z Limit +/-" + this.limitZ + ", Y Locked.");
                break;

            case ArticulationControlType.Knee_Z_Only:
                joint.angularXMotion = ConfigurableJointMotion.Locked;  // X軸鎖定
                joint.angularYMotion = ConfigurableJointMotion.Locked;  // Y軸鎖定
                joint.angularZMotion = ConfigurableJointMotion.Limited; // 只允許Z軸

                joint.angularZLimit = limitValZ;                         // Z軸限制 (對稱)
                joint.lowAngularXLimit = lockedLimit;                    // X軸鎖定
                joint.highAngularXLimit = lockedLimit;
                joint.angularYLimit = lockedLimit;                         // Y軸鎖定
                Debug.Log(gameObject.name + " (JointInitializer) 設定為 Knee_Z_Only: Z Limit +/-" + this.limitZ + ", X, Y Locked.");
                break;
        }

        // 設定關節驅動 (如果勾選了)
        if (configureDrive)
        {
            JointDrive drive = new JointDrive
            {
                positionSpring = driveSpringForce,
                positionDamper = driveDamperForce,
                maximumForce = driveMaxForce
            };
            joint.slerpDrive = drive; // 使用 Slerp Drive 統一驅動到 targetRotation
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            Debug.Log(gameObject.name + " (JointInitializer) Slerp Drive 已設定 (Spring: " + driveSpringForce + ", Damper: " + driveDamperForce + ").", gameObject);
        }
        else
        {
            Debug.Log(gameObject.name + " (JointInitializer) 跳過 Slerp Drive 設定 (configureDrive=false)。請確保已手動在 Inspector 中為此 ConfigurableJoint 設定 Drive。", gameObject);
        }
        // 關節的初始 targetRotation 應為 Quaternion.identity (在其自身配置空間中無旋轉)
        // RobotAgent 的 OnEpisodeBegin 會重設 joint.transform.localRotation，並可能重設 targetRotation
        joint.targetRotation = Quaternion.identity;
        Debug.Log(gameObject.name + " (JointInitializer) 初始化完畢。", gameObject);
    }
}
