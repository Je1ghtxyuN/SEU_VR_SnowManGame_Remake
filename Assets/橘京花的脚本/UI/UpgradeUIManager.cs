using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TextMeshProUGUI leftButtonText;
    [SerializeField] private TextMeshProUGUI rightButtonText;
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image rightIcon;

    [Header("特效引用")]
    [SerializeField] private GameObject levelUpEffectPrefab;

    [Header("升级池设置")]
    [SerializeField] private List<UpgradeData> upgradePool = new List<UpgradeData>();

    [Header("VR显示设置")]
    [SerializeField] private float displayDistance = 2f;
    [SerializeField] private float heightOffset = -0.3f;
    [SerializeField] private float playerFeetOffset = -1.7f;

    private UpgradeData currentLeftUpgrade;
    private UpgradeData currentRightUpgrade;
    private Transform playerCamera;
    private Dictionary<UpgradeData, int> pickCounts = new Dictionary<UpgradeData, int>();

    void Start()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;
        if (upgradePanel != null) upgradePanel.SetActive(false);
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
        List<UpgradeData> validPool = new List<UpgradeData>();

        foreach (var data in upgradePool)
        {
            if (data == null) continue;

            // 检查次数限制
            int picked = pickCounts.ContainsKey(data) ? pickCounts[data] : 0;
            if (picked >= data.maxPickCount) continue;

            // 剑已解锁则跳过
            if (data.upgradeType == UpgradeType.UnlockSword &&
                PlayerUpgradeHandler.Instance != null && PlayerUpgradeHandler.Instance.IsSwordUnlocked())
                continue;

            validPool.Add(data);
        }

        if (validPool.Count < 2)
        {
            Debug.LogWarning("有效升级选项不足2个！");
            return;
        }

        // 权重随机选择
        currentLeftUpgrade = PickWeighted(validPool, null);
        currentRightUpgrade = PickWeighted(validPool, currentLeftUpgrade);

        // 更新 UI
        if (leftButtonText != null) leftButtonText.text = currentLeftUpgrade.GetDisplayText();
        if (rightButtonText != null) rightButtonText.text = currentRightUpgrade.GetDisplayText();

        if (leftIcon != null) leftIcon.sprite = currentLeftUpgrade.icon;
        if (rightIcon != null) rightIcon.sprite = currentRightUpgrade.icon;
    }

    private UpgradeData PickWeighted(List<UpgradeData> pool, UpgradeData exclude)
    {
        float totalWeight = 0f;
        foreach (var d in pool)
        {
            if (d != exclude) totalWeight += d.weight;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var d in pool)
        {
            if (d == exclude) continue;
            cumulative += d.weight;
            if (roll <= cumulative) return d;
        }

        // 兜底
        foreach (var d in pool)
        {
            if (d != exclude) return d;
        }
        return pool[0];
    }

    private void ApplyUpgradeEffect(UpgradeData upgrade)
    {
        Debug.Log($"执行升级: {upgrade.upgradeName} ({upgrade.upgradeType})");

        SpawnVisualEffect();

        var handler = PlayerUpgradeHandler.Instance;
        if (handler == null)
        {
            Debug.LogError("场景中找不到 PlayerUpgradeHandler，升级无法生效！");
            return;
        }

        switch (upgrade.upgradeType)
        {
            case UpgradeType.Heal: handler.UpgradeHeal(upgrade.value); break;
            case UpgradeType.Damage: handler.UpgradeDamage(upgrade.value); break;
            case UpgradeType.Speed: handler.UpgradeSpeed(upgrade.value); break;
            case UpgradeType.UnlockSword: handler.UnlockSword(); break;
            case UpgradeType.PetMultishot: handler.UpgradePetMultishot(); break;
            case UpgradeType.PetFireRate: handler.UpgradePetFireRate(upgrade.value); break;
            case UpgradeType.PetDamage: handler.UpgradePetDamage(upgrade.value); break;
            case UpgradeType.RecoveryBonus: handler.UpgradeHealToFull(); break;
            default: Debug.LogWarning($"未知的升级类型: {upgrade.upgradeType}"); break;
        }

        // 记录选择次数
        if (!pickCounts.ContainsKey(upgrade)) pickCounts[upgrade] = 0;
        pickCounts[upgrade]++;
    }

    private void SpawnVisualEffect()
    {
        if (PlayerVoiceSystem.Instance != null)
        {
            PlayerVoiceSystem.Instance.PlayVoice("Level_Up");
        }

        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            return;
        }

        if (levelUpEffectPrefab != null && playerCamera != null)
        {
            GameObject effect = Instantiate(levelUpEffectPrefab, playerCamera);
            effect.transform.localPosition = new Vector3(0, playerFeetOffset, 0);
            effect.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("LevelUpEffectPrefab 未赋值 或 找不到玩家相机！");
        }
    }

    public void OnLeftButtonClicked()
    {
        if (currentLeftUpgrade != null) ApplyUpgradeEffect(currentLeftUpgrade);
        GameRoundManager.Instance.FinishUpgrade();
    }

    public void OnRightButtonClicked()
    {
        if (currentRightUpgrade != null) ApplyUpgradeEffect(currentRightUpgrade);
        GameRoundManager.Instance.FinishUpgrade();
    }
}
