using UnityEngine;
using System.Collections.Generic;

// 确保你已经有了 FilterColor 枚举和 GameConstants 类
// public enum FilterColor { Red, Blue, Green, Yellow, Purple }
// public static class GameConstants { ... } // 你的 GameConstants 类
// Assuming GameConstants and FilterColor are defined elsewhere and accessible

public class FilterSystem : MonoBehaviour
{

    [Header("VFX")]
    [SerializeField] private bool playTransitionEffects = true;
    [SerializeField] private Transform effectOriginPoint; // 特效起点（通常绑定到玩家）

    // 用于缓存找到的符合条件的GameObject，避免重复查找和内存抖动
    private List<GameObject> affectedObjects = new List<GameObject>();
    private float filterAlpha = 0.4f;

    // Interaction Layer 的索引。
    [SerializeField] private int interactionLayerIndex = 8; // **请根据你的Unity设置修改此值！**

    // 用于显示当前Filter颜色的SpriteRenderer
    private SpriteRenderer spriteRenderer;

    // 当前Filter的颜色，由外部设置
    [SerializeField] private FilterColor currentFilterColor = FilterColor.Red;

    // Filter的Tag，由currentFilterColor决定
    // 每次OnEnable/OnDisable前会重新计算，确保是最新的
    private string currentFilterTag;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("FilterSystem requires a SpriteRenderer component on the same GameObject to change color.", this);
        }

        // 确保Filter对象本身不在Interaction层
        if (gameObject.layer == interactionLayerIndex)
        {
            Debug.LogWarning($"FilterSystem GameObject '{gameObject.name}' is on the 'Interaction' layer. " +
                             "This might lead to unexpected behavior (e.g., filter disabling itself). " +
                             "Consider moving the FilterSystem to a different layer.", this);
        }

        // 在Awake时根据初始颜色设置一次自身的Tag和颜色，但不扫描场景
        // PlayerController will call SetFilterColorAndTag, so we just initialize our internal state.
        UpdateSelfFilterSettings(currentFilterColor);
    }

    // 当Filter GameObject被启用时调用
    void OnEnable()
    {
        // 在启用时，确保Filter自身的Tag和颜色是最新的
        UpdateSelfFilterSettings(currentFilterColor);

        // 然后根据最新的Tag扫描场景并禁用匹配的物体
        Debug.Log($"Filter '{gameObject.name}' (Tag: {currentFilterTag}) 启用。禁用场景中Interaction层的同Tag物体。", this);
        FindAndCacheAffectedObjects(); // 重新查找受影响的物体
        SetInteractionObjectsActive(false); // 禁用符合条件的物体
    }

    // 当Filter GameObject被禁用时调用
    void OnDisable()
    {
        // 在禁用时，确保Filter自身的Tag和颜色是最新的
        UpdateSelfFilterSettings(currentFilterColor);

        // 然后根据最新的Tag扫描场景并启用匹配的物体
        Debug.Log($"Filter '{gameObject.name}' (Tag: {currentFilterTag}) 禁用。启用场景中Interaction层的同Tag物体。", this);
        FindAndCacheAffectedObjects(); // 重新查找受影响的物体
        SetInteractionObjectsActive(true); // 启用符合条件的物体
    }

    /// <summary>
    /// 外部调用此方法来修改Filter的颜色和对应的Tag。
    /// 此方法只改变Filter自身的Tag和颜色，不会立即触发对场景中Interaction层物体的操作。
    /// 实际的场景物体操作会在Filter的OnEnable/OnDisable时发生。
    /// </summary>
    /// <param name="newColor">新的Filter颜色。</param>
    public void SetFilterColorAndTag(FilterColor newColor)
    {
        if (currentFilterColor != newColor)
        {
            // 播放切换特效
            if (playTransitionEffects && FilterEffectManager2D.Instance != null)
            {
                Vector3 effectPosition = effectOriginPoint != null ?
                    effectOriginPoint.position : transform.position;
                FilterEffectManager2D.Instance.PlayFilterTransition(newColor, effectPosition);
            }

            Debug.Log($"External call: Changing Filter color from {currentFilterColor} to {newColor}.", this);
            currentFilterColor = newColor;
            UpdateSelfFilterSettings(currentFilterColor);


            // --- 新增逻辑：触发屏幕抽搐 ---
            CRTTrigger trigger = FindFirstObjectByType<CRTTrigger>();
            if (trigger != null)
            {
                trigger.TriggerGlitch();
            }

            currentFilterColor = newColor;
            UpdateSelfFilterSettings(currentFilterColor);


            // If the filter is currently active, we need to refresh the affected objects
            // to reflect the new color immediately.
            if (gameObject.activeSelf)
            {
                // First, reactivate objects that were affected by the OLD color.
                // This requires us to know the old tag, which is why we must clear and re-scan.
                // A more robust solution might cache objects per color or use a global manager.
                // For this minimal change, we'll re-scan and apply the new state.
                Debug.Log($"Filter color changed while active. Refreshing affected objects for new color: {newColor}.", this);
                FindAndCacheAffectedObjects(); // Rescan with new tag
                SetInteractionObjectsActive(false); // Apply new state (disable for the new tag)
            }

        }
    }

    /// <summary>
    /// 内部方法：根据传入的FilterColor枚举，设置Filter自身的GameObject的Tag和SpriteRenderer的颜色。
    /// 此方法不触发现场中其他Interaction层物体的查找或状态改变。
    /// </summary>
    /// <param name="color">要应用的Filter颜色。</param>
    private void UpdateSelfFilterSettings(FilterColor color)
    {
        // 1. 设置Filter的GameObject的Tag
        currentFilterTag = GameConstants.GetColorTag(color);
        gameObject.tag = currentFilterTag; // 更新GameObject的Tag

        // 2. 设置Filter的SpriteRenderer的颜色
        if (spriteRenderer != null)
        {
            Color tempColor = GameConstants.GetColor(color);
            tempColor.a = filterAlpha;
            spriteRenderer.color = tempColor;
        }
        // Debug.Log($"Filter self settings updated to Tag: {currentFilterTag}, Color: {color}.", this);
    }


    /// <summary>
    /// 查找所有场景中Interaction层且Tag与Filter当前Tag相同的物体，并缓存到affectedObjects列表中。
    /// 此方法会在Filter启用/禁用时调用，以确保使用最新的Filter Tag。
    /// </summary>
    private void FindAndCacheAffectedObjects()
    {
        affectedObjects.Clear(); // 清空之前的列表

        // 使用 LayerMask 来更有效地过滤 GameObject
        int layerMask = 1 << interactionLayerIndex; // 创建一个只包含 Interaction 层的LayerMask

        // FindObjectsOfTypeAll 是在所有场景加载的物体中查找，包括非激活的
        // 但通常我们关心的是场景中的实际物体，所以我们遍历所有 Transform 并过滤
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform t in allTransforms)
        {
            // 过滤Editor内部对象和Prefab Assets
            if (t.gameObject.hideFlags == HideFlags.NotEditable || t.gameObject.hideFlags == HideFlags.HideAndDontSave)
            {
                continue;
            }
            if (t.gameObject.scene.rootCount == 0 && !string.IsNullOrEmpty(t.gameObject.scene.name))
            {
                continue;
            }

            // 忽略FilterSystem自身
            if (t.gameObject == this.gameObject) // Use 'this.gameObject' for clarity and safety
            {
                continue;
            }

            // 检查Layer是否匹配 Interaction Layer
            // 使用位运算检查Layer是否在LayerMask中
            if ((1 << t.gameObject.layer & layerMask) != 0)
            {
                // 检查Tag是否匹配Filter的当前Tag
                if (t.gameObject.CompareTag(currentFilterTag))
                {
                    affectedObjects.Add(t.gameObject);
                }
            }
        }
        Debug.Log($"FilterSystem found {affectedObjects.Count} objects in Interaction layer with tag '{currentFilterTag}'.", this);
    }


    /// <summary>
    /// 设置缓存列表中Interaction层且Tag与Filter相同的物体的Active状态。
    /// </summary>
    /// <param name="isActive">true为启用，false为禁用。</param>
    private void SetInteractionObjectsActive(bool isActive)
    {
        foreach (GameObject obj in affectedObjects)
        {
            // 只有当物体当前状态与目标状态不同时才改变，减少不必要的Set Active操作
            if (obj != null && obj.activeSelf != isActive)
            {
                obj.SetActive(isActive);
            }
        }
    }

    // 在Editor中添加一个按钮，用于手动刷新affectedObjects列表
    // 以防在运行时有新的Interaction物体被创建或销毁
    [ContextMenu("Refresh Affected Objects List")]
    private void EditorRefreshAffectedObjects()
    {
        // 手动刷新时也需要确保Filter自身的Tag和颜色是最新的
        UpdateSelfFilterSettings(currentFilterColor);
        FindAndCacheAffectedObjects();
    }
}