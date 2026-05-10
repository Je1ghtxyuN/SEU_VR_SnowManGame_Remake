using UnityEngine;

/// <summary>
/// 升级数据 ScriptableObject — 在 Inspector 中配置每个升级选项的所有参数
/// 创建方式：右键 Create > Snowscape > Upgrade Data
/// </summary>
[CreateAssetMenu(menuName = "Snowscape/Upgrade Data", fileName = "NewUpgrade")]
public class UpgradeData : ScriptableObject
{
    [Header("显示")]
    public string upgradeName = "新升级";
    [TextArea(2, 4)]
    public string description = "";
    public Sprite icon;
    public UpgradeRarity rarity = UpgradeRarity.Common;

    [Header("效果")]
    public UpgradeType upgradeType = UpgradeType.Heal;
    [Tooltip("效果数值：Heal=回复量, Damage/Speed/PetFireRate/PetDamage=百分比增量, PetMultishot=弹数增量, UnlockSword/RecoveryBonus 忽略")]
    public float value = 0f;

    [Header("池控制")]
    [Tooltip("最大可选次数（99=无限）")]
    public int maxPickCount = 99;
    [Tooltip("随机权重（越高越容易出现）")]
    public float weight = 1f;

    /// <summary>
    /// 生成带富文本的显示字符串（兼容旧的 displayText 格式）
    /// </summary>
    public string GetDisplayText()
    {
        string desc = GetShortDescription();
        if (string.IsNullOrEmpty(desc))
            return upgradeName;
        return $"{upgradeName}\n<size=60%>{desc}</size>";
    }

    private string GetShortDescription()
    {
        switch (upgradeType)
        {
            case UpgradeType.Heal: return $"回复 {value:0} 点血量";
            case UpgradeType.Damage: return $"提升 {value * 100:0}% 伤害";
            case UpgradeType.Speed: return $"提升 {value * 100:0}% 移速";
            case UpgradeType.UnlockSword: return "解锁近战武器 (按B切换)";
            case UpgradeType.PetMultishot: return $"精灵子弹数量 +{(int)value}";
            case UpgradeType.PetFireRate: return $"精灵射速提升 {value * 100:0}%";
            case UpgradeType.PetDamage: return $"精灵伤害提升 {value * 100:0}%";
            case UpgradeType.RecoveryBonus: return "完全恢复生命";
            default: return description;
        }
    }
}
