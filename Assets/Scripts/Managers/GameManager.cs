using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentState = GameState.Playing;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    public Transform playerSpawnPoint;

    [Header("UI References")]
    public UIManager uiManager;
    public GameObject pauseMenu;
    public GameObject gameOverMenu;

    [Header("Game Settings")]
    public float respawnDelay = 2f;
    public bool canPause = true;

    // Singleton pattern
    public static GameManager Instance { get; private set; }

    // Game components
    private PlayerController player;
    private GameObject filterSystem;
    private GameObject maskSystem;

    // Game data
    private int score = 0;
    private int lives = 3;
    private float gameTime = 0f;

    // Events
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnLivesChanged;
    public System.Action OnPlayerDeath;
    public System.Action OnPlayerRespawn;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        // Find or create UI Manager
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();

        // Find player or spawn if needed
        FindOrSpawnPlayer();

        // Initialize game data
        score = 0;
        lives = 3;
        gameTime = 0f;

        Debug.Log("GameManager initialized successfully");
    }

    private void FindOrSpawnPlayer()
    {
        // Try to find existing player
        player = FindObjectOfType<PlayerController>();

        if (player == null && playerPrefab != null && playerSpawnPoint != null)
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        player = playerObj.GetComponent<PlayerController>();

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
                if (gameOverMenu != null) gameOverMenu.SetActive(false);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                if (pauseMenu != null) pauseMenu.SetActive(true);
                break;

            case GameState.GameOver:
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

        // Debug keys (remove in production)
        //if (Debug.isDebugBuild)
        //{
        //    if (Input.GetKeyDown(KeyCode.R))
        //        RestartGame();
        //    if (Input.GetKeyDown(KeyCode.K))
        //        PlayerDeath();
        //}
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
            SetGameState(GameState.Paused);
        else if (currentState == GameState.Paused)
            SetGameState(GameState.Playing);
    }
    #endregion

    #region Player Management
    public void PlayerDeath()
    {
        if (currentState != GameState.Playing) return;

        lives--;
        OnLivesChanged?.Invoke(lives);
        OnPlayerDeath?.Invoke();

        if (lives <= 0)
        {
            GameOver();
        }
        else
        {
            StartCoroutine(RespawnPlayer());
        }
    }

    private IEnumerator RespawnPlayer()
    {
        SetGameState(GameState.Loading);
        yield return new WaitForSeconds(respawnDelay);

        if (player != null)
        {
            // Reset player position
            Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            player.transform.position = spawnPos;

            // Reset player state
            //player.ResetPlayer();
        }
        else
        {
            SpawnPlayer();
        }

        SetGameState(GameState.Playing);
    }

    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    public void AddLife()
    {
        lives++;
        OnLivesChanged?.Invoke(lives);
    }
    #endregion

    #region Game Flow
    public void GameOver()
    {
        SetGameState(GameState.GameOver);
        Debug.Log($"Game Over! Final Score: {score}, Time: {gameTime:F1}s");
    }

    public void RestartGame()
    {
        SetGameState(GameState.Loading);

        // Reset game data
        score = 0;
        lives = 3;
        gameTime = 0f;

        // Notify UI
        OnScoreChanged?.Invoke(score);
        OnLivesChanged?.Invoke(lives);

        // Reset player
        if (player != null)
        {
            Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            player.transform.position = spawnPos;
            // player.ResetPlayer();
        }
        else
        {
            FindOrSpawnPlayer();
        }

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
    public int GetLives() => lives;
    public PlayerController GetPlayer() => player;
    public GameState GetGameState() => currentState;
    #endregion

    #region Debug
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.Label($"State: {currentState}");
        GUILayout.Label($"Score: {score}");
        GUILayout.Label($"Lives: {lives}");
        GUILayout.Label($"Time: {gameTime:F1}s");

        if (GUILayout.Button("Restart (R)"))
            RestartGame();
        if (GUILayout.Button("Kill Player (K)"))
            PlayerDeath();

        GUILayout.EndArea();
    }
    #endregion
}

public enum GameState
{
    Playing,
    Paused,
    GameOver,
    Loading
}