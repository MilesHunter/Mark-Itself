using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Clips")]
    [SerializeField] private AnimationClip idleAnimation;
    [SerializeField] private AnimationClip runAnimation;
    [SerializeField] private AnimationClip jumpAnimation;

    private Animation animationComponent;
    private string currentAnimationName;

    // Animation state tracking
    private bool isRunning;
    private bool isGrounded;
    private bool wasGrounded;

    void Awake()
    {
        animationComponent = GetComponent<Animation>();

        // Add animation clips to the Animation component
        if (idleAnimation != null)
            animationComponent.AddClip(idleAnimation, "Idle");
        if (runAnimation != null)
            animationComponent.AddClip(runAnimation, "Run");
        if (jumpAnimation != null)
            animationComponent.AddClip(jumpAnimation, "Jump");

        // Set default animation
        PlayAnimation("Idle");
    }

    public void UpdateAnimationState(bool running, bool grounded, float verticalVelocity)
    {
        wasGrounded = isGrounded;
        isRunning = running;
        isGrounded = grounded;

        // Determine which animation to play
        string targetAnimation = DetermineAnimation(running, grounded, verticalVelocity);

        // Only change animation if it's different from current
        if (targetAnimation != currentAnimationName)
        {
            PlayAnimation(targetAnimation);
        }
    }

    private string DetermineAnimation(bool running, bool grounded, float verticalVelocity)
    {
        // Priority: Jump > Run > Idle
        if (!grounded)
        {
            return "Jump";
        }
        else if (running && grounded)
        {
            return "Run";
        }
        else
        {
            return "Idle";
        }
    }

    private void PlayAnimation(string animationName)
    {
        if (animationComponent != null && animationComponent[animationName] != null)
        {
            // Set wrap mode for looping animations
            switch (animationName)
            {
                case "Idle":
                case "Run":
                    animationComponent[animationName].wrapMode = WrapMode.Loop;
                    break;
                case "Jump":
                    animationComponent[animationName].wrapMode = WrapMode.ClampForever;
                    break;
            }

            animationComponent.CrossFade(animationName, 0.1f);
            currentAnimationName = animationName;

            Debug.Log($"Playing animation: {animationName}");
        }
    }

    public void PlayIdleAnimation()
    {
        PlayAnimation("Idle");
    }

    public void PlayRunAnimation()
    {
        PlayAnimation("Run");
    }

    public void PlayJumpAnimation()
    {
        PlayAnimation("Jump");
    }

    public string GetCurrentAnimation()
    {
        return currentAnimationName;
    }
}