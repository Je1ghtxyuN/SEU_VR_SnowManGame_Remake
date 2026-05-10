using UnityEngine;

/// <summary>
/// VR 镜头霜冻特效 — 使用 VRFrost shader 的程序化冰晶噪声
/// 四边形覆盖模式，VR 立体渲染安全（shader 含 UNITY_VERTEX_OUTPUT_STEREO）
/// 仅在实验组 A（ShouldShowVisuals）显示
/// </summary>
public class FrostScreenEffect : MonoBehaviour
{
    public static FrostScreenEffect Instance { get; private set; }

    [Header("资源引用")]
    [SerializeField] private Shader frostShader;

    [Header("效果参数")]
    [SerializeField] private float maxIntensity = 0.6f;
    [SerializeField] private float fadeInSpeed = 4f;
    [SerializeField] private float fadeOutSpeed = 0.8f;
    [SerializeField] private Color frostColor = new Color(0.85f, 0.92f, 1.0f, 1f);
    [SerializeField] private float edgeFrost = 0.8f;
    [SerializeField] private float centerFrost = 0.3f;

    private Material runtimeMat;
    private GameObject effectQuad;
    private float currentIntensity;
    private float targetIntensity;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (FeatureToggle.Instance != null && !FeatureToggle.Instance.useLensFrost)
        {
            enabled = false;
            return;
        }
        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            enabled = false;
            return;
        }
        InitializeFrost();
    }

    private void InitializeFrost()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 如果没有指定 shader，尝试加载
        if (frostShader == null)
            frostShader = Shader.Find("Custom/VRFrost");

        if (frostShader == null)
        {
            Debug.LogWarning("VRFrost shader not found, frost effect disabled");
            enabled = false;
            return;
        }

        runtimeMat = new Material(frostShader);
        runtimeMat.SetColor("_FrostColor", frostColor);
        runtimeMat.SetFloat("_EdgeFrost", edgeFrost);
        runtimeMat.SetFloat("_CenterFrost", centerFrost);
        runtimeMat.SetFloat("_Intensity", 0f);

        CreateEffectQuad(cam);
    }

    private void CreateEffectQuad(Camera cam)
    {
        effectQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        effectQuad.name = "VR_Frost_Overlay";
        Destroy(effectQuad.GetComponent<Collider>());
        effectQuad.transform.SetParent(cam.transform);
        effectQuad.transform.localPosition = new Vector3(0, 0, 0.45f);
        effectQuad.transform.localRotation = Quaternion.identity;
        effectQuad.transform.localScale = new Vector3(1.8f, 1.2f, 1f);
        effectQuad.GetComponent<Renderer>().material = runtimeMat;
        effectQuad.SetActive(false);
    }

    void Update()
    {
        if (runtimeMat == null) return;

        float speed = (targetIntensity > currentIntensity) ? fadeInSpeed : fadeOutSpeed;
        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, speed * Time.deltaTime);

        if (effectQuad != null)
        {
            bool shouldShow = currentIntensity > 0.01f;
            if (effectQuad.activeSelf != shouldShow)
                effectQuad.SetActive(shouldShow);
        }

        runtimeMat.SetFloat("_Intensity", currentIntensity);
    }

    /// <summary>触发霜冻效果</summary>
    public void TriggerFrost()
    {
        if (!enabled || runtimeMat == null) return;
        targetIntensity = maxIntensity;
        // 持续一小段时间后自动衰减
        CancelInvoke(nameof(FadeOut));
        Invoke(nameof(FadeOut), 0.3f);
    }

    private void FadeOut()
    {
        targetIntensity = 0f;
    }

    void OnDestroy()
    {
        if (runtimeMat != null) Destroy(runtimeMat);
        if (effectQuad != null) Destroy(effectQuad);
    }
}
