using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WaveTransitionEffect2D : MonoBehaviour
{
    [Header("Wave Settings")]
    public float expansionSpeed; // Changed from private to public
    public float maxSize; // Changed from private to public
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Visual Settings")]
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve distortionCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private SpriteRenderer spriteRenderer;
    private Color effectColor;
    private float currentSize = 0f;
    private float currentAlpha = 1f;
    private Vector3 originalScale;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        // 确保渲染顺序正确
        spriteRenderer.sortingOrder = 100;
    }

    public void Initialize(Color color)
    {
        effectColor = color;
        spriteRenderer.color = color;
        currentSize = 0f;
        currentAlpha = 1f;
        transform.localScale = originalScale;

        // 设置材质（如果有自定义Shader）
        if (spriteRenderer.material != null)
        {
            spriteRenderer.material.SetColor("_Color", color);
            spriteRenderer.material.SetFloat("_WaveOffset", 0);
        }
    }

    void Update()
    {
        if (currentSize < maxSize)
        {
            // 更新大小
            float progress = currentSize / maxSize;
            currentSize += expansionSpeed * Time.deltaTime;

            // 应用动画曲线
            float scaleMultiplier = scaleCurve.Evaluate(progress);
            float targetScale = Mathf.Lerp(0.1f, maxSize, scaleMultiplier);
            transform.localScale = originalScale * targetScale;

            // 更新透明度
            currentAlpha = alphaCurve.Evaluate(progress);
            Color newColor = effectColor;
            newColor.a = currentAlpha;
            spriteRenderer.color = newColor;

            // 添加扭曲效果（使用Shader或手动偏移）
            AddWaveDistortion(progress);

            // 更新Shader参数（如果有）
            if (spriteRenderer.material != null)
            {
                spriteRenderer.material.SetFloat("_WaveOffset", Time.time);
                spriteRenderer.material.SetFloat("_Progress", progress);
            }
        }
        else
        {
            // 淡出效果
            currentAlpha -= fadeSpeed * Time.deltaTime;
            if (currentAlpha <= 0)
            {
                Destroy(gameObject);
                return;
            }

            Color newColor = effectColor;
            newColor.a = currentAlpha;
            spriteRenderer.color = newColor;
        }
    }

    private void AddWaveDistortion(float progress)
    {
        // 简单的波动效果
        float distortion = Mathf.Sin(Time.time * 10f + progress * Mathf.PI * 2) * 0.05f;
        transform.localScale = transform.localScale * (1 + distortion);

        // 轻微旋转
        transform.Rotate(0, 0, Mathf.Sin(Time.time * 5f) * 0.5f);
    }

    // 创建Shader效果
    public void AddShaderEffects()
    {
        // 可以创建一个简单的Shader，或使用Unity的Sprite Shader
    }
}