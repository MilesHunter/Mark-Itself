using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FilterEffectManager2D : MonoBehaviour
{
    [Header("2D Effect Prefabs")]
    [SerializeField] private WaveTransitionEffect2D filterWavePrefab;
    [SerializeField] private MaskEnergyField2D maskEnergyFieldPrefab; // 需要创建这个类
    [SerializeField] private ColorParticleEffect2D particleEffectPrefab;

    [Header("Screen Shake")]
    [SerializeField] private float screenShakeIntensity = 0.3f;
    [SerializeField] private float screenShakeDuration = 0.2f;
    [SerializeField] private float maskShakeMultiplier = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip filterSwitchSound;
    [SerializeField] private AudioClip maskSwitchSound;
    [SerializeField] private AudioClip colorChangeSound;

    [Header("Visual Settings")]
    [SerializeField] private float waveExpansionSpeed = 15f;
    [SerializeField] private float waveMaxSize = 20f;
    [SerializeField] private float effectDuration = 1.5f;

    [Header("Color Override")]
    [SerializeField] private bool overrideColors = false;
    [SerializeField] private Color[] customFilterColors = new Color[5];
    [SerializeField] private Color[] customMaskColors = new Color[5];

    private Camera mainCamera;
    private Vector3 cameraOriginalPos;
    private List<GameObject> activeEffects = new List<GameObject>();

    public static FilterEffectManager2D Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        mainCamera = Camera.main;
    }

    void OnDestroy()
    {
        // 清理所有活跃的效果
        foreach (var effect in activeEffects)
        {
            if (effect != null)
                Destroy(effect);
        }
        activeEffects.Clear();
    }

    // 公共接口：播放滤镜切换特效
    public void PlayFilterTransition(FilterColor color, Vector2 position)
    {
        StartCoroutine(FilterTransitionCoroutine(color, position));
    }

    // 公共接口：播放遮罩切换特效
    public void PlayMaskTransition(FilterColor color, Vector2 position)
    {
        StartCoroutine(MaskTransitionCoroutine(color, position));
    }

    // 公共接口：播放颜色改变特效（通用）
    public void PlayColorChangeEffect(FilterColor color, Vector2 position, bool isFilter = true)
    {
        StartCoroutine(ColorChangeCoroutine(color, position, isFilter));
    }

    private IEnumerator FilterTransitionCoroutine(FilterColor color, Vector2 position)
    {
        // 获取颜色
        Color effectColor = GetFilterColor(color);

        // 1. 播放音效
        PlaySound(filterSwitchSound);

        // 2. 轻微屏幕震动
        yield return StartCoroutine(ScreenShake(screenShakeIntensity));

        // 3. 创建波纹效果
        if (filterWavePrefab != null)
        {
            var wave = Instantiate(filterWavePrefab, position, Quaternion.identity);
            wave.Initialize(effectColor);
            wave.expansionSpeed = waveExpansionSpeed;
            wave.maxSize = waveMaxSize;

            activeEffects.Add(wave.gameObject);

            // 自动清理
            Destroy(wave.gameObject, effectDuration);
        }

        // 4. 创建粒子效果
        if (particleEffectPrefab != null)
        {
            var particles = Instantiate(particleEffectPrefab, position, Quaternion.identity);
            particles.PlayColorEffect(effectColor);

            activeEffects.Add(particles.gameObject);
        }

        // 5. 播放颜色改变音效（延迟一点）
        yield return new WaitForSeconds(0.1f);
        PlaySound(colorChangeSound);
    }

    private IEnumerator MaskTransitionCoroutine(FilterColor color, Vector2 position)
    {
        // 获取颜色
        Color effectColor = GetMaskColor(color);

        // 1. 播放音效
        PlaySound(maskSwitchSound);

        // 2. 更强屏幕震动
        yield return StartCoroutine(ScreenShake(screenShakeIntensity * maskShakeMultiplier));

        // 3. 创建能量场效果（如果存在）
        if (maskEnergyFieldPrefab != null)
        {
            var energyField = Instantiate(maskEnergyFieldPrefab, position, Quaternion.identity);
            energyField.Initialize(effectColor);

            activeEffects.Add(energyField.gameObject);
            Destroy(energyField.gameObject, effectDuration);
        }

        // 4. 创建环形粒子效果
        if (particleEffectPrefab != null)
        {
            var particles = Instantiate(particleEffectPrefab, position, Quaternion.identity);
            particles.PlayRippleEffect(position, effectColor, 1f);

            activeEffects.Add(particles.gameObject);
        }

        // 5. 创建向外发射的粒子
        if (particleEffectPrefab != null)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;

                var particles = Instantiate(particleEffectPrefab, position, Quaternion.identity);
                particles.PlayDirectedEffect(position, effectColor, direction);

                activeEffects.Add(particles.gameObject);
            }
        }

        // 6. 播放颜色改变音效
        yield return new WaitForSeconds(0.2f);
        PlaySound(colorChangeSound);
    }

    private IEnumerator ColorChangeCoroutine(FilterColor color, Vector2 position, bool isFilter)
    {
        Color effectColor = isFilter ? GetFilterColor(color) : GetMaskColor(color);

        // 播放音效
        PlaySound(colorChangeSound);

        // 创建小型粒子爆发
        if (particleEffectPrefab != null)
        {
            var particles = Instantiate(particleEffectPrefab, position, Quaternion.identity);
            particles.PlayColorEffect(effectColor);

            activeEffects.Add(particles.gameObject);
        }

        yield return null;
    }

    private IEnumerator ScreenShake(float intensity)
    {
        if (mainCamera == null) yield break;

        cameraOriginalPos = mainCamera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < screenShakeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 随机偏移
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            mainCamera.transform.position = cameraOriginalPos + new Vector3(x, y, 0);

            yield return null;
        }

        mainCamera.transform.position = cameraOriginalPos;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private Color GetFilterColor(FilterColor color)
    {
        if (overrideColors && customFilterColors.Length > (int)color)
        {
            return customFilterColors[(int)color];
        }
        return GameConstants.GetColor(color);
    }

    private Color GetMaskColor(FilterColor color)
    {
        if (overrideColors && customMaskColors.Length > (int)color)
        {
            return customMaskColors[(int)color];
        }
        return GameConstants.GetColor(color);
    }

    // 清除所有活跃效果
    public void ClearAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            if (effect != null)
                Destroy(effect);
        }
        activeEffects.Clear();
    }

    // 暂停所有效果
    public void PauseAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            if (effect != null)
            {
                var particle = effect.GetComponent<ParticleSystem>();
                if (particle != null)
                    particle.Pause();

                var wave = effect.GetComponent<WaveTransitionEffect2D>();
                if (wave != null)
                    wave.enabled = false;
            }
        }
    }

    // 恢复所有效果
    public void ResumeAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            if (effect != null)
            {
                var particle = effect.GetComponent<ParticleSystem>();
                if (particle != null)
                    particle.Play();

                var wave = effect.GetComponent<WaveTransitionEffect2D>();
                if (wave != null)
                    wave.enabled = true;
            }
        }
    }
}