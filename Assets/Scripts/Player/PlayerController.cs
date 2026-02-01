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

    // Player State
    public bool IsDead { get; private set; } = false;

    // Skill system
    private bool skillActive = false;
    private FilterSystem filterSystemComponent;
    private MaskSystem maskSystemComponent;

    // 新增：技能解锁状态
    public bool isFilterSkillUnlocked { get; private set; } = false;
    public bool isMaskSkillUnlocked { get; private set; } = false;

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
    public System.Action OnPlayerResetAndEnabled;
    public System.Action<SkillType> OnSkillUnlocked; // 新增：技能解锁事件

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();

        if (filterSystem != null)
            filterSystemComponent = filterSystem.GetComponent<FilterSystem>();
        if (maskSystem != null)
            maskSystemComponent = maskSystem.GetComponent<MaskSystem>();

        // 技能系统初始状态：确保所有技能系统都处于非激活状态，
        // 并且它们的颜色被初始化，但我们不立即激活它们。
        // GameManager 负责玩家的初始生成，这里只初始化组件
        if (filterSystem != null) filterSystem.SetActive(false);
        if (maskSystem != null) maskSystem.SetActive(false);

        if (filterSystemComponent != null)
            filterSystemComponent.SetFilterColorAndTag(currentColor);
        if (maskSystemComponent != null)
            maskSystemComponent.SetMaskColor(currentColor);

        IsDead = false;
        SetControlsAndPhysics(true);

        // 确保初始时玩家没有激活任何技能
        DeactivateSkill();
        skillActive = false; // 明确设置技能为非激活状态

        // 初始时默认技能可能未解锁，或根据游戏设定决定
        // 默认情况下，如果游戏一开始没有技能球，玩家将没有技能可用。
        // 如果你希望玩家初始就拥有某个技能，可以这里将其设置为 true。
        // 例如：
        // isFilterSkillUnlocked = true; // 玩家初始拥有 Filter 技能
        // currentSkill = SkillType.FilterSystem; // 将初始技能设置为 Filter
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerNeedRespawn += HandlePlayerNeedsRespawn;
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerNeedRespawn -= HandlePlayerNeedsRespawn;
        }
    }

    void Update()
    {
        if (IsDead) return;

        HandleInput();
        UpdateGroundedState();
        UpdateTimers();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (IsDead)
        {
            rb.velocity = Vector2.zero;
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

        // 技能切换逻辑修改
        if (Input.GetKeyDown(KeyCode.R))
        {
            SwitchSkill();
        }

        // 技能激活逻辑修改
        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            // 只有当至少一个技能解锁时才能尝试激活/停用
            if (isFilterSkillUnlocked || isMaskSkillUnlocked)
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
            else
            {
                Debug.Log("No skills unlocked yet. Cannot activate.");
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

        if (Mathf.Abs(velocityY) < 0.1f)
        {
            timeSinceYZero += Time.deltaTime;
        }
        else
        {
            timeSinceYZero = 0f;
        }

        bool canLand = velocityY < -5f || timeSinceYZero > 0.1f;
        animator.SetBool("canland", canLand);
        // 注意：这里 'if (!isGrounded && canLand) isGrounded = true;' 可能会与 UpdateGroundedState 冲突或导致逻辑混乱
        // 通常 isGrounded 应该完全由 Physics2D.OverlapCircle 决定。
        // 如果你的动画需要一个单独的 'canland' 触发，可以保留，但不要用它来覆盖 isGrounded 的物理检测结果。
        // 例如，可以这样修改：
        // if (!isGrounded && canLand && !wasGrounded) { Debug.Log("Animation suggests landing, but physics says still in air."); }

        bool canJump = false;
        if (velocityY > 0.2f)
            canJump = true;
        animator.SetFloat("VelocityX", velocityX);
        animator.SetFloat("VelocityY", velocityY);
        animator.SetBool("canjump", canJump);
        animator.SetBool(IsGrounded, isGrounded);
        animator.SetBool(IsRunning, Mathf.Abs(velocityX) > 0.1f && isGrounded);
    }

    // 新增：封装逻辑以在切换技能或颜色时停用技能
    private void EnsureSkillDeactivated()
    {
        if (skillActive)
        {
            DeactivateSkill();
            Debug.Log("Deactivated active skill before color/skill change.");
        }
    }

    // 技能切换逻辑 (主要修改部分)
    private void SwitchSkill()
    {
        // 确保在切换技能前，当前激活的技能被关闭
        EnsureSkillDeactivated();

        int unlockedSkillCount = 0;
        if (isFilterSkillUnlocked) unlockedSkillCount++;
        if (isMaskSkillUnlocked) unlockedSkillCount++;

        if (unlockedSkillCount <= 1)
        {
            Debug.Log("Cannot switch skills: Less than two skills unlocked.");
            if (isFilterSkillUnlocked) currentSkill = SkillType.FilterSystem;
            else if (isMaskSkillUnlocked) currentSkill = SkillType.MaskSystem;

            // 如果只有一个技能，并且它是激活状态，确保切换后保持激活 (取决于你的具体需求)
            // 例如，如果之前 Filter 是激活的，现在只有一个 Mask 技能，那么 Mask 应该激活。
            // 简单起见，这里默认切换前已关闭，切换后需要用户再次激活。
            return;
        }

        // 切换到下一个已解锁的技能
        if (currentSkill == SkillType.FilterSystem)
        {
            if (isMaskSkillUnlocked)
            {
                currentSkill = SkillType.MaskSystem;
            }
            // else 理论上不会发生，因为 unlockedSkillCount > 1 保证了另一个技能已解锁
        }
        else if (currentSkill == SkillType.MaskSystem)
        {
            if (isFilterSkillUnlocked)
            {
                currentSkill = SkillType.FilterSystem;
            }
            // else 同上
        }

        OnSkillChanged?.Invoke(currentSkill);
        Debug.Log($"Switched to skill: {currentSkill}. New current skill is unlocked and ready to be activated.");

        // 切换后，技能处于非激活状态，需要用户再次右键激活
        skillActive = false;
    }

    private void ActivateSkill()
    {
        if (skillActive) return;

        // 只有当前选择的技能已解锁才能激活
        if ((currentSkill == SkillType.FilterSystem && !isFilterSkillUnlocked) ||
            (currentSkill == SkillType.MaskSystem && !isMaskSkillUnlocked))
        {
            Debug.LogWarning($"Attempted to activate {currentSkill} skill, but it is not unlocked!");
            return;
        }

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

    public void SetPlayerDeathState(bool dead)
    {
        if (IsDead == dead) return;

        IsDead = dead;
        SetControlsAndPhysics(!dead);

        if (IsDead)
        {
            Debug.Log("Player is now Dead.");
            DeactivateSkill(); // 死亡时禁用所有技能
        }
        else
        {
            Debug.Log("Player is now Alive.");
        }
    }

    public void ResetPlayerStateAndEnableControls()
    {
        Debug.Log("PlayerController: Resetting state and enabling controls.");
        SetPlayerDeathState(false);
        rb.velocity = Vector2.zero;

        // 确保所有技能UI或状态也被重置到非激活状态
        DeactivateSkill();

        OnPlayerResetAndEnabled?.Invoke();
    }

    private void SetControlsAndPhysics(bool enabled)
    {
        controlsEnabled = enabled;
        if (rb != null)
        {
            rb.simulated = enabled;
        }
        if (playerCollider != null)
        {
            playerCollider.enabled = enabled;
        }
    }

    private void HandlePlayerNeedsRespawn()
    {
        SetPlayerDeathState(true);
    }

    // 碰撞检测，处理 DeathZone 和 SkillBall (主要修改部分)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathZone") || other.CompareTag("Trap"))
        {
            Debug.Log("Player entered DeathZone. Notifying GameManager.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerNeedsRespawn();
            }
        }
        else if (other.CompareTag("SkillBall")) // 检测技能球
        {
            // 根据技能球的名字或组件判断是哪种技能球
            // 例如，你可以给不同的技能球 GameObject 命名为 "FilterSkillBall" 和 "MaskSkillBall"
            // 或者给它们挂载不同的组件来标识
            if (other.gameObject.name.Contains("FilterSkillBall"))
            {
                UnlockSkill(SkillType.FilterSystem);
                Destroy(other.gameObject); // 销毁技能球
            }
            else if (other.gameObject.name.Contains("MaskSkillBall"))
            {
                UnlockSkill(SkillType.MaskSystem);
                Destroy(other.gameObject); // 销毁技能球
            }
            else
            {
                Debug.LogWarning($"Unknown SkillBall type encountered: {other.gameObject.name}");
            }
        }
    }

    // 新增：解锁技能的方法
    public void UnlockSkill(SkillType skillType)
    {
        bool wasAlreadyUnlocked = false;
        if (skillType == SkillType.FilterSystem && !isFilterSkillUnlocked)
        {
            isFilterSkillUnlocked = true;
            Debug.Log("Filter System Skill Unlocked!");
            wasAlreadyUnlocked = false;
        }
        else if (skillType == SkillType.MaskSystem && !isMaskSkillUnlocked)
        {
            isMaskSkillUnlocked = true;
            Debug.Log("Mask System Skill Unlocked!");
            wasAlreadyUnlocked = false;
        }
        else
        {
            wasAlreadyUnlocked = true;
            Debug.Log($"{skillType} Skill was already unlocked or invalid type.");
        }

        if (!wasAlreadyUnlocked)
        {
            // 如果这是解锁的第一个技能，则自动将其设为当前技能
            if (!isFilterSkillUnlocked && skillType == SkillType.MaskSystem && !isMaskSkillUnlocked) // 之前没有技能，现在解锁了Mask
            {
                currentSkill = SkillType.MaskSystem;
            }
            else if (!isMaskSkillUnlocked && skillType == SkillType.FilterSystem && !isFilterSkillUnlocked) // 之前没有技能，现在解锁了Filter
            {
                currentSkill = SkillType.FilterSystem;
            }
            // 如果两个都解锁了或者已经有当前技能了，就不改变 currentSkill

            OnSkillUnlocked?.Invoke(skillType); // 触发技能解锁事件
        }
    }

    /// <summary>
    /// Sets the player's current skill color. This method can be called by UI Buttons.
    /// </summary>
    public void SetSkillColor(FilterColor newColor)
    {
        if (currentColor == newColor) return;

        // 确保在切换颜色前，当前激活的技能被关闭
        // 这是为了防止旧颜色对应的标签物品没有被禁用就被新颜色覆盖
        EnsureSkillDeactivated();

        currentColor = newColor;

        // 技能系统组件的颜色总是更新，无论技能是否激活
        // 这样可以确保当技能被重新激活时，它会使用正确的颜色
        if (filterSystemComponent != null)
        {
            filterSystemComponent.SetFilterColorAndTag(currentColor);
        }
        if (maskSystemComponent != null)
        {
            maskSystemComponent.SetMaskColor(currentColor);
        }

        // 注意：这里没有自动重新激活技能。如果需要，你可以在这里添加逻辑
        // 例如：if (wasSkillActiveBeforeColorChange) { ActivateSkill(); }

        OnColorChanged?.Invoke(currentColor);
        Debug.Log($"Skill color changed to: {currentColor}. Active skill (if any) will need to be re-activated.");
    }

    // Public methods for external systems
    public FilterColor GetCurrentColor() => currentColor;
    public SkillType GetCurrentSkill() => currentSkill;
    public SkillType GetCurrentSkillType() => currentSkill;
    public bool IsSkillActive() => skillActive;
    public void SetControlsEnabled(bool enabled) => controlsEnabled = enabled;
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