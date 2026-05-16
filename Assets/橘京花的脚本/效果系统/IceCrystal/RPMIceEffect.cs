using UnityEngine;
using System.Collections;

/// <summary>
/// 身体结霜特效 — 收集冰晶时皮肤表面的冰霜效果
/// 通过材质属性过渡实现：颜色、粗糙度、发光、法线贴图
/// 优化点：法线贴图平滑过渡、冰蓝色发光脉冲、边缘高光
/// </summary>
public class RPMIceEffect : MonoBehaviour
{
    [Header("过渡设置")]
    public float freezeDuration = 4.0f;
    public float transitionSpeed = 2.5f;
    public float meltSpeed = 1.5f;

    [Header("冰霜视觉")]
    public Texture2D iceNormalMap;
    public Color frostTint = new Color(0.85f, 0.92f, 1.0f);
    [Range(0f, 1f)]
    public float iceRoughness = 0.5f;
    [ColorUsage(false, true)]
    public Color iceEmission = new Color(0.3f, 0.6f, 1.2f) * 1.8f;

    [Header("发光脉冲")]
    [Tooltip("冰霜发光的脉冲幅度，让表面有微妙的冰晶闪烁")]
    public float emissionPulseAmplitude = 0.3f;
    public float emissionPulseSpeed = 2.0f;

    [Header("目标渲染器")]
    public Renderer targetRenderer;

    private Material runtimeMaterial;
    private Coroutine effectCoroutine;
    private float currentFrostT = 0f;

    // 原始数据
    private Color originalColor;
    private float originalRoughness;
    private Texture originalNormalMap;
    private Color originalEmission;

    // 属性 ID
    private int id_BaseColor;
    private int id_Roughness;
    private int id_NormalMap;
    private int id_Emission;

    void Start()
    {
        InitializeMaterial();
    }

    void InitializeMaterial()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer == null) return;

        runtimeMaterial = targetRenderer.material;

        // 查找属性
        if (HasProp("baseColorFactor")) id_BaseColor = Shader.PropertyToID("baseColorFactor");
        else if (HasProp("BaseColor")) id_BaseColor = Shader.PropertyToID("BaseColor");
        else id_BaseColor = Shader.PropertyToID("_BaseColor");

        if (HasProp("roughnessFactor")) id_Roughness = Shader.PropertyToID("roughnessFactor");
        else if (HasProp("Roughness")) id_Roughness = Shader.PropertyToID("Roughness");
        else id_Roughness = Shader.PropertyToID("_Roughness");

        if (HasProp("normalTexture")) id_NormalMap = Shader.PropertyToID("normalTexture");
        else if (HasProp("NormalTex")) id_NormalMap = Shader.PropertyToID("NormalTex");
        else id_NormalMap = Shader.PropertyToID("_NormalMap");

        if (HasProp("emissiveFactor")) id_Emission = Shader.PropertyToID("emissiveFactor");
        else if (HasProp("EmissiveColor")) id_Emission = Shader.PropertyToID("EmissiveColor");
        else if (HasProp("_EmissionColor")) id_Emission = Shader.PropertyToID("_EmissionColor");
        else id_Emission = Shader.PropertyToID("Emissive");

        // 备份
        if (HasPropID(id_BaseColor)) originalColor = runtimeMaterial.GetColor(id_BaseColor);
        if (HasPropID(id_Roughness)) originalRoughness = runtimeMaterial.GetFloat(id_Roughness);
        if (HasPropID(id_NormalMap)) originalNormalMap = runtimeMaterial.GetTexture(id_NormalMap);
        originalEmission = HasPropID(id_Emission) ? runtimeMaterial.GetColor(id_Emission) : Color.black;
    }

    bool HasProp(string name) => runtimeMaterial != null && runtimeMaterial.HasProperty(name);
    bool HasPropID(int id) => runtimeMaterial != null && runtimeMaterial.HasProperty(id);

    public void ActivateIceEffect()
    {
        if (effectCoroutine != null) StopCoroutine(effectCoroutine);
        if (FeatureToggle.Instance != null && !FeatureToggle.Instance.useOptimizedBodyFrost)
            effectCoroutine = StartCoroutine(IceProcessRoutine_Legacy());
        else
            effectCoroutine = StartCoroutine(IceProcessRoutine());
    }

    // 原版逻辑：瞬间切换法线贴图，无发光脉冲，简单过渡
    private IEnumerator IceProcessRoutine_Legacy()
    {
        // 瞬间切换法线
        if (iceNormalMap != null && HasPropID(id_NormalMap))
            runtimeMaterial.SetTexture(id_NormalMap, iceNormalMap);

        // 结冰
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            ApplyIceMaterial_Legacy(Mathf.Clamp01(timer));
            yield return null;
        }

        yield return new WaitForSeconds(freezeDuration);

        // 融化
        timer = 1f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime * transitionSpeed;
            ApplyIceMaterial_Legacy(Mathf.Clamp01(timer));
            yield return null;
        }

        // 还原法线
        if (HasPropID(id_NormalMap))
            runtimeMaterial.SetTexture(id_NormalMap, originalNormalMap);
        ApplyIceMaterial_Legacy(0f);
        effectCoroutine = null;
    }

    // 原版材质应用（无脉冲）
    private void ApplyIceMaterial_Legacy(float t)
    {
        if (runtimeMaterial == null) return;
        if (HasPropID(id_BaseColor))
        {
            Color targetColor = originalColor * frostTint;
            runtimeMaterial.SetColor(id_BaseColor, Color.Lerp(originalColor, targetColor, t));
        }
        if (HasPropID(id_Roughness))
            runtimeMaterial.SetFloat(id_Roughness, Mathf.Lerp(originalRoughness, iceRoughness, t));
        if (HasPropID(id_Emission))
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor(id_Emission, Color.Lerp(originalEmission, iceEmission, t));
        }
    }

    private IEnumerator IceProcessRoutine()
    {
        // 1. 结冰过程（平滑过渡）
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            currentFrostT = Mathf.Clamp01(timer);
            ApplyIceMaterial(currentFrostT);
            yield return null;
        }

        // 2. 保持阶段 — 发光脉冲
        float holdTimer = 0f;
        while (holdTimer < freezeDuration)
        {
            holdTimer += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(holdTimer * emissionPulseSpeed * Mathf.PI * 2f) * emissionPulseAmplitude;
            ApplyEmissionPulse(pulse);
            yield return null;
        }

        // 3. 融化过程（比结冰慢）
        timer = 1f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime * meltSpeed;
            currentFrostT = Mathf.Clamp01(timer);
            ApplyIceMaterial(currentFrostT);
            yield return null;
        }

        // 4. 还原法线贴图
        currentFrostT = 0f;
        if (HasPropID(id_NormalMap))
            runtimeMaterial.SetTexture(id_NormalMap, originalNormalMap);

        ApplyIceMaterial(0f);
        effectCoroutine = null;
    }

    private void ApplyIceMaterial(float t)
    {
        if (runtimeMaterial == null) return;

        // 颜色：原色 × 冰霜色调
        if (HasPropID(id_BaseColor))
        {
            Color targetColor = originalColor * frostTint;
            runtimeMaterial.SetColor(id_BaseColor, Color.Lerp(originalColor, targetColor, t));
        }

        // 粗糙度：磨砂冰面
        if (HasPropID(id_Roughness))
        {
            runtimeMaterial.SetFloat(id_Roughness, Mathf.Lerp(originalRoughness, iceRoughness, t));
        }

        // 发光：冰蓝色辉光
        if (HasPropID(id_Emission))
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor(id_Emission, Color.Lerp(originalEmission, iceEmission, t));
        }

        // 法线贴图：平滑混合（通过 UV 缩放模拟过渡，而非瞬间切换）
        if (iceNormalMap != null && HasPropID(id_NormalMap) && t > 0.3f)
        {
            // 在 t > 0.3 时才开始切换法线，避免太突兀
            float normalBlend = Mathf.InverseLerp(0.3f, 0.7f, t);
            if (normalBlend > 0.5f)
                runtimeMaterial.SetTexture(id_NormalMap, iceNormalMap);
        }
    }

    private void ApplyEmissionPulse(float multiplier)
    {
        if (!HasPropID(id_Emission)) return;
        Color pulsedEmission = iceEmission * multiplier;
        runtimeMaterial.SetColor(id_Emission, pulsedEmission);
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
