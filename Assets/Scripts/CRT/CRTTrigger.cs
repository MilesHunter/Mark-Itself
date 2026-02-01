using UnityEngine;
using System.Collections;

public class CRTTrigger : MonoBehaviour
{
    public Material crtMaterial; // 拖入你的 CRT 材质球

    [Header("Effect Settings")]
    public float duration = 0.15f;    // 效果持续时间
    public float maxFlash = 0.5f;     // 闪烁最高亮度
    public float maxJitter = 0.02f;   // 最大抖动幅度

    // 在你的 FilterSystem.cs 的 SetFilterColorAndTag 方法中调用这个
    public void TriggerGlitch()
    {
        StopAllCoroutines();
        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        float elapsed = 0;

        while (elapsed < duration)
        {
            float t = 1 - (elapsed / duration); // 随时间衰减

            // 设置 Shader 参数
            crtMaterial.SetFloat("_FlashIntensity", maxFlash * t);
            crtMaterial.SetFloat("_Jitter", maxJitter * t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 恢复正常
        crtMaterial.SetFloat("_FlashIntensity", 0);
        crtMaterial.SetFloat("_Jitter", 0);
    }
}