using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UnityEngine.UI.Image healthFillImage; //Ѫ�����ͼ
    [SerializeField] private GameObject healthBarObject; //����Ѫ������

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0); //Ѫ����ͷ����λ��ƫ��
 
    private Transform targetTransform; //�����Ŀ��(���˻����)
    private Health healthSystem; //����������ֵϵͳ

    public void Initialize(Transform target, Health health)
    {
        targetTransform = target;
        healthSystem = health;

        //ע��Ѫ���仯�¼�
        healthSystem.OnHealthChanged += UpdateHealthBar;
        healthSystem.OnDeath += HideHealthBar;

        UpdateHealthBar();
    }

    private void Update()
    {
        //����Ѫ��λ�ã�ʹ�����Ŀ��
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;

            //ʹѪ��ʼ�ճ��������
            transform.rotation = Quaternion.LookRotation(
                Camera.main.transform.forward,
                Camera.main.transform.up
            );
        }
    }

    private void UpdateHealthBar()
    {
        //����Ѫ��������
        healthFillImage.fillAmount = healthSystem.GetHealthPercentage();
        UnityEngine.Debug.Log("Ѫ��ui����");
    }

    private IEnumerator ShowHealthBarTemporarily(float duration)
    {
        healthBarObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        healthBarObject.SetActive(false);
    }

    private void HideHealthBar()
    {
        healthBarObject.SetActive(false);
        //��������������Ч��
    }

    void OnDestroy()
    {
        //ע���¼�����
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
            healthSystem.OnDeath -= HideHealthBar;
        }
    }
}