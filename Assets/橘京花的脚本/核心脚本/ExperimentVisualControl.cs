using UnityEngine;

/// <summary>
/// 实验总控制器：管理所有实验分组
/// </summary>
[DefaultExecutionOrder(-100)]
public class ExperimentVisualControl : MonoBehaviour
{
    public static ExperimentVisualControl Instance { get; private set; }

    public enum ExperimentGroup
    {
        GroupA_FullExperience, // 实验组：全特效 + 全环境
        GroupB_NoVisuals,      // 对照组1：无视觉特效（但有水环境）
        GroupC_NoWaterEnv      // 对照组2：无水环境（连瀑布和河都没了）
    }

    [Header("当前实验分组")]
    [Tooltip("在运行前选择当前被试所属的组别")]
    public ExperimentGroup currentGroup = ExperimentGroup.GroupA_FullExperience;

    void Awake()
    {
        if (Instance == null)
        {
            // 创建独立的持久化 GameObject，避免 DontDestroyOnLoad 连带其他组件（如 GameRoundManager）一起存活
            var holder = new GameObject("ExperimentController");
            var ctrl = holder.AddComponent<ExperimentVisualControl>();
            ctrl.currentGroup = currentGroup;
            Instance = ctrl;
            DontDestroyOnLoad(holder);

            // 移除当前组件（保留 GameManager 上的其他组件）
            Destroy(this);
        }
        else
        {
            // 场景重载产生的副本：同步分组设置后销毁自身
            Instance.currentGroup = currentGroup;
            Destroy(this);
        }
    }

    // --- 对外提供的判断接口 ---

    /// <summary>
    /// 是否应该显示魔法特效（光柱、发光、材质变化、粒子）
    /// </summary>
    public bool ShouldShowVisuals()
    {
        // 只有 A 组显示特效
        return currentGroup == ExperimentGroup.GroupA_FullExperience;
    }

    /// <summary>
    /// 是否应该保留水体环境（瀑布、河流模型）
    /// </summary>
    public bool ShouldEnableWaterEnvironment()
    {
        // A组 和 B组 都有水，只有 C组 没有
        return currentGroup != ExperimentGroup.GroupC_NoWaterEnv;
    }
}