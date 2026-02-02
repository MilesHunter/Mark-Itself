using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic; // 添加这个命名空间


public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentState = GameState.Playing;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    // 移除 playerRespawnPoint，现在由 RespawnPoint 列表管理

    [Header("UI References")]
    public UIManager uiManager;
    public GameObject pauseMenu;
    public GameObject gameOverMenu; // 游戏结束菜单可能不再需要，因为没有生命值

    [Header("Game Settings")]
    public float respawnDelay = 1f; // 重生延迟
    public bool canPause = true;

    [Header("Spawn Points")]
    public List<RespawnPoint> allRespawnPoints = new List<RespawnPoint>(); // 新增：所有重生点的配置表

    // Singleton pattern
    public static GameManager Instance { get; private set; }

    // Game components
    private PlayerController player;
    private GameObject filterSystem;
    private GameObject maskSystem;

    // Game data (分数和时间保持，生命值移除)
    private int score = 0;
    private float gameTime = 0f;

    // Events (移除 OnLivesChanged, OnPlayerDeath 含义变为“玩家需要重生”)
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<int> OnScoreChanged;
    public System.Action OnPlayerNeedRespawn; // 当玩家需要重生时触发

    // 假设 PlayerController 存在一个接口或公共方法来设置其状态
    // 或者 GameManager 会直接收到玩家死亡的通知
    public enum PlayerState { Alive, Dead, Stunned /*或其他状态*/ } // 假设玩家有状态枚举

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetGameState(GameState.Playing);
    }

    void Update()
    {
        HandleInput();
        UpdateGameTime();
    }

    #region Game Initialization
    private void InitializeGame()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();

        // 在这里收集场景中的所有重生点
        RefreshRespawnPoints();
        FindOrSpawnPlayer();

        score = 0;
        gameTime = 0f;

        Debug.Log("GameManager initialized successfully");
    }

    // 新增：刷新重生点列表
    public void RefreshRespawnPoints()
    {
        allRespawnPoints.Clear();
        RespawnPoint[] foundPoints = FindObjectsOfType<RespawnPoint>();
        foreach (RespawnPoint sp in foundPoints)
        {
            allRespawnPoints.Add(sp);
        }
        Debug.Log($"Found {allRespawnPoints.Count} spawn points in the scene.");
    }

    private void FindOrSpawnPlayer()
    {
        player = FindObjectOfType<PlayerController>();

        if (player == null && playerPrefab != null)
        {
            SpawnPlayerAtDefault(); // 初始生成到默认重生点
        }
        // 如果玩家已经存在，并且你希望它初始也在默认重生点，可以在这里添加逻辑
        // else if (player != null)
        // {
        //    MovePlayerToDefaultSpawn();
        // }
    }

    // 新增：在默认重生点生成玩家
    private void SpawnPlayerAtDefault()
    {
        RespawnPoint defaultSpawn = GetDefaultRespawnPoint();
        Vector3 spawnPosition = defaultSpawn != null ? defaultSpawn.transform.position : Vector3.zero;

        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        player = playerObj.GetComponent<PlayerController>();
        Debug.Log($"Player spawned at default spawn point: {spawnPosition}");
    }

    // 新增：获取默认重生点
    private RespawnPoint GetDefaultRespawnPoint()
    {
        foreach (RespawnPoint sp in allRespawnPoints)
        {
            if (sp.isDefaultSpawn)
            {
                return sp;
            }
        }
        Debug.LogWarning("No default spawn point found! Using Vector3.zero.");
        return null; // 没有找到默认重生点
    }
    #endregion

    #region Game State Management
    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        HandleGameStateChange(previousState, newState);
        OnGameStateChanged?.Invoke(newState);
    }

    private void HandleGameStateChange(GameState from, GameState to)
    {
        switch (to)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                if (pauseMenu != null) pauseMenu.SetActive(false);
                if (gameOverMenu != null) gameOverMenu.SetActive(false); // 可能不再需要
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                if (pauseMenu != null) pauseMenu.SetActive(true);
                break;

            case GameState.GameOver: // 可能不再需要 Game Over 状态
                Time.timeScale = 0f;
                if (gameOverMenu != null) gameOverMenu.SetActive(true);
                break;

            case GameState.Loading:
                Time.timeScale = 1f;
                break;
        }

        Debug.Log($"Game state changed from {from} to {to}");
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
        {
            TogglePause();
        }

        // 新增：脱离卡死按钮 (例如 F5)
        if (Input.GetKeyDown(KeyCode.F5)) // F5 键作为脱离卡死
        {
            Debug.Log("Player initiated 'Unstuck' action.");
            TeleportPlayerToNearestLeftRespawnPoint();
        }
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
            SetGameState(GameState.Paused);
        else if (currentState == GameState.Paused)
            SetGameState(GameState.Playing);
    }
    #endregion

    #region Player Management (重命名和修改)

    // 新增：玩家死亡或需要重生的统一入口
    public void PlayerNeedsRespawn()
    {
        if (currentState != GameState.Playing) return; // 只有在游戏中才处理重生

        Debug.Log("Player needs respawn!");
        OnPlayerNeedRespawn?.Invoke(); // 触发玩家需要重生的事件

        StartCoroutine(RespawnPlayerProcess());
    }

    // 在 GameManager.cs 中找到 RespawnPlayerProcess 协程
    private IEnumerator RespawnPlayerProcess()
    {
        SetGameState(GameState.Loading);

        // 此时 PlayerController 已经通过 HandlePlayerNeedsRespawn() 将自己设置为死亡状态
        // 并禁用了控制和物理。

        yield return new WaitForSeconds(respawnDelay);

        TeleportPlayerToNearestLeftRespawnPoint(); // 传送逻辑

        // 玩家已被传送，现在重置其状态并启用控制
        if (player != null)
        {
            player.ResetPlayerStateAndEnableControls(); // 调用 PlayerController 的新方法
        }
        else
        {
            // 如果玩家对象在死亡期间被销毁（例如，在 PlayerNeedsRespawn 中销毁），则重新生成
            // 此时需要确保 SpawnPlayerAtDefault 方法会创建新的 PlayerController 实例，
            // 并且该实例的 Awake/Start 会正确初始化 IsDead = false 和启用控制。
            SpawnPlayerAtDefault(); // 如果玩家不存在，重新生成
            player.ResetPlayerStateAndEnableControls(); // 对新生成的玩家也调用一次
        }

        SetGameState(GameState.Playing);
        Debug.Log("Player respawned and returned to Playing state.");
    }

    // 新增：传送到左侧最近的重生点
    public void TeleportPlayerToNearestLeftRespawnPoint()
    {
        if (player == null || allRespawnPoints.Count == 0)
        {
            Debug.LogWarning("Cannot teleport: Player not found or no spawn points available.");
            return;
        }

        Vector3 currentPlayerPosition = player.transform.position;
        RespawnPoint nearestLeftSpawn = null;
        float minDistance = float.MaxValue;

        foreach (RespawnPoint sp in allRespawnPoints)
        {
            // 只考虑在玩家左侧的重生点 (X坐标小于玩家)
            if (sp.transform.position.x < currentPlayerPosition.x)
            {
                float distance = Vector3.Distance(currentPlayerPosition, sp.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestLeftSpawn = sp;
                }
            }
        }

        if (nearestLeftSpawn != null)
        {
            player.transform.position = nearestLeftSpawn.transform.position;
            Debug.Log($"Teleported player to nearest left spawn point: {nearestLeftSpawn.gameObject.name} at {nearestLeftSpawn.transform.position}");
        }
        else
        {
            // 如果没有找到左侧的重生点，可以考虑传送到默认重生点或最近的任何重生点
            Debug.LogWarning("No spawn point found to the left of the player. Teleporting to default/any nearest spawn.");
            RespawnPoint defaultSpawn = GetDefaultRespawnPoint();
            if (defaultSpawn != null)
            {
                player.transform.position = defaultSpawn.transform.position;
                Debug.Log($"Teleported player to default spawn point: {defaultSpawn.gameObject.name} at {defaultSpawn.transform.position}");
            }
            else // 如果连默认的都没有，就传送到第一个找到的
            {
                player.transform.position = allRespawnPoints[0].transform.position;
                Debug.Log($"Teleported player to first available spawn point: {allRespawnPoints[0].gameObject.name} at {allRespawnPoints[0].transform.position}");
            }
        }
    }

    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }
    #endregion

    #region Game Flow
    public void GameOver() // 这个方法可能不再需要，因为没有生命值，只有即死和重生
    {
        SetGameState(GameState.GameOver);
        Debug.Log($"Game Over! Final Score: {score}, Time: {gameTime:F1}s");
    }

    public void RestartGame()
    {
        SetGameState(GameState.Loading);

        score = 0;
        gameTime = 0f;
        OnScoreChanged?.Invoke(score);

        // 重新生成玩家到默认重生点
        if (player != null)
        {
            Destroy(player.gameObject); // 销毁现有玩家
            player = null;
        }
        SpawnPlayerAtDefault(); // 在默认重生点重新生成玩家

        SetGameState(GameState.Playing);
    }

    public void LoadScene(string sceneName)
    {
        SetGameState(GameState.Loading);
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    #endregion

    #region Skill System Integration
    public void ActivateColorFilter(FilterColor color)
    {
        if (filterSystem != null)
        {
            filterSystem.GetComponent<FilterSystem>().SetFilterColorAndTag(color);
            filterSystem.SetActive(true);
            Debug.Log($"Activated {color} filter");
        }
    }

    public void DeactivateColorFilter()
    {
        if (filterSystem != null)
        {
            filterSystem.SetActive(false);
            Debug.Log("Deactivated color filter");
        }
    }

    public void ToggleColorMask(FilterColor color)
    {
        if (maskSystem != null)
        {
            maskSystem.GetComponent<MaskSystem>().SetMaskColor(color);
            if (maskSystem.activeSelf)
            {
                maskSystem.SetActive(false);
                Debug.Log($"Deactivated {color} mask");
            }
            else
            {
                maskSystem.SetActive(true);
                Debug.Log($"Activated {color} mask");
            }
        }
    }
    #endregion

    #region Utility
    private void UpdateGameTime()
    {
        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }
    }

    public float GetGameTime() => gameTime;
    public int GetScore() => score;
    public PlayerController GetPlayer() => player;
    public GameState GetGameState() => currentState;
    #endregion
}

public enum GameState
{
    Playing,
    Paused,
    GameOver, // 游戏结束可能不再是必要的独立状态，直接通过重启或退出处理
    Loading
}
