# GameFlowTest - 游戏流程自动化测试脚本

## 用途

验证游戏重启后（死亡/通关）的核心流程是否正常：
- 空气墙在教程结束后正确消失
- 敌人在每回合正常刷新
- `GameRoundManager` 在场景重载后正确重建（不是旧实例残留）
- `ExperimentVisualControl` 正确持久化且不污染 `GameRoundManager`

## 使用方法

### 1. 添加到场景

在 Unity Editor 中，将 `GameFlowTest.cs` 挂到场景中任意 GameObject 上，或通过 MCP/脚本动态创建：

```
创建一个空 GameObject，添加 GameFlowTest 组件即可
```

### 2. 运行时操作

| 操作 | 说明 |
|---|---|
| **F9** | 开启/关闭测试面板 |
| **手动重启场景** | 按钮触发场景重载，手动测试重启流程 |
| **重置测试** | 将测试状态归零，重新开始监测 |
| **自动重启测试** | 勾选后，教程结束后自动触发场景重载并验证全流程 |

### 3. 自动测试流程（7 个阶段）

勾选 "自动重启测试" 后，脚本会自动执行以下流程：

| 阶段 | 内容 | 超时 |
|---|---|---|
| 0 | 等待教程结束（空气墙消失） | 50s |
| 1 | 验证敌人已生成 | 10s |
| 2 | 等待指定秒数后自动触发场景重载 | 可配置 |
| 3 | 验证新 GameRoundManager 实例已创建 | - |
| 4 | 验证重启后空气墙消失 | 50s |
| 5 | 验证重启后敌人生成 | 15s |
| 6 | 测试完成 | - |

### 4. 面板信息

运行时面板实时显示：
- `GameRoundManager` 的 InstanceID（确认是否为新实例）
- 当前回合数 / 存活敌人数
- 空气墙启用/禁用状态
- 测试阶段和计时
- PASS/FAIL 测试结果
- 详细日志（最多 30 条）

## 配合 MCP 自动化测试

此脚本设计为可通过 Unity MCP（`com.coplaydev.unity-mcp`）远程操控：

1. 通过 MCP 在场景中创建 GameFlowTest GameObject
2. 进入 Play Mode
3. 通过 `execute_code` 查询 GameRoundManager 和空气墙状态
4. 通过 `SceneManager.LoadScene()` 触发场景重载
5. 对比重启前后的 GRM InstanceID，确认实例已刷新

## 注意事项

- 此脚本仅用于开发测试，**不应包含在发布版本中**
- `read_console` 日志搜索 "空气墙" 可快速定位问题
- 脚本使用反射读取 `startWall` 字段，如果字段名变更需要同步更新
