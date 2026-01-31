using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HiddenObject : MonoBehaviour
{
    [Header("Hidden Object Settings")]
    [SerializeField] public bool enableColliderWhenRevealed = true;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private float revealAnimationDuration = 0.3f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject revealEffect; // 显现特效预制体
    [SerializeField] private AudioClip revealSound;   // 显现音效

    // 组件引用
    private SpriteRenderer spriteRenderer;
    private Collider2D objectCollider;
    private AudioSource audioSource;

    // 状态
    private bool isRevealed = false;
    private bool isAnimating = false;

    // 原始透明度值
    private float originalAlpha;

    // 事件
    public System.Action<HiddenObject> OnObjectRevealed;
    public System.Action<HiddenObject> OnObjectHidden;

    void Awake()
    {
        // 获取组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        objectCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();

        // 记录原始透明度
        originalAlpha = spriteRenderer.color.a;

        //// 确保物体有正确的标签
        //if (!gameObject.CompareTag(GameConstants.TAG_HIDDEN_OBJECT))
        //{
        //    // gameObject.tag = GameConstants.TAG_HIDDEN_OBJECT;
        //    Debug.LogWarning($"HiddenObject {gameObject.name} didn't have the correct tag. Auto-assigned {GameConstants.TAG_HIDDEN_OBJECT}");
        //}
    }

    void Start()
    {
        //if (startHidden)
        //{
        //    HideImmediate();
        //}
    }

    public void RevealObject()
    {
        if (isRevealed || isAnimating) return;

        isAnimating = true;

        // 启用渲染器
        spriteRenderer.enabled = true;

        // 启用碰撞器（如果设置为显现时启用）
        if (enableColliderWhenRevealed && objectCollider != null)
        {
            objectCollider.enabled = true;
        }

        // 播放显现动画
        StartCoroutine(RevealAnimation());

        // 播放音效
        PlayRevealSound();

        // 播放特效
        PlayRevealEffect();

        // 触发事件
        OnObjectRevealed?.Invoke(this);

        Debug.Log($"Hidden object {gameObject.name} revealed");
    }

    public void HideObject()
    {
        if (!isRevealed || isAnimating) return;

        isAnimating = true;

        // 播放隐藏动画
        StartCoroutine(HideAnimation());

        // 触发事件
        OnObjectHidden?.Invoke(this);

        Debug.Log($"Hidden object {gameObject.name} hidden");
    }

    public void HideImmediate()
    {
        isRevealed = false;
        isAnimating = false;

        // 禁用渲染器
        spriteRenderer.enabled = false;

        // 禁用碰撞器
        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }

        // 重置透明度
        Color color = spriteRenderer.color;
        color.a = 0f;
        spriteRenderer.color = color;
    }

    private System.Collections.IEnumerator RevealAnimation()
    {
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;
        color.a = 0f;
        spriteRenderer.color = color;

        while (elapsedTime < revealAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / revealAnimationDuration;

            // 使用缓动函数让动画更自然
            float easedProgress = EaseOutCubic(progress);

            // 更新透明度
            color.a = Mathf.Lerp(0f, originalAlpha, easedProgress);
            spriteRenderer.color = color;

            // 可选：添加缩放效果
            float scale = Mathf.Lerp(0.8f, 1f, easedProgress);
            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // 确保最终状态正确
        color.a = originalAlpha;
        spriteRenderer.color = color;
        transform.localScale = Vector3.one;

        isRevealed = true;
        isAnimating = false;
    }

    private System.Collections.IEnumerator HideAnimation()
    {
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;

        while (elapsedTime < revealAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / revealAnimationDuration;

            // 使用缓动函数
            float easedProgress = EaseInCubic(progress);

            // 更新透明度
            color.a = Mathf.Lerp(originalAlpha, 0f, easedProgress);
            spriteRenderer.color = color;

            // 可选：添加缩放效果
            float scale = Mathf.Lerp(1f, 0.8f, easedProgress);
            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // 最终隐藏
        spriteRenderer.enabled = false;
        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }

        transform.localScale = Vector3.one;
        isRevealed = false;
        isAnimating = false;
    }

    private void PlayRevealSound()
    {
        if (revealSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(revealSound);
        }
    }

    private void PlayRevealEffect()
    {
        if (revealEffect != null)
        {
            GameObject effect = Instantiate(revealEffect, transform.position, Quaternion.identity);

            // 自动销毁特效（假设特效持续2秒）
            Destroy(effect, 2f);
        }
    }

    // 缓动函数
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }

    // 公共方法
    public bool IsRevealed()
    {
        return isRevealed;
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }

    public void SetRevealAnimationDuration(float duration)
    {
        revealAnimationDuration = Mathf.Max(0.1f, duration);
    }

    public void SetRevealEffect(GameObject effect)
    {
        revealEffect = effect;
    }

    public void SetRevealSound(AudioClip sound)
    {
        revealSound = sound;
    }

    // Debug可视化
    private void OnDrawGizmosSelected()
    {
        // 绘制物体边界
        Gizmos.color = isRevealed ? Color.green : Color.red;

        Bounds bounds = GetComponent<Renderer>()?.bounds ?? new Bounds(transform.position, Vector3.one);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // 绘制标识
        Gizmos.color = Color.yellow;
        Gizmos.DrawIcon(transform.position, "HiddenObject", true);
    }
}