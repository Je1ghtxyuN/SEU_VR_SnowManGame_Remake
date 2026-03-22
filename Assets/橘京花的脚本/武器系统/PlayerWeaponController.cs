using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("输入设置")]
    public InputActionReference switchWeaponAction;

    [Header("武器引用")]
    [Tooltip("原本的雪球投掷脚本")]
    public SnowballThrower snowballThrower;
    [Tooltip("寒冰剑的游戏物体")]
    public GameObject iceSwordObject;

    [Header("动画控制")]
    [Tooltip("请把玩家模型身上的 Animator 组件拖进来")]
    public Animator handAnimator;

    [Header("状态")]
    public bool hasUnlockedSword = false;
    private bool isSwordActive = false;

    private string gripBoolName = "IsHoldingSword";

    void Start()
    {
        if (snowballThrower != null) snowballThrower.enabled = true;
        if (iceSwordObject != null) iceSwordObject.SetActive(false);

        if (switchWeaponAction != null)
        {
            switchWeaponAction.action.Enable();
            switchWeaponAction.action.performed += OnSwitchWeaponPressed;
        }
    }

    // 每一帧检查游戏是否暂停以及武器状态
    void Update()
    {
        // 1. 如果游戏暂停 (Time.timeScale == 0)
        if (Time.timeScale == 0f)
        {
            // 隐藏所有武器
            if (iceSwordObject != null) iceSwordObject.SetActive(false);
            // 暂停时禁止投掷
            if (snowballThrower != null) snowballThrower.canThrow = false;
        }
        // 2. 如果游戏正常进行 (Time.timeScale > 0)
        else
        {
            if (isSwordActive)
            {
                if (iceSwordObject != null && !iceSwordObject.activeSelf)
                    iceSwordObject.SetActive(true);

                // ⭐ 剑模式：禁止投掷
                if (snowballThrower != null) snowballThrower.canThrow = false;
            }
            else
            {
                if (iceSwordObject != null && iceSwordObject.activeSelf)
                    iceSwordObject.SetActive(false);

                // ⭐ 雪球模式：允许投掷
                if (snowballThrower != null) snowballThrower.canThrow = true;
            }
        }
    }

    private void OnSwitchWeaponPressed(InputAction.CallbackContext context)
    {
        // 暂停时禁止切换
        if (Time.timeScale == 0f) return;

        if (!hasUnlockedSword) return;
        ToggleWeapon();
    }

    public void UnlockSword()
    {
        if (!hasUnlockedSword)
        {
            if (PetVoiceSystem.Instance != null)
            {           
                PetVoiceSystem.Instance.PlayVoice("Weapon_Sword");
            }
        }

        hasUnlockedSword = true;
        if (!isSwordActive)
        {
            ToggleWeapon();
        }
        Debug.Log("武器解锁：寒冰剑！");
    }

    private void ToggleWeapon()
    {
        isSwordActive = !isSwordActive;
        // 具体的 SetActive/Enabled 逻辑已移交 Update 处理
        if (handAnimator != null)
        {
            handAnimator.SetBool(gripBoolName, isSwordActive);
        }
    }

    void OnDestroy()
    {
        if (switchWeaponAction != null)
        {
            switchWeaponAction.action.performed -= OnSwitchWeaponPressed;
            switchWeaponAction.action.Disable();
        }
    }
}