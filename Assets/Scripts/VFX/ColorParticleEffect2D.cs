using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ColorParticleEffect2D : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private float burstCount = 30;
    [SerializeField] private float particleLifetime = 2f;
    [SerializeField] private float sizeMultiplier = 1f;
    [SerializeField] private string sortingLayer = "Effects";
    [SerializeField] private int sortingOrder = 100;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();

        // ����2D��Ⱦ��
        if (psRenderer != null)
        {
            psRenderer.sortingLayerName = sortingLayer;
            psRenderer.sortingOrder = sortingOrder;
        }
    }

    public void PlayColorEffect(Color color)
    {
        var main = ps.main;
        main.startColor = color;
        main.startSizeMultiplier = sizeMultiplier;
        main.startLifetime = particleLifetime;

        // ʹ�ñ��������ǳ�������
        var emission = ps.emission;
        emission.enabled = true;

        var burst = new ParticleSystem.Burst(0f, (short)burstCount);
        emission.SetBurst(0, burst);

        ps.Play();
        Destroy(gameObject, particleLifetime + 0.5f);
    }

    public void PlayRippleEffect(Vector3 position, Color color, float radius = 0.5f)
    {
        transform.position = position;

        var main = ps.main;
        main.startColor = color;
        main.startLifetime = 1f;

        // ���û��η���
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.arc = 360f;

        // �ӱ�Ե����
        shape.radiusMode = ParticleSystemShapeMultiModeValue.Random;
        shape.radiusSpread = 0.1f;

        var emission = ps.emission;
        emission.rateOverTime = 50f;

        ps.Play();
        Destroy(gameObject, 2f);
    }

    public void PlayDirectedEffect(Vector3 position, Color color, Vector2 direction)
    {
        transform.position = position;

        var main = ps.main;
        main.startColor = color;
        // Ensure we use the inspector value or a consistent default if simpler
        main.startSpeed = 5f; // Keeping existing logic or move to inspector variable
        main.startLifetime = 1f;

        // 设置发射形状
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;

        // 设置方向
        // Handle 2D rotation safely: Align Z axis (emission) with direction
        // Using Vector3.back as the 'up' reference avoids gimbal lock when direction is (0,1)
        transform.rotation = Quaternion.LookRotation(direction, Vector3.back);

        ps.Play();
        Destroy(gameObject, main.startLifetime.constant + 1f); // Ensure destroy matches lifetime
    }
}