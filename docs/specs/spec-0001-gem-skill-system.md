# Spec: Gem Skill System

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0001                        |
| Status   | Implemented                      |
| Author   | rpabo                            |
| Created  | 2026-03-25                       |
| Updated  | 2026-03-30                       |

## Implementation Note

**Rename:** This system shipped as the **Module System** (`Systems/ModuleSystem/`). "Gem" was renamed to "Module" throughout the codebase for clarity. `GemDefinition` → `ModuleDefinition`, `GemRegistry` → `ModuleRegistry`, `GemModifierPipeline` → `ModulePipeline`, `GemSystem` → `ModuleSystem`. JSON assets live in `Assets/modules/`. All spec references to "gems" map to "modules" in code.

**Remaining content (not blocking):** `shot_spread`, `shot_rapid`, and `shield_forward` weapon modules are designed in the catalogue below but not yet added as JSON assets. These are deferred to Milestone M6 (module variety pass).

## Overview

Replace the hard-coded normal/charged shot system with a PoE-style gem system. The player has 4 skill slots, each with 2 linked support gem slots (12 gem slots total). Gems are the primary progression currency — dropped by enemies, collected, and swapped between levels via the loadout screen (see spec-0006).

## Goals

- Replace `Player.FireBullet(chargeLevel)` with a gem-driven pipeline that resolves `ProjectileParameters` at fire time.
- Support skill gem categories: shot types, shields, beams, etc.
- Support modifier gems: damage boost, homing, expanded radius, pierce, chain, etc.
- Keep the hot path allocation-free — resolve parameters to a cached struct, not a new object per frame.

## Non-Goals

- In-level gem swapping (loadout UI is spec-0006).
- Networked gem state.
- Gem crafting / combining.

---

## Data Model

### GemDefinition

Describes a gem type. Stored in `Assets/gems/*.json`.

```json
{
  "id": "shot_normal",
  "displayName": "Normal Shot",
  "category": "Skill",
  "skillCategory": "Shot",
  "baseProjectileParameters": {
    "speed": 800.0,
    "width": 12.0,
    "height": 4.0,
    "damage": 1,
    "pierce": 0,
    "homing": false,
    "homingStrength": 0.0,
    "radiusMultiplier": 1.0,
    "count": 1,
    "spreadAngleDeg": 0.0
  },
  "tags": ["physical", "projectile"]
}
```

```json
{
  "id": "shot_charged",
  "displayName": "Charged Shot",
  "category": "Skill",
  "skillCategory": "Shot",
  "baseProjectileParameters": {
    "speed": 600.0,
    "width": 32.0,
    "height": 16.0,
    "damage": 3,
    "pierce": 0,
    "homing": false,
    "homingStrength": 0.0,
    "radiusMultiplier": 1.0,
    "count": 1,
    "spreadAngleDeg": 0.0
  },
  "tags": ["physical", "projectile", "charged"]
}
```

Support gem example:

```json
{
  "id": "support_pierce",
  "displayName": "Pierce",
  "category": "Support",
  "modifiers": {
    "pierceDelta": 2
  },
  "requiresTags": ["projectile"],
  "tags": ["physical"]
}
```

```json
{
  "id": "support_homing",
  "displayName": "Homing",
  "category": "Support",
  "modifiers": {
    "homingOverride": true,
    "homingStrength": 3.0
  },
  "requiresTags": ["projectile"],
  "tags": []
}
```

```json
{
  "id": "support_damage",
  "displayName": "Added Damage",
  "category": "Support",
  "modifiers": {
    "damageFlat": 1,
    "damageMultiplier": 1.0
  },
  "requiresTags": [],
  "tags": []
}
```

### JSON Schema — GemDefinition

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "GemDefinition",
  "type": "object",
  "required": ["id", "displayName", "category"],
  "properties": {
    "id":          { "type": "string" },
    "displayName": { "type": "string" },
    "category":    { "type": "string", "enum": ["Skill", "Support"] },
    "skillCategory": { "type": "string", "enum": ["Shot", "Shield", "Beam", "Mine"] },
    "baseProjectileParameters": { "$ref": "#/definitions/ProjectileParameters" },
    "modifiers": { "$ref": "#/definitions/GemModifiers" },
    "requiresTags": { "type": "array", "items": { "type": "string" } },
    "tags": { "type": "array", "items": { "type": "string" } }
  },
  "definitions": {
    "ProjectileParameters": {
      "type": "object",
      "properties": {
        "speed":             { "type": "number" },
        "width":             { "type": "number" },
        "height":            { "type": "number" },
        "damage":            { "type": "integer" },
        "pierce":            { "type": "integer" },
        "homing":            { "type": "boolean" },
        "homingStrength":    { "type": "number" },
        "radiusMultiplier":  { "type": "number" },
        "count":             { "type": "integer" },
        "spreadAngleDeg":    { "type": "number" }
      }
    },
    "GemModifiers": {
      "type": "object",
      "properties": {
        "damageFlat":        { "type": "integer" },
        "damageMultiplier":  { "type": "number" },
        "pierceDelta":       { "type": "integer" },
        "homingOverride":    { "type": "boolean" },
        "homingStrength":    { "type": "number" },
        "radiusMultiplier":  { "type": "number" },
        "countDelta":        { "type": "integer" },
        "spreadAngleDeg":    { "type": "number" }
      }
    }
  }
}
```

---

## C# Types

### ProjectileParameters (struct)

```csharp
// Systems/GemSystem/ProjectileParameters.cs
public struct ProjectileParameters
{
    public float Speed;
    public float Width;
    public float Height;
    public int   Damage;
    public int   Pierce;
    public bool  Homing;
    public float HomingStrength;
    public float RadiusMultiplier;
    public int   Count;
    public float SpreadAngleDeg;
}
```

### GemDefinition (class, loaded from JSON)

```csharp
// Systems/GemSystem/GemDefinition.cs
public class GemDefinition
{
    public string               Id { get; init; }
    public string               DisplayName { get; init; }
    public GemCategory          Category { get; init; }
    public SkillCategory?       SkillCategory { get; init; }
    public ProjectileParameters BaseProjectileParameters { get; init; }
    public GemModifiers         Modifiers { get; init; }
    public string[]             RequiresTags { get; init; }
    public string[]             Tags { get; init; }
}

public enum GemCategory  { Skill, Support }
public enum SkillCategory { Shot, Shield, Beam, Mine }
```

### GemRegistry

```csharp
// Systems/GemSystem/GemRegistry.cs
/// <summary>
/// Loads all GemDefinitions from Assets/gems/*.json at startup.
/// Thread-safe read access after initialization.
/// </summary>
public class GemRegistry
{
    private readonly Dictionary<string, GemDefinition> _gems;

    public GemRegistry(string gemsDirectory) { /* load JSON files */ }

    public GemDefinition Get(string id) => _gems[id];
    public bool TryGet(string id, out GemDefinition def) => _gems.TryGetValue(id, out def);
    public IReadOnlyCollection<GemDefinition> All => _gems.Values;
}
```

### GemSlot / PlayerLoadout

```csharp
// Systems/GemSystem/PlayerLoadout.cs
public const int SkillSlotCount   = 4;
public const int SupportSlotCount = 2;

public struct GemSlot
{
    public string?    GemId;   // null = empty
    public bool       Active;
}

/// <summary>
/// Represents the player's current skill configuration.
/// 4 skill slots × (1 skill gem + 2 support gems).
/// </summary>
public class PlayerLoadout
{
    // [skillIndex]         — the active skill gem
    public GemSlot[] SkillGems    { get; } = new GemSlot[SkillSlotCount];
    // [skillIndex, supportIndex] — 0..1 per skill slot
    public GemSlot[,] SupportGems { get; } = new GemSlot[SkillSlotCount, SupportSlotCount];

    public bool TryEquipSkill(int slot, string gemId, GemRegistry registry) { /* validate */ }
    public bool TryEquipSupport(int skillSlot, int supportSlot, string gemId, GemRegistry registry) { /* validate */ }
    public void ClearSlot(int skillSlot, int supportSlot = -1) { /* -1 clears skill gem */ }
}
```

### GemModifierPipeline

```csharp
// Systems/GemSystem/GemModifierPipeline.cs
/// <summary>
/// Applies support gems to a base ProjectileParameters.
/// Called once when the loadout changes (not per-frame).
/// Result is cached in GemSystem.ResolvedParameters[slot].
/// </summary>
public static class GemModifierPipeline
{
    public static ProjectileParameters Resolve(
        GemDefinition skillGem,
        ReadOnlySpan<GemDefinition?> supportGems)
    {
        var p = skillGem.BaseProjectileParameters;
        foreach (var sup in supportGems)
        {
            if (sup == null) continue;
            Apply(ref p, sup.Modifiers, skillGem.Tags);
        }
        return p;
    }

    private static void Apply(ref ProjectileParameters p, GemModifiers m, string[] skillTags) { /* mutate p */ }
}
```

### GemSystem

```csharp
// Systems/GemSystem/GemSystem.cs
/// <summary>
/// Owns the registry, loadout, and resolved parameter cache.
/// GameState holds one GemSystem instance.
/// </summary>
public class GemSystem
{
    public GemRegistry    Registry  { get; }
    public PlayerLoadout  Loadout   { get; }
    public GemInventory   Inventory { get; }  // collected gems (see spec-0005)

    // Cached resolved parameters — updated on loadout change, not per frame
    public ProjectileParameters[] ResolvedParameters { get; }  // length = SkillSlotCount

    public GemSystem(string gemsDirectory) { /* init */ }

    /// <summary>Call when loadout changes to rebuild ResolvedParameters.</summary>
    public void RebuildCache() { /* pipeline pass */ }

    /// <summary>Returns resolved params for the active skill slot.</summary>
    public ref readonly ProjectileParameters GetActive(int slot) => ref ResolvedParameters[slot];
}
```

---

## Behavior

### Firing

`Player.Update()` queries `GemSystem.GetActive(activeSlot)` to get the cached `ProjectileParameters`. `FireBullet()` uses these parameters when spawning a `Projectile` from the pool. No allocation occurs in the hot path.

### Loadout Change

Swapping a gem during the between-level screen calls `PlayerLoadout.TryEquip*()` then `GemSystem.RebuildCache()`. The resolver runs once and caches the result. The loadout screen (spec-0006) owns this flow.

### Incompatibility

A support gem is incompatible with a skill slot if its `requiresTags` are not a subset of the skill gem's `tags`. `TryEquipSupport()` returns `false` in this case. The UI must display feedback (see spec-0006).

---

## Skill Gem Catalogue (initial set)

| ID              | Category | Description                             |
|-----------------|----------|-----------------------------------------|
| shot_normal     | Skill    | Fast single projectile (current normal) |
| shot_charged    | Skill    | Slow large projectile (current charged) |
| shot_spread     | Skill    | 3-way spread shot                       |
| shot_rapid      | Skill    | High fire-rate low damage shot          |
| shield_forward  | Skill    | Temporary directional shield            |

## Support Gem Catalogue (initial set)

| ID               | Modifies           | Description                          |
|------------------|--------------------|--------------------------------------|
| support_damage   | All shots          | +1 flat damage, 1.1× multiplier      |
| support_pierce   | Projectile shots   | +2 pierce (pass through enemies)     |
| support_homing   | Projectile shots   | Tracks nearest enemy                 |
| support_radius   | All shots          | 1.5× hitbox radius multiplier        |
| support_chain    | Projectile shots   | Bounces to adjacent enemy on hit     |
| support_speed    | Projectile shots   | +200 speed                           |
| support_multishot| Projectile shots   | +1 projectile count, spread ±10°     |

---

## File Locations

```
Systems/
  GemSystem/
    GemDefinition.cs
    GemModifiers.cs
    GemRegistry.cs
    GemModifierPipeline.cs
    PlayerLoadout.cs
    GemSystem.cs
    ProjectileParameters.cs
    GemInventory.cs          (see spec-0005)
Assets/
  gems/
    shot_normal.json
    shot_charged.json
    shot_spread.json
    support_damage.json
    support_pierce.json
    support_homing.json
    ...
```

---

## Open Questions

1. Should skill gems have a fire-rate or cooldown property, or is that a player-level constant?
2. Do we want gem "level" (tiered power) in the data model, or keep gems flat at 1.0 for now?
3. How do we handle the `shield` skill category in terms of projectile parameters — separate `ShieldParameters` struct, or overload the existing struct?
