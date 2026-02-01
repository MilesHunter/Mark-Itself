using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private GameObject maskSystem;
    [SerializeField] private GameObject filterSystem;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D playerCollider; // 添加玩家碰撞体引用

    // Movement variables
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool controlsEnabled = true;
    private float timeSinceYZero;

    // Player State (新增)
    public bool IsDead { get; private set; } = false; // 玩家生死状态

    // Skill system
    private bool skillActive = false;
    private FilterSystem filterSystemComponent; // Added to cache component
    private MaskSystem maskSystemComponent;     // Added to cache component

    // Animation hashes
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

    public enum SkillType
    {
        FilterSystem,
        MaskSystem
    }

    // Events (OnPlayerRespawn 含义变为“玩家已被重置并启用”)
    public System.Action<SkillType> OnSkillChanged;
    public System.Action<FilterColor> OnColorChanged;
    public System.Action OnPlayerResetAndEnabled; // 当玩家被重置并重新启用后触发

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>(); // 获取玩家碰撞体

        if (filterSystem != null)
            filterSystemComponent = filterSystem.GetComponent<FilterSystem>();
        if (maskSystem != null)
            maskSystemComponent = maskSystem.GetComponent<MaskSystem>();

        if (filterSystemComponent != null)
            filterSystemComponent.SetFilterColorAndTag(currentColor);
        if (maskSystemComponent != null)
            maskSystemComponent.SetMaskColor(currentColor);

        // 初始化玩家为活着的，且控制已启用
        IsDead = false;
        SetControlsAndPhysics(true); // 确保初始时控制和物理是启用的
    }

    void OnEnable()
    {
        // 订阅 GameManager 的事件，当玩家被传送后进行重置
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerNeedRespawn += HandlePlayerNeedsRespawn;
        }
    }

    void OnDisable()
    {
        // 取消订阅
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerNeedRespawn -= HandlePlayerNeedsRespawn;
        }
    }

    void Update()
    {
        if (IsDead) return; // 死亡状态下不处理输入和动画更新

        HandleInput();
        UpdateGroundedState();
        UpdateTimers();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (IsDead)
        {
            rb.velocity = Vector2.zero; // 死亡时停止移动
            return;
        }

        HandleMovement();
    }

    private void HandleInput()
    {
        if (!controlsEnabled) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SwitchSkill();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (skillActive)
            {
                DeactivateSkill();
            }
            else
            {
                ActivateSkill();
            }
        }
    }

    private void HandleMovement()
    {
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);

        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;

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
        bool currentGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask) != null;
        if (currentGrounded && !isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        wasGrounded = isGrounded;
        isGrounded = currentGrounded;
    }

    private void UpdateTimers()
    {
        if (!isGrounded)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter);

        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        float velocityX = rb.velocity.x;
        float velocityY = rb.velocity.y;

        // 计算 velocity.y 接近 0 的时间（误差范围）
        if (Mathf.Abs(velocityY) < 0.1f)  // velocity.y 接近 0 的误差范围
        {
            timeSinceYZero += Time.deltaTime;
        }
        else
        {
            timeSinceYZero = 0f;
        }

        // 判断 canland 参数
        bool canLand = velocityY < -5f || timeSinceYZero > 0.1f;
        animator.SetBool("canland", canLand);
        if (!isGrounded && canLand)
            isGrounded = true;
        bool canJump = false;
        if (velocityY > 0.2f)
            canJump = true;
        animator.SetFloat("VelocityX", velocityX);
        animator.SetFloat("VelocityY", velocityY);
        animator.SetBool("canjump", canJump);
        animator.SetBool(IsGrounded, isGrounded);
        animator.SetBool(IsRunning, Mathf.Abs(velocityX) > 0.1f && isGrounded);
    }


    private void SwitchSkill()
    {
        DeactivateSkill();

        currentSkill = currentSkill == SkillType.FilterSystem ? SkillType.MaskSystem : SkillType.FilterSystem;

        if (currentSkill == SkillType.FilterSystem && filterSystemComponent != null)
        {
            filterSystemComponent.SetFilterColorAndTag(currentColor);
        }
        else if (currentSkill == SkillType.MaskSystem && maskSystemComponent != null)
        {
            maskSystemComponent.SetMaskColor(currentColor);
        }

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
                    filterSystem.SetActive(true);
                break;

            case SkillType.MaskSystem:
                if (maskSystem != null)
                    maskSystem.SetActive(true);
                break;
        }
        Debug.Log($"Skill activated: {currentSkill}");
    }

    private void DeactivateSkill()
    {
        if (!skillActive) return;

        skillActive = false;

        switch (currentSkill)
        {
            case SkillType.FilterSystem:
                if (filterSystem != null)
                    filterSystem.SetActive(false);
                break;

            case SkillType.MaskSystem:
                if (maskSystem != null)
                    maskSystem.SetActive(false);
                break;
        }
        Debug.Log($"Skill deactivated: {currentSkill}");
    }

    /// <summary>
    /// 当玩家进入死亡区域或需要重置时，GameManager 会调用此方法。
    /// 玩家的实际传送由 GameManager 处理。
    /// </summary>
    public void SetPlayerDeathState(bool dead)
    {
        if (IsDead == dead) return;

        IsDead = dead;
        SetControlsAndPhysics(!dead); // 死亡时禁用控制和物理，否则启用

        if (IsDead)
        {
            Debug.Log("Player is now Dead.");
            DeactivateSkill(); // 死亡时禁用技能
            // 这里可以触发死亡动画，停止所有声音等
            // animator.SetTrigger("Die");
        }
        else
        {
            Debug.Log("Player is now Alive.");
            // 玩家重生后的初始化逻辑，例如重置动画状态
            // animator.SetTrigger("Respawn");
        }
    }

    // 新增：由 GameManager 在重生流程中调用，用于重置玩家状态并启用
    public void ResetPlayerStateAndEnableControls()
    {
        Debug.Log("PlayerController: Resetting state and enabling controls.");
        SetPlayerDeathState(false); // 设为活着
        rb.velocity = Vector2.zero; // 重置速度
                                    // 其他可能需要重置的状态：例如，玩家的动画状态，技能冷却等

        OnPlayerResetAndEnabled?.Invoke(); // 触发事件，通知其他组件玩家已重置并启用
    }

    // 控制玩家的输入和物理行为
    private void SetControlsAndPhysics(bool enabled)
    {
        controlsEnabled = enabled;
        // 禁用/启用 Rigidbody2D 的移动和碰撞
        if (rb != null)
        {
            rb.simulated = enabled; // 禁用物理模拟
        }
        if (playerCollider != null)
        {
            playerCollider.enabled = enabled; // 禁用碰撞体
        }
        // spriteRenderer.enabled = enabled; // 如果希望死亡时隐藏玩家
    }

    // 监听 GameManager 的 OnPlayerNeedRespawn 事件
    private void HandlePlayerNeedsRespawn()
    {
        SetPlayerDeathState(true); // 将玩家设置为死亡状态
        // 此时玩家可能处于隐藏状态或死亡动画中，等待 GameManager 传送和重置
    }

    // 玩家不再自行管理 RespawnPoint
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (other.CompareTag("RespawnPoint"))
    //     {
    //         // 这部分逻辑将由 GameManager 结合 SpawnPoint 脚本来管理
    //         // player.SetRespawnPoint(other.transform.position) 不再需要
    //     }
    // }

    /// <summary>
    /// Sets the player's current skill color. This method can be called by UI Buttons.
    /// </summary>
    public void SetSkillColor(FilterColor newColor)
    {
        if (currentColor == newColor) return;

        currentColor = newColor;

        if (currentSkill == SkillType.FilterSystem && filterSystemComponent != null)
        {
            filterSystemComponent.SetFilterColorAndTag(currentColor);
        }
        else if (currentSkill == SkillType.MaskSystem && maskSystemComponent != null)
        {
            maskSystemComponent.SetMaskColor(currentColor);
        }

        OnColorChanged?.Invoke(currentColor);
        Debug.Log($"Skill color changed to: {currentColor}");
    }

    // Public methods for external systems
    public FilterColor GetCurrentColor() => currentColor;
    public SkillType GetCurrentSkill() => currentSkill;
    public SkillType GetCurrentSkillType() => currentSkill;
    public bool IsSkillActive() => skillActive;
    public void SetControlsEnabled(bool enabled) => controlsEnabled = enabled; // 外部可控是否启用控制
    public bool GetControlsEnabled() => controlsEnabled;

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}