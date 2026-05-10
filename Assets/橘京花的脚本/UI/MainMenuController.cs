using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI 面板引用")]
    [Tooltip("包含开始游戏、退出游戏按钮的父物体")]
    public GameObject mainPanel;

    [Tooltip("包含简单、普通、困难、无尽按钮的父物体")]
    public GameObject difficultyPanel;

    [Header("场景设置")]
    public string gameSceneName = "1"; // 你的游戏场景名字

    void Start()
    {
        // 初始化：显示主面板，隐藏难度面板
        ShowMainPanel();
    }

    // --- 按钮事件绑定 ---

    private bool UseVRPanel(GameObject panel) =>
        panel != null && FeatureToggle.Instance != null && FeatureToggle.Instance.useVRUIPanel;

    private void SwitchPanel(GameObject oldPanel, GameObject newPanel)
    {
        if (oldPanel != null)
        {
            var oldVRP = oldPanel.GetComponent<VRUIPanel>();
            if (UseVRPanel(oldPanel) && oldVRP != null)
                oldVRP.HidePanel(() => ActivatePanel(newPanel));
            else
            { oldPanel.SetActive(false); ActivatePanel(newPanel); }
        }
        else
        {
            ActivatePanel(newPanel);
        }
    }

    public void ShowDifficultyPanel()
    {
        SwitchPanel(mainPanel, difficultyPanel);
    }

    public void ShowMainPanel()
    {
        SwitchPanel(difficultyPanel, mainPanel);
    }

    private void ActivatePanel(GameObject panel)
    {
        if (panel == null) return;
        var vrp = panel.GetComponent<VRUIPanel>();
        if (UseVRPanel(panel) && vrp != null) vrp.ShowPanel();
        else panel.SetActive(true);
    }

    // 选择难度并开始游戏
    public void SelectDifficultyAndStart(int difficultyIndex)
    {
        // 0:Easy, 1:Normal, 2:Hard, 3:Endless
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.currentDifficulty = (DifficultyLevel)difficultyIndex;
        }
        else
        {
            Debug.LogError("❌ 场景中缺少 GameSettings 组件！无法保存难度。");
        }

        // 切换场景
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}