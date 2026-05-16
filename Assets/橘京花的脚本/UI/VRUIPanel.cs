using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 通用 VR 世界空间面板组件：定位在玩家前方 + 弹入/淡出动画
/// 挂在任何需要在 VR 中显示/隐藏的 Canvas 面板上
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class VRUIPanel : MonoBehaviour
{
    [Header("动画")]
    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("定位")]
    [SerializeField] private float displayDistance = 2f;
    [SerializeField] private float heightOffset = -0.3f;

    private CanvasGroup canvasGroup;
    private Transform panelRoot;
    private SimpleTween.TweenHandle showHandle;
    private SimpleTween.TweenHandle hideHandle;
    private Action onHidden;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        panelRoot = transform;
    }

    /// <summary>显示面板：定位到相机前方 + 弹入动画</summary>
    public void ShowPanel()
    {
        // 取消进行中的动画
        SimpleTween.Cancel(hideHandle);
        SimpleTween.Cancel(showHandle);

        // 定位
        PositionInFrontOfCamera();

        // 激活
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        panelRoot.localScale = Vector3.one * 0.5f;

        // 动画
        showHandle = SimpleTween.FadeIn(canvasGroup, showDuration);
        SimpleTween.ScaleIn(panelRoot, showDuration, showCurve);
    }

    /// <summary>立即隐藏面板，无动画</summary>
    public void HidePanelImmediate()
    {
        SimpleTween.Cancel(showHandle);
        SimpleTween.Cancel(hideHandle);
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    /// <summary>隐藏面板：淡出 + 缩小，完成后 SetActive(false)</summary>
    public void HidePanel(Action onComplete = null)
    {
        SimpleTween.Cancel(showHandle);
        SimpleTween.Cancel(hideHandle);

        onHidden = onComplete;

        hideHandle = SimpleTween.FadeOut(canvasGroup, hideDuration, () =>
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
            onHidden = null;
        });
        SimpleTween.ScaleOut(panelRoot, hideDuration, hideCurve);
    }

    /// <summary>定位到玩家相机前方</summary>
    public void PositionInFrontOfCamera()
    {
        Transform cam = Camera.main?.transform;
        if (cam == null) return;

        Vector3 newPos = cam.position + cam.forward * displayDistance + Vector3.up * heightOffset;
        panelRoot.position = newPos;
        panelRoot.LookAt(cam);
        panelRoot.Rotate(0, 180f, 0);
    }
}
