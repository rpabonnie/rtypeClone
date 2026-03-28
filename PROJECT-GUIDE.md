# R-Type Clone — Project Guide

## Quick Start

```bash
dotnet build    # Compile
dotnet run      # Build and run the game
```

**Controls:**
- **Keyboard:** WASD/arrows to move, Space/mouse to shoot (hold to charge)
- **Xbox controller:** Left stick to move, A button to shoot (hold to charge)
- **F3** (or both thumbsticks): Toggle debug overlay

## Architecture Overview

### Game Loop

`Program.cs` runs the main loop: `Update(dt)` → `BeginDrawing()` → `Draw()` → `EndDrawing()`. All game state changes happen in `Update()`. `Draw()` is read-only.

### Entity System

All game objects inherit from `Entity` (position, velocity, bounds, active flag).

| Entity       | File                      | Notes                                           |
|-------------|---------------------------|-------------------------------------------------|
| Player      | `Entities/Player.cs`      | Movement, gem-driven shooting, i-frames         |
| Enemy       | `Entities/Enemy.cs`       | AI-driven, rarity tiers, affix modifiers        |
| Projectile  | `Entities/Projectile.cs`  | Gem-parameterized bullets (pooled)              |
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

### Gem Skill System

Player shooting is driven by a PoE-style gem system. The player has 4 skill slots, each with 2 linked support gem slots (12 sockets total). Gem definitions are loaded from `Assets/gems/*.json`.

The `GemModifierPipeline` resolves a skill gem + its support gems into a cached `ProjectileParameters` struct. This resolution happens once on loadout change, not per frame. `Player.FireGemBullet()` reads the cached parameters to spawn projectiles.

**Default loadout:** Slot 0 = `shot_normal` (tap fire), Slot 1 = `shot_charged` (charge-release).

**Key types:** `ProjectileParameters`, `GemDefinition`, `GemRegistry`, `GemModifierPipeline`, `PlayerLoadout`, `GemSystem`.

### Debug Overlay

Press **F3** to toggle. Shows:
- Green hitbox outlines on all entities
- Yellow AI profile label above each enemy
- Frame time (ms) and FPS in top-right corner

### Sprite Atlas System

`AssetManager.LoadAtlas(jsonPath)` parses Free Texture Packer format (`{ "frames": {...}, "meta": {...} }`). Use `GetSourceRect(frameName)` for frame lookup. No atlas content yet — geometric placeholders are used for all visuals.

## File Structure

```
Program.cs                          Entry point only
Core/
  Constants.cs                      All magic numbers
  GameState.cs                      Game loop orchestrator
  InputManager.cs                   Controller + keyboard abstraction
  ObjectPool.cs                     Generic object pool
  AssetManager.cs                   Texture loading + atlas system
Entities/
  Entity.cs                         Base class
  Player.cs                         Player ship
  Enemy.cs                          Enemy (AI-driven)
  Projectile.cs                     Bullets (pooled)
  EnemyHealth.cs                    HP + shield struct
  DamageNumber.cs                   Floating damage text (pooled)
Entities/
  EnemyRarity.cs                    Rarity enum + constants
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
  RaritySystem/
    AffixDefinition.cs              Affix data model
    AffixModifiers.cs               Combinable modifier struct
    AffixRegistry.cs                JSON affix loader
    RarityRoller.cs                 Weighted rarity + affix rolling
  GemSystem/
    ProjectileParameters.cs         Projectile stats struct
    GemModifiers.cs                 Support gem modifier struct
    GemDefinition.cs                Gem data model + enums
    GemRegistry.cs                  JSON gem loader
    GemModifierPipeline.cs          Skill + support → resolved params
    PlayerLoadout.cs                4×3 socket grid
    GemSystem.cs                    Registry + loadout + cache owner
Assets/
  ai_profiles/                      AI profile JSON files
  affixes/                          Enemy affix JSON files
  gems/                             Gem definition JSON files
  uniques/                          Unique enemy preset JSON files
  M484BulletCollection1.png         Bullet sprite sheet
```

## Development Phases

Per spec-0007, the project follows four phases:

- **Phase 0 (complete):** EnemyHealth, AI profiles, debug overlay, atlas loader
- **Phase 1 (current):** Enemy rarity + affixes, gem data model, gem-driven shooting
- **Phase 2:** Drop tables, gem inventory, loadout screen, enemy shooting AI
- **Phase 3:** Level editor (ImGui), full AI handler set, content pass
