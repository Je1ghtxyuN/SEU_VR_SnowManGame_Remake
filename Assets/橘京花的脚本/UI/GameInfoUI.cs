using UnityEngine;
using TMPro;

public class GameInfoUI : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private GameObject uiPrefab; // 以前的分数UI预制体
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 0.28f, 0);
    [SerializeField] private float uiScale = 0.001f;

    private Camera vrCamera;
    private GameObject uiInstance;
    private TextMeshProUGUI infoText;

    void Start()
    {
        vrCamera = Camera.main;
        if (vrCamera == null) return;

        CreateUI();
    }

    void Update()
    {
        if (uiInstance != null && vrCamera != null)
        {
            uiInstance.transform.position = vrCamera.transform.position + vrCamera.transform.TransformVector(uiOffset);
            uiInstance.transform.rotation = Quaternion.LookRotation(uiInstance.transform.position - vrCamera.transform.position);
        }
    }

    private void CreateUI()
    {
        uiInstance = Instantiate(uiPrefab, vrCamera.transform.position, Quaternion.identity, vrCamera.transform);
        uiInstance.transform.localScale = Vector3.one * uiScale;
        infoText = uiInstance.GetComponentInChildren<TextMeshProUGUI>();
    }

    // ⭐ 提供给 GameManager 调用的更新方法
    public void UpdateInfo(string roundStr, int enemiesLeft)
    {
        if (infoText != null)
        {
            if (GameRoundManager.Instance != null && GameRoundManager.Instance.isGameComplete)
            {
                infoText.text = "<color=green>Mission Complete！\nGo to the gate</color>";
            }
            else
            {
                infoText.text = $"round: {roundStr}\nenemiesLeft: {enemiesLeft}";
            }
        }
    }
}