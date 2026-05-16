using UnityEngine;

public class IceCrystalEffectSystem : MonoBehaviour
{
    [Header("核心组件")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AudioSource audioSource;

    [Header("视觉表现引用")]
    public RPMIceEffect iceVisualEffect;
    [SerializeField] private ParticleSystem iceCrystalParticles;

    [Header("游戏逻辑参数")]
    [SerializeField] private AudioClip collectSound;
    [Range(0f, 3f)] public float collectVolume = 1.5f;
    [SerializeField] private float healAmount = 20f;

    private int crystalCount = 0;
    private bool hasPlayedFirstAbsorb = false;

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (iceVisualEffect == null) iceVisualEffect = GetComponentInChildren<RPMIceEffect>();
    }

    public void CollectIceCrystal()
    {
        crystalCount++;

        // 1. 播放声音 (保留)
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound, collectVolume);
        }

        // 仅在实验组播放视觉特效
        if (ExperimentVisualControl.Instance == null || ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            if (iceCrystalParticles != null) iceCrystalParticles.Play();
            if (iceVisualEffect != null) iceVisualEffect.ActivateIceEffect();

            // 霜冻镜头特效（受 FeatureToggle 控制）
            if (FrostScreenEffect.Instance != null)
            {
                if (FeatureToggle.Instance == null || FeatureToggle.Instance.useLensFrost)
                    FrostScreenEffect.Instance.TriggerFrost();
            }

            if (!hasPlayedFirstAbsorb && PetVoiceSystem.Instance != null)
            {
                PetVoiceSystem.Instance.PlayVoice("Tutorial_Absorb", 1.0f);
                hasPlayedFirstAbsorb = true;
            }
        }

        // 3. 玩家回血 (保留)
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        // 4. 触发语音 (保留)
      

        // 5. 触发烧伤恢复进度 (保留，BurnRecoverySystem内部会处理是否显示变化)
        if (BurnRecoverySystem.Instance != null)
        {
            BurnRecoverySystem.Instance.AddRecoveryProgress();
        }
    }

    public int GetCrystalCount() => crystalCount;
}