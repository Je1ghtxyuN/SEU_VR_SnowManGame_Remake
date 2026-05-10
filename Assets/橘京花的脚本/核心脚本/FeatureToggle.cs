using UnityEngine;

/// <summary>
/// 功能开关 — 在 Inspector 中逐项控制本次优化的启用/禁用
/// 挂在场景中任意 GameObject 上（建议放在 GameRoundManager 或单独的空物体上）
/// 关闭某项 → 自动回退到原本的旧逻辑
/// </summary>
public class FeatureToggle : MonoBehaviour
{
    public static FeatureToggle Instance { get; private set; }

    [Header("━━━━━━ UI 改进 ━━━━━━")]
    [Tooltip("VRUIPanel 弹入/淡出动画。关闭后回退为原版 SetActive 切换")]
    public bool useVRUIPanel = true;

    [Tooltip("圆角面板 Shader 材质。关闭后使用 Unity 默认 UI 材质")]
    public bool useRoundedPanel = true;

    [Header("━━━━━━ 特效优化 ━━━━━━")]
    [Tooltip("优化后的身体结霜效果（平滑法线过渡 + 发光脉冲）。关闭后回退为原版瞬间切换逻辑")]
    public bool useOptimizedBodyFrost = true;

    [Tooltip("收集冰晶时的 VR 镜头霜冻覆盖层。关闭后不创建镜头霜冻")]
    public bool useLensFrost = true;

    [Tooltip("25%/50%/75%/100% 里程碑粒子 + 语音。关闭后不触发里程碑")]
    public bool useMilestoneSystem = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
