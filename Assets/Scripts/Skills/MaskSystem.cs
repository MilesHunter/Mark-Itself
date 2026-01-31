using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MaskSystem : MonoBehaviour
{
    [Header("Hidden Object Pool")]
    [SerializeField] private RectTransform[] HiddenObjectPool = new RectTransform[0];
    [SerializeField] private RectTransform rt;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float maskAlpha = 0.4f;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        tag = "Red";
        spriteRenderer = GetComponent<SpriteRenderer>();
        Color tempColor = new Color(255, 0, 0);
        tempColor.a = 0.4f;
        spriteRenderer.color = tempColor;
    }

    private void Update()
    {
        JudgeOverlap();
    }
    private void JudgeOverlap()
    {
        for (int i = 0; i < HiddenObjectPool.Length; i++)
        {
            if(RectTransToScreenPos(rt, null).Overlaps(RectTransToScreenPos(HiddenObjectPool[i], null)) && tag == HiddenObjectPool[i].tag)
            {
                HiddenObjectPool[i].GetComponent<Collider>().enabled = true;
            }
            else
            {
                HiddenObjectPool[i].GetComponent<Collider>().enabled = false;
            }
        }
    }

    public static Rect RectTransToScreenPos(RectTransform rt, Camera cam)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 v0 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 v1 = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        Rect rect = new Rect(v0, v1 - v0);
        return rect;
    }

    public void SetMaskColor(FilterColor col)
    {
        Color tempColor = new Color(0, 0, 0);
        switch (col)
        {
            case FilterColor.Red:
                tempColor = new Color(255, 0, 0);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = 0.4f;
                spriteRenderer.color = tempColor;
                break;
            case FilterColor.Green:
                tempColor = new Color(0, 255, 0);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = 0.4f;
                spriteRenderer.color = tempColor;
                break;
            case FilterColor.Blue:
                tempColor = new Color(0, 0, 255);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = 0.4f;
                spriteRenderer.color = tempColor;
                break;
            case FilterColor.Yellow:
                tempColor = new Color(255, 255, 0);
                tag = GameConstants.TAG_RED_OBJECT;
                tempColor.a = 0.4f;
                spriteRenderer.color = tempColor;
                break;
        }
    }
}