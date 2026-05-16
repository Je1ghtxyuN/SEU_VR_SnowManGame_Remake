using UnityEngine;
using UnityEngine.UI;

public class FixedHealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UnityEngine.UI.Image healthFillImage; // Ѫ�����ͼ
    [SerializeField] private UnityEngine.UI.Image healthBackground; // Ѫ������

    [Header("Color Settings")]
    [SerializeField] private Color fullHealthColor = Color.red; // ��Ѫ��ɫ
    [SerializeField] private Color lowHealthColor = Color.blue; // ��Ѫ��ɫ
    [SerializeField] private float criticalThreshold = 0.2f; // ��ɫ�����ٽ��

    private Health healthSystem; // ����������ֵϵͳ

    public void Initialize(Health health)
    {
        healthSystem = health;

        // ע��Ѫ���仯�¼�
        healthSystem.OnHealthChanged += UpdateHealthBar;
        healthSystem.OnDeath += HandleDeath;

        // ��ʼ����
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthSystem == null || healthFillImage == null) return;

        // ����Ѫ��������
        float healthPercent = healthSystem.GetHealthPercentage();
        healthFillImage.fillAmount = healthPercent;

        // ����Ѫ���ٷֱȸ�����ɫ
        UpdateHealthColor(healthPercent);
    }

    private void UpdateHealthColor(float healthPercent)
    {
        // ���Բ�ֵ������ɫ�������콥�䣩
        if (healthPercent > criticalThreshold)
        {
            // ֱ��ʹ�ñ�������Ѫ�����ӽ�1����ɫ������Ѫ�����ӽ�0����ɫ��
            float lerpValue = healthPercent;
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, lerpValue);
        }
        else
        {
            // �ٽ�ֵ���±�����ɫ
            healthFillImage.color = lowHealthColor;
        }
    }

    private void HandleDeath()
    {
        // ����ʱ����Ѫ������ʾ����Ч��
        healthFillImage.fillAmount = 0;
        healthFillImage.color = Color.gray;

        // ��������������Ч��
    }

    void OnDestroy()
    {
        // ע���¼�����
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
            healthSystem.OnDeath -= HandleDeath;
        }
    }
}