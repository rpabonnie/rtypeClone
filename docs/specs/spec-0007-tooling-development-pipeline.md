# Spec: Tooling & Development Pipeline

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0007                        |
| Status   | Draft                            |
| Author   | rpabo                            |
| Created  | 2026-03-25                       |
| Updated  | 2026-03-27                       |

## Overview

This is the most important spec. It defines the end-to-end development pipeline: framework confirmation, art tools, data editing workflow, level editor plan, AI debug overlay, and a phased build order. Nothing here requires changing the Raylib-cs framework. The pipeline is 100% CLI-compatible and editor-agnostic.

---

## Framework Confirmation: Stay with Raylib-cs

Raylib-cs remains the correct choice. See `docs/research-0001-game-framework-selection.md` for full rationale. The short version:

- `dotnet add package Raylib-cs` + `dotnet run` = complete workflow.
- No GUI editor dependency at any stage.
- XInput wrapping is a single function call.
- OpenGL backend handles our rendering requirements (sprites, particles, hundreds of bullets) with headroom to spare.

No migration to Unity, Godot, MonoGame, or any other framework is warranted. The complexity specs 0001–0006 introduce is data-model complexity (JSON files, C# types), not framework complexity.

---

## Art Pipeline

### Philosophy

The game targets hand-drawn pen/pencil art — digitized on iPad with Procreate, not pixel art. Sprites should look like illustrations rather than retro sprites. Target resolution is 1920×1080 with no upscaling filter.

### Tools

#### Procreate (primary drawing tool — iPad)

- **Why Procreate:** The artist (Ray) draws on iPad using Procreate. Apple Pencil pressure sensitivity, natural pen/pencil brushes, and layer support make it ideal for the hand-drawn illustration style this game targets.
- **Workflow:**
  1. Sketch and ink in Procreate at 2× or 4× target sprite size (e.g., draw player ship at 192×128 px if final is 96×64 px).
  2. Name layers consistently with a prefix and zero-padded index: `player_idle_00`, `player_idle_01`, etc.
  3. Export each sprite frame as a PNG with transparency (File → Share → PNG). For multi-frame sprites, export each layer/group separately.
  4. Transfer PNGs to PC (AirDrop, iCloud Drive, or USB).
  5. Combine frames into a sprite atlas (see below).

#### Free Texture Packer (atlas packing)

- **Why Free Texture Packer:** Free, GUI-based, CLI-exportable. Takes a folder of PNGs and outputs a packed atlas PNG + JSON metadata (compatible with `AssetManager`).
- **Workflow:**
  1. Drop all PNGs for a sprite into a folder: `Assets/sprites/source/player/`.
  2. Run Free Texture Packer (or its CLI) to generate:
     - `Assets/sprites/player.png` — packed atlas.
     - `Assets/sprites/player.json` — frame metadata (x, y, w, h per frame name).
  3. `AssetManager` loads the atlas PNG with `Raylib.LoadTexture()` and parses the JSON for source rectangles.

#### Atlas JSON Format (output from Free Texture Packer)

```json
{
  "frames": {
    "player_idle_00": { "x": 0,   "y": 0,  "w": 96, "h": 64 },
    "player_idle_01": { "x": 96,  "y": 0,  "w": 96, "h": 64 },
    "enemy_a_00":     { "x": 0,   "y": 64, "w": 40, "h": 32 }
  },
  "meta": {
    "image": "sprites.png",
    "size": { "w": 512, "h": 512 }
  }
}
```

`AssetManager` will load this format and provide `GetSourceRect(string frameName) → Rectangle`.

#### Aseprite (optional, for frame animation editing on PC)

If frame-by-frame animation editing becomes important on the PC side, Aseprite is the best tool. It exports PNG sheets directly and supports `.aseprite` project files. Not required for Phase 0–1. Drawing still happens in Procreate; Aseprite would be used for post-processing or tweening if needed.

---

## Data Editing

All game data (gems, affixes, drop tables, AI profiles, enemy presets) lives in JSON files under `Assets/`. The editing workflow is:

### VS Code + JSON Schemas

1. Each JSON file type has a `$schema` field pointing to its schema (defined in specs 0001–0005).
2. Add schemas to `.vscode/settings.json`:

```json
{
  "json.schemas": [
    {
      "fileMatch": ["Assets/gems/*.json"],
      "url": "./.schemas/gem-definition.schema.json"
    },
    {
      "fileMatch": ["Assets/affixes/*.json"],
      "url": "./.schemas/affix-definition.schema.json"
    },
    {
      "fileMatch": ["Assets/drop_tables/*.json"],
      "url": "./.schemas/drop-table.schema.json"
    },
    {
      "fileMatch": ["Assets/ai_profiles/*.json"],
      "url": "./.schemas/ai-profile.schema.json"
    },
    {
      "fileMatch": ["Assets/uniques/*.json"],
      "url": "./.schemas/unique-enemy.schema.json"
    }
  ]
}
```

3. VS Code provides autocomplete, inline validation, and hover documentation while editing any data file — no custom editor needed.

### Schema Files Location

```
.schemas/
  gem-definition.schema.json
  affix-definition.schema.json
  drop-table.schema.json
  ai-profile.schema.json
  unique-enemy.schema.json
```

Schemas are extracted directly from the data model sections of specs 0001–0005.

---

## Level Editor Plan (Phase 3)

Build a custom in-game level editor using **ImGui.NET** inside Raylib-cs. This is Phase 3 work — do not start until Phase 2 is complete.

### Why ImGui.NET

- ImGui.NET is a NuGet package (`dotnet add package ImGui.NET`).
- It renders via Raylib's OpenGL backend using a custom Raylib–ImGui bridge (one file, well-known pattern).
- No separate editor application — the editor runs inside the game window, toggled by a hotkey.
- All ImGui state is immediate-mode; no scene graph or serialization needed for the editor UI itself.

### Level Editor MVP Features

1. **Wave editor:** Add/remove/reorder enemy spawns in a wave. Set spawn time, position, AI profile ID, rarity weights.
2. **Background editor:** Set parallax layer textures and scroll speeds.
3. **Save/load:** Output wave definitions to `Assets/levels/level_<n>.json`.

### Level JSON Format (target)

```json
{
  "id": "level_01",
  "backgroundLayers": [
    { "texture": "bg_stars.png", "scrollSpeed": 0.2 },
    { "texture": "bg_nebula.png", "scrollSpeed": 0.5 }
  ],
  "waves": [
    {
      "startTime": 0.0,
      "spawns": [
        { "time": 0.5, "aiProfileId": "straight", "x": 1980, "y": 400, "rarityWeights": [80, 15, 5, 0] },
        { "time": 1.0, "aiProfileId": "sine_wave", "x": 1980, "y": 600, "rarityWeights": [80, 15, 5, 0] }
      ]
    }
  ]
}
```

---

## AI Debug Overlay

A built-in debug mode toggled by pressing F3 (keyboard) or pressing both thumbsticks (controller). When active:

### Visualizations

| Element                   | Display                                              |
|---------------------------|------------------------------------------------------|
| Enemy AI profile ID       | Text label above each enemy                          |
| Active behaviour node     | Highlighted text (current node type)                 |
| Enemy velocity vector     | Arrow from enemy center                              |
| AI profile trigger zones  | Colored circles (charge radius, shield radius, etc.) |
| Collision hitboxes        | Outlines on all entities                             |
| Drop table roll           | Brief text pop on enemy death (`"Rolled: support_pierce"`) |
| Current rarity            | Enemy name + rarity color label                      |
| Frame time                | Top-right corner: frame ms, FPS                      |

### Implementation

`GameState` holds `bool DebugOverlay`. All debug drawing is gated on this flag and lives in a `DebugDraw.cs` utility class. Debug draw calls are **inside** `Draw()` (read-only, no state mutation) but outside the normal render pass — they never run in release builds unless explicitly enabled.

```csharp
// Systems/DebugDraw.cs
public static class DebugDraw
{
    public static bool Enabled = false;

    public static void DrawEnemyAiInfo(Enemy enemy, AiSystem ai) { /* if Enabled */ }
    public static void DrawHitboxes(Entity entity) { /* if Enabled */ }
    public static void DrawFrameTime(float frameMs) { /* if Enabled */ }
}
```

---

## Four-Phase Development Order

### Phase 0 — Foundation (implement first)

**Goal:** Replace hard-coded health and movement with the extensible systems. No new content, just a better architecture.

| Task                        | Spec      | Notes                                           |
|-----------------------------|-----------|--------------------------------------------------|
| EnemyHealth struct          | 0003      | Replace `int Health` on Enemy                   |
| DamageEvent / DamageType    | 0003      | Wire through CollisionSystem                    |
| Floating DamageNumbers pool | 0003      | New pooled entity, add to GameState             |
| AI profile system (core)   | 0004      | Port Straight, Sine, Zigzag to handler classes  |
| AiProfileRegistry + JSON   | 0004      | Load straight.json, sine_wave.json, zigzag.json |
| AiContext                   | 0004      | Wire PlayerPosition into WaveSpawner → AiSystem |
| Sprite atlas loader         | 0007      | `AssetManager.LoadAtlas()` + frame rect lookup  |
| AI debug overlay (basic)    | 0007      | Hitboxes + AI label + frame time                |

**Exit criteria:** Game plays identically to today but uses EnemyHealth, AiSystem, and atlas loader.

---

### Phase 1 — Rarity + Gem Data Model

**Goal:** Enemies have rarity and affixes. Gem data model exists and drives the normal/charged shot (no loadout screen yet).

| Task                        | Spec      | Notes                                           |
|-----------------------------|-----------|--------------------------------------------------|
| EnemyRarity enum + colors   | 0002      | Visual color change on enemies by rarity        |
| AffixDefinition + Registry  | 0002      | Load affix JSONs                                |
| RarityRoller                | 0002      | Wire into WaveSpawner.Spawn()                   |
| Score multipliers           | 0002      | CollisionSystem reads Rarity on kill            |
| Unique enemy preset loading | 0002      | Load the_guardian.json as a demo Unique         |
| GemDefinition + Registry    | 0001      | Load shot_normal.json, shot_charged.json        |
| GemModifierPipeline         | 0001      | Wire into Player shooting (replaces chargeLevel)|
| GemSystem + default loadout | 0001      | Hard-code slot 0 = shot_normal, slot 1 = shot_charged |
| Procreate art kickoff       | 0007      | Start player ship sketch, enemy A sketch in Procreate |

**Exit criteria:** Enemies spawn with rarity colors, gem pipeline drives shooting, first Procreate sprites in progress.

---

### Phase 2 — Drops + Gem Runtime + Loadout Screen

**Goal:** The complete progression loop: enemies drop gems, player collects inventory, loadout screen lets them build a loadout.

| Task                        | Spec      | Notes                                           |
|-----------------------------|-----------|--------------------------------------------------|
| DropTableRegistry           | 0005      | Load drop_tables/*.json                         |
| DropSystem.Roll()           | 0005      | Wire into enemy death in CollisionSystem        |
| DroppedGem entity + pool    | 0005      | Floating gem pickup entity                      |
| GemInventory on Player      | 0005      | Collect DroppedGems at wave end                 |
| LoadoutScreen               | 0006      | Socket grid + inventory panel + controller input|
| LoadoutPersistence          | 0006      | Save/load loadout.json                          |
| Add charge, retreat handlers| 0004      | Expand AI with 2 new node types                 |
| shoot_at_player handler     | 0004      | Enemy projectile pool + handler                 |
| Support gems runtime        | 0001      | pierce, homing, damage supports wired up        |
| Sprite atlas integration    | 0007      | Replace placeholder rectangles with Procreate art |

**Exit criteria:** Player can clear a wave, collect gems, open the loadout screen, swap gems, and see stat changes take effect in the next wave.

---

### Phase 3 — Level Editor + AI Debug + Content Pass

**Goal:** Authoring tools and content volume increase.

| Task                        | Spec      | Notes                                           |
|-----------------------------|-----------|--------------------------------------------------|
| ImGui.NET integration       | 0007      | Raylib-ImGui bridge file                        |
| Wave editor (ImGui)         | 0007      | Edit spawns, save level JSON                    |
| Background editor (ImGui)   | 0007      | Layer textures + scroll speeds                  |
| AI debug overlay (full)     | 0007      | All visualizations from debug overlay section   |
| formation handler           | 0004      | Formation leader + follower nodes               |
| shield_ally handler         | 0004      | Shield ally AI node                             |
| Remaining support gems      | 0001      | chain, radius, multishot                        |
| Content pass                | all       | 3+ Unique enemies, 5+ wave patterns, full art set|
| VS Code JSON schemas        | 0007      | .schemas/ folder, .vscode/settings.json         |

**Exit criteria:** Game has level 1 fully authored, all 8 AI node types implemented, level editor functional.

---

## Dependency Graph

```
Phase 0
  EnemyHealth (0003)
  AiSystem core (0004)
  Atlas loader (0007)
        ↓
Phase 1
  EnemyRarity (0002) ← depends on EnemyHealth
  GemSystem data (0001)
        ↓
Phase 2
  DropSystem (0005) ← depends on GemSystem, EnemyRarity
  LoadoutScreen (0006) ← depends on GemSystem, GemInventory
        ↓
Phase 3
  Level editor (0007) ← depends on AiSystem profiles (0004)
  Content pass ← depends on all systems
```

---

## What NOT to Change

- **Do not switch frameworks.** Raylib-cs handles everything in this spec list.
- **Do not add a GUI editor dependency.** All tooling (Procreate, Free Texture Packer, VS Code, ImGui) is additive and optional. The game builds and runs with `dotnet run` at every phase.
- **Do not break the Update/Draw separation.** ImGui and debug overlays draw in `Draw()` but must never mutate game state.
- **Do not allocate in the hot loop.** Every new system introduced in Phases 0–3 must pre-allocate its pools at startup.

---

## File Locations

```
.schemas/
  gem-definition.schema.json
  affix-definition.schema.json
  drop-table.schema.json
  ai-profile.schema.json
  unique-enemy.schema.json
.vscode/
  settings.json
Systems/
  DebugDraw.cs
Assets/
  sprites/
    source/          ← Procreate exports (.png per frame, not committed, gitignored)
    player.png       ← packed atlas
    player.json      ← frame metadata
    enemies.png
    enemies.json
  levels/
    level_01.json
```

---

## Open Questions

1. Should Procreate source files (`.procreate`) be committed to the repo (large binary) or kept local on iPad with a documented export process?
2. Which ImGui-Raylib bridge implementation should we use — write our own or adopt an existing community one?
3. Should the level editor be a separate `dotnet run --project LevelEditor` project or toggled inside the main game binary?
