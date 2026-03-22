using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject fixedHealthBarPrefab;
    [SerializeField] private GameObject deathUIPrefab;
    [SerializeField] private float deathTimeScale = 0.3f;
    [SerializeField] private GameObject leftHandController;

    private Health healthSystem;
    private FixedHealthUI healthUI;
    private Camera vrCamera;
    private GameObject deathUIInstance;
    private bool isDead = false;

    public bool isInvincible = false;

    private float lastLowHealthVoiceTime = -999f;
    // 特效管理器引用
    private VRLensEffectManager lensEffectManager;

    void Start()
    {
        healthSystem = new Health(100f);
        healthSystem.OnDeath += Die;
        // 注册血量变化
        healthSystem.OnHealthChanged += OnHealthChanged;

        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            Debug.LogError("Main Camera not found");
            return;
        }

        // ⭐ 自动查找并报错提示
        lensEffectManager = FindObjectOfType<VRLensEffectManager>();
        if (lensEffectManager == null)
        {
            Debug.LogWarning("⚠️ PlayerHealth: 场景中没找到 VRLensEffectManager，低血量特效将不显示！");
        }

        CreateHealthBar();
    }

    private void CreateHealthBar()
    {
        GameObject healthBarObj = Instantiate(
            fixedHealthBarPrefab,
            vrCamera.transform.position,
            vrCamera.transform.rotation,
            vrCamera.transform
        );

        healthBarObj.transform.localPosition = new Vector3(0, -0.2f, 1.5f);
        healthBarObj.transform.localRotation = Quaternion.identity;
        healthBarObj.transform.localScale = Vector3.one * 0.002f;

        healthUI = healthBarObj.GetComponent<FixedHealthUI>();
        healthUI.Initialize(healthSystem);
    }

    // ⭐ 核心：血量变化回调
    private void OnHealthChanged()
    {
        if (lensEffectManager != null)
        {
            float percent = healthSystem.GetHealthPercentage();
            // Debug.Log($"当前血量百分比: {percent}"); // 调试用
            lensEffectManager.UpdateHealthEffect(percent);

            if (percent < 0.2f && Time.time - lastLowHealthVoiceTime > 60f)
            {
                if (PetVoiceSystem.Instance != null)
                {
                    PetVoiceSystem.Instance.PlayVoice("Player_LowHealth");
                    lastLowHealthVoiceTime = Time.time;

                    Heal(50f); 
                }
            }
        }


    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (isInvincible)
        {
            return;
        }

        if (!isDead)
        {
            healthSystem.TakeDamage(amount);
            // 这里不需要手动调用 OnHealthChanged，因为上面已经订阅了 healthSystem.OnHealthChanged
            // 前提是你的 Health.cs 在 TakeDamage 里正确 Invoke 了事件
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        var thrower = GetComponentInChildren<SnowballThrower>();
        if (thrower != null) thrower.canThrow = false;

        var snowmanManager = FindObjectOfType<AdvancedSnowmanManager>();
        if (snowmanManager != null)
        {
            snowmanManager.ClearAllSnowmen();
        }

        if (PetVoiceSystem.Instance != null)
        {
            // 语音ID: "Game_Over"
            PetVoiceSystem.Instance.PlayVoice("Game_Over");
        }

        ShowDeathUI();
        Time.timeScale = deathTimeScale;

        healthSystem.OnDeath -= Die;
        healthSystem.OnHealthChanged -= OnHealthChanged;

        // ⭐ 修改：移除自动重启，等待玩家手动点击重新开始按钮
        // 不再调用 StartCoroutine(RestartGameRoutine());
    }

    public void Heal(float amount)
    {
        if (!isDead)
        {
            healthSystem.Heal(amount);
        }
    }

    // ... [ShowDeathUI 和 DisableLeftHandController 保持不变]
    private void ShowDeathUI()
    {
        deathUIInstance = Instantiate(
            deathUIPrefab,
            vrCamera.transform.position + vrCamera.transform.forward * 1.5f + (-vrCamera.transform.up) * 1f,
            vrCamera.transform.rotation
        );
    }

    // 公共方法：供死亡UI按钮调用，重新开始游戏
    public void ManualRestartGame()
    {
#if UNITY_EDITOR
        Debug.Log("🔄 PlayerHealth.ManualRestartGame() 被调用，准备重新开始游戏");
#endif

        // 恢复时间缩放
        Time.timeScale = 1f;

        // 销毁死亡UI
        if (deathUIInstance != null)
        {
            Destroy(deathUIInstance);
            deathUIInstance = null;
        }

        // 设置跳过教程标志
        ButtonFunction.skipTutorialOnReload = true;
#if UNITY_EDITOR
        Debug.Log($"✅ 已设置跳过教程标志: {ButtonFunction.skipTutorialOnReload}");
#endif

        // 重新加载当前场景（完全重置游戏状态）
        string currentSceneName = SceneManager.GetActiveScene().name;
#if UNITY_EDITOR
        Debug.Log($"🔄 重新加载场景: {currentSceneName}");
#endif
        SceneManager.LoadScene(currentSceneName);
    }
}