using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets; // �������ƶ��ű���Sample�����ռ���
using UnityEngine.XR.Interaction.Toolkit;
// ע�⣺������Ҳ��� ActionBasedContinuousMoveProvider��������� Locomotion �����ϵ������

public class PlayerUpgradeHandler : MonoBehaviour
{
    public static PlayerUpgradeHandler Instance { get; private set; }

    [Header("����")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider moveProvider;


    [Header("��ǰ����")]
    public float damageMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public int maxAmmoLevel = 0; // ��ҩ�߼���ʱԤ��

    [Header("������������")]
    public int petProjectileCount = 1;      // �ӵ�����
    public float petFireRateMultiplier = 1.0f; // ���ٱ��� 
    public float petDamageMultiplier = 1.0f;   // �˺�����

    private float initialMoveSpeed = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // �Զ����Ի�ȡ���
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();

        // ��������������Ѱ�� WeaponController (����������)
        if (weaponController == null) weaponController = GetComponentInChildren<PlayerWeaponController>();

        // ����Ѱ���ƶ��ű� (ͨ���� Locomotion ��������)
        if (moveProvider == null) moveProvider = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider>();

        // ��¼��ʼ�ٶ�
        if (moveProvider != null)
        {
            initialMoveSpeed = moveProvider.moveSpeed;
        }
        else
        {
            Debug.LogError("δ�ҵ��ƶ����ƽű� (ActionBasedContinuousMoveProvider)���ٶ���������Ч��");
        }
    }

    // --- ����ִ�з��� ---

    public void UpgradeHeal(float amount)
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(amount);
            Debug.Log($"��һظ��� {amount} ��Ѫ��");
        }
    }

    public void UpgradeDamage(float percentage)
    {
        damageMultiplier += percentage; // ���紫�� 0.2f�����ʱ�Ϊ 1.2
        Debug.Log($"�˺���������ǰ����: {damageMultiplier}");
    }

    public void UpgradeSpeed(float percentage)
    {
        if (moveProvider != null)
        {
            speedMultiplier += percentage;
            moveProvider.moveSpeed = initialMoveSpeed * speedMultiplier;
            Debug.Log($"�ٶ���������ǰ�ٶ�: {moveProvider.moveSpeed}");
        }
    }

    public void UnlockSword()
    {
        if (weaponController != null)
        {
            weaponController.UnlockSword();
        }
    }

    public bool IsSwordUnlocked()
    {
        if (weaponController != null) return weaponController.hasUnlockedSword;
        return false;
    }

    // --- ������������ ---
    public void UpgradePetMultishot()
    {
        petProjectileCount++;
        Debug.Log($"���������������������ǰ����: {petProjectileCount}");
    }

    public void UpgradePetFireRate(float amount) // amount ���� 0.2 ��ʾ��20%
    {
        petFireRateMultiplier += amount;
        Debug.Log($"����������������������ǰ����: {petFireRateMultiplier}");
    }

    public void UpgradePetDamage(float amount)
    {
        petDamageMultiplier += amount;
        Debug.Log($"宠物伤害+{amount * 100}%，当前倍率: {petDamageMultiplier}");
    }

    public void UpgradeHealToFull()
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(999f);
            Debug.Log("完全恢复生命！");
        }
    }
}