using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class XRUIToggle : MonoBehaviour
{
    [Header("输入绑定")]
    [SerializeField] private InputActionReference menuAction;
    [SerializeField] private GameObject uiCanvas;

    [Header("游戏状态")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    private VRUIPanel vrPanel;
    private bool isUIVisible;
    private float previousTimeScale;

    private Transform playerCamera;

    void Start()
    {
        playerCamera = Camera.main?.transform;
        menuAction.action.Enable();
        menuAction.action.performed += ToggleUI;
        menuAction.action.AddBinding("<XRController>{LeftHand}/menuButton");

        // 检查功能开关：如果启用 VRUIPanel 且面板上有该组件则使用
        if (uiCanvas != null)
        {
            if (FeatureToggle.Instance != null && FeatureToggle.Instance.useVRUIPanel)
                vrPanel = uiCanvas.GetComponent<VRUIPanel>();
            else
                vrPanel = null;
        }

        if (vrPanel != null)
            vrPanel.HidePanelImmediate();
        else
            uiCanvas.SetActive(false);

        previousTimeScale = Time.timeScale;
    }

    private void ToggleUI(InputAction.CallbackContext ctx)
    {
        isUIVisible = !isUIVisible;

        if (isUIVisible)
        {
            if (vrPanel != null)
            {
                vrPanel.ShowPanel();
            }
            else
            {
                // 回退到原版逻辑：手动定位 + SetActive
                PositionUI();
                uiCanvas.SetActive(true);
            }

            if (pauseGameWhenOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
        }
        else
        {
            if (vrPanel != null)
                vrPanel.HidePanel();
            else
                uiCanvas.SetActive(false);

            if (pauseGameWhenOpen)
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }

    // 原版定位逻辑（FeatureToggle 关闭时的回退）
    private void PositionUI()
    {
        if (playerCamera == null) return;
        uiCanvas.transform.position = playerCamera.position
            + playerCamera.forward * 2f + Vector3.up * -0.3f;
        uiCanvas.transform.LookAt(playerCamera);
        uiCanvas.transform.Rotate(0, 180f, 0);
    }

    void OnDestroy()
    {
        if (isUIVisible && pauseGameWhenOpen)
        {
            Time.timeScale = previousTimeScale;
        }
        menuAction.action.performed -= ToggleUI;
    }
}