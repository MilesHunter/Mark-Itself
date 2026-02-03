# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Mark-Itself** is a Unity 2D platformer featuring a unique skill-based visibility system. Players can unlock and switch between two skills (FilterSystem and MaskSystem) to reveal hidden objects of specific colors, enabling puzzle-solving and exploration gameplay.

**Unity Version:** 2021.3+
**Rendering Pipeline:** Universal Render Pipeline (URP)
**Platform:** Windows (win32)

## Development Commands

### Opening the Project
- Open `Mark-Itself.sln` in Visual Studio or Rider
- Open the project folder in Unity Editor (2021.3+)

### Building
- Unity Editor: `File → Build Settings → Build`
- Main scene: `Assets/Scenes/Level 01.unity`

### Testing
- Play Mode: Press Play button in Unity Editor
- Test scenes available in `Assets/Scenes/`:
  - `PlayerAnimatorTestScene.unity` - Player animation testing
  - `ShaderTestScene.unity` - Shader testing
  - `BackgroundParallaxTest.unity` - Background parallax testing

### In-Game Controls
- **Movement:** Arrow keys / A-D
- **Jump:** Space
- **Switch Skill:** R key
- **Activate/Deactivate Skill:** Right Mouse Button
- **Pause:** ESC
- **Debug Respawn:** F5 (teleports to nearest left respawn point)

## Core Architecture

### Singleton Managers
Three core singleton managers control the game:

1. **GameManager** (`Assets/Scripts/Managers/GameManager.cs`)
   - Central game state machine (Playing, Paused, GameOver, Loading)
   - Player lifecycle (spawning, respawning, death)
   - Respawn point management
   - Score and time tracking
   - Skill system activation/deactivation

2. **UIManager** (`Assets/Scripts/UI/UIManager.cs`)
   - Color selection UI (dynamically generated buttons)
   - Skill display (current skill icon)
   - Pause menu
   - Game state UI updates

3. **FilterEffectManager2D** (`Assets/Scripts/VFX/FilterEffectManager2D.cs`)
   - Visual effects for skill transitions
   - Screen shake effects
   - Audio effects
   - Particle effects

### Skill System Architecture

The game features two mutually exclusive skills that must be unlocked via SkillBall pickups:

#### FilterSystem (`Assets/Scripts/Skills/FilterSystem.cs`)
- **Type:** Global visibility filter
- **Mechanism:** When activated, scans scene for objects on "Interaction" layer with matching color tag and disables them (SetActive(false))
- **Deactivation:** Re-enables all previously disabled objects
- **Layer Requirement:** Objects must be on "Interaction" layer
- **Tag Requirement:** Objects must have matching color tag (Red, Blue, Green, Yellow, Purple)
- **Important:** FilterSystem itself should NOT be on Interaction layer

#### MaskSystem (`Assets/Scripts/Skills/MaskSystem.cs`)
- **Type:** Player-following collision mask
- **Mechanism:** Uses OnTriggerEnter2D/OnTriggerExit2D to detect collisions
  - Matching tag: sets isTrigger = false (solid collision)
  - Different tag: sets isTrigger = true (pass-through)
- **Follows:** Player position
- **Updates:** Tag and SpriteRenderer color based on FilterColor

### Player Controller (`Assets/Scripts/Player/PlayerController.cs`)

Central player logic handling:
- Movement with coyote time (0.2s) and jump buffering (0.2s)
- Ground detection using `Collider2D.Cast` (NOT OverlapCircle)
- Animation state management via Animator parameters:
  - `IsRunning`, `IsGrounded`, `VerticalVelocity`, `canland`, `canjump`
- Skill management (switching, activation, color selection)
- Skill unlocking via SkillBall pickups
- Death state management

**Events:**
- `OnSkillChanged` - Fired when skill switches
- `OnColorChanged` - Fired when skill color changes
- `OnPlayerResetAndEnabled` - Fired when player respawns
- `OnSkillUnlocked` - Fired when new skill is unlocked

### Hidden Object System (`Assets/Scripts/Level/HiddenObject.cs`)

Objects that can be revealed/hidden with animations:
- Reveal animation with easing (EaseOutCubic)
- Hide animation with easing (EaseInCubic)
- Optional collider enabling on reveal
- Visual and sound effects on reveal
- Scale animation during reveal/hide

**Events:**
- `OnObjectRevealed` - Fired when object is revealed
- `OnObjectHidden` - Fired when object is hidden

### Respawn System

**RespawnPoint** (`Assets/Scripts/Level/RespawnPoint.cs`)
- Defines spawn locations
- `isDefaultSpawn` flag marks initial spawn point
- GameManager discovers all RespawnPoints in scene on initialization

**Respawn Logic:**
- Triggered by DeathZone or Trap collision
- Always teleports to **nearest LEFT spawn point** (by X coordinate)
- Falls back to default spawn if no left spawn found
- Player state is reset (velocity zeroed, controls re-enabled)
- Respawn delay configurable in GameManager

**DeathZone** (`Assets/Scripts/Level/DeathZone.cs`)
- Trigger zones that kill player
- Calls `GameManager.Instance.PlayerNeedsRespawn()`

## Layer & Tag System

### Sorting Layers (in order)
1. `Background` - Background elements
2. `Filter` - Filter system objects
3. `Interaction` - Objects affected by Filter/Mask systems
4. `Mask` - Mask system objects
5. `UI` - UI elements

### Physics2D Layers
- `Default` (Layer 0)
- `HiddenObjects` (Layer 8) - Hidden objects
- `Player` (Layer 9) - Player character

### Tags
**Color Tags:**
- `Red`, `Blue`, `Green`, `Yellow`, `Purple` - Color-coded objects
- `Black` - Default/uncolored objects

**System Tags:**
- `Player` - Player character
- `HiddenObject` - Hidden objects
- `DeathZone` - Death trigger zones
- `Trap` - Trap hazards
- `RespawnPoint` - Spawn points
- `SkillBall` - Skill pickup objects (FilterSkillBall, MaskSkillBall)

## Constants & Enums

**File:** `Assets/Scripts/Utils/Constants.cs`

### FilterColor Enum
```csharp
public enum FilterColor { Red, Blue, Green, Yellow, Purple }
```

### SkillType Enum
```csharp
public enum SkillType { FilterSystem, MaskSystem }
```

### GameConstants Class
- Color tags: `TAG_RED_OBJECT`, `TAG_BLUE_OBJECT`, etc.
- Layer names: `LAYER_BACKGROUND`, `LAYER_FILTER`, `LAYER_INTERACTION`, `LAYER_MASK`
- Color values: `RED_COLOR`, `BLUE_COLOR`, etc.
- Helper methods: `GetColorTag()`, `GetColor()`

## Game Flow

### Initialization Sequence
1. GameManager.Awake() - Singleton setup
2. GameManager.InitializeGame() - Finds/spawns player, discovers respawn points
3. UIManager.Awake() - Singleton setup
4. UIManager.Start() - Generates color buttons, initializes UI
5. PlayerController.Awake() - Initializes movement, skills (deactivated)
6. CameraController.Awake() - Finds player target

### Gameplay Loop
1. PlayerController.Update() - Input handling, animation updates
2. PlayerController.FixedUpdate() - Movement physics
3. CameraController.LateUpdate() - Camera follow
4. Skill activation via right mouse button
5. Color selection via UI buttons

### Player Death Flow
1. Player enters DeathZone or Trap
2. DeathZone calls `GameManager.PlayerNeedsRespawn()`
3. GameManager sets `player.IsDead = true`
4. GameManager starts `RespawnPlayerProcess` coroutine
5. After respawnDelay, teleports player to nearest left spawn point
6. Resets player state and returns to Playing state

### Skill Activation Flow
1. Player collects SkillBall (FilterSkillBall or MaskSkillBall)
2. PlayerController.OnTriggerEnter2D() detects collision
3. PlayerController.UnlockSkill() sets skill unlock flag
4. Player presses R to switch between unlocked skills
5. Player presses right mouse button to activate/deactivate current skill
6. FilterSystem/MaskSystem OnEnable/OnDisable triggers object visibility changes

## Important Implementation Notes

### FilterSystem vs MaskSystem
- **FilterSystem:** Global effect, hides ALL matching objects when active, affects entire screen
- **MaskSystem:** Local effect, follows player, changes collision based on proximity to player

### Respawn Behavior
- Always teleports to nearest **LEFT** spawn point (X coordinate comparison)
- This is intentional design for level progression
- Falls back to default spawn if no left spawn found
- Player velocity is zeroed on respawn

### Skill Unlocking
- Skills start locked by default
- Unlocked via SkillBall pickups (FilterSkillBall, MaskSkillBall)
- Only unlocked skills can be switched to and activated
- Color can be changed independently of skill unlock state

### Physics Setup
- Player uses Rigidbody2D with gravity
- Ground detection uses `Collider2D.Cast`, NOT `Physics2D.OverlapCircle`
- Colliders must be properly configured:
  - `Is Trigger = true` for hazards (DeathZone, Trap)
  - `Is Trigger = false` for platforms and solid objects

### Animation System
- Uses Animator component with parameters:
  - `IsRunning` (bool) - Horizontal movement
  - `IsGrounded` (bool) - Ground contact
  - `VerticalVelocity` (float) - Y velocity
  - `canland` (bool) - Landing state
  - `canjump` (bool) - Jump state
- PlayerAnimationController is deprecated and not used

### UI Color System
- Color buttons are dynamically generated from `presetColors` array in UIManager
- Selected button scales up and changes sprite
- Color changes trigger `PlayerController.SetSkillColor()`
- Color selection is independent of skill activation

## Setup Guides

Two comprehensive setup guides are available in the root directory:

1. **HiddenObjectsSetupGuide.md** - Detailed guide for setting up hidden objects with FilterSystem and MaskSystem
2. **MaskSystem设置指南.md** - Detailed guide for configuring MaskSystem (in Chinese)

Refer to these guides when:
- Creating new hidden objects
- Configuring FilterSystem or MaskSystem
- Setting up layers and tags
- Troubleshooting visibility issues

## Project Structure

```
Mark-Itself/
├── Assets/
│   ├── Scripts/
│   │   ├── Managers/        # GameManager, UIManager
│   │   ├── Player/          # PlayerController
│   │   ├── Skills/          # FilterSystem, MaskSystem
│   │   ├── Level/           # HiddenObject, DeathZone, RespawnPoint
│   │   ├── Camera/          # CameraController
│   │   ├── UI/              # UI components
│   │   ├── VFX/             # FilterEffectManager2D
│   │   └── Utils/           # Constants, helpers
│   ├── Scenes/              # Game scenes
│   ├── Prefabs/             # Reusable GameObjects
│   ├── Shader/              # Custom shaders
│   ├── Sounds/              # Audio assets
│   ├── Arts/                # Visual assets
│   └── Datas/               # Game data files
├── ProjectSettings/         # Unity project settings
├── Packages/                # Unity packages
└── Library/                 # Unity generated files (ignored)
```

## Common Patterns

### Singleton Pattern
Used by GameManager, UIManager, and FilterEffectManager2D:
```csharp
public static GameManager Instance { get; private set; }
void Awake() {
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
}
```

### Event System
Action delegates for state changes:
```csharp
public static event Action<GameState> OnGameStateChanged;
public static event Action<SkillType> OnSkillChanged;
```

### Component-Based Architecture
Modular scripts attached to GameObjects, following Unity's component pattern.

### Coroutine-Based Async
Used for delays, animations, and effects:
```csharp
IEnumerator RespawnPlayerProcess() {
    yield return new WaitForSeconds(respawnDelay);
    // Respawn logic
}
```

## Version History

Based on git commits:
- **v0.1最终版** - Initial release
- **v1.0补丁2** - Patch 2
- **v1.0补丁** - Patch 1
- **v1.0** - Version 1.0
- **背景修改·终极版** - Background modifications

Current branch: `develop-v0.2`
Main branch: `main`
