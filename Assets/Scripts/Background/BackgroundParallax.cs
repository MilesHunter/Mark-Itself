using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// ----
/// 背景 parallax 效果
/// 
/// 该脚本应挂载到背景层中的一个图层父物体上
/// 该父物体名字类似于“Speed-数字”
/// 而不是一个单独的sprite物体上
/// 

public class BackgroundParallax : MonoBehaviour
{
    ///将场景的摄像机挂载到此处
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerTransform;
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    private float lastPlayerY;

    // 速度参数
    [SerializeField] private float parallaxSpeedX = 1f;
    [SerializeField] private float parallaxSpeedY = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
            lastCameraPosition = cameraTransform.position;
        }

        if (playerTransform != null)
            lastPlayerY = playerTransform.position.y;
    }

    void LateUpdate()
    {
        ParallaxMove();
    }

    private void ParallaxMove()
    {
        if (cameraTransform == null) return;

        Vector3 deltaCamera = cameraTransform.position - lastCameraPosition;

        float deltaPlayerY = 0f;
        if (playerTransform != null)
        {
            deltaPlayerY = playerTransform.position.y - lastPlayerY;
            lastPlayerY = playerTransform.position.y;
        }

        Vector3 move = new Vector3(
            deltaCamera.x * parallaxSpeedX,
            deltaPlayerY * parallaxSpeedY,
            0f
        );

        transform.position += move;
        lastCameraPosition = cameraTransform.position;
    }
}
