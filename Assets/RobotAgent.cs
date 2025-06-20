using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic; // 使用 List<> 需要此命名空間

// 【參數表】
[System.Serializable]
public class TargetSettings
{
    [Header("目標設定")]
    [Tooltip("機器人的目標物件")]
    public Transform targetPoint;
    [Tooltip("啟用後，使用物理碰撞判定成功；否則使用距離判定")]
    public bool useCollisionForSuccess = true;
    [Tooltip("成功目標的標籤 (Tag)")]
    public string targetTag = "Target";
    [Tooltip("當 useCollisionForSuccess 為 false 時，判定成功的距離閾值")]
    public float successDistance = 1.0f;
    
    [Header("陷阱設定")]
    [Tooltip("陷阱物件的標籤 (Tag)")]
    public string trapTag = "Trap";
}

[System.Serializable]
public class MovementSettings
{
    [Tooltip("肩膀/臀部關節的最大目標角度")]
    public float armpitMaxTargetAngle = 45f;
    [Tooltip("膝蓋/手肘關節的最大目標角度")]
    public float kneeMaxTargetAngle = 60f;
}

[System.Serializable]
public class StabilitySettings
{
    [Header("跌倒偵測")]
    [Tooltip("身體傾斜超過此角度(度)將被判定為跌倒。數值越大，越不容易判定為跌倒。")]
    public float fallAngleThreshold = 85f;
    
    [Header("直立穩定性")]
    [Tooltip("身體傾斜在此角度內視為穩定")]
    public float maxStableAngle = 45f;

    [Header("身高管理")]
    [Tooltip("期望的最低身體離地高度")]
    public float desiredMinBodyHeight = 0.8f;
    [Tooltip("危險的過低身體離地高度")]
    public float dangerousLowBodyHeight = 0.3f;
    [Tooltip("用於偵測地面的圖層")]
    public LayerMask groundLayerMask;
    [Tooltip("偵測地面的射線最大長度")]
    public float maxRaycastDistance = 2f;
}

[System.Serializable]
public class RewardSettings
{
    [Header("目標達成獎勵")]
    public float successReward = 1.0f;
    
    [Header("過程獎勵")]
    [Tooltip("接近目標的位能獎勵因子")]
    public float targetProximityPotentialFactor = 15.0f;
    [Tooltip("鼓勵朝向目標移動的速度獎勵因子")]
    public float velocityTowardTargetReward = 0.1f;
    public float distanceEpsilon = 0.1f;

    [Header("穩定性獎勵")]
    public float stableAngleReward = 0.01f;
    public float bodyHeightReward = 0.02f;

    [Header("懲罰")]
    public float fallPenalty = -1.0f;
    public float lowBodyHeightPenalty = -0.03f;
    [Tooltip("碰到陷阱時的懲罰值")]
    public float trapPenalty = -1.0f;
    public float lowHeightPenaltyInterval = 1.0f;
}


public class RobotAgent : Agent
{
    [Header("參數表")]
    public TargetSettings targetSettings;
    public MovementSettings movementSettings;
    public StabilitySettings stabilitySettings;
    public RewardSettings rewardSettings;
    
    [Header("核心組件")]
    public Rigidbody body;
    public ConfigurableJoint[] joints = new ConfigurableJoint[8];
    [Tooltip("【指定成功部位】將帶有 Rigidbody 的肢體物件拖到此列表中。")]
    public List<Rigidbody> successTriggerBodies; 
    
    [Header("功能模組開關")]
    public bool enableProximityReward = true;
    public bool enableHeightManagement = true;
    public bool enableStableAngleReward = true;
    public bool enableFallPenaltyAndEndEpisode = true;
    
    private float previousDistanceToTarget;
    private float timeSinceLastLowHeightPenalty = 0f;

    public override void OnEpisodeBegin()
    {
        if (body == null || targetSettings.targetPoint == null) { return; }
        body.velocity = Vector3.zero; 
        body.angularVelocity = Vector3.zero;
        body.transform.localPosition = Vector3.zero; 
        body.transform.localRotation = Quaternion.identity;
        if (joints != null)
        {
            foreach (var joint in joints)
            {
                if (joint != null && joint.transform != null)
                {
                    joint.transform.localRotation = Quaternion.identity;
                    joint.targetRotation = Quaternion.identity;
                }
            }
        }
        previousDistanceToTarget = Vector3.Distance(body.transform.position, targetSettings.targetPoint.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (body == null || targetSettings.targetPoint == null || joints == null) { return; }
        sensor.AddObservation(body.velocity);
        sensor.AddObservation(body.angularVelocity);
        sensor.AddObservation(Vector3.Dot(Vector3.up, body.transform.up));
        sensor.AddObservation(targetSettings.targetPoint.position - body.transform.position);
        sensor.AddObservation(GetBodyHeight());
        foreach (var j in joints)
        {
            Transform t = j.transform;
            sensor.AddObservation(t.localRotation);
            Quaternion targetRotationInTransformLocal = ConvertJointConfigSpaceRotationToTransformLocalSpace(j, j.targetRotation);
            float angleError = Quaternion.Angle(t.localRotation, targetRotationInTransformLocal);
            sensor.AddObservation(angleError / 180f);
            Vector3 deltaEuler = ShortestEulerDiff(t.localRotation.eulerAngles, targetRotationInTransformLocal.eulerAngles);
            sensor.AddObservation(deltaEuler.x / 180f);
            sensor.AddObservation(deltaEuler.y / 180f);
            sensor.AddObservation(deltaEuler.z / 180f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (body == null || targetSettings.targetPoint == null || joints == null) { return; }
        var continuousActions = actions.ContinuousActions;
        int actionIndex = 0;
        foreach (var joint in joints)
        {
            Quaternion desiredRotation;
            if (IsArmpitTypeJoint(joint))
            {
                float x = Mathf.Clamp(continuousActions[actionIndex++], -1f, 1f) * movementSettings.armpitMaxTargetAngle;
                float z = Mathf.Clamp(continuousActions[actionIndex++], -1f, 1f) * movementSettings.armpitMaxTargetAngle;
                desiredRotation = Quaternion.Euler(x, 0f, z);
            }
            else
            {
                float z = Mathf.Clamp(continuousActions[actionIndex++], -1f, 1f) * movementSettings.kneeMaxTargetAngle;
                desiredRotation = Quaternion.Euler(0f, 0f, z);
            }
            joint.targetRotation = ConvertTransformLocalRotationToJointConfigSpace(joint, desiredRotation);
        }
        float currentDistanceToTarget = Vector3.Distance(body.transform.position, targetSettings.targetPoint.position);
        if (enableProximityReward)
        {
            float potentialCurrent = rewardSettings.targetProximityPotentialFactor / (currentDistanceToTarget + rewardSettings.distanceEpsilon);
            float potentialPrevious = rewardSettings.targetProximityPotentialFactor / (previousDistanceToTarget + rewardSettings.distanceEpsilon);
            AddReward(potentialCurrent - potentialPrevious);
            Vector3 directionToTarget = (targetSettings.targetPoint.position - body.transform.position).normalized;
            float velocityTowardsTarget = Vector3.Dot(body.velocity, directionToTarget);
            if (velocityTowardsTarget > 0)
            {
                AddReward(velocityTowardsTarget * rewardSettings.velocityTowardTargetReward);
            }
        }
        previousDistanceToTarget = currentDistanceToTarget;

        if (enableHeightManagement)
        {
            float currentHeight = GetBodyHeight();
            if (currentHeight >= stabilitySettings.desiredMinBodyHeight) { AddReward(rewardSettings.bodyHeightReward); timeSinceLastLowHeightPenalty = 0f; }
            else if (currentHeight < stabilitySettings.dangerousLowBodyHeight)
            {
                timeSinceLastLowHeightPenalty += Time.deltaTime;
                if (timeSinceLastLowHeightPenalty >= rewardSettings.lowHeightPenaltyInterval)
                { 
                    // 使用 rewardSettings 中的 lowBodyHeightPenalty
                    AddReward(rewardSettings.lowBodyHeightPenalty); 
                    timeSinceLastLowHeightPenalty = 0f; 
                }
            }
            else { timeSinceLastLowHeightPenalty = 0f; }
        }
        if (enableStableAngleReward)
        {
            float forwardTilt = NormalizeAngle(body.rotation.eulerAngles.z);
            float sideTilt = NormalizeAngle(body.rotation.eulerAngles.x);
            if (Mathf.Abs(forwardTilt) <= stabilitySettings.maxStableAngle) AddReward(rewardSettings.stableAngleReward);
            if (Mathf.Abs(sideTilt) <= stabilitySettings.maxStableAngle) AddReward(rewardSettings.stableAngleReward);
        }
        if (enableFallPenaltyAndEndEpisode)
        {
            float forwardTilt = NormalizeAngle(body.rotation.eulerAngles.z);
            float sideTilt = NormalizeAngle(body.rotation.eulerAngles.x);
            if (Mathf.Abs(forwardTilt) >= stabilitySettings.fallAngleThreshold || Mathf.Abs(sideTilt) >= stabilitySettings.fallAngleThreshold)
            {
                SetReward(rewardSettings.fallPenalty);
                EndEpisode(); return;
            }
        }
        if (!targetSettings.useCollisionForSuccess)
        {
            if (currentDistanceToTarget < targetSettings.successDistance)
            {
                SetReward(rewardSettings.successReward);
                EndEpisode(); return;
            }
        }
    }
    
    private void ProcessCollision(Collision other, Rigidbody hittingBody)
    {
        if (!targetSettings.useCollisionForSuccess) return;
        if (other.gameObject.CompareTag(targetSettings.targetTag))
        {
            if (hittingBody != null && successTriggerBodies.Contains(hittingBody))
            {
                Debug.Log("成功碰撞！指定的成功部位 [" + hittingBody.name + "] 已接觸到目標。");
                SetReward(rewardSettings.successReward);
                EndEpisode();
            }
        }
        else if (other.gameObject.CompareTag(targetSettings.trapTag))
        {
            Debug.Log("碰到了陷阱！接觸部位: [" + hittingBody.name + "]");
            SetReward(rewardSettings.trapPenalty);
            EndEpisode();
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        ProcessCollision(other, this.body);
    }
    
    public void ReportCollision(Collision other, Rigidbody reportingRigidbody)
    {
        ProcessCollision(other, reportingRigidbody);
    }
    
    #region 輔助函式
    private bool IsArmpitTypeJoint(ConfigurableJoint joint) { if (joint == null || string.IsNullOrEmpty(joint.name)) return false; string nameLower = joint.name.ToLower(); return nameLower.Contains("hip") || nameLower.Contains("shoulder") || nameLower.Contains("thigh") || nameLower.Contains("armpit"); }
    private float GetBodyHeight() { if (body == null) return 0f; RaycastHit hit; if (Physics.Raycast(body.transform.position, Vector3.down, out hit, stabilitySettings.maxRaycastDistance, stabilitySettings.groundLayerMask)) { return hit.distance; } return stabilitySettings.maxRaycastDistance; }
    private float NormalizeAngle(float angle) { angle %= 360; if (angle > 180f) angle -= 360f; else if (angle < -180f) angle += 360f; return angle; }
    private Vector3 ShortestEulerDiff(Vector3 eulerA, Vector3 eulerB) { return new Vector3(Mathf.DeltaAngle(eulerA.x, eulerB.x), Mathf.DeltaAngle(eulerA.y, eulerB.y), Mathf.DeltaAngle(eulerA.z, eulerB.z)); }
    private Quaternion ConvertTransformLocalRotationToJointConfigSpace(ConfigurableJoint joint, Quaternion desiredTransformLocalRotation) { if (joint == null) return Quaternion.identity; return Quaternion.Inverse(Quaternion.LookRotation(joint.axis, joint.secondaryAxis)) * desiredTransformLocalRotation; }
    private Quaternion ConvertJointConfigSpaceRotationToTransformLocalSpace(ConfigurableJoint joint, Quaternion jointConfigSpaceRotation) { if (joint == null) return Quaternion.identity; return Quaternion.LookRotation(joint.axis, joint.secondaryAxis) * jointConfigSpaceRotation; }
    public override void Heuristic(in ActionBuffers actionsOut) { /* 留空或根據需要實現 */ }
    
    void OnDrawGizmosSelected() 
    { 
        if (body != null) 
        { 
            Vector3 bodyPos = body.transform.position; 
            float rayStartOffset = 0.01f; 
            
            Gizmos.color = Color.green; 
            Vector3 desiredMinLineStart = bodyPos + Vector3.up * rayStartOffset; 
            Vector3 desiredMinLineEnd = desiredMinLineStart + Vector3.down * stabilitySettings.desiredMinBodyHeight; 
            Gizmos.DrawLine(desiredMinLineStart, desiredMinLineEnd); 
            Gizmos.DrawWireSphere(desiredMinLineEnd, 0.05f); 
            
            Gizmos.color = Color.red; 
            Vector3 dangerousLowLineStart = bodyPos + Vector3.up * rayStartOffset; 
            Vector3 dangerousLowLineEnd = dangerousLowLineStart + Vector3.down * stabilitySettings.dangerousLowBodyHeight; 
            Gizmos.DrawLine(dangerousLowLineStart, dangerousLowLineEnd); 
            Gizmos.DrawWireSphere(dangerousLowLineEnd, 0.05f); 
            
            if (Application.isPlaying) 
            { 
                RaycastHit hit; 
                if (Physics.Raycast(desiredMinLineStart, Vector3.down, out hit, stabilitySettings.maxRaycastDistance, stabilitySettings.groundLayerMask)) 
                { 
                    Gizmos.color = Color.yellow; 
                    Gizmos.DrawLine(desiredMinLineStart, hit.point); 
                    Gizmos.DrawSphere(hit.point, 0.05f); 
                } else 
                { 
                    Gizmos.color = Color.gray; 
                    Gizmos.DrawLine(desiredMinLineStart, desiredMinLineStart + Vector3.down * stabilitySettings.maxRaycastDistance); 
                } 
            } 
        } 
    }
    #endregion
}