using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 游戏流程测试脚本 —— 验证重启后空气墙消失 + 敌人刷新是否正常
/// 使用方法：挂在场景中任意 GameObject 上，运行后按 F9 开启/关闭测试面板
/// </summary>
public class GameFlowTest : MonoBehaviour
{
    [Header("测试开关")]
    [Tooltip("总开关，运行时按 F9 切换")]
    public bool enableTest = true;

    [Tooltip("自动测试：教程结束后自动触发重启（用于无人值守测试）")]
    public bool autoRestartTest = false;

    [Tooltip("自动重启等待秒数（教程结束后等这么久再重启）")]
    public float restartDelay = 5f;

    // --- 运行时状态 ---
    private GameRoundManager grm;
    private int lastRoundIndex = -999;
    private int lastEnemiesAlive = -999;
    private bool lastWallState = true;
    private string lastGameState = "";

    private int testPhase = 0; // 0=等待教程, 1=教程结束验证, 2=等待重启, 3=重启后验证
    private float phaseTimer = 0f;
    private int grmInstanceId = 0;

    private string testResult = "";
    private bool testPassed = false;

    // 日志
    private readonly System.Collections.Generic.List<string> logs = new System.Collections.Generic.List<string>();
    private const int MAX_LOGS = 30;

    // GUI
    private GUIStyle headerStyle;
    private GUIStyle passStyle;
    private GUIStyle failStyle;
    private GUIStyle logStyle;
    private bool stylesInit = false;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddLog($"[场景加载] {scene.name}");
        testPhase = 0;
        phaseTimer = 0f;
        grm = null;
        grmInstanceId = 0;
        testResult = "";
        testPassed = false;
    }

    void Update()
    {
        // F9 切换面板
        if (Input.GetKeyDown(KeyCode.F9))
        {
            enableTest = !enableTest;
            AddLog(enableTest ? "[测试] 面板已开启" : "[测试] 面板已关闭");
        }

        if (!enableTest) return;

        // 查找 GameRoundManager
        if (grm == null)
        {
            grm = GameRoundManager.Instance;
            if (grm != null)
            {
                grmInstanceId = grm.GetInstanceID();
                AddLog($"[找到 GRM] InstanceID={grmInstanceId}");
            }
        }

        if (grm == null) return;

        // 监控状态变化
        MonitorState();

        // 自动测试流程
        if (autoRestartTest) RunAutoTest();
    }

    void MonitorState()
    {
        if (grm == null) return;

        // 回合数变化
        if (grm.currentRoundIndex != lastRoundIndex)
        {
            AddLog($"[回合] currentRoundIndex: {lastRoundIndex} -> {grm.currentRoundIndex}");
            lastRoundIndex = grm.currentRoundIndex;
        }

        // 敌人数变化
        if (grm.enemiesAlive != lastEnemiesAlive)
        {
            AddLog($"[敌人] enemiesAlive: {lastEnemiesAlive} -> {grm.enemiesAlive}");
            lastEnemiesAlive = grm.enemiesAlive;
        }

        // 空气墙状态
        bool wallActive = IsWallActive();
        if (wallActive != lastWallState)
        {
            AddLog($"[空气墙] {(wallActive ? "启用" : "禁用")}");
            lastWallState = wallActive;
        }
    }

    bool IsWallActive()
    {
        if (grm == null) return false;
        // 通过反射读取 startWall 字段
        var field = typeof(GameRoundManager).GetField("startWall",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var wall = field.GetValue(grm) as GameObject;
            if (wall != null) return wall.activeSelf;
        }
        return false;
    }

    void RunAutoTest()
    {
        phaseTimer += Time.deltaTime;

        switch (testPhase)
        {
            case 0: // 等待教程结束（空气墙消失）
                if (!IsWallActive())
                {
                    AddLog("[自动测试] 教程结束，空气墙已消失 -> 验证通过");
                    testPhase = 1;
                    phaseTimer = 0f;
                }
                else if (phaseTimer > 50f)
                {
                    AddLog("[自动测试] 超时：50秒后空气墙仍然存在 -> 验证失败");
                    testResult = "FAIL: 空气墙未在50秒内消失";
                    testPassed = false;
                    testPhase = -1; // 停止
                }
                break;

            case 1: // 教程结束，等待敌人生成
                if (grm.enemiesAlive > 0 || grm.currentRoundIndex >= 0)
                {
                    AddLog($"[自动测试] 敌人已生成 (round={grm.currentRoundIndex}, alive={grm.enemiesAlive}) -> 验证通过");
                    testPhase = 2;
                    phaseTimer = 0f;
                }
                else if (phaseTimer > 10f)
                {
                    AddLog("[自动测试] 超时：教程结束10秒后仍无敌人 -> 验证失败");
                    testResult = "FAIL: 教程结束后敌人未生成";
                    testPassed = false;
                    testPhase = -1;
                }
                break;

            case 2: // 等待一段时间后触发重启
                if (phaseTimer > restartDelay)
                {
                    AddLog("[自动测试] 触发场景重载...");
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    testPhase = 3;
                    phaseTimer = 0f;
                }
                break;

            case 3: // 重启后等待新 GRM 初始化
                if (phaseTimer > 2f)
                {
                    var newGrm = GameRoundManager.Instance;
                    if (newGrm != null)
                    {
                        int newId = newGrm.GetInstanceID();
                        if (newId != grmInstanceId)
                        {
                            AddLog($"[自动测试] GRM 已刷新 (旧={grmInstanceId}, 新={newId}) -> 验证通过");
                            grm = newGrm;
                            grmInstanceId = newId;
                            testPhase = 4;
                            phaseTimer = 0f;
                        }
                        else
                        {
                            AddLog($"[自动测试] GRM InstanceID 未变化 ({newId}) -> 可能是旧实例存活");
                            testResult = "FAIL: GameRoundManager 未重新创建（旧实例存活）";
                            testPassed = false;
                            testPhase = -1;
                        }
                    }
                }
                break;

            case 4: // 重启后验证空气墙消失
                if (!IsWallActive())
                {
                    AddLog("[自动测试] 重启后空气墙已消失 -> 验证通过");
                    testPhase = 5;
                    phaseTimer = 0f;
                }
                else if (phaseTimer > 50f)
                {
                    AddLog("[自动测试] 超时：重启后50秒空气墙仍存在 -> 验证失败");
                    testResult = "FAIL: 重启后空气墙未消失";
                    testPassed = false;
                    testPhase = -1;
                }
                break;

            case 5: // 重启后验证敌人生成
                if (grm.enemiesAlive > 0 || grm.currentRoundIndex >= 0)
                {
                    AddLog($"[自动测试] 重启后敌人已生成 (round={grm.currentRoundIndex}, alive={grm.enemiesAlive}) -> 验证通过");
                    testResult = "PASS: 重启后空气墙消失 + 敌人正常生成";
                    testPassed = true;
                    testPhase = 6;
                }
                else if (phaseTimer > 15f)
                {
                    AddLog("[自动测试] 超时：重启后15秒仍无敌人 -> 验证失败");
                    testResult = "FAIL: 重启后敌人未生成";
                    testPassed = false;
                    testPhase = -1;
                }
                break;

            case 6: // 测试完成
                break;
        }
    }

    void AddLog(string msg)
    {
        string entry = $"[{Time.time:F1}s] {msg}";
        logs.Add(entry);
        if (logs.Count > MAX_LOGS) logs.RemoveAt(0);
        Debug.Log($"[GameFlowTest] {msg}");
    }

    // ==================== GUI ====================

    void InitStyles()
    {
        if (stylesInit) return;
        stylesInit = true;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        passStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.green }
        };

        failStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.red }
        };

        logStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white },
            wordWrap = false
        };
    }

    void OnGUI()
    {
        if (!enableTest) return;
        InitStyles();

        float x = 10, y = 10, w = 500;

        // 标题
        GUI.Label(new Rect(x, y, w, 25), "=== GameFlowTest (F9 切换) ===", headerStyle);
        y += 28;

        // 当前状态
        if (grm != null)
        {
            GUI.Label(new Rect(x, y, w, 20), $"GRM InstanceID: {grmInstanceId}  |  Round: {grm.currentRoundIndex}  |  Enemies: {grm.enemiesAlive}  |  Complete: {grm.isGameComplete}", logStyle);
            y += 18;
            GUI.Label(new Rect(x, y, w, 20), $"空气墙: {(IsWallActive() ? "启用" : "禁用")}  |  测试阶段: {testPhase}  |  计时: {phaseTimer:F1}s", logStyle);
            y += 22;
        }
        else
        {
            GUI.Label(new Rect(x, y, w, 20), "等待 GameRoundManager 初始化...", logStyle);
            y += 22;
        }

        // 测试结果
        if (!string.IsNullOrEmpty(testResult))
        {
            var style = testPassed ? passStyle : failStyle;
            GUI.Label(new Rect(x, y, w, 25), $"结果: {testResult}", style);
            y += 25;
        }

        // 自动测试开关
        autoRestartTest = GUI.Toggle(new Rect(x, y, w, 20), autoRestartTest, " 自动重启测试（教程结束后自动重载场景验证）");
        y += 25;

        // 手动操作按钮
        if (GUI.Button(new Rect(x, y, 120, 25), "手动重启场景"))
        {
            AddLog("[手动] 触发场景重载");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (GUI.Button(new Rect(x + 130, y, 120, 25), "重置测试"))
        {
            testPhase = 0;
            phaseTimer = 0f;
            testResult = "";
            testPassed = false;
            AddLog("[手动] 测试已重置");
        }
        y += 30;

        // 日志
        GUI.Label(new Rect(x, y, w, 20), "--- 日志 ---", headerStyle);
        y += 20;

        for (int i = 0; i < logs.Count; i++)
        {
            GUI.Label(new Rect(x, y, w, 16), logs[i], logStyle);
            y += 15;
        }
    }
}
