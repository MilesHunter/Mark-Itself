using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    [Header("按钮设置")]
    [SerializeField] private FilterColor buttonColor = FilterColor.Red;
    [SerializeField] private int colorIndex = 0;

    [Header("视觉反馈")]
    [SerializeField] private GameObject selectedIndicator; // 选中状态指示器
    [SerializeField] private float selectedScale = 1.1f; // 选中时的缩放
    [SerializeField] private float normalScale = 1f; // 正常状态的缩放
    [SerializeField] private float scaleAnimationSpeed = 5f; // 缩放动画速度

    [Header("音效")]
    [SerializeField] private AudioClip clickSound; // 点击音效

    // 组件引用
    private Button button;
    private Image buttonImage;
    private AudioSource audioSource;
    private RectTransform rectTransform;

    // 状态变量
    private bool isSelected = false;
    private Vector3 targetScale;

    // 事件
    public System.Action<int, FilterColor> OnColorButtonClicked;

    void Awake()
    {
        SetupComponents();
    }

    void Start()
    {
        InitializeButton();
    }

    void Update()
    {
        UpdateScaleAnimation();
    }

    private void SetupComponents()
    {
        // 获取必要组件
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // 设置音频源
        if (clickSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // 添加按钮点击事件
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void InitializeButton()
    {
        // 设置按钮颜色
        UpdateButtonColor();

        // 设置初始缩放
        targetScale = Vector3.one * normalScale;
        rectTransform.localScale = targetScale;

        // 设置选中状态指示器
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(false);
        }
    }

    private void UpdateButtonColor()
    {
        if (buttonImage != null)
        {
            // 根据FilterColor枚举设置按钮颜色
            Color unityColor = ConvertFilterColorToUnityColor(buttonColor);
            buttonImage.color = unityColor;
        }
    }

    private Color ConvertFilterColorToUnityColor(FilterColor filterColor)
    {
        switch (filterColor)
        {
            case FilterColor.Red:
                return GameConstants.RED_COLOR;
            case FilterColor.Green:
                return GameConstants.GREEN_COLOR;
            case FilterColor.Blue:
                return GameConstants.BLUE_COLOR;
            case FilterColor.Yellow:
                return GameConstants.YELLOW_COLOR;
            case FilterColor.Purple:
                return GameConstants.PURPLE_COLOR;
            default:
                return Color.white;
        }
    }

    private void OnButtonClick()
    {
        // 播放点击音效
        PlayClickSound();

        // 触发颜色选择事件
        OnColorButtonClicked?.Invoke(colorIndex, buttonColor);

        // 通知UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SelectColor(colorIndex);
        }

        Debug.Log($"颜色按钮被点击: {buttonColor} (索引: {colorIndex})");
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private void UpdateScaleAnimation()
    {
        // 平滑缩放动画
        if (rectTransform.localScale != targetScale)
        {
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.deltaTime * scaleAnimationSpeed
            );
        }
    }

    // 公共方法
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;

        // 更新缩放目标
        targetScale = Vector3.one * (selected ? selectedScale : normalScale);

        // 更新选中指示器
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(selected);
        }

        Debug.Log($"颜色按钮 {buttonColor} 选中状态: {selected}");
    }

    public void SetColorIndex(int index)
    {
        colorIndex = index;
    }

    public void SetButtonColor(FilterColor color)
    {
        buttonColor = color;
        UpdateButtonColor();
    }

    public void SetButtonColor(Color unityColor, FilterColor filterColor)
    {
        buttonColor = filterColor;
        if (buttonImage != null)
        {
            buttonImage.color = unityColor;
        }
    }

    public FilterColor GetButtonColor()
    {
        return buttonColor;
    }

    public int GetColorIndex()
    {
        return colorIndex;
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    // 设置按钮交互状态
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    // 设置选中状态的视觉效果参数
    public void SetScaleSettings(float normal, float selected, float speed)
    {
        normalScale = normal;
        selectedScale = selected;
        scaleAnimationSpeed = speed;

        // 更新当前目标缩放
        targetScale = Vector3.one * (isSelected ? selectedScale : normalScale);
    }

    // Unity生命周期
    void OnDestroy()
    {
        // 清理事件监听
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    // Unity编辑器中的辅助方法
    void OnValidate()
    {
        // 在编辑器中实时更新颜色
        if (Application.isPlaying && buttonImage != null)
        {
            UpdateButtonColor();
        }
    }
}