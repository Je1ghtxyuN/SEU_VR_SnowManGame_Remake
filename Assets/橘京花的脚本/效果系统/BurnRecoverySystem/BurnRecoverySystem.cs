using UnityEngine;
using UnityEngine.UI;

public class BurnRecoverySystem : MonoBehaviour
{
    public static BurnRecoverySystem Instance { get; private set; }

    [Header("UI 生成设置")]
    public GameObject bodyUIPrefab;
    public Vector3 uiPositionOffset = new Vector3(-0.4f, -0.2f, 1.5f);
    public float uiScale = 0.002f;

    [Header("康复逻辑设置")]
    public int targetCrystalCount = 10;
    public string targetRendererName = "Renderer_Body";
    public float colorSmoothSpeed = 2f;

    [Header("里程碑系统")]
    [Tooltip("里程碑系统（可选，场景中已有的实例）")]
    public RecoveryMilestoneSystem milestoneSystem;

    [Header("视觉效果：烧伤状态 (起点)")]
    public Color burnTint = new Color(0.7f, 0.5f, 0.5f);
    [ColorUsage(false, true)]
    public Color burnEmission = new Color(0.4f, 0.1f, 0.0f);

    [Header("视觉效果：完全恢复/冷白状态 (终点)")]
    [ColorUsage(false, true)]
    public Color healthyColdTint = new Color(1.1f, 1.15f, 1.4f);
    [ColorUsage(false, true)]
    public Color healthyEmission = Color.black;

    [Header("调试功能")]
    public bool debugPreviewFinalEffect = false;

    // --- 内部变量 ---
    private Image bodyFillImage;
    private int currentCrystals = 0;
    private Renderer targetRenderer;
    private Material runtimeMaterial;

    private Color originalBaseColor;
    private Color currentTargetBase;
    private Color currentTargetEmission;

    private bool _lastDebugState = false;
    private int idBaseColor;
    private int idEmissionColor;
    private bool hasPlayedRecoveryVoice = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CreateRecoveryUI();
        InitializeMaterial();

        // 自动查找里程碑系统
        if (milestoneSystem == null)
            milestoneSystem = FindAnyObjectByType<RecoveryMilestoneSystem>();

        // ⭐ 修改：如果是对照组，直接设置为终点状态（或者你希望的无特效状态）
        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            // 强制设置为健康状态，不显示烧伤
            UpdateVisuals(1.0f);
            // 立即应用一次
            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetColor(idBaseColor, currentTargetBase);
                runtimeMaterial.SetColor(idEmissionColor, currentTargetEmission);
            }
        }
        else
        {
            UpdateVisuals(0);
        }
    }

    void Update()
    {
        // ⭐ 修改：对照组不执行任何材质 Lerp 更新
        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals()) return;

        if (debugPreviewFinalEffect != _lastDebugState)
        {
            _lastDebugState = debugPreviewFinalEffect;
            UpdateVisuals(debugPreviewFinalEffect ? 1.0f : GetRecoveryProgress());
        }

        if (runtimeMaterial != null)
        {
            Color currentBase = runtimeMaterial.GetColor(idBaseColor);
            Color smoothBase = Color.Lerp(currentBase, currentTargetBase, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idBaseColor, smoothBase);

            Color currentEmis = runtimeMaterial.GetColor(idEmissionColor);
            Color smoothEmis = Color.Lerp(currentEmis, currentTargetEmission, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idEmissionColor, smoothEmis);
        }
    }

    public void AddRecoveryProgress()
    {
        currentCrystals++;
        float progress = GetRecoveryProgress();

        // 对照组不更新视觉目标值，只跑逻辑
        if (ExperimentVisualControl.Instance == null || ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            if (!debugPreviewFinalEffect) UpdateVisuals(progress);
        }

        // 里程碑检查（受 FeatureToggle 控制）
        if (milestoneSystem != null)
        {
            if (FeatureToggle.Instance == null || FeatureToggle.Instance.useMilestoneSystem)
                milestoneSystem.CheckMilestone(progress);
        }

        // 语音逻辑保留
        if (progress >= 1.0f && !hasPlayedRecoveryVoice)
        {
            if (PlayerVoiceSystem.Instance != null)
            {
                PlayerVoiceSystem.Instance.PlayVoice("Full_Recovery");
            }
            hasPlayedRecoveryVoice = true;
        }
    }

    public float GetRecoveryProgress()
    {
        if (targetCrystalCount == 0) return 0f;
        return Mathf.Clamp01((float)currentCrystals / targetCrystalCount);
    }

    private void UpdateVisuals(float progress)
    {
        // UI 进度条可能需要保留？如果连UI也要隐藏，可以在这里加判断
        if (bodyFillImage != null) bodyFillImage.fillAmount = progress;

        currentTargetBase = Color.Lerp(originalBaseColor * burnTint, originalBaseColor * healthyColdTint, progress);
        currentTargetEmission = Color.Lerp(burnEmission, healthyEmission, progress);
    }

    void InitializeMaterial()
    {
        Transform foundObj = FindDeepChild(transform, targetRendererName);
        if (foundObj == null) targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        else targetRenderer = foundObj.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;

            if (runtimeMaterial.HasProperty("_BaseColor")) idBaseColor = Shader.PropertyToID("_BaseColor");
            else if (runtimeMaterial.HasProperty("baseColorFactor")) idBaseColor = Shader.PropertyToID("baseColorFactor");
            else idBaseColor = Shader.PropertyToID("_Color");

            if (runtimeMaterial.HasProperty("_EmissionColor")) idEmissionColor = Shader.PropertyToID("_EmissionColor");
            else idEmissionColor = Shader.PropertyToID("emissiveFactor");

            if (runtimeMaterial.HasProperty(idBaseColor))
            {
                originalBaseColor = runtimeMaterial.GetColor(idBaseColor);
            }

            runtimeMaterial.EnableKeyword("_EMISSION");
        }
    }

    private void CreateRecoveryUI()
    {
        if (bodyUIPrefab == null) return;
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        GameObject uiInstance = Instantiate(bodyUIPrefab, mainCam.transform);
        uiInstance.transform.localPosition = uiPositionOffset;
        uiInstance.transform.localRotation = Quaternion.identity;
        uiInstance.transform.localScale = Vector3.one * uiScale;

        Transform fillTransform = FindDeepChild(uiInstance.transform, "Fill_Blue");
        if (fillTransform != null) bodyFillImage = fillTransform.GetComponent<Image>();
        else
        {
            Image[] images = uiInstance.GetComponentsInChildren<Image>();
            foreach (var img in images) if (img.type == Image.Type.Filled) { bodyFillImage = img; break; }
        }

    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}