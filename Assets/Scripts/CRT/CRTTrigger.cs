using UnityEngine;
using System.Collections;

public class CRTTrigger : MonoBehaviour
{
    // 静态实例，方便其他脚本以后调用
    public static CRTTrigger Instance;

    [Header("绑定你的材质球")]
    public Material crtMaterial;

    [Header("效果参数")]
    public float duration = 0.15f;    // 持续时间
    public float maxFlash = 0.7f;     // 闪烁亮度
    public float maxJitter = 0.03f;   // 抖动幅度

    void Awake()
    {
        // 确保 Instance 定义正确
        if (Instance == null) Instance = this;

        // 游戏开始时重置 Shader 参数，防止残留效果
        ResetShader();
    }

    void Update()
    {
        // 只要检测到按下鼠标右键 (0是左键, 1是右键, 2是中键)
        if (Input.GetMouseButtonDown(1))
        {
            TriggerGlitch();
        }
    }

    public void TriggerGlitch()
    {
        if (crtMaterial == null) return;

        Debug.Log("右键点击：触发屏幕抽搐！");
        StopAllCoroutines();
        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = 1 - (elapsed / duration);

            // 修改 Shader 参数
            crtMaterial.SetFloat("_FlashIntensity", maxFlash * t);
            crtMaterial.SetFloat("_Jitter", maxJitter * t);

            elapsed += Time.deltaTime;
            yield return null;
        }
        ResetShader();
    }

    private void ResetShader()
    {
        if (crtMaterial != null)
        {
            crtMaterial.SetFloat("_FlashIntensity", 0);
            crtMaterial.SetFloat("_Jitter", 0);
        }
    }

    // 退出游戏时也清理一下
    private void OnDisable() => ResetShader();
}