using UnityEngine;

/// <summary>
/// 這是一個簡單的「碰撞轉發器」腳本。
/// 它的唯一工作，就是將發生在自己身上的碰撞事件，報告給主 Agent 腳本。
/// </summary>
public class LimbCollisionForwarder : MonoBehaviour
{
    // 這個欄位用來在 Inspector 中存放主 Agent 腳本的引用
    public RobotAgent mainAgent;

    private void OnCollisionEnter(Collision collision)
    {
        // 檢查 mainAgent 是否已經被設定
        if (mainAgent != null)
        {
            // 當這個肢體發生碰撞時，立刻呼叫主 Agent 腳本的 ReportCollision 方法，
            // 並把完整的碰撞資訊 (collision) 和這個肢體自己的 Rigidbody 傳遞過去。
            mainAgent.ReportCollision(collision, this.GetComponent<Rigidbody>());
        }
        else
        {
            Debug.LogError("LimbCollisionForwarder 在物件 " + this.gameObject.name + " 上沒有設定 Main Agent！", this.gameObject);
        }
    }
}