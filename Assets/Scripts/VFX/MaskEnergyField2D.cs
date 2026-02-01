using UnityEngine;
using System.Collections;

public class MaskEnergyField2D : MonoBehaviour
{
    [Header("Energy Field Settings")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private string sortingLayer = "Effects";
    [SerializeField] private int sortingOrder = 99;

    [Header("Components")]
    [SerializeField] private SpriteRenderer coreRenderer;
    [SerializeField] private SpriteRenderer outerRing;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private SpriteMask spriteMask; // 可选：用于遮罩效果

    private Color energyColor;
    private float timer = 0f;
    private bool isActive = true;

    void Awake()
    {
        // 确保渲染顺序
        if (coreRenderer != null)
        {
            coreRenderer.sortingLayerName = sortingLayer;
            coreRenderer.sortingOrder = sortingOrder;
        }

        if (outerRing != null)
        {
            outerRing.sortingLayerName = sortingLayer;
            outerRing.sortingOrder = sortingOrder - 1;
        }
    }

    public void Initialize(Color color)
    {
        energyColor = color;
        timer = 0f;
        isActive = true;

        // 设置核心颜色
        if (coreRenderer != null)
        {
            coreRenderer.color = new Color(color.r, color.g, color.b, 0.4f);
        }

        // 设置外环颜色
        if (outerRing != null)
        {
            outerRing.color = new Color(color.r, color.g, color.b, 0.2f);
        }

        // 设置粒子颜色
        if (particles != null)
        {
            var main = particles.main;
            main.startColor = color;
            particles.Play();
        }

        // 开始生命周期
        StartCoroutine(LifeTimeCoroutine());
    }

    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;

        // 脉冲效果
        float pulse = (Mathf.Sin(timer * pulseSpeed) + 1) * 0.5f; // 0到1之间

        // 缩放
        float scale = Mathf.Lerp(minScale, maxScale, pulse);
        transform.localScale = Vector3.one * scale;

        // 透明度变化
        if (coreRenderer != null)
        {
            Color coreColor = coreRenderer.color;
            coreColor.a = Mathf.Lerp(0.2f, 0.6f, pulse);
            coreRenderer.color = coreColor;
        }

        if (outerRing != null)
        {
            Color ringColor = outerRing.color;
            ringColor.a = Mathf.Lerp(0.1f, 0.3f, pulse);
            outerRing.color = ringColor;

            // 外环旋转
            outerRing.transform.Rotate(0, 0, 45 * Time.deltaTime);
        }

        // 核心旋转（反向）
        if (coreRenderer != null)
        {
            coreRenderer.transform.Rotate(0, 0, -30 * Time.deltaTime);
        }
    }

    private IEnumerator LifeTimeCoroutine()
    {
        yield return new WaitForSeconds(lifeTime);

        // 淡出效果
        float fadeTime = 0.5f;
        float elapsedTime = 0f;

        Color startCoreColor = coreRenderer?.color ?? Color.clear;
        Color startRingColor = outerRing?.color ?? Color.clear;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeTime;

            // 淡出核心
            if (coreRenderer != null)
            {
                Color currentColor = coreRenderer.color;
                currentColor.a = Mathf.Lerp(startCoreColor.a, 0, progress);
                coreRenderer.color = currentColor;
            }

            // 淡出外环
            if (outerRing != null)
            {
                Color currentColor = outerRing.color;
                currentColor.a = Mathf.Lerp(startRingColor.a, 0, progress);
                outerRing.color = currentColor;
            }

            // 缩小
            transform.localScale = Vector3.one * Mathf.Lerp(transform.localScale.x, 0, progress);

            yield return null;
        }

        // 停止粒子
        if (particles != null)
        {
            particles.Stop();
            yield return new WaitForSeconds(1f); // 等待粒子消失
        }

        Destroy(gameObject);
    }

    public void EndEarly()
    {
        isActive = false;
        StartCoroutine(LifeTimeCoroutine());
    }
}