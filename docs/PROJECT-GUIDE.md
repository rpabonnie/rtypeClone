# R-Type Clone — Project Guide

**Audience:** Ray (game director + artist + co-developer)
**Written by:** Lead programmer perspective
**Updated:** 2026-03-27

This document is your orientation to the codebase. It assumes you understand C# and can read code; it focuses on the patterns and conventions that aren't obvious from reading any single file in isolation.

---

## 1. Project at a Glance

Native Windows 2D side-scrolling space shooter. C# + .NET 10 + Raylib-cs. No engine, no GUI editor — everything builds and runs from the terminal with `dotnet run`.

**Primary input:** Xbox controller (keyboard always works too).
**Target resolution:** 1920×1080.
**Art style:** Hand-drawn pen/pencil illustration (your Procreate work), not pixel art.

Quick commands:

```bash
dotnet build     # compile only
dotnet run       # compile and launch the game
dotnet clean     # wipe build artifacts
```

---

## 2. Folder Structure

```
rtypeClone/
  Program.cs                 # Entry point: window init, main loop, shutdown — stays thin
  Core/
    Constants.cs             # All magic numbers live here, nowhere else
    GameState.cs             # Central update/draw coordinator
    InputManager.cs          # Controller + keyboard abstraction
    ObjectPool.cs            # Generic object pool (bullets, enemies, particles)
    AssetManager.cs          # Texture loading + sprite atlas management
  Entities/
    Entity.cs                # Abstract base: Position, Velocity, Active, Bounds
    Player.cs                # Ship movement, shooting, charge mechanic, health, i-frames
    Enemy.cs                 # Movement patterns (Straight/Sine/Zigzag), health
    Projectile.cs            # Pooled bullets (normal + charged)
  Systems/
    CollisionSystem.cs       # AABB detection: bullets vs enemies, enemies vs player
    ScrollingBackground.cs   # Parallax layers
    WaveSpawner.cs           # Enemy wave timing and pattern selection
  Assets/
    sprites/                 # Packed atlases (.png + .json) — generated, not drawn here
  docs/
    specs/                   # Design specs (read before implementing a feature)
    adr/                     # Architecture Decision Records
```

---

## 3. Game Loop Architecture

The cardinal rule of this codebase:

```
Update(deltaTime)   ← ALL state changes happen here
BeginDrawing()
Draw()              ← ONLY reads state, never writes it
EndDrawing()
```

`Update()` is where everything happens: input, movement, spawning, collision, scoring, pooling.
`Draw()` is read-only. If you find yourself writing `if (something) entity.Active = false` inside a `Draw()` method, you've put logic in the wrong place.

Delta time comes from `Raylib.GetFrameTime()`. Never assume 60 FPS or any fixed frame rate. Always multiply by `dt` when moving things.

---

## 4. Object Pooling — The Most Important Pattern

We never use `new` for bullets, enemies, particles, or any frequently created object inside the game loop. Instead we use `ObjectPool<T>`:

```csharp
// Pre-allocate at startup (in GameState constructor)
var bulletPool = new ObjectPool<Projectile>(64, () => new Projectile());

// Acquire an inactive slot
Projectile p = bulletPool.Get();
p.Init(position, velocity, chargeLevel);

// Release when done (return to pool, doesn't free memory)
bulletPool.Return(index);

// Iterate only active objects — zero allocation
bulletPool.ForEachActive((p, i) => p.Update(dt));
```

If you're adding a new entity type that spawns repeatedly (damage numbers, dropped gems, explosion particles), it needs a pool. Add the pool to `GameState` and pre-allocate it in the constructor.

The pool uses an internal `bool[]` array to track active slots. `ForEachActive()` skips inactive slots automatically — no per-frame allocation, no LINQ.

---

## 5. Entity System

All game objects inherit from `Entity`:

```csharp
public abstract class Entity
{
    public Vector2 Position;
    public Vector2 Velocity;
    public int Width, Height;
    public bool Active;
    public Rectangle Bounds => new Rectangle(Position.X, Position.Y, Width, Height);

    public abstract void Update(float dt);
    public abstract void Draw();
    public bool IsOffScreen(float rightMargin = 0) { ... }
}
```

Each entity owns its own `Update()` and `Draw()`. `GameState` calls them via `ForEachActive()` — it doesn't care what kind of entity it's talking to.

**When adding a new entity type:**
1. Create a new file in `Entities/` (e.g., `Entities/Boss.cs`).
2. Inherit from `Entity`, implement `Update()` and `Draw()`.
3. Add a pool for it in `GameState`.
4. Call `ForEachActive()` on it in `GameState.Update()` and `GameState.Draw()`.
5. Add collision handling in `CollisionSystem.cs` if needed.

---

## 6. Adding a New Enemy Type

The current `Enemy` class has three movement patterns as an enum: `Straight`, `Sine`, `Zigzag`. These will eventually be replaced by the AI profile system (spec-0004), but for now:

1. Add a new `MovementPattern` enum value in `Enemy.cs`.
2. Add its movement logic inside `Enemy.Update()` in the pattern switch.
3. Wire the new pattern into `WaveSpawner.cs` so waves can spawn it.

For a fundamentally different enemy (a boss, a turret, a different ship type), create a new file in `Entities/`. Don't force it into the existing `Enemy` class if the behavior is sufficiently different.

---

## 7. Adding a New Projectile Type

`Projectile.cs` currently handles normal and charged bullets via a `chargeLevel` int. To add a new projectile type:

1. Add a new charge level or create a separate class if the behavior is distinct enough.
2. Add its sprite source rectangle in `AssetManager.cs`.
3. Define its dimensions and speed in `Constants.cs`.
4. Expand the pool size in `GameState` if needed.

---

## 8. Input — Always Both Controller and Keyboard

All input goes through `InputManager`. You never call `Raylib.IsKeyDown()` directly in entity code. The pattern:

```csharp
// In InputManager.Update():
Movement = /* read gamepad axis OR WASD, whichever was used more recently */;
ShootPressed = /* gamepad A OR Space, first frame only */;
ShootHeld = /* held down */;
ShootReleased = /* just released */;
```

If you add a new input action (e.g., dodge, open inventory), add it to `InputManager` with both controller and keyboard paths. Never add keyboard-only shortcuts.

---

## 9. Constants — No Magic Numbers

Every numeric value that appears more than once or isn't self-evident belongs in `Constants.cs`:

```csharp
public static class Constants
{
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;
    public const float PlayerSpeed = 400f;
    public const float BulletSpeed = 800f;
    // etc.
}
```

If you find yourself typing `1920` anywhere other than `Constants.cs`, stop and add a constant instead.

---

## 10. Spec-Driven Workflow

Every significant feature starts with a spec in `docs/specs/`. The workflow is:

1. **Read the spec** before touching any code. The spec defines data models, interfaces, and behavior — implementation should match it, not the other way around.
2. **Implement against the spec.** If the spec needs to change as you learn something, update the spec first, then code.
3. **Mark the spec as implemented** when done (update the `Status` field from `Draft` to `Implemented`).

Current specs and their purpose:

| Spec | What It Covers |
|------|---------------|
| spec-0001 | Gem Skill System — PoE-style gem slots driving weapon behavior |
| spec-0002 | Enemy Rarity System — Normal/Magic/Rare/Unique tiers with affixes |
| spec-0003 | Enemy HP System — Shield layers, damage types, floating damage numbers |
| spec-0004 | Enemy AI Profile System — JSON-driven behavior nodes replacing hardcoded patterns |
| spec-0005 | Drop Table System — Weighted gem drops on enemy death |
| spec-0006 | Ship Loadout Socket System — 4×3 grid UI for equipping gems |
| spec-0007 | Tooling & Development Pipeline — Art pipeline, data editing, level editor, phase plan |

---

## 11. Four-Phase Development Roadmap

This comes from spec-0007. The phases are ordered by dependency, not by what sounds most exciting.

### Phase 0 — Foundation (implement first)
Replace the current hardcoded health and movement with extensible systems. The game should play identically after this phase — it's all architecture.

- `EnemyHealth` struct with shield layers (spec-0003)
- `DamageEvent` / `DamageType` value types wired through `CollisionSystem`
- Floating `DamageNumber` pool (pooled entity)
- AI profile system core: port Straight/Sine/Zigzag to handler classes (spec-0004)
- `AiProfileRegistry` loading JSON profiles from `Assets/ai_profiles/`
- Sprite atlas loader: `AssetManager.LoadAtlas()` + `GetSourceRect(string frameName)` (spec-0007)
- Basic debug overlay: hitboxes + AI label + frame time

### Phase 1 — Rarity + Gem Data Model
Enemies have visual rarity. Gem data model drives shooting behavior.

- Enemy rarity enum + color visual (spec-0002)
- Affix definitions + rarity roller (spec-0002)
- Score multipliers by rarity (spec-0002)
- `GemDefinition` + `GemModifierPipeline` (spec-0001)
- Default gem loadout wired into Player shooting
- **Art:** Start player ship and enemy A in Procreate

### Phase 2 — Drops + Progression Loop
The full gameplay loop: kill enemies, collect gems, upgrade loadout.

- Drop table registry + `DropSystem.Roll()` on enemy death (spec-0005)
- `DroppedGem` pooled entity (spec-0005)
- `GemInventory` on Player (spec-0005)
- `LoadoutScreen` with socket grid + controller navigation (spec-0006)
- Loadout persistence to `loadout.json` (spec-0006)
- Expand AI with charge and retreat handlers (spec-0004)
- Enemy projectile pool + `shoot_at_player` handler (spec-0004)
- Support gems runtime: pierce, homing, damage (spec-0001)
- **Art:** Replace placeholder rectangles with Procreate sprites

### Phase 3 — Level Editor + Content
Authoring tools and content volume.

- ImGui.NET integration (Raylib-ImGui bridge)
- Wave editor: add/remove/reorder spawns, save to `Assets/levels/level_n.json`
- Background editor: layer textures + scroll speeds
- Full AI debug overlay (all visualizations)
- Formation and shield_ally AI handlers (spec-0004)
- Remaining support gems: chain, radius, multishot (spec-0001)
- VS Code JSON schemas for all data files (spec-0007)
- Content pass: 3+ Unique enemies, 5+ wave patterns, full art set

---

## 12. Art Pipeline — Procreate → Game

You draw on iPad in Procreate. Here's how your art gets into the game:

```
Procreate (iPad)
  → Export each frame as PNG with transparency
  → Transfer PNGs to PC (AirDrop, iCloud Drive, etc.)
  → Drop into Assets/sprites/source/<entity>/
     e.g., Assets/sprites/source/player/player_idle_00.png

Free Texture Packer (PC, GUI tool)
  → Load the source folder
  → Pack into atlas:
     Assets/sprites/player.png     ← packed texture
     Assets/sprites/player.json    ← frame metadata (x, y, w, h per frame name)

AssetManager.cs
  → Raylib.LoadTexture("Assets/sprites/player.png")
  → Parse player.json for source rectangles
  → GetSourceRect("player_idle_00") → Rectangle
```

**Export naming convention from Procreate:**
Name each layer/frame with a consistent prefix and zero-padded index:
`player_idle_00`, `player_idle_01`, `enemy_a_00`, `enemy_a_01`, etc.

This name becomes the key in the atlas JSON and what you pass to `GetSourceRect()` in code.

**Draw at 2× or 4× target size** in Procreate, then let Free Texture Packer scale/pack. This gives you clean lines when displayed at 1920×1080.

---

## 13. Data-Driven Systems — Editing JSON Files

Once Phase 0 is in, game data lives in JSON files under `Assets/`. You'll edit these directly in VS Code — no custom tool needed.

```
Assets/
  gems/
    shot_normal.json
    shot_charged.json
  affixes/
    fast.json
    shield.json
  ai_profiles/
    straight.json
    sine_wave.json
  drop_tables/
    normal_enemy.json
  uniques/
    the_guardian.json
```

After Phase 3, VS Code will provide autocomplete and inline validation via JSON schemas defined in `.schemas/`. Until then, refer to the spec data model sections for the correct field names and types.

---

## 14. Git Workflow

**Branch naming:** `claude/<adjective>-<noun>` for AI-assisted branches. For your own work, use descriptive names like `feature/boss-enemy` or `fix/player-wall-clip`.

**Main branch is protected** — never push directly to `main`. Open a PR.

**Commit messages:** Lead with the verb. Examples:
- `Add EnemyHealth struct and DamageEvent types`
- `Fix zigzag pattern going off-screen`
- `Update player sprite to Procreate art`

**Before committing new JSON files:** Make sure they're valid JSON (VS Code will underline errors). Broken JSON files will crash the game at startup when the registry tries to load them.

---

## 15. Debugging Tips

**F3 (or both thumbsticks):** Toggles the debug overlay (once implemented in Phase 0). Shows hitboxes, AI state, frame time, drop rolls.

**`Constants.cs` for quick tuning:** Enemy speed too high? Player charge time too short? Change `Constants.cs` and rerun — no other files need touching.

**Pool exhaustion:** If enemies or bullets suddenly stop appearing, a pool is full. Check the pool sizes in `Constants.cs` (`EnemyPoolSize`, `ProjectilePoolSize`) and increase them.

**Performance check:** If the game feels choppy, check `dotnet run` output for GC warnings, or enable the frame time display in the debug overlay. The no-allocation-in-hot-loop rule is strict — any `new` inside `Update()` or `Draw()` is a bug.

**Collision feels wrong:** All collision is AABB (axis-aligned bounding boxes). `Entity.Bounds` uses `Position.X/Y` as top-left corner. If a sprite is drawn offset from its logical position, the hitbox and sprite will appear misaligned — make sure `Position` is the visual top-left of the sprite.

---

## 16. Performance Rules (Non-Negotiable)

Inside `Update()` and `Draw()`:

- No `new` for classes.
- No `List<>` creation (reuse pre-allocated collections).
- No string concatenation with `+` (cache formatted strings, update only when value changes).
- No LINQ in hot paths (`ForEachActive()` instead of `.Where().Select()`).
- No `new Vector2()` if you can update an existing field in place.

These rules exist because the GC pausing during gameplay causes visible stutters. One allocation per frame is 3600+ allocations per minute — it adds up fast.

---

## 17. What Not to Do

- Do not put logic in `Draw()`.
- Do not write game code in `Program.cs` beyond the main loop scaffold.
- Do not add input handling that only works on keyboard (controller must always work).
- Do not scatter numeric literals through entity files (add them to `Constants.cs`).
- Do not start implementing a feature before reading its spec — the spec defines the interface, and getting that wrong means rework.
- Do not add error handling for code paths that can't happen in normal gameplay. Keep the hot path clean.
