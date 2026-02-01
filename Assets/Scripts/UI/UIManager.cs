using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("颜色选择系统")]
    [SerializeField] private Transform placeRGB; // PlaceRGB容器（包含Vertical Layout Group）
    [SerializeField] private GameObject colorButtonPrefab; // ColorButton预制体
    [SerializeField]
    private FilterColor[] presetColors = new FilterColor[] // 预设颜色数组
    {
        FilterColor.Red,
        FilterColor.Green,
        FilterColor.Blue
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
    private FilterColor currentSelectedFilterColor; // 当前选择的FilterColor枚举
    private int currentSelectedColorIndex = 0; // 当前选择的颜色索引
    private bool isPaused = false; // 游戏是否暂停
    private PlayerController playerController; // 玩家控制器引用

    // 单例模式
    public static UIManager Instance { get; private set; }

    // 事件系统
    public System.Action<FilterColor> OnFilterColorChanged; // 颜色改变事件，现在直接使用FilterColor
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
        if (playerController == null)
        {
            Debug.LogError("UIManager: PlayerController not found in the scene!", this);
        }
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
            SelectFilterColor(presetColors[0]); // 使用新的方法设置初始颜色
        }
    }

    private void GenerateColorButtons()
    {
        if (placeRGB == null || colorButtonPrefab == null)
        {
            Debug.LogError("PlaceRGB容器或ColorButton预制体未设置！", this);
            return;
        }

        // 清除现有按钮
        foreach (Transform child in placeRGB)
        {
            Destroy(child.gameObject);
        }
        colorButtons.Clear();
        Debug.Log($"当前有{presetColors.Length}个颜色");
        // 根据预设颜色数量生成按钮
        for (int i = 0; i < presetColors.Length; i++)
        {
            GameObject buttonObj = Instantiate(colorButtonPrefab, placeRGB);
            buttonObj.SetActive(true);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                // 设置按钮颜色
                Image buttonImage = button.GetComponent<Image>();
                FilterColor colorToAssign = presetColors[i]; // 捕获当前迭代的颜色

                if (buttonImage != null)
                {
                    buttonImage.color = GameConstants.GetColor(colorToAssign);
                    Debug.Log($"buttonImage Exists! The color is {buttonImage.color}");
                }

                // 修改：直接将 FilterColor 传递给一个新的 public 方法
                button.onClick.AddListener(() => SelectFilterColor(colorToAssign));

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
        //if (escapeStuckButton != null)
        //{
        //    escapeStuckButton.onClick.AddListener(EscapeStuck);
        //}

        // 返回主菜单按钮事件
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        // 监听玩家技能切换事件
        if (playerController != null)
        {
            // 假设PlayerController有技能切换事件，现在已在PlayerController中添加
            // playerController.OnSkillChanged += UpdateSkillDisplay;
            playerController.OnColorChanged += (newColor) => {
                // 当PlayerController的颜色通过其他方式改变时，更新UI的选中状态
                currentSelectedFilterColor = newColor;
                UpdateColorButtonStates();
            };
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

    /// <summary>
    /// 公开方法，用于UI按钮点击时选择一个FilterColor。
    /// </summary>
    /// <param name="selectedColor">要选择的FilterColor。</param>
    public void SelectFilterColor(FilterColor selectedColor)
    {
        // 查找当前颜色的索引
        int index = -1;
        for (int i = 0; i < presetColors.Length; i++)
        {
            if (presetColors[i] == selectedColor)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            Debug.LogWarning($"选择的颜色 {selectedColor} 不在预设颜色列表中！", this);
            return;
        }

        currentSelectedColorIndex = index;
        currentSelectedFilterColor = selectedColor;

        // 更新按钮视觉状态
        UpdateColorButtonStates();

        // 触发颜色改变事件
        OnFilterColorChanged?.Invoke(currentSelectedFilterColor);

        // 通知PlayerController颜色改变
        NotifySkillSystemColorChange(currentSelectedFilterColor);

        Debug.Log($"选择了颜色: {currentSelectedFilterColor}");
    }

    // 将原有的 SelectColor(int colorIndex) 调整为私有方法，并在内部调用 SelectFilterColor
    private void SelectColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= presetColors.Length)
        {
            Debug.LogWarning($"颜色索引 {colorIndex} 超出范围！", this);
            return;
        }
        SelectFilterColor(presetColors[colorIndex]);
    }


    private void UpdateColorButtonStates()
    {
        for (int i = 0; i < colorButtons.Count; i++)
        {
            if (colorButtons[i] != null)
            {
                Transform buttonTransform = colorButtons[i].transform;
                if (presetColors[i] == currentSelectedFilterColor) // 直接比较FilterColor枚举
                {
                    buttonTransform.localScale = Vector3.one * 1.1f; // 选中时稍微放大
                    // 可选：添加其他选中状态的视觉效果，例如修改Image的Sprite或添加边框
                }
                else
                {
                    buttonTransform.localScale = Vector3.one;
                }
            }
        }
    }

    private void NotifySkillSystemColorChange(FilterColor newColor)
    {
        // 通知PlayerController颜色改变，让它处理技能系统的颜色更新
        if (playerController != null)
        {
            playerController.SetSkillColor(newColor);
        }
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
    //public void EscapeStuck()
    //{
    //    Debug.Log("执行脱离卡死功能");

    //    // 恢复游戏时间
    //    ResumeGame();

    //    // 将玩家传送到最近的复活点
    //    if (playerController != null)
    //    {
    //        // Assuming RespawnPoint.GetCurrentRespawnPosition() is a static method returning Vector3
    //        // If it's not static, you'd need a reference to a RespawnPoint instance.
    //        // For now, I'll assume it's static or you have a way to get this.
    //        // If playerController also manages respawn points, you could call playerController.RespawnPlayer() directly.
    //        Vector3 respawnPosition = playerController.GetRespawnPoint(); // Use playerController's stored respawn point
    //        if (respawnPosition != Vector3.zero) // Check if the respawn point is valid
    //        {
    //            playerController.transform.position = respawnPosition;

    //            // 重置玩家物理状态
    //            Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
    //            if (playerRb != null)
    //            {
    //                playerRb.velocity = Vector2.zero;
    //            }

    //            Debug.Log($"玩家已传送到复活点: {respawnPosition}");
    //        }
    //        else
    //        {
    //            Debug.LogError("没有找到可用的复活点！");
    //        }
    //    }
    //}

    public void ReturnToMainMenu()
    {
        Debug.Log("返回主菜单");

        // 恢复时间缩放
        Time.timeScale = 1f;

        // 加载主菜单场景
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // 公共访问方法
    public FilterColor GetCurrentSelectedFilterColor()
    {
        return currentSelectedFilterColor;
    }

    public int GetCurrentSelectedColorIndex()
    {
        return currentSelectedColorIndex;
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    public void SetPresetColors(FilterColor[] colors)
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

        // 取消订阅PlayerController的事件，防止内存泄漏
        //if (playerController != null)
        //{
        //    playerController.OnSkillChanged -= UpdateSkillDisplay;
        //    // Also unsubscribe from OnColorChanged if you add it.
        //    // playerController.OnColorChanged -= (newColor) => { ... }; // This specific lambda needs to be re-assigned to a named method to unsubscribe properly.
        //}
    }
}