# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

Snowscape Quest (雪境奇遇) is a VR therapy game for burn injury patients. It uses distraction therapy through a snowscape environment to aid pain management during rehabilitation. By Je1ghtProduction, version 2.2.0.

## Technical Stack

- **Unity**: 2022.3.53f1c1 (LTS, China-region build; international build 2022.3.62f3 is also compatible)
- **Render Pipeline**: URP 14.0.11 (Linear color space)
- **VR Framework**: XR Interaction Toolkit 3.1.2 + OpenXR 1.14.1
- **Input**: New Input System only (`activeInputHandler: 1`)
- **Scripting Backend**: IL2CPP (both Standalone and Android)
- **Avatar SDK**: Ready Player Me (via git package)
- **Ocean**: Crest Ocean System
- **Sky**: TENKOKU Dynamic Sky
- **IK**: RootMotion (FinalIK/VRIK)

## Build & Development

- **Target platform**: Standalone (PC VR via SteamVR/OpenXR) — deployment is on Windows
- **Build scenes** (in `EditorBuildSettings`):
  1. `Assets/Scenes/SnowmanGame/C组/主菜单.unity` — Main menu
  2. `Assets/Scenes/SnowmanGame/C组/1.unity` — Primary gameplay scene
- **No automated tests** exist. Test scenes (`UItest`, `testscene`) are for manual testing only.
- **Cross-platform note**: `Packages/packages-lock.json` is gitignored (regenerated per platform). `PackageManagerSettings.asset` uses `packages.unity.cn` which works on both Mac and Windows. Do not commit platform-specific lock files.

## MCP Integration

Unity MCP server (`com.coplaydev.unity-mcp`) is installed. After starting the Unity Editor, run the MCP server from Unity's menu or console to enable Codex to interact with the editor (scene manipulation, console reading, script editing, etc.).

## Code Architecture

### Directory Layout (Chinese names are intentional)

| Path | Purpose |
|---|---|
| `Assets/橘京花的脚本/` | **Primary game scripts** — all core gameplay logic |
| `Assets/奶龙的小屋/` | Companion pet system scripts + Health UI |
| `Assets/预制体/` | Prefabs (player, enemies, weapons, UI, particles) |
| `Assets/Scenes/` | Game scenes (organized by experiment group) |
| `Assets/声音/` | Audio assets |
| `Assets/场景文件/` | Third-party scene assets |

### Core Systems (`橘京花的脚本/核心脚本/`)

- **`GameRoundManager`** — Singleton. Wave/round system. Difficulty modes: Easy, Normal, Hard, Endless. Spawns enemies per round, triggers upgrade phase on completion.
- **`GameSettings`** — Singleton (DontDestroyOnLoad). Stores difficulty and round configs. Defines `DifficultyLevel` enum.
- **`PlayerUpgradeHandler`** — Singleton. Player upgrades: heal, damage, speed, sword unlock, pet multishot/fire-rate/damage. Integrates with `DynamicMoveProvider`.
- **`ExperimentVisualControl`** — Singleton (DontDestroyOnLoad). Clinical experiment A/B/C group controller. GroupA = full experience, GroupB = no visual effects (water present), GroupC = no water environment.
- **`ExperimentEnvironmentManager`** — Controls environment visibility per experiment group.

### Enemy AI (`橘京花的脚本/敌人逻辑/`)

- **`EnemyAI`** — Basic AI: patrol, OverlapSphere detection, line-of-sight, chase, snowball projectile attacks.
- **`AdvancedEnemyAI`** — Enhanced AI: random wandering, anti-stuck detection, out-of-bounds recovery, player memory duration.
- **`AdvancedSnowmanManager`** — Enemy spawner with MaterialPropertyBlock-based visual randomization (hat/scarf colors).

### Weapon System (`橘京花的脚本/武器系统/`)

- **`PlayerWeaponController`** — Switches between snowball thrower and ice sword via Input System actions.
- **`IceSword`** — Melee, 40 base damage × upgrade multiplier, trigger-collider hit detection with deduplication.
- **`SnowballThrower`** — Ranged throwing (in `雪球交互/` subfolder).

### Pet/Companion (`橘京花的脚本/精灵系统/精灵脚本/`)

- **`PetCombatSystem`** — Auto-targets enemies, fires homing projectiles. Supports multishot (fan spread), upgradeable damage/fire-rate.
- **`PetVoiceSystem`** — Singleton. Queued voice lines with 3D spatial audio, cooldowns, first-encounter special voice.
- **`PetFollowVR`** (in `奶龙的小屋/`) — SmoothDamp follow, intelligent rotation, touch-triggered animations.

### Therapeutic Mechanic (`橘京花的脚本/效果系统/BurnRecoverySystem/`)

Collecting ice crystals gradually transitions the player avatar's material from "burned" appearance (red/warm emission) to healthy "recovered" state. Respects experiment group control (Group B/C skip visual effects).

### UI (`橘京花的脚本/UI/`)

- **`MainMenuController`** — Difficulty selection + scene loading.
- **`UpgradeUIManager`** — Between-round upgrade selection (2 random options). Pauses game, positions UI in VR space relative to camera.
- **`GameInfoUI`** — In-game HUD: round name, remaining enemies.

### VR Body (`橘京花的脚本/VRIK/`, `橘京花的脚本/角色动画自制/`)

- **`AutoVRIKCalibrator`** — Automatic VRIK calibration for VR body tracking.
- **`VRBodyAnimator`** / **`VRAnimationSpeedControl`** — VR body animation driven by movement.

### Health System (`奶龙的小屋/`)

- **`Health`** — Generic health component with `OnHealthChanged`/`OnDeath` events, damage, heal, percentage tracking.
- **`HealthUI`** — World-space enemy health bar (billboard to camera).
- **`FixedHealthUI`** — Fixed player health bar with color lerp based on health percentage.

## Patterns & Conventions

- **Singleton pattern** is used extensively (`Instance` static properties) for manager classes.
- **Chinese directory and file names** are used throughout — this is intentional and should be preserved.
- All scripts use `MonoBehaviour` — no ScriptableObjects for game logic (only for data/config assets).
- VR interactions use XR Interaction Toolkit's action-based system (Input System actions, not legacy input).
- Enemy visual randomization uses `MaterialPropertyBlock` (not material instances) for GPU instancing compatibility.
- Experiment group system (A/B/C) must be respected when adding visual/environment features — check `ExperimentVisualControl.Instance` before applying effects.

## Scripting Defines

Key preprocessor symbols: `CREST_OCEAN`, `READY_PLAYER_ME`, `USE_INPUT_SYSTEM_POSE_CONTROL`, `USE_STICK_CONTROL_THUMBSTICKS`, `UNITY_POST_PROCESSING_STACK_V2`.

## Windows Compatibility

All code changes must compile on both Mac and Windows. The project deploys on Windows. Do not introduce platform-specific code without preprocessor guards. The Unity China build (`c1` suffix) and international build are both used — avoid version-specific API calls.
