# Spec: Ship Loadout Socket System

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0006                        |
| Status   | Draft                            |
| Author   | rpabo                            |
| Created  | 2026-03-25                       |
| Updated  | 2026-03-25                       |

## Overview

Between-level loadout screen where the player manages gems on their ship. The screen shows a 4×3 socket grid (4 skill slots, each with 1 skill gem socket + 2 support gem sockets = 12 sockets total) alongside a scrollable inventory panel. The player selects a socket, picks a gem from inventory, and swaps it in. Incompatible gems show an error state. Tooltips show the resolved `ProjectileParameters` for the active skill + support combination. The screen supports both Xbox controller and keyboard.

## Goals

- Full-screen between-level UI rendered in Raylib (no external GUI lib).
- 4×3 socket grid navigation via d-pad / arrow keys.
- Inventory panel: scrollable list of collected gem IDs.
- Direct gem swaps: selecting a socket + gem swaps them.
- Incompatibility feedback displayed inline.
- Resolved stats tooltip: shows final damage, speed, pierce, etc. for the selected skill slot.
- Save/load loadout to JSON so it persists across sessions.
- Returns to `GameState` on confirm (controller Start / keyboard Enter).

## Non-Goals

- In-level gem swapping.
- Drag-and-drop with mouse (controller + keyboard only in this spec; mouse may be added later).
- Gem crafting UI.

---

## Screen Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  LOADOUT                                              [START] Go │
│                                                                  │
│  ┌──────────────────────────────┐  ┌───────────────────────┐    │
│  │  SHIP SOCKETS                │  │  INVENTORY            │    │
│  │                              │  │                       │    │
│  │  [Skill 1] [Sup 1] [Sup 2]   │  │  > shot_normal        │    │
│  │  [Skill 2] [Sup 1] [Sup 2]   │  │    support_damage     │    │
│  │  [Skill 3] [Sup 1] [Sup 2]   │  │    support_pierce     │    │
│  │  [Skill 4] [Sup 1] [Sup 2]   │  │    shot_spread        │    │
│  │                              │  │    ...                │    │
│  │  ┌───────────────────────┐   │  └───────────────────────┘    │
│  │  │ STATS (selected slot) │   │                               │
│  │  │ Dmg: 4  Speed: 800    │   │  [Y] Swap  [B] Cancel        │
│  │  │ Pierce: 2  Homing: ✓  │   │                               │
│  │  └───────────────────────┘   │                               │
│  └──────────────────────────────┘                               │
└─────────────────────────────────────────────────────────────────┘
```

Navigation has two focus zones: **Socket Grid** and **Inventory Panel**. The player switches between them with LB/RB (controller) or Tab (keyboard).

---

## Navigation Model

### Socket Grid

The grid is indexed `(skillSlot: 0..3, column: 0..2)`:
- column 0 = skill gem socket
- column 1 = support gem socket A
- column 2 = support gem socket B

D-pad / arrow keys move the cursor within the grid. Left from column 0 moves focus to the Inventory Panel.

### Inventory Panel

A flat list of gem IDs. Up/Down scrolls. Right moves focus back to the Socket Grid.

### Interaction

1. Player navigates to a socket in the grid.
2. Player presses A (controller) / Enter (keyboard) to "pick up" the gem in that socket (or select an empty socket as the target).
3. Focus shifts to Inventory Panel with the cursor on the gem that was in the socket (or top of inventory if socket was empty).
4. Player selects a gem from inventory and presses A / Enter to swap.
5. If the gem is compatible: swap occurs, `GemSystem.RebuildCache()` is called, stats tooltip updates.
6. If incompatible: socket flashes red for 0.5s, no swap, focus returns to grid.
7. Player presses B (controller) / Escape (keyboard) at any point to cancel the pending action.
8. Player presses Start (controller) / Enter (keyboard) from the grid (no pending action) to confirm and return to game.

---

## C# Types

### LoadoutScreenState

```csharp
// Core/LoadoutScreen.cs
public enum LoadoutFocus { SocketGrid, Inventory }

public class LoadoutScreen
{
    private readonly GemSystem   _gemSystem;
    private readonly InputManager _input;

    // Grid cursor
    private int _cursorSkill;    // 0..3
    private int _cursorColumn;   // 0..2

    // Inventory cursor
    private int _inventoryCursor;
    private int _inventoryScroll;  // top-visible index

    private LoadoutFocus _focus;

    // Pending swap state
    private bool    _pickupPending;
    private int     _pickupSkill;
    private int     _pickupColumn;
    private string? _pickupGemId;    // gem being moved (null = empty socket)

    // Feedback
    private float   _incompatTimer;  // > 0 = showing red flash

    public bool IsComplete { get; private set; }  // true when player confirms

    public LoadoutScreen(GemSystem gemSystem, InputManager input) { /* init */ }

    public void Update(float dt) { /* handle input, update timers */ }
    public void Draw() { /* render all elements */ }
}
```

### Socket Rendering

Each socket cell is drawn as a rectangle. Color depends on state:

| State               | Color           |
|---------------------|-----------------|
| Empty               | `Color.DarkGray` |
| Filled (skill gem)  | `Color.Blue`     |
| Filled (support gem)| `Color.DarkBlue` |
| Cursor              | `Color.Yellow` border |
| Incompatible flash  | `Color.Red` border, pulsing |
| Pending pickup      | `Color.Orange` border |

Gem display name is drawn inside the socket cell (truncated to fit).

### Stats Tooltip

When cursor is on a skill slot (column 0), or any column of a skill slot, the stats panel shows the resolved `ProjectileParameters` for that skill slot. Drawn as a fixed panel below the grid.

```csharp
private void DrawStatsPanel(int skillSlot)
{
    ref readonly var p = ref _gemSystem.GetActive(skillSlot);
    // Draw damage, speed, pierce, homing, count
}
```

Stats update live as gems are swapped (after `RebuildCache()`).

---

## Save / Load

The loadout is persisted as JSON to `%APPDATA%\rtypeClone\loadout.json`.

### Loadout Save Format

```json
{
  "version": 1,
  "slots": [
    { "skillGemId": "shot_normal",   "supportGemIds": ["support_damage", null] },
    { "skillGemId": "shot_charged",  "supportGemIds": [null, null] },
    { "skillGemId": null,            "supportGemIds": [null, null] },
    { "skillGemId": null,            "supportGemIds": [null, null] }
  ],
  "inventory": [
    "support_pierce",
    "shot_spread"
  ]
}
```

### JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "LoadoutSave",
  "type": "object",
  "required": ["version", "slots", "inventory"],
  "properties": {
    "version":   { "type": "integer" },
    "slots": {
      "type": "array",
      "minItems": 4,
      "maxItems": 4,
      "items": {
        "type": "object",
        "properties": {
          "skillGemId":    { "type": ["string", "null"] },
          "supportGemIds": {
            "type": "array",
            "minItems": 2,
            "maxItems": 2,
            "items": { "type": ["string", "null"] }
          }
        }
      }
    },
    "inventory": {
      "type": "array",
      "items": { "type": "string" }
    }
  }
}
```

### LoadoutPersistence

```csharp
// Core/LoadoutPersistence.cs
public static class LoadoutPersistence
{
    private static readonly string SavePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "rtypeClone",
            "loadout.json");

    public static void Save(PlayerLoadout loadout, GemInventory inventory) { /* serialize */ }
    public static bool TryLoad(PlayerLoadout loadout, GemInventory inventory, GemRegistry registry) { /* deserialize, validate gem IDs */ }
}
```

`TryLoad` validates that all gem IDs in the save file exist in the `GemRegistry`. Unknown IDs are silently dropped (forward compatibility for removed gems).

---

## Controller / Keyboard Mapping

| Action                | Controller        | Keyboard      |
|-----------------------|-------------------|---------------|
| Move cursor           | D-Pad             | Arrow keys    |
| Switch focus zone     | LB / RB           | Tab           |
| Select / confirm swap | A                 | Enter         |
| Cancel                | B                 | Escape        |
| Confirm & go          | Start             | Enter (no pending action) |

---

## GameState Integration

`GameState` tracks a `GameScene` enum: `Playing`, `Loadout`. On wave clear:

```csharp
_scene = GameScene.Loadout;
_loadoutScreen = new LoadoutScreen(_gemSystem, _input);
```

`GameState.Update()` routes to `_loadoutScreen.Update(dt)` when `_scene == GameScene.Loadout`. On `_loadoutScreen.IsComplete`, transition back to `GameScene.Playing` and reset the wave.

`LoadoutScreen` does not `new` any reference types during its `Update()` or `Draw()` loops — all strings come from the `GemRegistry` (pre-loaded), all collections are pre-allocated.

---

## File Locations

```
Core/
  LoadoutScreen.cs
  LoadoutPersistence.cs
```

---

## Open Questions

1. Should the loadout screen show a ship silhouette with sockets positioned on the hull, or is the grid layout sufficient for the first implementation?
2. Do we want a "preview fire" button that shows a projectile preview animation using the current resolved parameters?
3. Should inventory gems be sorted (by category, then name), or show in collection order?
