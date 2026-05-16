using UnityEngine;

/// <summary>
/// 康复里程碑系统：25%/50%/75%/100% 时播放粒子 + 音效
/// 仅在实验组 A（ShouldShowVisuals）播放视觉效果
/// </summary>
public class RecoveryMilestoneSystem : MonoBehaviour
{
    public static RecoveryMilestoneSystem Instance { get; private set; }

    [Header("里程碑阈值")]
    [SerializeField] private float[] milestones = { 0.25f, 0.5f, 0.75f, 1.0f };

    [Header("粒子效果")]
    [SerializeField] private ParticleSystem milestoneParticle25;
    [SerializeField] private ParticleSystem milestoneParticle50;
    [SerializeField] private ParticleSystem milestoneParticle75;
    [SerializeField] private ParticleSystem milestoneParticle100;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip milestoneSound;

    [Header("UI 提示")]
    [SerializeField] private GameObject milestoneUIPrefab;

    private int nextMilestoneIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    /// <summary>检查并触发里程碑</summary>
    public void CheckMilestone(float progress)
    {
        if (nextMilestoneIndex >= milestones.Length) return;

        // 可能一次跳过多个里程碑
        while (nextMilestoneIndex < milestones.Length && progress >= milestones[nextMilestoneIndex])
        {
            TriggerMilestone(nextMilestoneIndex);
            nextMilestoneIndex++;
        }
    }

    private void TriggerMilestone(int index)
    {
        bool showVisuals = ExperimentVisualControl.Instance == null ||
                           ExperimentVisualControl.Instance.ShouldShowVisuals();

        // 视觉效果：仅实验组
        if (showVisuals)
        {
            ParticleSystem ps = GetParticleForIndex(index);
            if (ps != null) ps.Play();
        }

        // 音效：所有组
        if (audioSource != null && milestoneSound != null)
        {
            audioSource.PlayOneShot(milestoneSound);
        }

        // 语音
        if (PlayerVoiceSystem.Instance != null)
        {
            string voiceKey = index switch
            {
                0 => "Recovery_25",
                1 => "Recovery_50",
                2 => "Recovery_75",
                3 => "Full_Recovery",
                _ => null
            };
            if (!string.IsNullOrEmpty(voiceKey))
                PlayerVoiceSystem.Instance.PlayVoice(voiceKey);
        }

        Debug.Log($"康复里程碑达成: {milestones[index] * 100:F0}%");
    }

    private ParticleSystem GetParticleForIndex(int index)
    {
        return index switch
        {
            0 => milestoneParticle25,
            1 => milestoneParticle50,
            2 => milestoneParticle75,
            3 => milestoneParticle100,
            _ => null
        };
    }

    /// <summary>重置里程碑（用于新游戏/重新开始）</summary>
    public void ResetMilestones()
    {
        nextMilestoneIndex = 0;
    }
}
