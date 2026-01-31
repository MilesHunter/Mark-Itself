using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RbotArmController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip runClip;
    [Range(0f, 1f)]
    public float runVolume = 1.0f; // 跑步音量

    public AudioClip jumpClip;
    [Range(0f, 1f)]
    public float jumpVolume = 1.0f; // 跳跃音量

    public AudioClip landClip;
    [Range(0f, 1f)]
    public float landVolume = 1.0f; // 落地音量

    [Header("Cooldown (秒)")]
    public float runCooldown = 0.2f;  // 跑步音效最小间隔
    public float jumpCooldown = 0.1f; // 跳跃音效最小间隔
    public float landCooldown = 0.1f; // 落地音效最小间隔

    private AudioSource audioSource;
    private Animator animator;

    // 上次播放时间
    private float lastRunTime = -10f;
    private float lastJumpTime = -10f;
    private float lastLandTime = -10f;

    // 跑步状态检测
    private bool isRunning = false;

    void Awake()
    {
        // 自动获取或添加 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 获取 Animator
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // 如果动画一开始就是跑步，手动触发一次
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Run_runing"))
        {
            RunningEvent();
            isRunning = true;
        }
    }

    void Update()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // 跑步状态检测：Run_running / Run_start / Run_stop
        if (state.IsName("Run_running") || state.IsName("Run_start") || state.IsName("Run_stop"))
        {
            RunningEvent(); // 每帧触发，但音效仍受 cooldown 限制
        }

        // 跳跃状态检测
        if (state.IsName("Jump_start"))
        {
            JumpingEvent();
        }

        // 落地状态检测
        if (state.IsName("Jump_land"))
        {
            LandingEvent();
        }
    }

    // --------- Animation Events / 播放音效 ---------
    public void RunningEvent()
    {
        if (runClip != null && Time.time - lastRunTime > runCooldown && Time.time - lastLandTime > landCooldown)
        {
            audioSource.PlayOneShot(runClip, runVolume);
            lastRunTime = Time.time;
            Debug.Log("RunningEvent at: " + Time.time + " volume: " + runVolume);
        }
    }

    public void JumpingEvent()
    {
        if (jumpClip != null && Time.time - lastJumpTime > jumpCooldown)
        {
            audioSource.PlayOneShot(jumpClip, jumpVolume);
            lastJumpTime = Time.time;
            Debug.Log("JumpingEvent at: " + Time.time + " volume: " + jumpVolume);
        }
    }

    public void LandingEvent()
    {
        if (landClip != null && Time.time - lastLandTime > landCooldown)
        {
            audioSource.PlayOneShot(landClip, landVolume);
            lastLandTime = Time.time;
            Debug.Log("LandingEvent at: " + Time.time + " volume: " + landVolume);
        }
    }
}

