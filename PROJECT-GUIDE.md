# R-Type Clone — Project Guide

## Quick Start

```bash
dotnet build    # Compile
dotnet run      # Build and run the game
```

**Controls:**
- **Keyboard:** WASD/arrows to move, Space/mouse to shoot (hold to charge)
- **Xbox controller:** Left stick to move, A button to shoot (hold to charge)
- **Escape:** Open pause menu (Resume / Inventory / Exit)
- **Controller Start/Menu:** Open Module Bay (loadout) directly
- **F3** (or both thumbsticks): Toggle debug overlay
- **F3 + A/Enter in Module Bay:** Debug module picker (equip any registered module)

## Architecture Overview

### Game Loop

`Program.cs` runs the main loop: `Update(dt)` → `BeginDrawing()` → `Draw()` → `EndDrawing()`. All game state changes happen in `Update()`. `Draw()` is read-only.

### Entity System

All game objects inherit from `Entity` (position, velocity, bounds, active flag).

| Entity       | File                      | Notes                                           |
|-------------|---------------------------|-------------------------------------------------|
| Player      | `Entities/Player.cs`      | Movement, module-driven shooting, i-frames      |
| Enemy       | `Entities/Enemy.cs`       | AI-driven, rarity tiers, affix modifiers        |
| Projectile  | `Entities/Projectile.cs`  | Module-parameterized bullets (pooled)           |
| DamageNumber| `Entities/DamageNumber.cs`| Floating damage text (pooled)                   |

### Object Pooling

All frequently created/destroyed entities use `ObjectPool<T>` (`Core/ObjectPool.cs`). Pools are pre-allocated at startup — no `new` in the game loop.

### Combat System

- `DamageEvent` (value struct) describes each hit: amount, type, bypass-shield flag.
- `DamageType` enum: `NonElemental`, `Energy`, `Fire`, `Cold`.
- `EnemyHealth` struct: base HP + shield layer with damage application logic.
- `CollisionSystem` builds `DamageEvent` from projectile stats and applies it.

### AI Profile System

Enemy movement is driven by JSON profiles in `Assets/ai_profiles/`. Each profile lists behaviour nodes executed per frame.

**Current handlers:** `straight`, `sine`, `zigzag`

**Key types:**
- `AiProfile` / `AiNodeConfig` — deserialized from JSON at startup
- `IBehaviourHandler` — interface for each node type
- `EnemyAiState` — per-enemy mutable state (struct, no allocation)
- `AiContext` — read-only world snapshot passed to handlers

### Enemy Rarity System

Enemies spawn with one of four rarity tiers: Normal (white), Magic (blue), Rare (gold), Unique (orange). Rarity is rolled at spawn time by `RarityRoller` using weighted random selection. Weights escalate per wave — later waves push toward higher tiers.

Magic and Rare enemies receive random affixes (speed boost, shields, armor, etc.) loaded from `Assets/affixes/*.json`. Unique enemies load fixed affix sets from `Assets/uniques/*.json`. Rarity drives score multipliers (×1 to ×10) and HP scaling.

**Key types:** `EnemyRarity` enum, `RarityConstants`, `AffixDefinition`, `AffixModifiers`, `AffixRegistry`, `RarityRoller`.

### Ship Module System

Player weapons are driven by a modular system. The player has 4 weapon slots, each with 2 linked support module slots (12 sockets total). Module definitions are loaded from `Assets/modules/*.json`.

Each shot-type weapon module defines both a **tap fire** (base parameters) and a **charge fire** (charged parameters) in the same definition. Tap shoots immediately; holding and releasing fires the charged version. The `ModulePipeline` resolves a weapon module + its support modules into cached `ProjectileParameters` for both modes. Resolution happens once on loadout change, not per frame.

**Default loadout:** Slot 0 = `shot_normal` ("Standard Blaster" — tap = fast small bullet, charge = slow large bullet).

**Available support modules:** Amplifier Coil (+dmg), Penetrator Lens (+pierce), Velocity Booster (+speed), Spread Emitter (+multishot), Tracking Beacon (homing).

**Key types:** `ProjectileParameters`, `ModuleDefinition`, `ModuleRegistry`, `ModulePipeline`, `PlayerLoadout`, `ModuleSystem`.

### Drop Table System

Enemies drop module gems on death. Drop tables are loaded from `Assets/drop_tables/*.json` at startup. Each table specifies a drop chance and weighted entries mapping to module IDs. Enemy rarity determines which table is used: Normal (10% chance), Magic (40%), Rare (100%), Unique (100%).

`DropSystem.Roll()` is called in `CollisionSystem` when an enemy dies. If the roll succeeds, a `DroppedGem` is spawned from the pool at the enemy's death position. Gems drift left with a sine-wave bob and despawn after 8 seconds. Players collect gems by overlapping them; collected gem IDs are stored in `GemInventory`.

**Key types:** `DropTable`, `DropTableEntry`, `DropTableRegistry`, `DropSystem`, `DroppedGem`, `GemInventory`.

### UI Scenes

The game supports three scenes: `Playing`, `PauseMenu`, and `Inventory`.

- **Pause menu** — Escape (keyboard) opens a dim overlay with Resume, Inventory, and Exit. D-pad/arrow navigation, A/Enter to select.
- **Module Bay** (loadout screen) — Controller Start opens directly. Shows 4 weapon slots in a 2×2 grid with module names, tap/charged stats, and support slot status. Cursor navigates both weapon and support sub-slots.
- **Debug module picker** — when F3 debug overlay is active, pressing A/Enter on any slot opens a filtered list of all registered modules. Allows equipping any module or clearing slots for testing.
- Game world renders behind menus but freezes while paused.

### Debug Overlay

Press **F3** to toggle. Shows:
- Green hitbox outlines on all entities
- Yellow AI profile label above each enemy
- Frame time (ms) and FPS in top-right corner
- Enables debug module picker in Module Bay

### Sprite Atlas System

`AssetManager.LoadAtlas(jsonPath)` parses Free Texture Packer format (`{ "frames": {...}, "meta": {...} }`). Use `GetSourceRect(frameName)` for frame lookup. No atlas content yet — geometric placeholders are used for all visuals.

## File Structure

```
Program.cs                          Entry point only
Core/
  Constants.cs                      All magic numbers
  GameState.cs                      Game loop orchestrator + scene management
  InputManager.cs                   Controller + keyboard abstraction
  ObjectPool.cs                     Generic object pool
  AssetManager.cs                   Texture loading + atlas system
  GemInventory.cs                   Collected module gem storage
Entities/
  Entity.cs                         Base class
  Player.cs                         Player ship
  Enemy.cs                          Enemy (AI-driven)
  Projectile.cs                     Bullets (pooled)
  EnemyHealth.cs                    HP + shield struct
  EnemyRarity.cs                    Rarity enum + constants
  DamageNumber.cs                   Floating damage text (pooled)
  DroppedGem.cs                     Module gem drop (pooled)
Systems/
  CollisionSystem.cs                Collision detection + rarity score
  ScrollingBackground.cs            Parallax background
  WaveSpawner.cs                    Enemy wave timing + rarity rolling
  DebugDraw.cs                      Debug overlay rendering
  CombatSystem/
    DamageType.cs                   Damage type enum
    DamageEvent.cs                  Damage event struct
  AiSystem/
    AiSystem.cs                     AI update loop
    AiProfile.cs                    Profile data model
    AiNodeConfig.cs                 Node configuration
    AiProfileRegistry.cs            JSON profile loader
    AiContext.cs                     World state snapshot
    EnemyAiState.cs                 Per-enemy AI state
    IBehaviourHandler.cs            Handler interface
    BehaviourRegistry.cs            Handler registry
    Handlers/
      StraightHandler.cs            Linear movement
      SineHandler.cs                Sine wave oscillation
      ZigzagHandler.cs              Vertical bounce pattern
  DropSystem/
    DropTable.cs                    Drop table data model
    DropTableRegistry.cs            JSON drop table loader
    DropSystem.cs                   Rarity-based drop rolling
  RaritySystem/
    AffixDefinition.cs              Affix data model
    AffixModifiers.cs               Combinable modifier struct
    AffixRegistry.cs                JSON affix loader
    RarityRoller.cs                 Weighted rarity + affix rolling
  ModuleSystem/
    ProjectileParameters.cs         Projectile stats struct
    ModuleModifiers.cs              Support module modifier struct
    ModuleDefinition.cs             Module data model + enums
    ModuleRegistry.cs               JSON module loader
    ModulePipeline.cs               Weapon + support → resolved params
    PlayerLoadout.cs                4×3 socket grid
    ModuleSystem.cs                 Registry + loadout + cache owner
  UI/
    PauseMenu.cs                    Pause overlay (Resume/Inventory/Exit)
    LoadoutScreen.cs                Module Bay (2×2 grid + debug picker)
Assets/
  ai_profiles/                      AI profile JSON files
  affixes/                          Enemy affix JSON files
  modules/                          Ship module JSON files
  drop_tables/                      Drop table JSON files
  uniques/                          Unique enemy preset JSON files
  M484BulletCollection1.png         Bullet sprite sheet
```

## Development Phases

Per spec-0007, the project follows four phases:

- **Phase 0 (complete):** EnemyHealth, AI profiles, debug overlay, atlas loader
- **Phase 1 (complete):** Enemy rarity + affixes, module data model, module-driven shooting, unified tap + charge
- **Phase 2 (current):** Pause menu, Module Bay, scene management, debug module picker, support modules. Next: drop tables, enemy shooting AI
- **Phase 3:** Level editor (ImGui), full AI handler set, content pass
