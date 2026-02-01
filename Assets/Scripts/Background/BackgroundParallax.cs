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
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    // 速度参数
    [SerializeField] private float parallaxSpeed = 1f;

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
    }

    void LateUpdate()
    {
        ParallaxMove();
    }

    private void ParallaxMove()
    {
        Vector3 delta = cameraTransform.position - lastCameraPosition;
        transform.position += delta * parallaxSpeed;
        lastCameraPosition = cameraTransform.position;
    }
}
