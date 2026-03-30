# Spec: Enemy AI Profile System

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0004                        |
| Status   | Implemented                      |
| Author   | rpabo                            |
| Created  | 2026-03-25                       |
| Updated  | 2026-03-30                       |

## Implementation Note

Core system fully implemented: `IBehaviourHandler`, `BehaviourRegistry`, `AiSystem`, `AiProfileRegistry`, `AiContext`, `EnemyAiState` (zero-allocation), and JSON profile loading. Four handlers ship: `StraightHandler`, `SineHandler`, `ZigzagHandler`, `AttackHandler`. Profiles: `straight.json`, `sine_wave.json`, `zigzag.json`, `fodder_shooter.json`.

**Remaining handlers** (`charge`, `retreat`, `formation`, `shield_ally`, `dive`, `warp`, `strafe`, `dodge`, `maintain_distance`, `retreat_on_low_hp`, `boss_phase`, `command_aura`) are being added incrementally through spec-0008 milestones M2–M5. Each handler gets its own PR; this spec tracks the framework, not handler inventory.

## Overview

Replace the `EnemyMovePattern` enum with a file-based JSON AI profile system. Each enemy type references an AI profile by ID. Profiles are composed of named behaviour nodes that execute in sequence or in parallel. This enables 8+ distinct movement and attack behaviors without code changes for each new variant.

## Goals

- `IBehaviourHandler` interface + `BehaviourRegistry` for handler registration.
- `EnemyAiState` value type — all per-enemy state, no heap allocation.
- `AiContext` read-only snapshot passed to handlers each frame.
- 8 built-in node types: `straight`, `sine`, `zigzag` (port existing), plus `charge`, `retreat`, `formation`, `shoot_at_player`, `shield_ally`.
- Composable: a profile lists one or more nodes; the system runs them in order per frame.
- One AI profile per JSON file for easy content authoring.

## Non-Goals

- Visual behaviour editor (planned for Phase 3, spec-0007).
- Network-replicated AI state.
- Full behaviour tree with condition nodes (future spec).

---

## Data Model

### AiProfile

One file per profile in `Assets/ai_profiles/<id>.json`.

```json
{
  "id": "straight",
  "nodes": [
    { "type": "straight" }
  ]
}
```

```json
{
  "id": "sine_wave",
  "nodes": [
    { "type": "sine", "amplitude": 120.0, "frequency": 2.5 }
  ]
}
```

```json
{
  "id": "zigzag",
  "nodes": [
    {
      "type": "zigzag",
      "verticalSpeed": 150.0,
      "flipInterval": 0.8
    }
  ]
}
```

```json
{
  "id": "charger",
  "nodes": [
    { "type": "retreat", "retreatDistance": 200.0, "retreatSpeed": 120.0, "duration": 1.5 },
    { "type": "charge",  "chargeSpeed": 700.0, "targetPlayer": true }
  ]
}
```

```json
{
  "id": "shooter",
  "nodes": [
    { "type": "straight" },
    { "type": "shoot_at_player", "fireCooldown": 2.0, "projectileSpeed": 400.0 }
  ]
}
```

```json
{
  "id": "formation_leader",
  "nodes": [
    { "type": "sine", "amplitude": 60.0, "frequency": 1.5 },
    { "type": "formation", "role": "leader", "slotCount": 3, "slotSpacing": 60.0 }
  ]
}
```

```json
{
  "id": "shielder",
  "nodes": [
    { "type": "straight" },
    { "type": "shield_ally", "shieldRadius": 120.0, "shieldedRarity": "Rare" }
  ]
}
```

### JSON Schema — AiProfile

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "AiProfile",
  "type": "object",
  "required": ["id", "nodes"],
  "properties": {
    "id":    { "type": "string" },
    "nodes": {
      "type": "array",
      "minItems": 1,
      "items": { "$ref": "#/definitions/AiNode" }
    }
  },
  "definitions": {
    "AiNode": {
      "type": "object",
      "required": ["type"],
      "properties": {
        "type":             { "type": "string" },
        "amplitude":        { "type": "number" },
        "frequency":        { "type": "number" },
        "verticalSpeed":    { "type": "number" },
        "flipInterval":     { "type": "number" },
        "chargeSpeed":      { "type": "number" },
        "targetPlayer":     { "type": "boolean" },
        "retreatDistance":  { "type": "number" },
        "retreatSpeed":     { "type": "number" },
        "duration":         { "type": "number" },
        "fireCooldown":     { "type": "number" },
        "projectileSpeed":  { "type": "number" },
        "role":             { "type": "string", "enum": ["leader", "follower"] },
        "slotCount":        { "type": "integer" },
        "slotSpacing":      { "type": "number" },
        "shieldRadius":     { "type": "number" },
        "shieldedRarity":   { "type": "string" }
      }
    }
  }
}
```

---

## C# Types

### AiNodeConfig

Deserialized from the node JSON. One instance per node per profile — loaded at startup, not per-frame.

```csharp
// Systems/AiSystem/AiNodeConfig.cs
public class AiNodeConfig
{
    public string  Type            { get; init; }
    // Movement
    public float   Amplitude       { get; init; }
    public float   Frequency       { get; init; }
    public float   VerticalSpeed   { get; init; }
    public float   FlipInterval    { get; init; }
    // Charge / Retreat
    public float   ChargeSpeed     { get; init; }
    public bool    TargetPlayer    { get; init; }
    public float   RetreatDistance { get; init; }
    public float   RetreatSpeed    { get; init; }
    public float   Duration        { get; init; }
    // Shooting
    public float   FireCooldown    { get; init; }
    public float   ProjectileSpeed { get; init; }
    // Formation
    public string  Role            { get; init; }
    public int     SlotCount       { get; init; }
    public float   SlotSpacing     { get; init; }
    // Shield
    public float   ShieldRadius    { get; init; }
    public string  ShieldedRarity  { get; init; }
}
```

### AiProfile (runtime)

```csharp
// Systems/AiSystem/AiProfile.cs
public class AiProfile
{
    public string         Id      { get; init; }
    public AiNodeConfig[] Nodes   { get; init; }
}
```

### EnemyAiState

Per-enemy mutable state — stored as a struct field on `Enemy`. No heap allocation.

```csharp
// Systems/AiSystem/EnemyAiState.cs
public struct EnemyAiState
{
    // General
    public float AliveTimer;        // seconds since spawn
    public float SpawnY;            // Y at spawn time (sine reference)

    // Zigzag
    public float ZigzagTimer;
    public float ZigzagDirection;   // +1 or -1

    // Charge / Retreat
    public int   PhaseIndex;        // which node in a multi-node profile is active
    public float PhaseTimer;        // time in current phase
    public bool  ChargeActive;

    // Shooting
    public float FireCooldownTimer;

    // Formation
    public int   FormationSlot;     // assigned slot index
}
```

### AiContext

Read-only snapshot of world state needed by behaviour handlers. Passed by `in` to avoid copying.

```csharp
// Systems/AiSystem/AiContext.cs
public readonly struct AiContext
{
    public readonly float       Dt;
    public readonly Vector2     PlayerPosition;
    public readonly float       ScreenWidth;
    public readonly float       ScreenHeight;
    // Formation: array of active enemy positions for follower nodes
    public readonly ReadOnlySpan<Vector2> ActiveEnemyPositions;
}
```

### IBehaviourHandler

```csharp
// Systems/AiSystem/IBehaviourHandler.cs
public interface IBehaviourHandler
{
    string TypeName { get; }

    /// <summary>
    /// Updates enemy position/velocity/state. Called once per frame per active node.
    /// Must NOT allocate.
    /// </summary>
    void Update(
        ref Vector2       position,
        ref Vector2       velocity,
        ref EnemyAiState  state,
        AiNodeConfig      config,
        in AiContext      ctx);

    /// <summary>
    /// Called when the handler needs to fire a projectile.
    /// Returns true if a projectile should be spawned this frame.
    /// </summary>
    bool TryGetFireEvent(
        in EnemyAiState  state,
        AiNodeConfig     config,
        out Vector2      projectileVelocity);
}
```

### BehaviourRegistry

```csharp
// Systems/AiSystem/BehaviourRegistry.cs
public class BehaviourRegistry
{
    private readonly Dictionary<string, IBehaviourHandler> _handlers;

    public BehaviourRegistry()
    {
        Register(new StraightHandler());
        Register(new SineHandler());
        Register(new ZigzagHandler());
        Register(new ChargeHandler());
        Register(new RetreatHandler());
        Register(new FormationHandler());
        Register(new ShootAtPlayerHandler());
        Register(new ShieldAllyHandler());
    }

    public void Register(IBehaviourHandler handler) =>
        _handlers[handler.TypeName] = handler;

    public IBehaviourHandler Get(string typeName) => _handlers[typeName];
}
```

### AiSystem

```csharp
// Systems/AiSystem/AiSystem.cs
public class AiSystem
{
    private readonly BehaviourRegistry _registry;
    private readonly AiProfileRegistry _profiles;

    public AiSystem(string profilesDirectory)
    {
        _registry = new BehaviourRegistry();
        _profiles  = new AiProfileRegistry(profilesDirectory, _registry);
    }

    /// <summary>
    /// Updates a single enemy's AI. Called from Enemy.Update() via a delegate.
    /// Must NOT allocate.
    /// </summary>
    public void Update(
        ref Vector2      position,
        ref Vector2      velocity,
        ref EnemyAiState state,
        string           profileId,
        in AiContext     ctx)
    {
        var profile = _profiles.Get(profileId);
        foreach (var node in profile.Nodes)
        {
            var handler = _registry.Get(node.Type);
            handler.Update(ref position, ref velocity, ref state, node, in ctx);
        }
    }
}
```

### AiProfileRegistry

```csharp
// Systems/AiSystem/AiProfileRegistry.cs
public class AiProfileRegistry
{
    private readonly Dictionary<string, AiProfile> _profiles;

    public AiProfileRegistry(string directory, BehaviourRegistry registry) { /* load JSON */ }
    public AiProfile Get(string id) => _profiles[id];
}
```

---

## Built-in Behaviour Handlers

| Node Type        | Behaviour                                                   | Key Config Fields                       |
|------------------|-------------------------------------------------------------|-----------------------------------------|
| `straight`       | Constant velocity along X axis                             | —                                       |
| `sine`           | Horizontal advance + vertical sine oscillation             | `amplitude`, `frequency`                |
| `zigzag`         | Horizontal advance + vertical bounce at screen bounds      | `verticalSpeed`, `flipInterval`         |
| `charge`         | Accelerates toward player position at spawn                | `chargeSpeed`, `targetPlayer`           |
| `retreat`        | Moves away from player for `duration` seconds              | `retreatSpeed`, `retreatDistance`, `duration` |
| `formation`      | Leader holds sine path; followers mirror offset positions  | `role`, `slotCount`, `slotSpacing`      |
| `shoot_at_player`| Fires projectile toward player on cooldown                 | `fireCooldown`, `projectileSpeed`       |
| `shield_ally`    | Scans for nearby Rare+ enemies and reduces their damage    | `shieldRadius`, `shieldedRarity`        |

---

## Enemy Integration

`Enemy` drops `EnemyMovePattern` and gains:

```csharp
public string         AiProfileId;
public EnemyAiState   AiState;
```

`Enemy.Spawn()` sets `AiProfileId` and resets `AiState`. `Enemy.Update()` calls:

```csharp
_aiSystem.Update(ref Position, ref Velocity, ref AiState, AiProfileId, in ctx);
```

`AiSystem` is injected into `Enemy` (or provided via `GameState` callback) to avoid the enemy holding a reference to a system it shouldn't own.

---

## File Locations

```
Systems/
  AiSystem/
    AiNodeConfig.cs
    AiProfile.cs
    AiProfileRegistry.cs
    AiContext.cs
    EnemyAiState.cs
    IBehaviourHandler.cs
    BehaviourRegistry.cs
    AiSystem.cs
    Handlers/
      StraightHandler.cs
      SineHandler.cs
      ZigzagHandler.cs
      ChargeHandler.cs
      RetreatHandler.cs
      FormationHandler.cs
      ShootAtPlayerHandler.cs
      ShieldAllyHandler.cs
Assets/
  ai_profiles/
    straight.json
    sine_wave.json
    zigzag.json
    charger.json
    shooter.json
    formation_leader.json
    shielder.json
```

---

## Open Questions

1. How does `formation` handle followers when the leader is killed mid-formation?
2. Should `shoot_at_player` use the enemy projectile pool or the player's bullet pool (different visual)?
3. Do we want a `condition` node type (e.g., fire `charge` only when HP < 50%) in Phase 1, or defer to Phase 3?
