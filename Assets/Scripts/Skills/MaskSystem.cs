using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MaskSystem : MonoBehaviour
{
    [Header("Hidden Object Pool")]
    [SerializeField] private Transform ts;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float maskAlpha = 0.4f;
    private Collider2D selfCollider;

    void Awake()
    {
        // 在Awake或Start中获取当前物体的Collider2D组件
        selfCollider = GetComponent<Collider2D>();
        if (selfCollider == null)
        {
            Debug.LogError("OverlapDetector requires a Collider2D component on the same GameObject.", this);
            enabled = false; // 如果没有Collider2D，禁用脚本
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
            if (otherCollider.gameObject.layer == gameObject.layer)
            {
                otherCollider.isTrigger = true;
            }
        }
    }

    // 当有其他Collider2D离开当前Collider2D的触发区域时调用
    // 这个方法可以用于在物体分离时进行一些清理工作，
    // 例如，如果你希望当它们不再重叠时，将 isTrigger 恢复到默认状态。
    // 在本例中，我们只在进入时修改，所以OnTriggerExit2D不强制需要，
    // 但根据具体游戏逻辑你可能需要它。
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
        Color tempColor = new Color(0, 0, 0);
        switch (col)
        {
            case FilterColor.Red:
                tempColor = new Color(255, 0, 0);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = maskAlpha;
                spriteRenderer.color = tempColor;
                break;
            case FilterColor.Green:
                tempColor = new Color(0, 255, 0);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = maskAlpha;
                spriteRenderer.color = tempColor;
                break;
            case FilterColor.Blue:
                tempColor = new Color(0, 0, 255);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = maskAlpha;
                spriteRenderer.color = tempColor;
                break;
            case FilterColor.Yellow:
                tempColor = new Color(255, 255, 0);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = maskAlpha;
                spriteRenderer.color = tempColor;
                break;
        }
    }
}