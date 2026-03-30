# Spec: Enemy Rarity System

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0002                        |
| Status   | Implemented                      |
| Author   | rpabo                            |
| Created  | 2026-03-25                       |
| Updated  | 2026-03-30                       |

## Implementation Note

Core system fully implemented: `EnemyRarity` enum, `RarityConstants`, `AffixDefinition`/`AffixModifiers`, `AffixRegistry`, `RarityRoller` (with wave escalation and incompatibility checks), score multipliers in `CollisionSystem`, and 5 defensive affixes (`fast`, `shielded`, `splitter`, `armored`, `regenerating`). Unique preset `the_guardian.json` is live.

**Remaining content (not blocking):** Offensive affixes (`aggressive`, `deadeye`, `piercing_shots`, `multishot`, `homing_shots`, `vengeful`) are designed in spec-0008 and will be added in Milestone M3.

## Overview

Introduce four enemy rarity tiers — Normal, Magic, Rare, Unique — modeled after Path of Exile's item rarity system. Rarity controls visual color, score multiplier, number of affixes (modifiers), and drop table tier (see spec-0005). Unique enemies load from hand-crafted preset JSON files.

## Goals

- Add `EnemyRarity` enum and per-rarity visual constants.
- Define `AffixDefinition` data model and `AffixRegistry`.
- Add `RarityRoller` to determine rarity and roll affixes on spawn.
- Affix incompatibility rules prevent contradictory combinations.
- Score multipliers reward players for killing rare enemies.

## Non-Goals

- Player rarity / item rarity.
- Persistent affix state between waves (affixes are rolled fresh on each spawn).

---

## Rarity Tiers

| Tier   | Color        | Hex       | Score Multiplier | Max Affixes | Min Affixes |
|--------|--------------|-----------|-----------------|-------------|-------------|
| Normal | White        | `#FFFFFF` | ×1              | 0           | 0           |
| Magic  | Blue         | `#8888FF` | ×2              | 2           | 1           |
| Rare   | Yellow/Gold  | `#FFD700` | ×5              | 4           | 2           |
| Unique | Orange/Brown | `#C85000` | ×10             | Preset      | Preset      |

Unique enemies do not roll random affixes; their affix list is fully defined in their preset JSON.

---

## Spawn Probability

Default rarity weights (tunable per wave/level):

| Tier   | Weight |
|--------|--------|
| Normal | 70     |
| Magic  | 22     |
| Rare   | 7      |
| Unique | 1      |

`RarityRoller` performs a weighted random draw from these weights.

---

## Data Model

### AffixDefinition

Stored in `Assets/affixes/*.json`. Each file defines one affix.

```json
{
  "id": "fast",
  "displayName": "Swiftness",
  "description": "Moves 60% faster.",
  "incompatibleWith": [],
  "allowedRarities": ["Magic", "Rare", "Unique"],
  "modifiers": {
    "speedMultiplier": 1.6
  }
}
```

```json
{
  "id": "shielded",
  "displayName": "Shielded",
  "description": "Has a damage-absorbing shield layer.",
  "incompatibleWith": [],
  "allowedRarities": ["Magic", "Rare", "Unique"],
  "modifiers": {
    "shieldHp": 5
  }
}
```

```json
{
  "id": "splitter",
  "displayName": "Splitter",
  "description": "Splits into 2 Normal enemies on death.",
  "incompatibleWith": ["splitter"],
  "allowedRarities": ["Rare", "Unique"],
  "modifiers": {
    "splitsOnDeath": 2
  }
}
```

```json
{
  "id": "armored",
  "displayName": "Armored",
  "description": "50% physical damage reduction.",
  "incompatibleWith": [],
  "allowedRarities": ["Magic", "Rare", "Unique"],
  "modifiers": {
    "physicalDamageReduction": 0.5
  }
}
```

```json
{
  "id": "regenerating",
  "displayName": "Regenerating",
  "description": "Regenerates 1 HP per second.",
  "incompatibleWith": [],
  "allowedRarities": ["Rare", "Unique"],
  "modifiers": {
    "hpRegenPerSecond": 1.0
  }
}
```

### Unique Enemy Preset

Stored in `Assets/uniques/<id>.json`.

```json
{
  "id": "the_guardian",
  "displayName": "The Guardian",
  "rarity": "Unique",
  "baseHealth": 40,
  "baseSpeed": 150.0,
  "aiProfileId": "boss_charge",
  "affixes": ["shielded", "regenerating"],
  "dropTableId": "unique_guardian"
}
```

### JSON Schema — AffixDefinition

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "AffixDefinition",
  "type": "object",
  "required": ["id", "displayName", "allowedRarities", "modifiers"],
  "properties": {
    "id":               { "type": "string" },
    "displayName":      { "type": "string" },
    "description":      { "type": "string" },
    "incompatibleWith": { "type": "array", "items": { "type": "string" } },
    "allowedRarities":  {
      "type": "array",
      "items": { "type": "string", "enum": ["Magic", "Rare", "Unique"] }
    },
    "modifiers": {
      "type": "object",
      "properties": {
        "speedMultiplier":          { "type": "number" },
        "shieldHp":                 { "type": "integer" },
        "splitsOnDeath":            { "type": "integer" },
        "physicalDamageReduction":  { "type": "number" },
        "hpRegenPerSecond":         { "type": "number" },
        "damageMultiplier":         { "type": "number" },
        "projectileCount":          { "type": "integer" }
      }
    }
  }
}
```

---

## C# Types

### EnemyRarity

```csharp
// Entities/EnemyRarity.cs
public enum EnemyRarity { Normal, Magic, Rare, Unique }

public static class RarityConstants
{
    public static readonly Color NormalColor  = Color.White;
    public static readonly Color MagicColor   = new Color((byte)136, (byte)136, (byte)255, (byte)255);
    public static readonly Color RareColor    = new Color((byte)255, (byte)215, (byte)0,   (byte)255);
    public static readonly Color UniqueColor  = new Color((byte)200, (byte)80,  (byte)0,   (byte)255);

    public static float ScoreMultiplier(EnemyRarity r) => r switch
    {
        EnemyRarity.Normal => 1f,
        EnemyRarity.Magic  => 2f,
        EnemyRarity.Rare   => 5f,
        EnemyRarity.Unique => 10f,
        _ => 1f
    };
}
```

### AffixDefinition

```csharp
// Systems/RaritySystem/AffixDefinition.cs
public class AffixDefinition
{
    public string          Id               { get; init; }
    public string          DisplayName      { get; init; }
    public string          Description      { get; init; }
    public string[]        IncompatibleWith { get; init; }
    public EnemyRarity[]   AllowedRarities  { get; init; }
    public AffixModifiers  Modifiers        { get; init; }
}

public struct AffixModifiers
{
    public float  SpeedMultiplier;
    public int    ShieldHp;
    public int    SplitsOnDeath;
    public float  PhysicalDamageReduction;
    public float  HpRegenPerSecond;
    public float  DamageMultiplier;
    public int    ProjectileCount;
}
```

### AffixRegistry

```csharp
// Systems/RaritySystem/AffixRegistry.cs
public class AffixRegistry
{
    private readonly Dictionary<string, AffixDefinition> _affixes;

    public AffixRegistry(string affixDirectory) { /* load JSON */ }

    public AffixDefinition Get(string id) => _affixes[id];
    public IReadOnlyList<AffixDefinition> GetForRarity(EnemyRarity rarity) { /* filter */ }
}
```

### RarityRoller

```csharp
// Systems/RaritySystem/RarityRoller.cs
public class RarityRoller
{
    // Weights indexed by (int)EnemyRarity
    private readonly int[] _weights = { 70, 22, 7, 1 };

    public EnemyRarity RollRarity() { /* weighted random */ }

    /// <summary>
    /// Rolls affixes for a given rarity.
    /// Fills the provided span — avoids allocation.
    /// Returns the number of affixes written.
    /// </summary>
    public int RollAffixes(
        EnemyRarity rarity,
        AffixRegistry registry,
        Span<string> outAffixIds) { /* weighted sample without replacement, check incompatibility */ }
}
```

### Enemy Integration

`Enemy.Spawn()` gains a `rarity` parameter and an `affixIds` span:

```csharp
public void Spawn(
    Vector2 position,
    Vector2 velocity,
    int health,
    EnemyMovePattern pattern,
    EnemyRarity rarity,
    ReadOnlySpan<string> affixIds,
    AffixRegistry affixRegistry)
```

`Enemy` stores:
- `public EnemyRarity Rarity;`
- `private AffixModifiers _combinedModifiers;` — pre-combined from all affixes at spawn, not re-evaluated per frame.

`Enemy.Draw()` uses `RarityConstants.GetColor(Rarity)` as the fill color (placeholder until sprites exist).

---

## Affix Incompatibility Rules

When rolling affixes, `RarityRoller` maintains a set of excluded affix IDs. After selecting an affix, all IDs in its `incompatibleWith` list are added to the exclusion set. Subsequent rolls sample only from the remaining pool.

---

## Score Integration

`CollisionSystem` reads `Enemy.Rarity` on kill and calls `Player.AddScore(baseScore * RarityConstants.ScoreMultiplier(rarity))`.

---

## File Locations

```
Entities/
  EnemyRarity.cs
Systems/
  RaritySystem/
    AffixDefinition.cs
    AffixModifiers.cs
    AffixRegistry.cs
    RarityRoller.cs
Assets/
  affixes/
    fast.json
    shielded.json
    splitter.json
    armored.json
    regenerating.json
  uniques/
    the_guardian.json
```

---

## Resolved Questions

1. **Rarity weights escalate per wave.** `RarityRoller` shifts weights from Normal toward higher tiers each wave. By wave 20, roughly: Normal=30, Magic=42, Rare=22, Unique=6. (Decided 2026-03-27)
2. **Magic/Rare/Unique enemies show both color tint AND name text** above them, built from affix display names (e.g., "Swiftness Armored"). (Decided 2026-03-27)
3. **Split children inherit parent rarity minus one tier.** Rare → Magic children, Magic → Normal. `RarityConstants.DemoteOneTier()` implements this. Children do not inherit parent affixes. (Decided 2026-03-27)
