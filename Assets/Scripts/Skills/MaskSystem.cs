using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using static UnityEngine.GraphicsBuffer; // This might cause issues if not used or if GraphicsBuffer isn't available

// Assuming FilterColor and GameConstants are defined elsewhere and accessible
public class MaskSystem : MonoBehaviour
{
    [Header("Hidden Object Pool")]
    [SerializeField] private Transform ts; // Not used in provided code, consider removing if not needed
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float maskAlpha = 0.4f;
    private Collider2D selfCollider;

    [Header("VFX")]
    [SerializeField] private bool playTransitionEffects = true;
    [SerializeField] private Transform effectOriginPoint;
    void Awake()
    {
        // 在Awake或Start中获取当前物体的Collider2D组件
        selfCollider = GetComponent<Collider2D>();
        if (selfCollider == null)
        {
            Debug.LogError("OverlapDetector requires a Collider2D component on the same GameObject.", this);
            enabled = false; // 如果没有Collider2D，禁用脚本
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("MaskSystem requires a SpriteRenderer component to change color.", this);
            }
        }
    }

    // 当有其他Collider2D进入当前Collider2D的触发区域时调用
    void OnTriggerEnter2D(Collider2D otherCollider)
    {
        // 检查碰撞到的物体是否有我们感兴趣的Tag
        // 假设我们只关心与带有Tag的物体进行比较
        if (otherCollider.gameObject.CompareTag(gameObject.tag))
        {
            // 如果Tag一致，取消勾选对方的Is Trigger
            Debug.Log($"物体 {gameObject.name} (Tag: {gameObject.tag}) 与 {otherCollider.gameObject.name} (Tag: {otherCollider.gameObject.tag}) 发生碰撞，Tag一致，取消勾选 {otherCollider.gameObject.name} 的 Is Trigger。");
            otherCollider.isTrigger = false;
        }
        else
        {
            // 如果Tag不一致，勾选对方的Is Trigger
            Debug.Log($"物体 {gameObject.name} (Tag: {gameObject.tag}) 与 {otherCollider.gameObject.name} (Tag: {otherCollider.gameObject.tag}) 发生碰撞，Tag不一致，勾选 {otherCollider.gameObject.name} 的 Is Trigger。");
            // Only modify if it's on the same layer to prevent unintended interactions with other game elements
            // Note: Your original code had layer == gameObject.layer, which might be too restrictive.
            // If interaction layer is different, you might need a specific layer mask.
            // For now, retaining the original logic for minimal change.
            if (otherCollider.gameObject.layer == gameObject.layer)
            {
                otherCollider.isTrigger = true;
            }
        }
    }

    // 当有其他Collider2D离开当前Collider2D的触发区域时调用
    void OnTriggerExit2D(Collider2D otherCollider)
    {
        // 示例：当分离时，可以考虑将对方的 isTrigger 恢复到默认值 (例如，都设为true)
        // 这取决于你的游戏逻辑，如果希望 isTrigger 状态只在重叠时改变，分离后恢复，则需要这个
        if (otherCollider.gameObject.layer == gameObject.layer)
        {
            otherCollider.isTrigger = true;
        }
        Debug.Log($"{gameObject.name} 和 {otherCollider.gameObject.name} 分离。");
    }


    public void SetMaskColor(FilterColor col)
    {

        // 播放切换特效
        if (playTransitionEffects && FilterEffectManager2D.Instance != null)
        {
            Vector3 effectPosition = effectOriginPoint != null ?
                effectOriginPoint.position : transform.position;
            FilterEffectManager2D.Instance.PlayMaskTransition(col, effectPosition);
        }

        Color tempColor; // Declare once

        // 1. Set the GameObject's Tag based on the new color
        gameObject.tag = GameConstants.GetColorTag(col);

        // 2. Set the SpriteRenderer's color
        if (spriteRenderer != null)
        {
            tempColor = GameConstants.GetColor(col);
            tempColor.a = maskAlpha;
            spriteRenderer.color = tempColor;
        }
        else
        {
            Debug.LogWarning("MaskSystem: SpriteRenderer is not assigned, cannot set visual color.", this);
        }
        Debug.Log($"MaskSystem color set to {col}, Tag set to: {gameObject.tag}", this);
    }
}