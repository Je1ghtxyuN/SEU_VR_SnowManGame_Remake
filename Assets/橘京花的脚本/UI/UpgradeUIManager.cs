using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UpgradeUIManager : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeOption
    {
        public string id;
        public string displayText;
        [TextArea] public string description;
    }

    [Header("UI组件引用")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TextMeshProUGUI leftButtonText;
    [SerializeField] private TextMeshProUGUI rightButtonText;

    [Header("特效引用 ")]
    [SerializeField] private GameObject levelUpEffectPrefab;

    [Tooltip("请选择地面的Layer")]
    [SerializeField] private LayerMask groundLayer;

    [Header("升级池设置")]
    [SerializeField] private List<UpgradeOption> upgradePool = new List<UpgradeOption>();

    [Header("VR显示设置")]
    [SerializeField] private float displayDistance = 2f;
    [SerializeField] private float heightOffset = -0.3f;
    [SerializeField] private float playerFeetOffset = -1.7f;

    private UpgradeOption currentLeftUpgrade;
    private UpgradeOption currentRightUpgrade;
    private Transform playerCamera;

    void Start()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;
        if (upgradePanel != null) upgradePanel.SetActive(false);

        // 如果没有配置，添加默认配置
        if (upgradePool.Count == 0)
        {
            upgradePool.Add(new UpgradeOption { id = "HEAL", displayText = "恢复生命\n<size=60%>回复 30 点血量</size>" });
            upgradePool.Add(new UpgradeOption { id = "DAMAGE", displayText = "力量强化\n<size=60%>提升 20% 伤害</size>" });
            upgradePool.Add(new UpgradeOption { id = "SPEED", displayText = "迅捷步伐\n<size=60%>提升 20% 移速</size>" });
            upgradePool.Add(new UpgradeOption { id = "SWORD", displayText = "寒冰之剑\n<size=60%>解锁近战武器 (按B切换)</size>" });
            upgradePool.Add(new UpgradeOption { id = "PET_MULTI", displayText = "精灵散射\n<size=60%>精灵子弹数量 +1</size>" });
            upgradePool.Add(new UpgradeOption { id = "PET_RATE", displayText = "精灵急速\n<size=60%>精灵射速提升 25%</size>" });
            upgradePool.Add(new UpgradeOption { id = "PET_DMG", displayText = "精灵强化\n<size=60%>精灵伤害提升 30%</size>" });
        }
    }

    public void ShowUpgradePanel()
    {
        if (upgradePanel == null) return;

        if (playerCamera != null)
        {
            Vector3 newPos = playerCamera.position + playerCamera.forward * displayDistance + Vector3.up * heightOffset;
            upgradePanel.transform.position = newPos;
            upgradePanel.transform.LookAt(playerCamera);
            upgradePanel.transform.Rotate(0, 180f, 0);
        }

        RandomizeUpgrades();
        upgradePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void HideUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void RandomizeUpgrades()
    {
        List<UpgradeOption> validPool = new List<UpgradeOption>();

        foreach (var option in upgradePool)
        {
            if (option.id == "SWORD")
            {
                if (PlayerUpgradeHandler.Instance != null && !PlayerUpgradeHandler.Instance.IsSwordUnlocked())
                {
                    validPool.Add(option);
                }
            }
            else
            {
                validPool.Add(option);
            }
        }

        if (validPool.Count < 2)
        {
            Debug.LogWarning("有效升级选项不足2个！");
            return;
        }

        int index1 = Random.Range(0, validPool.Count);
        int index2 = index1;
        while (index2 == index1)
        {
            index2 = Random.Range(0, validPool.Count);
        }

        currentLeftUpgrade = validPool[index1];
        currentRightUpgrade = validPool[index2];

        if (leftButtonText != null) leftButtonText.text = currentLeftUpgrade.displayText;
        if (rightButtonText != null) rightButtonText.text = currentRightUpgrade.displayText;
    }

    private void ApplyUpgradeEffect(UpgradeOption upgrade)
    {
        Debug.Log($"执行升级: {upgrade.id}");

        SpawnVisualEffect();

        var handler = PlayerUpgradeHandler.Instance;
        if (handler == null)
        {
            Debug.LogError("场景中找不到 PlayerUpgradeHandler，升级无法生效！");
            return;
        }

        switch (upgrade.id)
        {
            case "HEAL": handler.UpgradeHeal(30f); break;
            case "DAMAGE": handler.UpgradeDamage(0.25f); break;
            case "SPEED": handler.UpgradeSpeed(0.2f); break;
            case "SWORD": handler.UnlockSword(); break;
            case "PET_MULTI": handler.UpgradePetMultishot(); break;
            case "PET_RATE": handler.UpgradePetFireRate(0.25f); break;
            case "PET_DMG": handler.UpgradePetDamage(0.3f); break;
            default: Debug.LogWarning("未知的升级ID"); break;
        }
    }

    private void SpawnVisualEffect()
    {
        // 播放语音 (心理暗示通常属于听觉干预，对照组一般保留，如果想关掉也可以加判断)
        if (PlayerVoiceSystem.Instance != null)
        {
            PlayerVoiceSystem.Instance.PlayVoice("Level_Up");
        }

        // ⭐ 修改：核心视觉控制逻辑
        // 如果是对照组 (ShouldShowVisuals 返回 false)，则直接 return，不生成光柱
        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            return;
        }

        if (levelUpEffectPrefab != null && playerCamera != null)
        {
            // 只有在实验组才会执行这里
            GameObject effect = Instantiate(levelUpEffectPrefab, playerCamera);
            effect.transform.localPosition = new Vector3(0, playerFeetOffset, 0);
            effect.transform.localRotation = Quaternion.identity;
            // 3秒后自动销毁效果对象，防止内存泄漏
            Destroy(effect, 3f);
        }
        else
        {
            Debug.LogWarning("LevelUpEffectPrefab 未赋值 或 找不到玩家相机！");
        }
    }

    public void OnLeftButtonClicked()
    {
        if (currentLeftUpgrade != null) ApplyUpgradeEffect(currentLeftUpgrade);
        if (GameRoundManager.Instance != null) GameRoundManager.Instance.FinishUpgrade();
    }

    public void OnRightButtonClicked()
    {
        if (currentRightUpgrade != null) ApplyUpgradeEffect(currentRightUpgrade);
        if (GameRoundManager.Instance != null) GameRoundManager.Instance.FinishUpgrade();
    }
}