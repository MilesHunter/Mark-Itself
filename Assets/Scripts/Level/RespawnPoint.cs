using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [Header("基础设置")]
    [SerializeField] private bool isDefaultSpawn = false; // 是否为默认出生点
    [SerializeField] private float activationRadius = 1.5f; // 激活半径

    [Header("视觉反馈 (可选)")]
    [SerializeField] private GameObject activeIndicator; // 激活状态指示器
    [SerializeField] private GameObject inactiveIndicator; // 未激活状态指示器
    [SerializeField] private AudioClip activationSound; // 激活音效

    [Header("复活设置")]
    [SerializeField] private Vector3 respawnOffset = new Vector3(0, 0.5f, 0); // 复活位置偏移

    // 私有变量
    private bool hasBeenActivated = false;
    private CircleCollider2D triggerCollider;
    private AudioSource audioSource;

    // 静态变量 - 当前激活的复活点
    private static RespawnPoint currentRespawnPoint;

    void Awake()
    {
        SetupComponents();
    }

    void Start()
    {
        // 如果是默认出生点，立即激活
        if (isDefaultSpawn)
        {
            ActivateRespawnPoint();
        }

        UpdateVisualState();
    }

    private void SetupComponents()
    {
        // 自动添加触发器
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.radius = activationRadius;

        // 自动设置标签
        if (!gameObject.CompareTag("RespawnPoint"))
        {
            gameObject.tag = "RespawnPoint";
        }

        // 设置音频源
        if (activationSound != null)
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
        // 玩家触碰时激活复活点
        if (other.CompareTag("Player") && !hasBeenActivated)
        {
            ActivateRespawnPoint();
        }
    }

    public void ActivateRespawnPoint()
    {
        if (hasBeenActivated) return;

        hasBeenActivated = true;
        currentRespawnPoint = this;

        // 播放音效
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }

        // 更新视觉状态
        UpdateVisualState();

        // 通知玩家控制器更新复活点
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetRespawnPoint(GetRespawnPosition());
        }

        Debug.Log($"复活点 {gameObject.name} 已激活");
    }

    private void UpdateVisualState()
    {
        // 更新视觉指示器
        if (activeIndicator != null)
            activeIndicator.SetActive(hasBeenActivated);

        if (inactiveIndicator != null)
            inactiveIndicator.SetActive(!hasBeenActivated);
    }

    public Vector3 GetRespawnPosition()
    {
        return transform.position + respawnOffset;
    }

    // 静态方法 - 获取当前激活的复活点
    public static Vector3 GetCurrentRespawnPosition()
    {
        if (currentRespawnPoint != null)
            return currentRespawnPoint.GetRespawnPosition();

        // 如果没有激活的复活点，寻找默认出生点
        RespawnPoint defaultSpawn = FindDefaultSpawnPoint();
        if (defaultSpawn != null)
        {
            defaultSpawn.ActivateRespawnPoint();
            return defaultSpawn.GetRespawnPosition();
        }

        return Vector3.zero;
    }

    private static RespawnPoint FindDefaultSpawnPoint()
    {
        RespawnPoint[] allRespawnPoints = FindObjectsOfType<RespawnPoint>();
        foreach (RespawnPoint point in allRespawnPoints)
        {
            if (point.isDefaultSpawn)
                return point;
        }
        return null;
    }

    // 公共方法
    public bool IsActivated() => hasBeenActivated;
    public bool IsDefaultSpawn() => isDefaultSpawn;

    // Unity编辑器中的可视化
    private void OnDrawGizmosSelected()
    {
        // 绘制激活范围
        Gizmos.color = hasBeenActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        // 绘制复活位置
        Vector3 respawnPos = transform.position + respawnOffset;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(respawnPos, Vector3.one * 0.5f);
        Gizmos.DrawLine(transform.position, respawnPos);
    }

    private void OnDrawGizmos()
    {
        // 默认出生点用特殊颜色标识
        if (isDefaultSpawn)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, activationRadius * 0.8f);
        }
    }
}