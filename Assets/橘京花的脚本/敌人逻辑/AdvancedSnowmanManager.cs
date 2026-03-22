using UnityEngine;
using System.Collections.Generic;

public class AdvancedSnowmanManager : MonoBehaviour
{
    [Header("雪人预制体")]
    public GameObject snowmanPrefab;

    [Header("生成参数")]
    public float minDistanceFromPlayer = 8f;
    public float minDistanceBetweenSnowmen = 3f;
    [Tooltip("生成高度偏移量")]
    public float spawnHeightOffset = 2.0f;

    [Header("区域设置")]
    public List<string> allowedAreas = new List<string>();

    [Header("🎨 雪人外观多样性设置")]
    [Tooltip("雪人模型里帽子的物体名称 (一定要和Hierarchy里的一致)")]
    public string hatObjectName = "Hat";
    [Tooltip("帽子出现的概率 (0-1)，1表示必定有帽子")]
    [Range(0f, 1f)] public float hatChance = 0.8f;
    [Tooltip("帽子可能出现的颜色")]
    public Color[] hatColors = new Color[] { Color.red, Color.blue, Color.green, Color.black };

    [Space(10)]
    [Tooltip("雪人模型里围巾的物体名称")]
    public string scarfObjectName = "Scarf";
    [Tooltip("围巾出现的概率 (0-1)")]
    [Range(0f, 1f)] public float scarfChance = 0.8f;
    [Tooltip("围巾可能出现的颜色")]
    public Color[] scarfColors = new Color[] { Color.yellow, Color.cyan, Color.magenta, Color.white };

    private AdvancedGameAreaManager areaManager;
    private Transform player;
    private List<GameObject> currentSnowmen = new List<GameObject>();

    void Awake()
    {
        areaManager = FindObjectOfType<AdvancedGameAreaManager>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (areaManager == null) Debug.LogError("❌ 未找到 AdvancedGameAreaManager！");
    }

    public void SpawnEnemies(int count)
    {
        ClearDeadSnowmen();

        if (player == null || areaManager == null) return;

        List<Vector3> spawnPositions = GenerateValidSpawnPositions(count);

        if (spawnPositions.Count == 0)
        {
            Debug.LogWarning("⚠️ 未能生成任何有效的雪人位置。");
            for (int i = 0; i < count; i++) 
            {
                if (GameRoundManager.Instance != null) GameRoundManager.Instance.OnEnemyKilled();
            }
            return;
        }

        int missingCount = count - spawnPositions.Count;
        for (int i = 0; i < missingCount; i++) 
        {
            if (GameRoundManager.Instance != null) GameRoundManager.Instance.OnEnemyKilled();
        }

        foreach (Vector3 position in spawnPositions)
        {
            GameObject snowman = Instantiate(snowmanPrefab, position, Quaternion.identity);

            // ⭐ 新增：生成后立即进行随机化外观处理
            RandomizeSnowmanAppearance(snowman);

            currentSnowmen.Add(snowman);
        }

        Debug.Log($"✅ 本回合成功生成 {spawnPositions.Count} 个雪人");
    }

    // ⭐⭐ 核心功能：随机化外观 ⭐⭐
    private void RandomizeSnowmanAppearance(GameObject snowman)
    {
        // 1. 处理帽子
        ApplyRandomAccessory(snowman, hatObjectName, hatChance, hatColors);

        // 2. 处理围巾
        ApplyRandomAccessory(snowman, scarfObjectName, scarfChance, scarfColors);
    }

    private void ApplyRandomAccessory(GameObject snowman, string partName, float chance, Color[] colors)
    {
        // 递归查找子物体，防止物体藏在层级深处
        Transform partTrans = FindDeepChild(snowman.transform, partName);

        if (partTrans != null)
        {
            GameObject partObj = partTrans.gameObject;

            // A. 随机决定是否有这个部位
            bool hasPart = Random.value < chance;
            partObj.SetActive(hasPart);

            // B. 如果有，随机颜色
            if (hasPart && colors.Length > 0)
            {
                Renderer rend = partObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    Color randomColor = colors[Random.Range(0, colors.Length)];

                    // 使用 PropertyBlock 修改颜色，性能更好且不会导致材质球内存泄漏
                    MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                    rend.GetPropertyBlock(propBlock);

                    // 自动尝试匹配常见的颜色属性名 (兼容 URP / Built-in / ShaderGraph)
                    if (rend.material.HasProperty("_BaseColor"))
                        propBlock.SetColor("_BaseColor", randomColor);
                    else if (rend.material.HasProperty("_Color"))
                        propBlock.SetColor("_Color", randomColor);

                    rend.SetPropertyBlock(propBlock);
                }
            }
        }
        // else { Debug.LogWarning($"在雪人身上没找到名为 '{partName}' 的物体，请检查名称设置"); }
    }

    // 辅助工具：递归查找子物体
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private List<Vector3> GenerateValidSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = count * 20;

        while (positions.Count < count && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomPos = GetRandomPositionInAllowedAreas();
            if (randomPos == Vector3.zero) continue;

            if (IsValidSpawnPosition(randomPos, positions))
            {
                positions.Add(randomPos);
            }
        }
        return positions;
    }

    private Vector3 GetRandomPositionInAllowedAreas()
    {
        List<AdvancedGameAreaManager.GameArea> availableAreas = new List<AdvancedGameAreaManager.GameArea>();
        if (areaManager.gameAreas == null) return Vector3.zero;

        foreach (var area in areaManager.gameAreas)
        {
            if (area.isActive)
            {
                if (allowedAreas.Count == 0 || allowedAreas.Contains(area.areaName))
                    availableAreas.Add(area);
            }
        }

        if (availableAreas.Count == 0) return Vector3.zero;

        AdvancedGameAreaManager.GameArea selectedArea = availableAreas[Random.Range(0, availableAreas.Count)];
        return GetRandomPointInArea(selectedArea);
    }

    private Vector3 GetRandomPointInArea(AdvancedGameAreaManager.GameArea area)
    {
        List<Vector3> polygon = area.GetBoundaryPoints();
        if (polygon.Count < 3) return Vector3.zero;

        Vector3 min = polygon[0];
        Vector3 max = polygon[0];
        foreach (Vector3 point in polygon)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Vector3 randomPoint;
        int attempts = 0;
        bool isValid = false;

        do
        {
            randomPoint = new Vector3(
                Random.Range(min.x, max.x),
                spawnHeightOffset,
                Random.Range(min.z, max.z)
            );
            attempts++;
            if (areaManager.IsPointInArea(randomPoint, area)) isValid = true;
        }
        while (!isValid && attempts < 50);

        return isValid ? randomPoint : Vector3.zero;
    }

    private bool IsValidSpawnPosition(Vector3 position, List<Vector3> existingPositions)
    {
        if (player == null) return false;
        
        Vector3 flatPos = new Vector3(position.x, 0, position.z);
        Vector3 flatPlayerPos = new Vector3(player.position.x, 0, player.position.z);

        if (Vector3.Distance(flatPos, flatPlayerPos) < minDistanceFromPlayer) return false;

        foreach (Vector3 existingPos in existingPositions)
        {
            Vector3 flatExisting = new Vector3(existingPos.x, 0, existingPos.z);
            if (Vector3.Distance(flatPos, flatExisting) < minDistanceBetweenSnowmen) return false;
        }
        return true;
    }

    private void ClearDeadSnowmen()
    {
        for (int i = currentSnowmen.Count - 1; i >= 0; i--)
        {
            if (currentSnowmen[i] == null) currentSnowmen.RemoveAt(i);
        }
    }

    public void ClearAllSnowmen()
    {
        for (int i = currentSnowmen.Count - 1; i >= 0; i--)
        {
            if (currentSnowmen[i] != null)
            {
                Destroy(currentSnowmen[i]);
            }
        }
        currentSnowmen.Clear();
        Debug.Log("🧹 已清空所有雪人");
    }
}