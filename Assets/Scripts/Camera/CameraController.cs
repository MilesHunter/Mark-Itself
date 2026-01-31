using UnityEngine;

/// <summary>
/// 相机控制器 - 实现平滑跟随玩家的2D相机系统
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private Transform target; // 跟随目标（玩家）
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -10); // 相机偏移量（Y轴稍微向上偏移）
    [SerializeField] private float smoothTime = 0.3f; // 平滑时间
    [SerializeField] private bool followX = true; // 是否跟随X轴（水平跟随）
    [SerializeField] private bool followY = false; // 是否跟随Y轴（垂直跟随，2D平台游戏通常关闭）

    [Header("边界限制")]
    [SerializeField] private bool useBounds = false; // 是否使用边界限制
    [SerializeField] private Vector2 minBounds = new Vector2(-10, -5); // 最小边界
    [SerializeField] private Vector2 maxBounds = new Vector2(10, 5); // 最大边界

    [Header("预测设置")]
    [SerializeField] private bool usePrediction = false; // 是否使用移动预测
    [SerializeField] private float predictionDistance = 2f; // 预测距离
    [SerializeField] private float predictionSmoothTime = 0.5f; // 预测平滑时间

    // 私有变量
    private Vector3 velocity = Vector3.zero; // SmoothDamp使用的速度变量
    private Vector3 predictionVelocity = Vector3.zero; // 预测平滑速度
    private Camera cam; // 相机组件
    private Rigidbody2D targetRigidbody; // 目标的刚体组件（用于预测）

    void Awake()
    {
        cam = GetComponent<Camera>();

        // 如果没有设置目标，尝试找到玩家
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // 获取目标的刚体组件（如果有的话）
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
        }
    }

    void Start()
    {
        // 如果有目标，立即设置到目标位置（避免初始跳跃）
        if (target != null)
        {
            Vector3 targetPosition = GetTargetPosition();
            transform.position = targetPosition;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 获取目标位置
        Vector3 targetPosition = GetTargetPosition();

        // 应用边界限制
        if (useBounds)
        {
            targetPosition = ApplyBounds(targetPosition);
        }

        // 平滑移动到目标位置
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.position = smoothedPosition;
    }

    /// <summary>
    /// 获取目标位置（包含预测和偏移）
    /// </summary>
    private Vector3 GetTargetPosition()
    {
        Vector3 basePosition = target.position;

        // 添加移动预测
        if (usePrediction && targetRigidbody != null)
        {
            Vector2 targetVelocity = targetRigidbody.velocity;
            Vector3 prediction = new Vector3(targetVelocity.x, targetVelocity.y, 0) * predictionDistance;

            // 平滑预测以避免抖动
            prediction = Vector3.SmoothDamp(Vector3.zero, prediction, ref predictionVelocity, predictionSmoothTime);
            basePosition += prediction;
        }

        // 应用轴向跟随设置
        Vector3 currentPos = transform.position;
        Vector3 targetPos = basePosition + offset;

        return new Vector3(
            followX ? targetPos.x : currentPos.x,
            followY ? targetPos.y : currentPos.y,
            offset.z // 保持Z轴偏移用于2D渲染
        );
    }

    /// <summary>
    /// 应用边界限制
    /// </summary>
    private Vector3 ApplyBounds(Vector3 targetPosition)
    {
        // 获取相机的视口边界
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        // 计算相机边界
        float minX = minBounds.x + camWidth;
        float maxX = maxBounds.x - camWidth;
        float minY = minBounds.y + camHeight;
        float maxY = maxBounds.y - camHeight;

        // 限制相机位置
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        return targetPosition;
    }

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
        }
    }

    /// <summary>
    /// 立即移动到目标位置（无平滑）
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        Vector3 targetPosition = GetTargetPosition();
        if (useBounds)
        {
            targetPosition = ApplyBounds(targetPosition);
        }

        transform.position = targetPosition;
        velocity = Vector3.zero; // 重置速度
    }

    /// <summary>
    /// 设置相机边界
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    /// <summary>
    /// 禁用边界限制
    /// </summary>
    public void DisableBounds()
    {
        useBounds = false;
    }

    /// <summary>
    /// 相机震动效果
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalOffset = offset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 添加随机偏移
            Vector3 randomOffset = Random.insideUnitSphere * magnitude;
            randomOffset.z = 0; // 保持Z轴不变
            offset = originalOffset + randomOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 恢复原始偏移
        offset = originalOffset;
    }

    // 在Scene视图中绘制边界线（仅在编辑器中）
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
        Gizmos.DrawWireCube(center, size);

        // 绘制相机视口边界
        if (cam != null)
        {
            Gizmos.color = Color.green;
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;
            Vector3 camSize = new Vector3(camWidth * 2, camHeight * 2, 0);
            Gizmos.DrawWireCube(transform.position, camSize);
        }
    }
}