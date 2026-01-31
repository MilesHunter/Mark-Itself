using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("颜色选择系统")]
    [SerializeField] private Transform placeRGB; // PlaceRGB容器（包含Vertical Layout Group）
    [SerializeField] private GameObject colorButtonPrefab; // ColorButton预制体
    [SerializeField] private Color[] presetColors = new Color[] // 预设颜色数组
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.white
    };

    [Header("技能显示区域")]
    [SerializeField] private Image skillDisplayImage; // 技能显示图片
    [SerializeField] private Sprite fullFilterIcon; // FullFilter技能图标
    [SerializeField] private Sprite partMaskIcon; // PartMask技能图标

    [Header("暂停系统")]
    [SerializeField] private Button pauseButton; // 暂停按钮
    [SerializeField] private GameObject pausePanel; // 暂停界面面板
    [SerializeField] private Button escapeStuckButton; // 脱离卡死按钮
    [SerializeField] private Button returnToMenuButton; // 返回主菜单按钮
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // 主菜单场景名称

    // 私有变量
    private List<Button> colorButtons = new List<Button>(); // 颜色按钮列表
    private Color currentSelectedColor = Color.white; // 当前选择的颜色
    private int currentSelectedColorIndex = 0; // 当前选择的颜色索引
    private bool isPaused = false; // 游戏是否暂停
    private PlayerController playerController; // 玩家控制器引用

    // 单例模式
    public static UIManager Instance { get; private set; }

    // 事件系统
    public System.Action<Color> OnColorChanged; // 颜色改变事件
    public System.Action<bool> OnPauseStateChanged; // 暂停状态改变事件

    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 获取玩家控制器引用
        playerController = FindObjectOfType<PlayerController>();
    }

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }

    void Update()
    {
        HandleInput();
    }

    private void InitializeUI()
    {
        // 初始化颜色选择系统
        GenerateColorButtons();

        // 初始化技能显示
        UpdateSkillDisplay();

        // 初始化暂停界面状态
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // 设置初始颜色
        if (presetColors.Length > 0)
        {
            SelectColor(0);
        }
    }

    private void GenerateColorButtons()
    {
        if (placeRGB == null || colorButtonPrefab == null)
        {
            Debug.LogError("PlaceRGB容器或ColorButton预制体未设置！");
            return;
        }

        // 清除现有按钮
        foreach (Transform child in placeRGB)
        {
            Destroy(child.gameObject);
        }
        colorButtons.Clear();

        // 根据预设颜色数量生成按钮
        for (int i = 0; i < presetColors.Length; i++)
        {
            GameObject buttonObj = Instantiate(colorButtonPrefab, placeRGB);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                // 设置按钮颜色
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = presetColors[i];
                }

                // 添加点击事件
                int colorIndex = i; // 闭包变量
                button.onClick.AddListener(() => SelectColor(colorIndex));

                colorButtons.Add(button);
            }
        }

        Debug.Log($"生成了 {presetColors.Length} 个颜色选择按钮");
    }

    private void SetupEventListeners()
    {
        // 暂停按钮事件
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        // 脱离卡死按钮事件
        if (escapeStuckButton != null)
        {
            escapeStuckButton.onClick.AddListener(EscapeStuck);
        }

        // 返回主菜单按钮事件
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        // 监听玩家技能切换事件
        if (playerController != null)
        {
            // 假设PlayerController有技能切换事件
            // playerController.OnSkillChanged += UpdateSkillDisplay;
        }
    }

    private void HandleInput()
    {
        // ESC键处理暂停
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // 颜色选择相关方法
    public void SelectColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= presetColors.Length)
        {
            Debug.LogWarning($"颜色索引 {colorIndex} 超出范围！");
            return;
        }

        currentSelectedColorIndex = colorIndex;
        currentSelectedColor = presetColors[colorIndex];

        // 更新按钮视觉状态
        UpdateColorButtonStates();

        // 触发颜色改变事件
        OnColorChanged?.Invoke(currentSelectedColor);

        // 通知技能系统颜色改变
        NotifySkillSystemColorChange();

        Debug.Log($"选择了颜色: {currentSelectedColor}");
    }

    private void UpdateColorButtonStates()
    {
        for (int i = 0; i < colorButtons.Count; i++)
        {
            if (colorButtons[i] != null)
            {
                // 可以在这里添加选中状态的视觉效果
                // 比如边框、缩放等
                Transform buttonTransform = colorButtons[i].transform;
                if (i == currentSelectedColorIndex)
                {
                    buttonTransform.localScale = Vector3.one * 1.1f; // 选中时稍微放大
                }
                else
                {
                    buttonTransform.localScale = Vector3.one;
                }
            }
        }
    }

    private void NotifySkillSystemColorChange()
    {
        // 通知PlayerController颜色改变，让它处理技能系统的颜色更新
        if (playerController != null)
        {
            // 将Unity Color转换为FilterColor枚举
            FilterColor filterColor = ConvertColorToFilterColor(currentSelectedColor);
            playerController.SetSkillColor(filterColor);
        }
    }

    private FilterColor ConvertColorToFilterColor(Color color)
    {
        // 根据颜色值匹配对应的FilterColor枚举
        if (ColorApproximatelyEqual(color, Color.red))
            return FilterColor.Red;
        else if (ColorApproximatelyEqual(color, Color.green))
            return FilterColor.Green;
        else if (ColorApproximatelyEqual(color, Color.blue))
            return FilterColor.Blue;
        else if (ColorApproximatelyEqual(color, Color.yellow))
            return FilterColor.Yellow;
        else if (ColorApproximatelyEqual(color, Color.magenta))
            return FilterColor.Magenta;
        else if (ColorApproximatelyEqual(color, Color.cyan))
            return FilterColor.Cyan;
        else
            return FilterColor.Red; // 默认返回红色
    }

    private bool ColorApproximatelyEqual(Color a, Color b, float threshold = 0.1f)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold;
    }

    // 技能显示相关方法
    public void UpdateSkillDisplay()
    {
        if (skillDisplayImage == null || playerController == null)
            return;

        // 根据当前技能更新显示图标
        if (playerController.GetCurrentSkillType() == PlayerController.SkillType.FilterSystem)
        {
            skillDisplayImage.sprite = fullFilterIcon;
        }
        else if (playerController.GetCurrentSkillType() == PlayerController.SkillType.MaskSystem)
        {
            skillDisplayImage.sprite = partMaskIcon;
        }
    }

    // 暂停系统相关方法
    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPauseState(isPaused);
    }

    public void SetPauseState(bool paused)
    {
        isPaused = paused;

        // 设置时间缩放
        Time.timeScale = isPaused ? 0f : 1f;

        // 显示/隐藏暂停界面
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        // 禁用/启用玩家控制
        if (playerController != null)
        {
            playerController.SetControlsEnabled(!isPaused);
        }

        // 触发暂停状态改变事件
        OnPauseStateChanged?.Invoke(isPaused);

        Debug.Log($"游戏 {(isPaused ? "暂停" : "继续")}");
    }

    public void PauseGame()
    {
        SetPauseState(true);
    }

    public void ResumeGame()
    {
        SetPauseState(false);
    }

    // 暂停界面功能
    public void EscapeStuck()
    {
        Debug.Log("执行脱离卡死功能");

        // 恢复游戏时间
        ResumeGame();

        // 将玩家传送到最近的复活点
        if (playerController != null)
        {
            Vector3 respawnPosition = RespawnPoint.GetCurrentRespawnPosition();
            if (respawnPosition != Vector3.zero)
            {
                playerController.transform.position = respawnPosition;

                // 重置玩家物理状态
                Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.velocity = Vector2.zero;
                }

                Debug.Log($"玩家已传送到复活点: {respawnPosition}");
            }
            else
            {
                Debug.LogError("没有找到可用的复活点！");
            }
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("返回主菜单");

        // 恢复时间缩放
        Time.timeScale = 1f;

        // 加载主菜单场景
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // 公共访问方法
    public Color GetCurrentSelectedColor()
    {
        return currentSelectedColor;
    }

    public int GetCurrentSelectedColorIndex()
    {
        return currentSelectedColorIndex;
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    public void SetPresetColors(Color[] colors)
    {
        presetColors = colors;
        GenerateColorButtons();
    }

    public void SetMainMenuSceneName(string sceneName)
    {
        mainMenuSceneName = sceneName;
    }

    // Unity生命周期
    void OnDestroy()
    {
        // 清理事件监听
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
        }

        if (escapeStuckButton != null)
        {
            escapeStuckButton.onClick.RemoveAllListeners();
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
        }

        // 清理颜色按钮事件
        foreach (Button button in colorButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        // 恢复时间缩放
        Time.timeScale = 1f;
    }
}