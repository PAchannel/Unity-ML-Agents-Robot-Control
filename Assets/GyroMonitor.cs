using TMPro;
using UnityEngine;

public class GyroMonitor : MonoBehaviour
{
    [Header("本體 Rigidbody")] // 確保這個欄位在 Inspector 中已正確指派
    public Rigidbody body;

    [Header("UI 顯示文字 (可選)")] // 確保這個欄位在 Inspector 中已正確指派 (如果需要顯示)
    public TextMeshProUGUI displayText;

    // 供外部（例如 AI Agent）讀取角速度
    public Vector3 AngularVelocity => body != null ? body.angularVelocity : Vector3.zero;

    // 供外部讀取傾斜角度
    // 假設 Z 軸是機身前後方向的傾斜 (俯仰 Pitch)，X 軸是機身左右方向的傾斜 (翻滾 Roll)
    // 你需要根據你的模型實際的軸向來確認這是否正確
    public float ForwardTiltAngle => body != null ? NormalizeAngle(body.rotation.eulerAngles.z) : 0f;
    public float SideTiltAngle => body != null ? NormalizeAngle(body.rotation.eulerAngles.x) : 0f;

    // 將角度正規化到 [-180, 180] 範圍
    float NormalizeAngle(float angle)
    {
        angle = angle % 360; // 先取模確保在 (-360, 360) 之間
        if (angle > 180f) angle -= 360f;
        else if (angle < -180f) angle += 360f;
        return angle;
    }

    void Update()
    {
        if (displayText != null && body != null)
        {
            Vector3 angVel = AngularVelocity;
            float forwardTilt = ForwardTiltAngle;
            float sideTilt = SideTiltAngle;

            // 使用 " deg" 作為角度單位
            displayText.text = $"Angular Velocity:\nX: {angVel.x:F2} Y: {angVel.y:F2} Z: {angVel.z:F2}\n" +
                               $"Forward Tilt (Pitch): {forwardTilt:F1} deg\n" +
                               $"Side Tilt (Roll): {sideTilt:F1} deg";
        }
    }
}

