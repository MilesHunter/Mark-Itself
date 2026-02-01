using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.1f;   // 悬停放大目标
    public float pressScale = 0.9f;   // 按下缩小目标
    public float smoothSpeed = 10f;   // 平滑速度

    private Vector3 originalScale;
    private bool isHovering = false;
    private bool isPressed = false;

    void Awake()
    {
        originalScale = transform.localScale;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white;
            btn.colors = cb;
            btn.transition = Selectable.Transition.None;
        }
    }

    void Update()
    {
        // 根据状态计算目标缩放（按下优先级最高）
        Vector3 targetScale = originalScale;
        if (isPressed)
            targetScale = originalScale * pressScale;
        else if (isHovering)
            targetScale = originalScale * hoverScale;

        // 指数平滑过渡到目标缩放
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1 - Mathf.Exp(-smoothSpeed * Time.deltaTime));
    }

    // 保留原接口
    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;
    public void OnPointerExit(PointerEventData eventData) => isHovering = false;
    public void OnPointerDown(PointerEventData eventData) => isPressed = true;
    public void OnPointerUp(PointerEventData eventData) => isPressed = false;
}
