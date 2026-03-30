# Spec: Drop Table System

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0005                        |
| Status   | Implemented                      |
| Author   | rpabo                            |
| Created  | 2026-03-25                       |
| Updated  | 2026-03-25                       |

## Overview

Define a weighted-random gem drop system tied to enemy rarity. Enemies drop gems on death. Drops are rarity-tiered: Normal enemies have a low chance at common gems; Rare enemies guarantee a drop and can yield rarer gems; Unique enemies guarantee a specific drop defined in their preset JSON. Dropped gems are collected into the player's `GemInventory` at end-of-wave.

## Goals

- `DropTable` data model loaded from JSON, one file per table ID.
- Rarity-indexed default tables (`drops_normal`, `drops_magic`, `drops_rare`, `drops_unique_default`).
- Unique enemies reference a named drop table in their preset JSON.
- `DropSystem` performs the roll and returns a `GemId` string or null (no drop).
- `DroppedGem` pooled entity that floats on screen after enemy death and is collected when the wave ends.
- `GemInventory` on `Player` stores collected gem IDs between levels.

## Non-Goals

- In-level gem collection by walking over them (gems are auto-collected at wave end).
- Gem quality tiers / gem rarity distinct from enemy rarity.
- Currency drops (gold, orbs) — future spec.

---

## Data Model

### DropTable

One file per table in `Assets/drop_tables/<id>.json`.

```json
{
  "id": "drops_normal",
  "guaranteedDrop": false,
  "dropChance": 0.1,
  "entries": [
    { "gemId": "shot_normal",    "weight": 60 },
    { "gemId": "support_damage", "weight": 30 },
    { "gemId": "support_speed",  "weight": 10 }
  ]
}
```

```json
{
  "id": "drops_magic",
  "guaranteedDrop": false,
  "dropChance": 0.4,
  "entries": [
    { "gemId": "shot_normal",      "weight": 30 },
    { "gemId": "shot_spread",      "weight": 20 },
    { "gemId": "support_damage",   "weight": 20 },
    { "gemId": "support_pierce",   "weight": 15 },
    { "gemId": "support_homing",   "weight": 10 },
    { "gemId": "support_speed",    "weight": 5  }
  ]
}
```

```json
{
  "id": "drops_rare",
  "guaranteedDrop": true,
  "dropChance": 1.0,
  "entries": [
    { "gemId": "shot_spread",      "weight": 25 },
    { "gemId": "shot_rapid",       "weight": 20 },
    { "gemId": "support_pierce",   "weight": 20 },
    { "gemId": "support_homing",   "weight": 15 },
    { "gemId": "support_radius",   "weight": 10 },
    { "gemId": "support_multishot","weight": 10 }
  ]
}
```

```json
{
  "id": "drops_unique_default",
  "guaranteedDrop": true,
  "dropChance": 1.0,
  "entries": [
    { "gemId": "support_homing",   "weight": 30 },
    { "gemId": "support_multishot","weight": 30 },
    { "gemId": "support_chain",    "weight": 20 },
    { "gemId": "shot_rapid",       "weight": 20 }
  ]
}
```

Unique enemy preset overrides the table via `dropTableId`:

```json
{
  "id": "the_guardian",
  "dropTableId": "unique_guardian"
}
```

```json
{
  "id": "unique_guardian",
  "guaranteedDrop": true,
  "dropChance": 1.0,
  "entries": [
    { "gemId": "support_chain", "weight": 100 }
  ]
}
```

### JSON Schema — DropTable

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "DropTable",
  "type": "object",
  "required": ["id", "guaranteedDrop", "dropChance", "entries"],
  "properties": {
    "id":             { "type": "string" },
    "guaranteedDrop": { "type": "boolean" },
    "dropChance":     { "type": "number", "minimum": 0.0, "maximum": 1.0 },
    "entries": {
      "type": "array",
      "minItems": 1,
      "items": {
        "type": "object",
        "required": ["gemId", "weight"],
        "properties": {
          "gemId":  { "type": "string" },
          "weight": { "type": "integer", "minimum": 1 }
        }
      }
    }
  }
}
```

---

## C# Types

### DropTableEntry / DropTable

```csharp
// Systems/DropSystem/DropTable.cs
public struct DropTableEntry
{
    public string GemId;
    public int    Weight;
}

public class DropTable
{
    public string          Id             { get; init; }
    public bool            GuaranteedDrop { get; init; }
    public float           DropChance     { get; init; }
    public DropTableEntry[] Entries       { get; init; }

    // Precomputed total weight to avoid per-roll summation
    public int TotalWeight { get; init; }
}
```

### DropTableRegistry

```csharp
// Systems/DropSystem/DropTableRegistry.cs
public class DropTableRegistry
{
    private readonly Dictionary<string, DropTable> _tables;

    public DropTableRegistry(string directory) { /* load JSON, precompute TotalWeight */ }

    public DropTable Get(string id) => _tables[id];

    public DropTable GetForRarity(EnemyRarity rarity) => rarity switch
    {
        EnemyRarity.Normal => Get("drops_normal"),
        EnemyRarity.Magic  => Get("drops_magic"),
        EnemyRarity.Rare   => Get("drops_rare"),
        EnemyRarity.Unique => Get("drops_unique_default"),
        _ => Get("drops_normal")
    };
}
```

### DropSystem

```csharp
// Systems/DropSystem/DropSystem.cs
public class DropSystem
{
    private readonly DropTableRegistry _tables;

    public DropSystem(DropTableRegistry registry) { _tables = registry; }

    /// <summary>
    /// Rolls a drop for the given enemy rarity and optional override table.
    /// Returns null if no drop occurs (roll missed dropChance).
    /// Must NOT allocate — returns a string ID from the pre-loaded registry.
    /// </summary>
    public string? Roll(EnemyRarity rarity, string? overrideTableId = null)
    {
        var table = overrideTableId != null
            ? _tables.Get(overrideTableId)
            : _tables.GetForRarity(rarity);

        if (!table.GuaranteedDrop && Random.Shared.NextSingle() > table.DropChance)
            return null;

        return WeightedRandom(table);
    }

    private static string WeightedRandom(DropTable table)
    {
        int roll = Random.Shared.Next(table.TotalWeight);
        int acc  = 0;
        foreach (ref var entry in table.Entries.AsSpan())
        {
            acc += entry.Weight;
            if (roll < acc) return entry.GemId;
        }
        return table.Entries[^1].GemId; // fallback
    }
}
```

### DroppedGem (pooled entity)

```csharp
// Entities/DroppedGem.cs
public class DroppedGem : Entity
{
    public string GemId;   // ID into GemRegistry

    private float _bobTimer;
    private const float BobSpeed     = 2.0f;
    private const float BobAmplitude = 4.0f;
    private float _baseY;

    public void Activate(Vector2 position, string gemId)
    {
        Position = position;
        _baseY   = position.Y;
        GemId    = gemId;
        Active   = true;
        _bobTimer = 0f;
    }

    public override void Update(float dt)
    {
        _bobTimer  += dt;
        Position.Y  = _baseY + MathF.Sin(_bobTimer * BobSpeed) * BobAmplitude;
        // Drift left slowly
        Position.X -= 30f * dt;
        if (Position.X < -32f) Active = false;
    }

    public override void Draw()
    {
        if (!Active) return;
        // Placeholder: small cyan gem shape
        Raylib.DrawCircleV(Position, 8f, Color.Cyan);
    }
}
```

Pool size: `Constants.DroppedGemPoolSize = 32`.

### GemInventory

```csharp
// Systems/GemSystem/GemInventory.cs
/// <summary>
/// Stores gem IDs the player has collected. Persists between levels.
/// No per-frame access — used only at loadout screen (spec-0006).
/// </summary>
public class GemInventory
{
    private readonly List<string> _gems;

    public GemInventory(int initialCapacity = 64)
    {
        _gems = new List<string>(initialCapacity);
    }

    public void Add(string gemId) => _gems.Add(gemId);
    public bool Remove(string gemId) => _gems.Remove(gemId);
    public IReadOnlyList<string> All => _gems;
    public int Count => _gems.Count;
}
```

`Player` owns one `GemInventory`. `GameState` calls `Player.Inventory.Add(gemId)` when a `DroppedGem` is collected.

---

## Collection Flow

1. Enemy dies → `DropSystem.Roll()` → returns `gemId` or null.
2. If `gemId` is non-null, get a `DroppedGem` from pool, call `Activate(deathPosition, gemId)`.
3. `DroppedGem` floats and drifts left.
4. On wave-end (level transition), all active `DroppedGem` entities are collected:
   - `Player.Inventory.Add(gem.GemId)` for each active gem.
   - All active `DroppedGem` entities are returned to pool.
5. Loadout screen opens (spec-0006), player swaps gems from inventory.

---

## File Locations

```
Entities/
  DroppedGem.cs
Systems/
  DropSystem/
    DropTable.cs
    DropTableRegistry.cs
    DropSystem.cs
  GemSystem/
    GemInventory.cs         (part of spec-0001 GemSystem folder)
Assets/
  drop_tables/
    drops_normal.json
    drops_magic.json
    drops_rare.json
    drops_unique_default.json
    unique_guardian.json
```

---

## Open Questions

1. Should `DroppedGem` have a short on-screen lifetime so the screen doesn't fill with gems that the player missed?
2. Should the inventory have a cap? If so, what happens when it overflows (oldest replaced, or drop is not spawned)?
3. Do we want gem rarity tiers (Normal/Magic gem vs Normal/Magic/Rare/Unique enemy rarity) in a future spec?
