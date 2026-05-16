using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 特效测试脚本 — 在测试场景中验证结霜和镜头霜冻效果
/// 按 1: 触发身体结霜 (RPMIceEffect)
/// 按 2: 触发镜头霜冻 (FrostScreenEffect)
/// 按 3: 同时触发两者
/// 按 0: 模拟 BurnRecovery 进度 (每按一次 +10%)
/// </summary>
public class EffectTestRunner : MonoBehaviour
{
    private RPMIceEffect iceEffect;
    private FrostScreenEffect frostScreen;
    private BurnRecoverySystem burnRecovery;
    private bool initialized;

    void Start()
    {
        iceEffect = FindAnyObjectByType<RPMIceEffect>();
        frostScreen = FindAnyObjectByType<FrostScreenEffect>();
        burnRecovery = FindAnyObjectByType<BurnRecoverySystem>();

        // 为 FrostScreenEffect 加载 shader
        if (frostScreen != null)
        {
            var shaderProp = frostScreen.GetType().GetField("frostShader",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (shaderProp != null && shaderProp.GetValue(frostScreen) == null)
            {
                var shader = Shader.Find("Custom/VRFrost");
                if (shader != null)
                {
                    shaderProp.SetValue(frostScreen, shader);
                    Debug.Log("VRFrost shader loaded");
                }
                else Debug.LogError("Custom/VRFrost shader not found!");
            }
        }

        // 手动初始化 FrostScreenEffect（因为 Start 可能已经跑过了）
        if (frostScreen != null)
        {
            var initMethod = frostScreen.GetType().GetMethod("InitializeFrost",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            initMethod?.Invoke(frostScreen, null);
        }

        initialized = true;
        Debug.Log($"EffectTest: iceEffect={iceEffect != null}, frostScreen={frostScreen != null}, burnRecovery={burnRecovery != null}");
    }

    void Update()
    {
        if (!initialized) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame && iceEffect != null)
        {
            Debug.Log("Trigger: Body Frost");
            iceEffect.ActivateIceEffect();
        }

        if (kb.digit2Key.wasPressedThisFrame && frostScreen != null)
        {
            Debug.Log("Trigger: Lens Frost");
            frostScreen.TriggerFrost();
        }

        if (kb.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("Trigger: Both");
            iceEffect?.ActivateIceEffect();
            frostScreen?.TriggerFrost();
        }

        if (kb.digit0Key.wasPressedThisFrame && burnRecovery != null)
        {
            Debug.Log("Trigger: +10% Recovery");
            burnRecovery.AddRecoveryProgress();
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.Box("=== Effect Test ===\n" +
            "[1] Body Frost (RPMIceEffect)\n" +
            "[2] Lens Frost (FrostScreenEffect)\n" +
            "[3] Both\n" +
            "[0] +10% Recovery Progress\n\n" +
            $"RPMIceEffect: {(iceEffect != null ? "OK" : "MISSING")}\n" +
            $"FrostScreen: {(frostScreen != null ? "OK" : "MISSING")}\n" +
            $"BurnRecovery: {(burnRecovery != null ? "OK" : "MISSING")}");
        GUILayout.EndArea();
    }
}
