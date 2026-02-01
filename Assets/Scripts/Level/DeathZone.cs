using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [Header("死亡区域设置")]
    [SerializeField] private bool respawnOnContact = true; // 接触时是否立即复活
    [SerializeField] private float respawnDelay = 0.5f; // 复活延迟时间

    [Header("视觉和音效")]
    [SerializeField] private GameObject deathEffect; // 死亡特效
    [SerializeField] private AudioClip deathSound; // 死亡音效

    private AudioSource audioSource;

    void Awake()
    {
        // 确保有正确的标签
        if (!gameObject.CompareTag("DeathZone"))
        {
            gameObject.tag = "DeathZone";
        }

        // 确保有触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;

        // 设置音频源
        if (deathSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否是玩家
        if (other.CompareTag("Player"))
        {
            HandlePlayerDeath(other.gameObject);
        }
    }

    private void HandlePlayerDeath(GameObject player)
    {
        Debug.Log($"玩家进入死亡区域: {gameObject.name}");

        // 播放死亡音效
        PlayDeathSound();

        // 播放死亡特效
        PlayDeathEffect(player.transform.position);

        if (respawnOnContact)
        {
            // 延迟复活玩家
            StartCoroutine(RespawnPlayerAfterDelay(player));
        }
    }

    private System.Collections.IEnumerator RespawnPlayerAfterDelay(GameObject player)
    {
        // 暂时禁用玩家控制
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
        }

        // 等待延迟时间
        yield return new WaitForSeconds(respawnDelay);

        // 复活玩家到最近的复活点
        Vector3 respawnPosition = RespawnPoint.GetCurrentRespawnPosition();
        if (respawnPosition != Vector3.zero)
        {
            player.transform.position = respawnPosition;

            // 重置玩家物理状态
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
            }

            Debug.Log($"玩家已复活到位置: {respawnPosition}");
        }
        else
        {
            Debug.LogError("没有找到可用的复活点！");
        }

        // 重新启用玩家控制
        if (playerController != null)
        {
            playerController.SetControlsEnabled(true);
        }
    }

    private void PlayDeathSound()
    {
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }

    private void PlayDeathEffect(Vector3 position)
    {
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, position, Quaternion.identity);

            // 自动销毁特效
            Destroy(effect, 2f);
        }
    }

    // 公共方法
    public void SetRespawnOnContact(bool respawn)
    {
        respawnOnContact = respawn;
    }

    public void SetRespawnDelay(float delay)
    {
        respawnDelay = Mathf.Max(0f, delay);
    }

    // Unity编辑器可视化
    private void OnDrawGizmos()
    {
        // 绘制死亡区域边界
        Gizmos.color = Color.red;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 选中时显示更详细的信息
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}