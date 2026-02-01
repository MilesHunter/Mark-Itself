using UnityEngine;

// 需要给DeathZone GameObject 挂载一个 Collider2D 组件 (例如 BoxCollider2D)
// 并且勾选 Is Trigger
// 还需要给 DeathZone GameObject 设置 Tag 为 "DeathZone" (或 "Trap")

public class DeathZone : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private Color gizmoColor = Color.red;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查碰撞对象是否是玩家，这里假设玩家的 GameObject 挂载 PlayerController 脚本
        // 或者玩家的 Tag 是 "Player"
        if (other.CompareTag("Player")) // 确保玩家 GameObject 的 Tag 设置为 "Player"
        {
            Debug.Log($"Player entered DeathZone: {gameObject.name}. Notifying GameManager.");
            // 通知 GameManager 玩家需要重生
            // 确保 GameManager.Instance 已经被初始化
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerNeedsRespawn();
            }
            else
            {
                Debug.LogError("GameManager.Instance is null! Cannot notify player death.");
            }
        }
    }

    // 可选：在Scene视图中显示DeathZone的范围
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            if (collider is BoxCollider2D box)
            {
                Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
            }
            // 可以根据需要添加其他 Collider2D 类型的绘制
        }
        else
        {
            Gizmos.DrawCube(transform.position, Vector3.one); // 如果没有Collider，绘制一个默认大小的Cube
        }
    }
}