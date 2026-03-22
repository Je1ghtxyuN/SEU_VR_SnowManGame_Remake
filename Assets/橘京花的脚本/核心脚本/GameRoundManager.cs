using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameRoundManager : MonoBehaviour
{
    public static GameRoundManager Instance { get; private set; }

    [System.Serializable]
    public struct RoundData
    {
        public string roundName;
        public int enemyCount;
    }

    [Header("引用设置")]
    public AdvancedSnowmanManager spawner;
    public UpgradeUIManager upgradeUI;
    public GameInfoUI gameInfoUI;
    public GameObject startWall;

    [Header("难度优化")]
    [Tooltip("每回合额外生成的雪人数量，让玩家更容易凑齐击杀数，不用找最后一只。")]
    public int extraSpawnCount = 2;

    // 运行时数据
    private List<RoundData> currentRoundsConfig;
    private bool isEndlessMode = false;

    [Header("状态监控 (只读)")]
    public int currentRoundIndex = -1;
    [Tooltip("这里显示的是【剩余需要击杀】的数量，而不是场上存活的数量")]
    public int enemiesAlive = 0;
    public bool isGameComplete = false;

    private enum GameState { Waiting, Spawning, Fighting, UpgradePhase, Victory }
    private GameState currentState = GameState.Waiting;

    // 缓存当前回合的名称，用于UI刷新
    private string currentRoundNameDisplay;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ResetGameState();
        
        // 检查是否需要跳过教程
        if (ButtonFunction.skipTutorialOnReload)
        {
            ButtonFunction.skipTutorialOnReload = false; // 重置标志
            StartCoroutine(SkipTutorialAndStart());
        }
        else
        {
            StartCoroutine(DelayedStart());
        }
    }

    private void ResetGameState()
    {
        currentRoundIndex = -1;
        enemiesAlive = 0;
        isGameComplete = false;
        currentState = GameState.Waiting;
        currentRoundNameDisplay = "";
#if UNITY_EDITOR
        Debug.Log("🔄 游戏状态已重置");
#endif
    }

    private IEnumerator DelayedStart()
    {
        yield return null; // 等待一帧，确保所有 Awake 方法执行完毕
        
        if (spawner == null) 
        {
            spawner = FindObjectOfType<AdvancedSnowmanManager>();
            if (spawner == null)
            {
                Debug.LogError("❌ 未找到 AdvancedSnowmanManager！敌人生成将无法进行。");
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"✅ 找到 AdvancedSnowmanManager: {spawner.gameObject.name}");
#endif
            }
        }

        // --- 从 GameSettings 读取配置 ---
        if (GameSettings.Instance != null)
        {
            isEndlessMode = (GameSettings.Instance.currentDifficulty == DifficultyLevel.Endless);

            if (!isEndlessMode)
            {
                currentRoundsConfig = GameSettings.Instance.GetRoundsForCurrentDifficulty();
#if UNITY_EDITOR
                Debug.Log($"🔵 已加载难度: {GameSettings.Instance.currentDifficulty}, 总回合数: {currentRoundsConfig.Count}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("🟣 已启动无尽模式");
#endif
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到 GameSettings，启用默认无尽模式测试");
            isEndlessMode = true;
        }

        // 播放开场语音
        if (PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.PlayVoice("Start", 1.0f);
            PetVoiceSystem.Instance.PlayVoice("Tutorial_Move", 1.5f);
            PetVoiceSystem.Instance.PlayVoice("Tutorial_Look", 1.0f);
        }

        StartCoroutine(WaitAndStartGameRoutine());
    }

    private IEnumerator SkipTutorialAndStart()
    {
        yield return null; // 等待一帧，确保所有 Awake 方法执行完毕
        
        if (spawner == null) 
        {
            spawner = FindObjectOfType<AdvancedSnowmanManager>();
            if (spawner == null)
            {
                Debug.LogError("❌ 未找到 AdvancedSnowmanManager！敌人生成将无法进行。");
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"✅ 找到 AdvancedSnowmanManager: {spawner.gameObject.name}");
#endif
            }
        }

        // --- 从 GameSettings 读取配置 ---
        if (GameSettings.Instance != null)
        {
            isEndlessMode = (GameSettings.Instance.currentDifficulty == DifficultyLevel.Endless);

            if (!isEndlessMode)
            {
                currentRoundsConfig = GameSettings.Instance.GetRoundsForCurrentDifficulty();
#if UNITY_EDITOR
                Debug.Log($"🔵 已加载难度: {GameSettings.Instance.currentDifficulty}, 总回合数: {currentRoundsConfig.Count}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("🟣 已启动无尽模式");
#endif
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到 GameSettings，启用默认无尽模式测试");
            isEndlessMode = true;
        }

        // 跳过开场语音，立即移除空气墙
        if (startWall != null)
        {
            startWall.SetActive(false);
#if UNITY_EDITOR
            Debug.Log("🔓 跳过教程，空气墙已立即移除，玩家可自由移动。");
#endif
        }

        // 直接开始第一回合
        StartCoroutine(StartNextRoundRoutine());
    }

    private IEnumerator WaitAndStartGameRoutine()
    {
        if (startWall != null) startWall.SetActive(true);

#if UNITY_EDITOR
        Debug.Log("⏳ 游戏流程：等待新手语音播放 (43秒)...");
#endif
        yield return new WaitForSecondsRealtime(43.0f);

        if (startWall != null)
        {
            startWall.SetActive(false);
#if UNITY_EDITOR
            Debug.Log("🔓 语音结束，空气墙已移除，玩家可自由移动。");
#endif
        }

        StartCoroutine(StartNextRoundRoutine());
    }

    private IEnumerator StartNextRoundRoutine()
    {
        currentRoundIndex++;

        // 检查是否通关 (普通模式)
        if (!isEndlessMode)
        {
            if (currentRoundIndex >= currentRoundsConfig.Count)
            {
                HandleVictory();
                yield break;
            }
        }

        currentState = GameState.Spawning;

        // --- ⭐ 1. 计算本回合的目标数据 ---
        int missionTargetCount = 0;

        if (isEndlessMode)
        {
            currentRoundNameDisplay = $"wave {currentRoundIndex + 1} (endless)";

            int baseCount = GameSettings.Instance ? GameSettings.Instance.endlessBaseEnemyCount : 5;
            float factor = GameSettings.Instance ? GameSettings.Instance.endlessGrowthFactor : 1.5f;

            missionTargetCount = Mathf.FloorToInt(baseCount + (currentRoundIndex * factor));
        }
        else
        {
            RoundData data = currentRoundsConfig[currentRoundIndex];
            currentRoundNameDisplay = data.roundName;
            missionTargetCount = data.enemyCount;
        }

        // --- ⭐ 2. 设置游戏状态 ---
        enemiesAlive = missionTargetCount;

        // --- ⭐ 3. 执行生成 ---
        int actualSpawnCount = missionTargetCount + extraSpawnCount;

        UpdateUI(currentRoundNameDisplay);
#if UNITY_EDITOR
        Debug.Log($"⚔️ 开始回合: {currentRoundNameDisplay}, 任务目标: {missionTargetCount}, 实际生成: {actualSpawnCount}");
#endif

        if (spawner != null)
        {
            spawner.SpawnEnemies(actualSpawnCount);
        }

        currentState = GameState.Fighting;
        UpdateUI(currentRoundNameDisplay);
    }

    public void OnEnemyKilled()
    {
        if (currentState != GameState.Fighting) return;

        // 减少剩余需要击杀的数量
        enemiesAlive--;
        if (enemiesAlive < 0) enemiesAlive = 0;

        // 刷新UI显示
        if (gameInfoUI != null) gameInfoUI.UpdateInfo(currentRoundNameDisplay, enemiesAlive);

        // 如果击杀数达标
        if (enemiesAlive == 0)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
#if UNITY_EDITOR
        Debug.Log("🟢 回合目标达成！结束回合。");
#endif
        currentState = GameState.UpgradePhase;

        if (spawner != null) spawner.ClearAllSnowmen();

        // 检查是否还有下一回合
        bool hasNextRound = isEndlessMode || (currentRoundIndex < currentRoundsConfig.Count - 1);

        if (hasNextRound)
        {


            if (upgradeUI != null) upgradeUI.ShowUpgradePanel();
            else FinishUpgrade();
        }
        else
        {
            HandleVictory();
        }
    }

    public void FinishUpgrade()
    {
        if (upgradeUI != null) upgradeUI.HideUpgradePanel();
        StartCoroutine(StartNextRoundRoutine());
    }

    private void HandleVictory()
    {
#if UNITY_EDITOR
        Debug.Log("🏆 游戏通关！");
#endif
        currentState = GameState.Victory;
        isGameComplete = true;

        if (gameInfoUI != null) gameInfoUI.UpdateInfo("任务完成", 0);

        if (PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.PlayVoice("Level_Complete", 1.0f);
        }
    }

    private void UpdateUI(string roundName)
    {
        if (gameInfoUI != null)
        {
            gameInfoUI.UpdateInfo(roundName, enemiesAlive);
        }
    }

    // ⭐ 新增：重新开始游戏方法（供玩家死亡时调用）
    public void RestartGame()
    {
        if (isGameComplete) return; // 如果游戏已通关，不重启
        
        StopAllCoroutines();
        ResetGameState();
        
        // 重置空气墙状态
        if (startWall != null) startWall.SetActive(true);
        
        // 清理所有敌人
        if (spawner != null) spawner.ClearAllSnowmen();
        
        // 重新开始游戏流程（跳过教程等待）
        StartCoroutine(RestartGameRoutine());
        
#if UNITY_EDITOR
        Debug.Log("🔄 游戏已重新开始（跳过教程）");
#endif
    }

    private IEnumerator RestartGameRoutine()
    {
        // 执行初始化
        yield return StartCoroutine(DelayedStart());
        
        // 立即移除空气墙，不等待43秒
        if (startWall != null)
        {
            startWall.SetActive(false);
#if UNITY_EDITOR
            Debug.Log("� 重启游戏，空气墙已移除");
#endif
        }
        
        // 直接开始第一回合
        StartCoroutine(StartNextRoundRoutine());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        StopAllCoroutines();
    }
}