using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayerMask = 1;

    [Header("Skills")]
    [SerializeField] private SkillType currentSkill = SkillType.FilterSystem;
    [SerializeField] private FilterColor currentColor = FilterColor.Red;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerAnimationController animationController;

    // Movement variables
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool controlsEnabled = true;

    // Skill system
    private FilterSystem filterSystem;
    private GameObject maskSystem;
    private bool skillActive = false;

    // Respawn system
    private Vector3 currentRespawnPoint;

    // Animation hashes
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

    public enum SkillType
    {
        FilterSystem,
        MaskSystem
    }

    // Events
    public System.Action<SkillType> OnSkillChanged;
    public System.Action<FilterColor> OnColorChanged;
    public System.Action<Vector3> OnPlayerRespawn;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        animationController = GetComponent<PlayerAnimationController>();

        // Get skill components
        filterSystem = GetComponent<FilterSystem>();

        // Initialize skill systems with current color
        if (filterSystem != null)
            filterSystem.SetFilterColor(currentColor);

        // Set initial respawn point
        currentRespawnPoint = transform.position;
    }

    void Start()
    {
        // Register with GameManager if it exists
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
    }

    void OnDestroy()
    {
        // Unregister from GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    void Update()
    {
        HandleInput();
        UpdateGroundedState();
        UpdateTimers();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        if (!controlsEnabled) return;

        // Movement input
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        // Skill switching
        if (Input.GetKeyDown(KeyCode.R))
        {
            SwitchSkill();
        }

        // Skill activation
        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            ActivateSkill();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            DeactivateSkill();
        }
    }

    private void HandleMovement()
    {
        // Horizontal movement
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);

        // Flip sprite based on movement direction
        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;

        // Jump logic with coyote time and jump buffering
        if (jumpBufferCounter > 0f && (isGrounded || coyoteTimeCounter > 0f))
        {
            Jump();
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void UpdateGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);

        // Reset coyote time when landing
        if (isGrounded && !wasGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
    }

    private void UpdateTimers()
    {
        // Update coyote time
        if (!isGrounded && wasGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else if (!isGrounded)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Update jump buffer
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void UpdateAnimations()
    {
        bool isRunning = Mathf.Abs(horizontalInput) > 0.1f && isGrounded;

        // Method 1: Using Animator Controller (recommended)
        if (animator != null)
        {
            animator.SetBool(IsRunning, isRunning);
            animator.SetBool(IsGrounded, isGrounded);
            animator.SetFloat(VerticalVelocity, rb.velocity.y);
        }

        // Method 2: Using direct Animation component
        if (animationController != null)
        {
            animationController.UpdateAnimationState(isRunning, isGrounded, rb.velocity.y);
        }
    }

    private void SwitchSkill()
    {
        // Deactivate current skill first
        DeactivateSkill();

        // Switch to the other skill
        currentSkill = currentSkill == SkillType.FilterSystem ? SkillType.MaskSystem : SkillType.FilterSystem;

        // Notify listeners
        OnSkillChanged?.Invoke(currentSkill);

        Debug.Log($"Switched to skill: {currentSkill}");
    }

    private void ActivateSkill()
    {
        if (skillActive) return;

        skillActive = true;

        switch (currentSkill)
        {
            case SkillType.FilterSystem:
                if (filterSystem != null)
                    filterSystem.ActivateFilter();
                break;

            case SkillType.MaskSystem:
                if (maskSystem != null)
                    maskSystem.SetActive(true);
                break;
        }
    }



    private void DeactivateSkill()
    {
        if (!skillActive) return;

        skillActive = false;

        switch (currentSkill)
        {
            case SkillType.FilterSystem:
                if (filterSystem != null)
                    filterSystem.DeactivateFilter();
                break;

            case SkillType.MaskSystem:
                if (maskSystem != null)
                    maskSystem.SetActive(false);
                break;
        }
    }

    public void SetRespawnPoint(Vector3 newRespawnPoint)
    {
        currentRespawnPoint = newRespawnPoint;
        Debug.Log($"Respawn point set to: {newRespawnPoint}");
    }

    public void RespawnPlayer()
    {
        // Deactivate any active skills
        DeactivateSkill();

        // Reset velocity
        rb.velocity = Vector2.zero;

        // Move to respawn point
        transform.position = currentRespawnPoint;

        // Notify listeners
        OnPlayerRespawn?.Invoke(currentRespawnPoint);

        Debug.Log($"Player respawned at: {currentRespawnPoint}");
    }

    // Called when player falls into trap or gets stuck
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathZone") || other.CompareTag("Trap"))
        {
            RespawnPlayer();
        }
        else if (other.CompareTag("RespawnPoint"))
        {
            SetRespawnPoint(other.transform.position);
        }
    }

    public void SetSkillColor(FilterColor newColor)
    {
        if (currentColor == newColor) return;

        currentColor = newColor;

        // 更新当前技能系统的颜色
        switch (currentSkill)
        {
            case SkillType.FilterSystem:
                if (filterSystem != null)
                    filterSystem.SetFilterColor(currentColor);
                break;

            case SkillType.MaskSystem:
                if (maskSystem != null)
                    maskSystem.GetComponent<MaskSystem>().SetMaskColor(currentColor);
                break;
        }

        OnColorChanged?.Invoke(currentColor);
        Debug.Log($"Skill color changed to: {currentColor}");
    }

    // Public methods for external systems
    public FilterColor GetCurrentColor()
    {
        return currentColor;
    }

    public SkillType GetCurrentSkill()
    {
        return currentSkill;
    }

    public SkillType GetCurrentSkillType()
    {
        return currentSkill;
    }

    public bool IsSkillActive()
    {
        return skillActive;
    }

    public Vector3 GetRespawnPoint()
    {
        return currentRespawnPoint;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
    }

    public bool GetControlsEnabled()
    {
        return controlsEnabled;
    }

    // GameManager integration methods
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                SetControlsEnabled(true);
                break;
            case GameState.Paused:
            case GameState.GameOver:
            case GameState.Loading:
                SetControlsEnabled(false);
                DeactivateSkill(); // Deactivate any active skills
                break;
        }
    }

    public void ResetPlayer()
    {
        // Reset velocity and position
        rb.velocity = Vector2.zero;
        transform.position = currentRespawnPoint;

        // Reset skill states
        DeactivateSkill();
        skillActive = false;

        // Reset movement states
        horizontalInput = 0f;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        // Enable controls
        SetControlsEnabled(true);

        Debug.Log("Player reset completed");
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Draw respawn point
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(currentRespawnPoint, Vector3.one * 0.5f);
    }
}