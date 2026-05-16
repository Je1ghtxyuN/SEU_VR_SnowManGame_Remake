using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 轻量级缓动工具，支持 TimeScale=0 下运行（VR 菜单动画）
/// 所有方法返回 TweenHandle 用于取消
/// </summary>
public static class SimpleTween
{
    // ==================== 数据结构 ====================

    public struct TweenHandle
    {
        internal int index;
        internal int generation;
        public bool IsValid => index >= 0;
        public static TweenHandle Invalid => new TweenHandle { index = -1, generation = -1 };
    }

    private struct TweenJob
    {
        public int generation;
        public bool active;
        public float elapsed;
        public float duration;
        public bool useScaledTime;
        public AnimationCurve curve;
        public Action<float> onUpdate;
        public Action onComplete;
    }

    // ==================== 状态 ====================

    private static TweenJob[] jobs = new TweenJob[64];
    private static int nextIndex = 0;
    private static bool initialized = false;
    private static GameObject runnerGO;

    // 默认曲线
    private static AnimationCurve _easeOutBack;
    private static AnimationCurve _easeInOutQuad;
    private static AnimationCurve _easeOutQuad;

    private static AnimationCurve EaseOutBack
    {
        get
        {
            if (_easeOutBack == null)
            {
                _easeOutBack = new AnimationCurve(
                    new Keyframe(0, 0, 0, 2.5f),
                    new Keyframe(0.5f, 1.1f),
                    new Keyframe(1, 1, 0.5f, 0)
                );
            }
            return _easeOutBack;
        }
    }

    private static AnimationCurve EaseInOutQuad
    {
        get
        {
            if (_easeInOutQuad == null)
            {
                _easeInOutQuad = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            return _easeInOutQuad;
        }
    }

    private static AnimationCurve EaseOutQuad
    {
        get
        {
            if (_easeOutQuad == null)
            {
                _easeOutQuad = new AnimationCurve(
                    new Keyframe(0, 0, 0, 2),
                    new Keyframe(1, 1, 0, 0)
                );
            }
            return _easeOutQuad;
        }
    }

    // ==================== 初始化 ====================

    private static void EnsureInit()
    {
        if (initialized) return;
        initialized = true;

        runnerGO = new GameObject("[SimpleTween]");
        runnerGO.AddComponent<SimpleTweenRunner>();
        UnityEngine.Object.DontDestroyOnLoad(runnerGO);
    }

    // ==================== 公开 API ====================

    /// <summary>从 zero 缩放到 one</summary>
    public static TweenHandle ScaleIn(Transform target, float duration = 0.3f, AnimationCurve curve = null)
    {
        if (target == null) return TweenHandle.Invalid;
        target.localScale = Vector3.zero;
        return AnimateFloat(t => { if (target != null) target.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t); },
            0f, 1f, duration, false, curve ?? EaseOutBack);
    }

    /// <summary>从当前缩放到 zero</summary>
    public static TweenHandle ScaleOut(Transform target, float duration = 0.2f, AnimationCurve curve = null, Action onComplete = null)
    {
        if (target == null) return TweenHandle.Invalid;
        Vector3 start = target.localScale;
        return AnimateFloat(t => { if (target != null) target.localScale = Vector3.LerpUnclamped(start, Vector3.zero, t); },
            0f, 1f, duration, false, curve ?? EaseOutQuad, onComplete);
    }

    /// <summary>CanvasGroup 淡入</summary>
    public static TweenHandle FadeIn(CanvasGroup cg, float duration = 0.3f)
    {
        if (cg == null) return TweenHandle.Invalid;
        cg.alpha = 0f;
        return AnimateFloat(t => { if (cg != null) cg.alpha = t; }, 0f, 1f, duration, false, EaseInOutQuad);
    }

    /// <summary>CanvasGroup 淡出</summary>
    public static TweenHandle FadeOut(CanvasGroup cg, float duration = 0.2f, Action onComplete = null)
    {
        if (cg == null) return TweenHandle.Invalid;
        return AnimateFloat(t => { if (cg != null) cg.alpha = 1f - t; }, 0f, 1f, duration, false, EaseInOutQuad, onComplete);
    }

    /// <summary>Image alpha 淡入</summary>
    public static TweenHandle FadeIn(Image img, float duration = 0.3f)
    {
        if (img == null) return TweenHandle.Invalid;
        Color c = img.color; c.a = 0f; img.color = c;
        return AnimateFloat(t => { if (img != null) { Color col = img.color; col.a = t; img.color = col; } },
            0f, 1f, duration, false, EaseInOutQuad);
    }

    /// <summary>Image alpha 淡出</summary>
    public static TweenHandle FadeOut(Image img, float duration = 0.2f, Action onComplete = null)
    {
        if (img == null) return TweenHandle.Invalid;
        return AnimateFloat(t => { if (img != null) { Color col = img.color; col.a = 1f - t; img.color = col; } },
            0f, 1f, duration, false, EaseInOutQuad, onComplete);
    }

    /// <summary>移动到目标位置</summary>
    public static TweenHandle MoveTo(Transform target, Vector3 endPos, float duration = 0.3f, AnimationCurve curve = null)
    {
        if (target == null) return TweenHandle.Invalid;
        Vector3 startPos = target.position;
        return AnimateFloat(t => { if (target != null) target.position = Vector3.LerpUnclamped(startPos, endPos, t); },
            0f, 1f, duration, false, curve ?? EaseOutQuad);
    }

    /// <summary>弹性缩放效果</summary>
    public static TweenHandle PunchScale(Transform target, float duration = 0.4f, float intensity = 0.2f)
    {
        if (target == null) return TweenHandle.Invalid;
        Vector3 start = target.localScale;
        Vector3 peak = start * (1f + intensity);
        return AnimateFloat(t =>
        {
            if (target == null) return;
            if (t < 0.5f)
                target.localScale = Vector3.LerpUnclamped(start, peak, t * 2f);
            else
                target.localScale = Vector3.LerpUnclamped(peak, start, (t - 0.5f) * 2f);
        }, 0f, 1f, duration, false, EaseOutQuad);
    }

    /// <summary>通用 float 插值</summary>
    public static TweenHandle AnimateFloat(Action<float> onUpdate, float from, float to, float duration,
        bool useScaledTime = false, AnimationCurve curve = null, Action onComplete = null)
    {
        EnsureInit();

        int slot = FindFreeSlot();
        jobs[slot] = new TweenJob
        {
            generation = jobs[slot].generation + 1,
            active = true,
            elapsed = 0f,
            duration = Mathf.Max(duration, 0.001f),
            useScaledTime = useScaledTime,
            curve = curve,
            onUpdate = t => onUpdate?.Invoke(Mathf.LerpUnclamped(from, to, t)),
            onComplete = onComplete
        };

        return new TweenHandle { index = slot, generation = jobs[slot].generation };
    }

    /// <summary>材质颜色插值</summary>
    public static TweenHandle ColorLerp(Material mat, int propertyID, Color from, Color to, float duration, Action onComplete = null)
    {
        if (mat == null) return TweenHandle.Invalid;
        mat.SetColor(propertyID, from);
        return AnimateFloat(t => { if (mat != null) mat.SetColor(propertyID, Color.LerpUnclamped(from, to, t)); },
            0f, 1f, duration, false, EaseInOutQuad, onComplete);
    }

    /// <summary>取消指定缓动</summary>
    public static void Cancel(TweenHandle handle)
    {
        if (!handle.IsValid) return;
        if (handle.index < jobs.Length && jobs[handle.index].generation == handle.generation)
        {
            jobs[handle.index].active = false;
            jobs[handle.index].onUpdate = null;
            jobs[handle.index].onComplete = null;
        }
    }

    /// <summary>取消所有缓动</summary>
    public static void CancelAll()
    {
        for (int i = 0; i < jobs.Length; i++)
        {
            jobs[i].active = false;
            jobs[i].onUpdate = null;
            jobs[i].onComplete = null;
        }
    }

    // ==================== 内部 ====================

    private static int FindFreeSlot()
    {
        for (int i = 0; i < jobs.Length; i++)
        {
            if (!jobs[i].active) return i;
        }
        // 扩容
        int oldLen = jobs.Length;
        Array.Resize(ref jobs, oldLen * 2);
        return oldLen;
    }

    /// <summary>由 SimpleTweenRunner 在 LateUpdate 中调用</summary>
    internal static void Tick(float unscaledDt, float scaledDt)
    {
        for (int i = 0; i < jobs.Length; i++)
        {
            ref TweenJob job = ref jobs[i];
            if (!job.active) continue;

            float dt = job.useScaledTime ? scaledDt : unscaledDt;
            job.elapsed += dt;
            float t = Mathf.Clamp01(job.elapsed / job.duration);
            float curveT = job.curve != null ? job.curve.Evaluate(t) : t;

            job.onUpdate?.Invoke(curveT);

            if (t >= 1f)
            {
                job.active = false;
                var cb = job.onComplete;
                job.onUpdate = null;
                job.onComplete = null;
                cb?.Invoke();
            }
        }
    }

    // ==================== Runner ====================

    private class SimpleTweenRunner : MonoBehaviour
    {
        void LateUpdate()
        {
            Tick(Time.unscaledDeltaTime, Time.deltaTime);
        }

        void OnDestroy()
        {
            CancelAll();
        }
    }
}
