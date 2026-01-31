using UnityEngine;
using System.Collections.Generic;

public class MaskSystem : MonoBehaviour
{
    [Header("Mask Settings")]
    [SerializeField] private FilterColor currentMaskColor = FilterColor.Red;
    [SerializeField] private float maskRadius = 2f;
    [SerializeField] private LayerMask hiddenObjectLayer = 1;
    [SerializeField] private GameObject maskPrefab; // 蒙版预制体

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask detectionLayer = 1;

    // 蒙版组件
    private SpriteMask spriteMask;
    private GameObject maskObject;
    private CircleCollider2D detectionCollider;

    // 当前被蒙版影响的隐藏物体列表
    private List<GameObject> revealedObjects = new List<GameObject>();
    private bool isMaskActive = false;

    // 跟随目标（通常是玩家）
    private Transform followTarget;

    // 事件
    public System.Action<FilterColor> OnMaskColorChanged;
    public System.Action<bool> OnMaskStateChanged;
    public System.Action<int> OnObjectsRevealed;

    void Awake()
    {
        followTarget = transform; // 默认跟随自身（玩家）

        // 创建或获取蒙版对象
        if (maskPrefab == null)
        {
            CreateMaskObject();
        }
        else
        {
            maskObject = Instantiate(maskPrefab, transform);
            spriteMask = maskObject.GetComponent<SpriteMask>();
        }

        // 创建检测范围
        CreateDetectionArea();
    }

    void Start()
    {
        // 确保蒙版初始状态为关闭
        DeactivateMask();
    }

    private void CreateMaskObject()
    {
        // 创建蒙版GameObject
        maskObject = new GameObject("MaskObject");
        maskObject.transform.SetParent(transform);
        maskObject.transform.localPosition = Vector3.zero;

        // 添加SpriteMask组件
        spriteMask = maskObject.AddComponent<SpriteMask>();
        spriteMask.sprite = CreateCircleMaskSprite();
        spriteMask.alphaCutoff = 0.1f;

        // 设置排序层
        spriteMask.frontSortingLayerID = SortingLayer.NameToID(GameConstants.LAYER_INTERACTION);
        spriteMask.backSortingLayerID = SortingLayer.NameToID(GameConstants.LAYER_BACKGROUND);

        // 初始状态为不可见
        maskObject.SetActive(false);
    }

    private void CreateDetectionArea()
    {
        // 创建检测区域GameObject
        GameObject detectionArea = new GameObject("DetectionArea");
        detectionArea.transform.SetParent(transform);
        detectionArea.transform.localPosition = Vector3.zero;

        // 添加CircleCollider2D作为触发器
        detectionCollider = detectionArea.AddComponent<CircleCollider2D>();
        detectionCollider.radius = detectionRadius;
        detectionCollider.isTrigger = true;

        // 添加检测脚本
        MaskDetectionArea detectionScript = detectionArea.AddComponent<MaskDetectionArea>();
        detectionScript.Initialize(this);
    }

    private Sprite CreateCircleMaskSprite()
    {
        // 创建圆形蒙版纹理
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float radius = textureSize / 2f - 2f; // 留一点边距

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                // 创建平滑的圆形边缘
                float alpha = distance <= radius ? 1f : 0f;
                if (distance > radius - 2f && distance <= radius)
                {
                    alpha = (radius - distance) / 2f; // 平滑边缘
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        // 创建Sprite
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize),
                           new Vector2(0.5f, 0.5f), textureSize / (maskRadius * 2f));
    }

    public void ActivateMask()
    {
        if (isMaskActive) return;

        isMaskActive = true;
        maskObject.SetActive(true);

        // 更新蒙版位置和大小
        UpdateMaskTransform();

        // 检测并显示范围内的隐藏物体
        RevealHiddenObjectsInRange();

        OnMaskStateChanged?.Invoke(true);
        Debug.Log("Mask activated");
    }

    public void DeactivateMask()
    {
        if (!isMaskActive) return;

        isMaskActive = false;
        maskObject.SetActive(false);

        // 隐藏所有被显示的物体
        HideRevealedObjects();

        OnMaskStateChanged?.Invoke(false);
        Debug.Log("Mask deactivated");
    }

    private void UpdateMaskTransform()
    {
        if (maskObject == null || followTarget == null) return;

        // 更新蒙版位置跟随目标
        maskObject.transform.position = followTarget.position;

        // 更新蒙版大小
        float scale = maskRadius * 2f;
        maskObject.transform.localScale = Vector3.one * scale;

        // 更新检测区域
        if (detectionCollider != null)
        {
            detectionCollider.transform.position = followTarget.position;
            detectionCollider.radius = detectionRadius;
        }
    }

    private void RevealHiddenObjectsInRange()
    {
        // 清空之前的列表
        HideRevealedObjects();

        // 获取当前蒙版颜色对应的标签
        string targetTag = GameConstants.GetColorTag(currentMaskColor);

        // 在检测范围内查找隐藏物体
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            followTarget.position, detectionRadius, hiddenObjectLayer);

        foreach (Collider2D col in colliders)
        {
            // 检查物体是否有HiddenObject组件且颜色匹配
            if (col.CompareTag(targetTag))
            {
                HiddenObject hiddenComponent = col.GetComponent<HiddenObject>();
                if (hiddenComponent != null)
                {
                    GameObject hiddenObj = col.gameObject;

                    // 显示隐藏物体
                    RevealObject(hiddenObj);
                    revealedObjects.Add(hiddenObj);
                }
            }
        }

        OnObjectsRevealed?.Invoke(revealedObjects.Count);
        Debug.Log($"Revealed {revealedObjects.Count} hidden objects with color: {currentMaskColor}");
    }

    private void RevealObject(GameObject obj)
    {
        // 启用物体的渲染器
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        // 启用碰撞器（如果需要交互）
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (collider != null && obj.GetComponent<HiddenObject>()?.enableColliderWhenRevealed == true)
        {
            collider.enabled = true;
        }

        // 播放显现特效
        PlayRevealEffect(obj.transform.position);
    }

    private void HideRevealedObjects()
    {
        foreach (GameObject obj in revealedObjects)
        {
            if (obj != null)
            {
                HideObject(obj);
            }
        }

        revealedObjects.Clear();
    }

    private void HideObject(GameObject obj)
    {
        // 禁用物体的渲染器
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // 禁用碰撞器
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void PlayRevealEffect(Vector3 position)
    {
        // TODO: 播放物体显现的粒子特效
        // 可以在这里实例化粒子系统或播放动画
        Debug.Log($"Playing reveal effect at {position}");
    }

    public void SetMaskColor(FilterColor newColor)
    {
        if (currentMaskColor == newColor) return;

        currentMaskColor = newColor;

        // 如果蒙版当前激活，重新扫描物体
        if (isMaskActive)
        {
            RevealHiddenObjectsInRange();
        }

        OnMaskColorChanged?.Invoke(currentMaskColor);
        Debug.Log($"Mask color changed to: {currentMaskColor}");
    }

    // 设置跟随目标
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    // 设置蒙版半径
    public void SetMaskRadius(float radius)
    {
        maskRadius = radius;
        if (isMaskActive)
        {
            UpdateMaskTransform();
        }
    }

    // 设置检测半径
    public void SetDetectionRadius(float radius)
    {
        detectionRadius = radius;
        if (detectionCollider != null)
        {
            detectionCollider.radius = radius;
        }
    }

    // 公共方法
    public bool IsMaskActive()
    {
        return isMaskActive;
    }

    public int GetRevealedObjectsCount()
    {
        return revealedObjects.Count;
    }

    public FilterColor GetMaskColor()
    {
        return currentMaskColor;
    }

    public float GetMaskRadius()
    {
        return maskRadius;
    }

    public float GetDetectionRadius()
    {
        return detectionRadius;
    }

    // 每帧更新蒙版位置
    void LateUpdate()
    {
        if (isMaskActive)
        {
            UpdateMaskTransform();
        }
    }

    // Debug可视化
    private void OnDrawGizmosSelected()
    {
        if (followTarget != null)
        {
            // 绘制蒙版范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(followTarget.position, maskRadius);

            // 绘制检测范围
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(followTarget.position, detectionRadius);

            // 绘制被显示的隐藏物体
            if (isMaskActive && revealedObjects.Count > 0)
            {
                Gizmos.color = Color.green;
                foreach (GameObject obj in revealedObjects)
                {
                    if (obj != null)
                    {
                        Gizmos.DrawWireCube(obj.transform.position, obj.transform.localScale);
                    }
                }
            }
        }
    }
}

// 蒙版检测区域辅助脚本
public class MaskDetectionArea : MonoBehaviour
{
    private MaskSystem maskSystem;

    public void Initialize(MaskSystem system)
    {
        maskSystem = system;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.TAG_HIDDEN_OBJECT) && maskSystem.IsMaskActive())
        {
            // 当新的隐藏物体进入检测范围时，重新扫描
            // 这里可以优化为只处理单个物体，但为了简单起见，重新扫描所有
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.TAG_HIDDEN_OBJECT) && maskSystem.IsMaskActive())
        {
            // 当隐藏物体离开检测范围时，重新扫描
        }
    }
}