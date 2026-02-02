using UnityEngine;
using System.Collections;

public class CinematicTrigger : MonoBehaviour
{
    [Header("玩家设置")]
    public string playerTag = "Player"; // 玩家物体的Tag
    public MonoBehaviour playerMovementScript; // 玩家移动脚本的引用，需要禁用
    private GameObject player; // 假设玩家移动脚本的类型是PlayerMovement，你需要根据实际情况修改

    [Header("Sprite过渡设置")]
    public SpriteRenderer targetSpriteRenderer; // 目标SpriteRenderer
    public float spriteFadeDuration = 3f; // Sprite过渡持续时间

    private bool triggered = false;

    void Start()
    {
        PlayerController pc = player.GetComponent<PlayerController>();

        if (targetSpriteRenderer == null)
        {
            Debug.LogWarning("请在Inspector中为CinematicTrigger脚本设置Target Sprite Renderer引用。", this);
        }
        else
        {
            // 初始化时将Sprite的alpha设置为0
            Color spriteColor = targetSpriteRenderer.color;
            spriteColor.a = 0f;
            targetSpriteRenderer.color = spriteColor;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            triggered = true;
            Debug.Log("玩家进入触发器！");
            StartCinematicSequence();
        }
    }

    void StartCinematicSequence()
    {
        // 1. 禁用玩家移动
        if (player != null)
        {
            player.GetComponent<PlayerController>().enabled = false;
            Debug.Log("玩家移动已禁用。");
        }
        else
        {
            Debug.LogWarning("未找到玩家移动脚本，无法禁用玩家移动。");
        }

        // 3. 平滑过渡Sprite的Alpha值
        if (targetSpriteRenderer != null)
        {
            StartCoroutine(FadeSpriteAlpha(targetSpriteRenderer, 0f, 1f, spriteFadeDuration));
        }
    }

    IEnumerator FadeSpriteAlpha(SpriteRenderer spriteRenderer, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        Color spriteColor = spriteRenderer.color;
        while (timer < duration)
        {
            spriteColor.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            spriteRenderer.color = spriteColor;
            timer += Time.deltaTime;
            yield return null;
        }
        spriteColor.a = endAlpha;
        spriteRenderer.color = spriteColor;
        Debug.Log("Sprite Alpha过渡完成。");
    }
}