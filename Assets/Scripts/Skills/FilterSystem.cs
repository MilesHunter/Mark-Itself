using UnityEngine;
using System.Collections.Generic;

public class FilterSystem : MonoBehaviour
{
    [Header("Filter Settings")]
    [SerializeField] private FilterColor currentFilterColor = FilterColor.Red;
    [SerializeField] private GameObject filterOverlay; // 滤镜覆盖层
    [SerializeField] private float filterAlpha = 0.7f;

    [Header("Layer Settings")]
    [SerializeField] private string interactionLayerName = "Interaction";
    [SerializeField] private string filterLayerName = "Filter";
    [SerializeField] private string backgroundLayerName = "Background";

    // 当前被滤镜影响的物体列表
    private List<GameObject> affectedObjects = new List<GameObject>();
    private bool isFilterActive = false;

    // 滤镜覆盖层组件
    private SpriteRenderer filterRenderer;

    // 事件
    public System.Action<FilterColor> OnFilterColorChanged;
    public System.Action<bool> OnFilterStateChanged;

    void Awake()
    {
        // 获取或创建滤镜覆盖层
        if (filterOverlay == null)
        {
            CreateFilterOverlay();
        }
        else
        {
            filterRenderer = filterOverlay.GetComponent<SpriteRenderer>();
        }

        // 初始化滤镜颜色
        UpdateFilterColor();
    }

    void Start()
    {
        // 确保滤镜初始状态为关闭
        DeactivateFilter();
    }

    private void CreateFilterOverlay()
    {
        // 创建滤镜覆盖层GameObject
        filterOverlay = new GameObject("FilterOverlay");
        filterOverlay.transform.SetParent(transform);

        // 添加SpriteRenderer组件
        filterRenderer = filterOverlay.AddComponent<SpriteRenderer>();

        // 设置为全屏大小的白色方块
        filterRenderer.sprite = CreateFullScreenSprite();
        filterRenderer.sortingLayerName = filterLayerName;
        filterRenderer.sortingOrder = 0;

        // 设置混合模式材质
        Material filterMaterial = new Material(Shader.Find("Sprites/Default"));
        filterRenderer.material = filterMaterial;

        // 初始状态为不可见
        filterOverlay.SetActive(false);
    }

    private Sprite CreateFullScreenSprite()
    {
        // 创建一个简单的白色纹理
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        // 创建Sprite
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    public void ActivateFilter()
    {
        if (isFilterActive) return;

        isFilterActive = true;
        filterOverlay.SetActive(true);

        // 应用滤镜效果到场景物体
        ApplyFilterToScene();

        // 调整滤镜覆盖层大小以覆盖整个屏幕
        //AdjustFilterOverlaySize();

        OnFilterStateChanged?.Invoke(true);
        Debug.Log($"Filter activated with color: {currentFilterColor}");
    }

    public void DeactivateFilter()
    {
        if (!isFilterActive) return;

        isFilterActive = false;
        filterOverlay.SetActive(false);

        // 恢复所有被影响的物体
        RestoreAffectedObjects();

        OnFilterStateChanged?.Invoke(false);
        Debug.Log("Filter deactivated");
    }

    public void SetFilterColor(FilterColor newColor)
    {
        if (currentFilterColor == newColor) return;

        currentFilterColor = newColor;
        UpdateFilterColor();

        // 如果滤镜当前激活，重新应用效果
        if (isFilterActive)
        {
            RestoreAffectedObjects();
            ApplyFilterToScene();
        }

        OnFilterColorChanged?.Invoke(currentFilterColor);
        Debug.Log($"Filter color changed to: {currentFilterColor}");
    }

    private void UpdateFilterColor()
    {
        if (filterRenderer != null)
        {
            Color filterColor = GameConstants.GetColor(currentFilterColor);
            filterColor.a = filterAlpha;
            filterRenderer.color = filterColor;
        }
    }

    private void ApplyFilterToScene()
    {
        // 清空之前的影响列表
        affectedObjects.Clear();

        // 获取当前滤镜颜色对应的标签
        string targetTag = GameConstants.GetColorTag(currentFilterColor);

        // 查找所有带有目标标签的物体
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);

        foreach (GameObject obj in taggedObjects)
        {
            // 禁用物体（使其不可见和不可交互）
            obj.SetActive(false);
            affectedObjects.Add(obj);
        }

        Debug.Log($"Applied filter to {affectedObjects.Count} objects with tag: {targetTag}");
    }

    private void RestoreAffectedObjects()
    {
        // 恢复所有被影响的物体
        foreach (GameObject obj in affectedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        affectedObjects.Clear();
        Debug.Log("Restored all affected objects");
    }

    private void AdjustFilterOverlaySize()
    {
        if (filterRenderer == null) return;

        // 获取主相机
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 计算相机视野范围
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // 调整滤镜覆盖层大小和位置
        filterOverlay.transform.position = mainCamera.transform.position;
        filterOverlay.transform.localScale = new Vector3(cameraWidth, cameraHeight, 1f);
    }

    // 公共方法
    public FilterColor GetCurrentFilterColor()
    {
        return currentFilterColor;
    }

    public bool IsFilterActive()
    {
        return isFilterActive;
    }

    public int GetAffectedObjectsCount()
    {
        return affectedObjects.Count;
    }

    // 在相机移动时更新滤镜位置
    void LateUpdate()
    {
        if (isFilterActive)
        {
            AdjustFilterOverlaySize();
        }
    }

    // Debug可视化
    private void OnDrawGizmosSelected()
    {
        if (isFilterActive && affectedObjects.Count > 0)
        {
            Gizmos.color = GameConstants.GetColor(currentFilterColor);
            foreach (GameObject obj in affectedObjects)
            {
                if (obj != null)
                {
                    Gizmos.DrawWireCube(obj.transform.position, obj.transform.localScale);
                }
            }
        }
    }
}