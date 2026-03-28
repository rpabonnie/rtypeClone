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

| Entity       | File                     | Notes                                        |
|-------------|--------------------------|----------------------------------------------|
| Player      | `Entities/Player.cs`     | Movement, charge shot, i-frames              |
| Enemy       | `Entities/Enemy.cs`      | AI-driven movement, EnemyHealth struct       |
| Projectile  | `Entities/Projectile.cs` | Normal and charged bullets (pooled)          |
| DamageNumber| `Entities/DamageNumber.cs`| Floating damage text (pooled)               |

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
Systems/
  CollisionSystem.cs                Collision detection + damage
  ScrollingBackground.cs            Parallax background
  WaveSpawner.cs                    Enemy wave timing
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
Assets/
  ai_profiles/                      AI profile JSON files
  M484BulletCollection1.png         Bullet sprite sheet
```

## Development Phases

Per spec-0007, the project follows four phases:

- **Phase 0 (current):** EnemyHealth, AI profiles, debug overlay, atlas loader
- **Phase 1:** Enemy rarity + affixes, gem data model, gem-driven shooting
- **Phase 2:** Drop tables, gem inventory, loadout screen, enemy shooting AI
- **Phase 3:** Level editor (ImGui), full AI handler set, content pass
