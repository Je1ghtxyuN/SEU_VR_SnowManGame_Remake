using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通用 VR 进度条组件：支持渐变填充、百分比文字、缓动动画
/// 挂在包含 Image(fillImage) 和可选 TextMeshProUGUI 的 GameObject 上
/// </summary>
public class VRUIProgressBar : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI percentText;

    [Header("颜色")]
    [SerializeField] private Color lowColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color highColor = new Color(0.4f, 1f, 0.8f, 1f);

    [Header("动画")]
    [SerializeField] private float tweenDuration = 0.4f;

    private float currentValue;
    private SimpleTween.TweenHandle tweenHandle;

    void Awake()
    {
        if (fillImage == null) fillImage = GetComponentInChildren<Image>();
        currentValue = 0f;
        ApplyVisual();
    }

    /// <summary>设置进度（0~1），带动画</summary>
    public void SetProgress(float target)
    {
        target = Mathf.Clamp01(target);
        SimpleTween.Cancel(tweenHandle);
        tweenHandle = SimpleTween.AnimateFloat(v =>
        {
            currentValue = v;
            ApplyVisual();
        }, currentValue, target, tweenDuration);
    }

    /// <summary>设置进度（0~1），无动画，立即生效</summary>
    public void SetProgressImmediate(float target)
    {
        SimpleTween.Cancel(tweenHandle);
        currentValue = Mathf.Clamp01(target);
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = currentValue;
            fillImage.color = Color.Lerp(lowColor, highColor, currentValue);
        }

        if (percentText != null)
        {
            percentText.text = $"{currentValue * 100f:0}%";
        }
    }
}
