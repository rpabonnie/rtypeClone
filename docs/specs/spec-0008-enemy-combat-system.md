# Spec: Enemy Combat System

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0008                        |
| Status   | In Progress (Phase 0 complete)   |
| Author   | rpabo + Claude                   |
| Created  | 2026-03-28                       |
| Updated  | 2026-03-30                       |
| Depends  | spec-0003 (HP), spec-0004 (AI profiles), spec-0002 (rarity/affixes), spec-0001 (modules) |

## Overview

Enemies currently only deal contact damage — they never shoot back. This spec adds a full enemy combat system: attack skills scaled by rarity, diverse movement patterns including reverse and on-screen AI, enemy roles that create synergy within waves, and telegraphing rules that keep combat fair and readable.

The core principle: **fodder enemies get unique, simple attacks that players can't access. Elite enemies fight back using the same `ProjectileParameters` system the player uses — effectively mirror-matching the player's own build potential.**

## Goals

- Enemy projectile pool (separate from player bullets, distinct visuals).
- Attack framework driven by `EnemyAttackConfig` — data-driven, JSON-defined, zero per-frame allocation.
- Fodder attack catalogue: aimed shot, burst fire, suicide dive, mine layer, spray.
- Elite/Rare attacks: reuse `ProjectileParameters` with homing, pierce, multishot.
- Boss signature attacks: bullet curtains, targeted barrages, summon adds, area denial.
- New movement profiles: reverse entry, top/bottom swoops, warp-in, strafe, dodge, maintain-distance.
- Enemy roles: tank, DPS, healer, commander — synergy within waves.
- Telegraphing rules for all enemy attacks (charge-up flash, distinct projectile colors, audio cues).

## Non-Goals

- Player-to-enemy damage types / elemental interactions (future spec, per spec-0003).
- Bullet-hell density patterns (this is R-Type, not Touhou — attacks are readable, not overwhelming).
- Enemy projectiles that the player can destroy (future consideration).
- Friendly fire between enemies.

---

## 1. Enemy Projectile Pool

Enemy bullets are visually and mechanically distinct from player bullets.

### EnemyProjectile (pooled entity)

```csharp
// Entities/EnemyProjectile.cs
public class EnemyProjectile : Entity
{
    public Vector2 Velocity;
    public int     Damage;
    public float   Lifetime;       // max seconds before auto-despawn
    public float   Age;
    public bool    Homing;
    public float   HomingStrength;
    public int     Pierce;         // hits remaining before despawn
    public bool    IsStationary;   // true for mines

    public void Spawn(Vector2 position, EnemyAttackConfig config, Vector2 aimDirection) { /* ... */ }
    public override void Update(float dt) { /* movement, homing, lifetime, off-screen check */ }
    public override void Draw() { /* distinct color per attack type */ }
}
```

- Pool size: `Constants.EnemyProjectilePoolSize = 128`.
- Collision with player handled in `CollisionSystem` alongside existing player-bullet-vs-enemy checks.
- **Visual distinction:** enemy projectiles are **red/orange** family; player projectiles are **blue/cyan** family. This is non-negotiable for readability.

---

## 2. Enemy Attack Framework

### EnemyAttackConfig

Each attack is defined as a JSON object, either inline in an AI profile node or in a shared attack definition file.

```csharp
// Systems/CombatSystem/EnemyAttackConfig.cs
public class EnemyAttackConfig
{
    // Identity
    public string Id              { get; init; }  // e.g., "aimed_shot", "burst_fire"
    public string Category        { get; init; }  // "fodder", "elite", "boss"

    // Timing
    public float  Cooldown        { get; init; }  // seconds between attacks
    public float  TelegraphTime   { get; init; }  // seconds of warning flash before firing
    public float  BurstCount      { get; init; }  // projectiles per burst (1 = single shot)
    public float  BurstInterval   { get; init; }  // seconds between burst rounds

    // Projectile properties
    public float  ProjectileSpeed { get; init; }
    public int    Damage          { get; init; }
    public float  Lifetime        { get; init; }  // seconds before auto-despawn
    public int    Count           { get; init; }  // simultaneous projectiles (fan/spread)
    public float  SpreadAngleDeg  { get; init; }  // total spread arc for Count > 1
    public bool   Homing          { get; init; }
    public float  HomingStrength  { get; init; }
    public int    Pierce          { get; init; }
    public bool   Stationary      { get; init; }  // true for mines

    // Aim
    public string AimMode         { get; init; }  // "at_player", "fixed_left", "forward", "random_arc"
    public float  AimOffsetDeg    { get; init; }  // offset from aim direction

    // Visual
    public string TelegraphColor  { get; init; }  // hex color for charge-up flash
    public string ProjectileColor { get; init; }  // hex color override (default: red)
    public float  ProjectileWidth { get; init; }
    public float  ProjectileHeight{ get; init; }
}
```

### JSON Example — Fodder Aimed Shot

```json
{
  "id": "aimed_shot",
  "category": "fodder",
  "cooldown": 2.5,
  "telegraphTime": 0.4,
  "burstCount": 1,
  "burstInterval": 0.0,
  "projectileSpeed": 350.0,
  "damage": 1,
  "lifetime": 4.0,
  "count": 1,
  "spreadAngleDeg": 0.0,
  "homing": false,
  "homingStrength": 0.0,
  "pierce": 0,
  "stationary": false,
  "aimMode": "at_player",
  "aimOffsetDeg": 0.0,
  "projectileWidth": 8.0,
  "projectileHeight": 8.0
}
```

### AI Profile Integration

Attacks are attached to AI profiles via a new `attack` node type (extending spec-0004). The `shoot_at_player` node from spec-0004 becomes a thin wrapper that references an `EnemyAttackConfig`.

```json
{
  "id": "fodder_shooter",
  "nodes": [
    { "type": "sine", "amplitude": 80.0, "frequency": 2.0 },
    { "type": "attack", "attackId": "aimed_shot" }
  ]
}
```

The `attack` node handler:
1. Decrements `FireCooldownTimer` each frame.
2. When cooldown expires, enters telegraph state (`TelegraphTimer` counts up).
3. When telegraph completes, fires projectile(s) per config.
4. Resets cooldown.

All state lives in `EnemyAiState` (extended with `TelegraphTimer`, `BurstShotsRemaining`, `BurstTimer`).

---

## 3. Attack Catalogue

### 3a. Fodder Attacks (Normal Rarity)

These are **unique enemy skills** — not available to the player. They're simple, readable, and add variety to basic enemies.

| Attack ID       | Description                                         | Cooldown | Telegraph | Aim Mode    | Notes                                    |
|-----------------|-----------------------------------------------------|----------|-----------|-------------|------------------------------------------|
| `aimed_shot`    | Single slow bullet toward player                    | 2.5s     | 0.4s      | at_player   | Bread-and-butter fodder attack            |
| `burst_fire`    | 3-round burst in a fixed direction (left)           | 3.0s     | 0.3s      | fixed_left  | burstCount=3, burstInterval=0.15s         |
| `suicide_dive`  | Accelerate toward player, explode on death          | one-shot | 0.6s      | at_player   | Not a projectile — modifies enemy movement; explosion spawns ring of 6 bullets |
| `mine_layer`    | Drop stationary mine behind as it flies             | 2.0s     | 0.0s      | none        | stationary=true, lifetime=5.0s, damage=2  |
| `spray`         | Fan of 3 bullets toward the player side             | 3.5s     | 0.5s      | fixed_left  | count=3, spreadAngleDeg=45                |

**Suicide dive** is special — it's not purely an attack node but combines with a movement override. When the telegraph completes, the enemy's AI profile switches to a charge-at-player movement, and on death it spawns an explosion ring. This is implemented as a composite: `attack` node triggers a flag in `EnemyAiState.SuicideDiveActive`, which the movement handler checks.

### 3b. Elite Attacks (Magic / Rare Rarity)

Elite enemies use the **same `ProjectileParameters` system as the player** (spec-0001). This creates a mirror-match dynamic: the player sees enemy attacks that mirror builds they could assemble.

Elite attack configs are built from `ProjectileParameters` with an `EnemyAttackConfig` wrapper for timing/telegraph:

| Rarity | Example Build                        | ProjectileParameters Used               | Cooldown | Telegraph |
|--------|--------------------------------------|-----------------------------------------|----------|-----------|
| Magic  | Rapid fire                           | speed=500, damage=1, count=1            | 0.8s     | 0.2s      |
| Magic  | Spread shot                          | speed=400, damage=1, count=3, spread=30 | 2.0s     | 0.3s      |
| Rare   | Homing missiles                      | speed=300, damage=2, homing=true, homingStrength=3.0 | 2.5s | 0.5s |
| Rare   | Pierce beam                          | speed=600, damage=2, pierce=3, width=6, height=24 | 3.0s | 0.6s |
| Rare   | Multishot homing                     | speed=250, damage=1, count=4, spread=60, homing=true | 4.0s | 0.7s |

**How elite attacks are assigned:**
- `WaveSpawner` rolls rarity per spec-0002.
- For Magic/Rare enemies, it also rolls an **attack profile** from a weighted table.
- The attack profile references an AI profile that combines a movement pattern with an `attack` node.
- Offensive affixes (new, see section 6) can modify the attack: `+projectileCount`, `homingEnabled`, `piercePlus`.

### 3c. Boss Signature Attacks (Unique Rarity)

Boss attacks are phase-based and hand-authored. Each boss has a preset AI profile with multiple phases.

| Attack              | Description                                              | Phase    |
|---------------------|----------------------------------------------------------|----------|
| `bullet_curtain`    | Dense wall of evenly-spaced bullets with safe lanes      | Any      |
| `targeted_barrage`  | Rapid burst of 8-12 aimed shots at player position       | Phase 2+ |
| `summon_adds`       | Spawn 2-4 fodder enemies from boss position              | Phase 2+ |
| `laser_sweep`       | Horizontal beam sweeps vertically across screen (1.5s)   | Phase 3  |
| `expanding_ring`    | Ring of bullets expanding outward from boss center       | Any      |

Boss phase transitions are driven by HP thresholds:
- **Phase 1:** 100%-66% HP — basic attacks + bullet curtain.
- **Phase 2:** 66%-33% HP — adds targeted barrage + summon adds, attacks speed up.
- **Phase 3:** Below 33% HP — unlocks laser sweep, desperation patterns.

Phase transition is signaled by a brief invulnerability flash (0.5s) and a screen shake.

---

## 4. Movement Profiles

Extending spec-0004 with new behaviour handlers and AI profiles.

### 4a. New Entry Directions

| Profile ID         | Entry Direction     | Description                                               |
|--------------------|---------------------|-----------------------------------------------------------|
| `reverse_entry`    | Left → Right        | Enters from behind the player. Panicking surprise.         |
| `dive_top`         | Top → Bottom        | Swoops down from above, then curves right-to-left.         |
| `dive_bottom`      | Bottom → Top        | Swoops up from below, then curves right-to-left.           |
| `warp_in`          | Appears mid-screen  | Brief shimmer effect (0.5s), then materializes. Good for ambush waves. |

Implementation: `WaveSpawner` sets initial position and velocity based on the profile's `entryDirection` field (new field on `AiProfile`).

```json
{
  "id": "reverse_entry",
  "entryDirection": "left",
  "entryPosition": { "x": -40, "yMin": 100, "yMax": 900 },
  "nodes": [
    { "type": "straight", "speedX": 250.0 }
  ]
}
```

```json
{
  "id": "dive_top",
  "entryDirection": "top",
  "entryPosition": { "y": -40, "xMin": 400, "xMax": 1600 },
  "nodes": [
    { "type": "dive", "diveSpeed": 400.0, "curveAfter": 300.0, "exitDirection": "left" }
  ]
}
```

```json
{
  "id": "warp_in",
  "entryDirection": "warp",
  "entryPosition": { "xMin": 600, "xMax": 1400, "yMin": 200, "yMax": 800 },
  "nodes": [
    { "type": "warp", "shimmerDuration": 0.5, "invulnerableDuringShimmer": true },
    { "type": "strafe", "preferredX": 1200, "verticalSpeed": 200.0 },
    { "type": "attack", "attackId": "aimed_shot" }
  ]
}
```

### 4b. New Behaviour Handlers

| Handler            | Description                                                     | Key Config Fields                            |
|--------------------|-----------------------------------------------------------------|----------------------------------------------|
| `dive`             | Fast diagonal entry, curves into horizontal exit                | `diveSpeed`, `curveAfter`, `exitDirection`   |
| `warp`             | Shimmer-in effect, enemy invulnerable during shimmer            | `shimmerDuration`, `invulnerableDuringShimmer`|
| `strafe`           | Hover at a preferred X, move vertically to align with player    | `preferredX`, `verticalSpeed`                |
| `dodge`            | React to nearby player projectiles, juke perpendicular          | `dodgeRadius`, `dodgeSpeed`, `reactionTime`  |
| `maintain_distance`| Keep a target distance from player, retreat/advance as needed   | `targetDistance`, `moveSpeed`, `tolerance`    |
| `retreat_on_low_hp`| When HP drops below threshold, move away from player            | `hpThreshold`, `retreatSpeed`                |
| `boss_phase`       | Position-based phases triggered by HP thresholds                | `phases[]` (array of {hpPercent, targetPosition, transitionTime}) |
| `attack`           | Fire projectiles per `EnemyAttackConfig` (see section 2)        | `attackId`                                   |

### 4c. Elite On-Screen AI (Magic/Rare)

Elite enemies don't just fly across the screen — they **stay and fight**. Their profiles combine movement handlers that keep them on-screen with attack nodes.

```json
{
  "id": "elite_strafe_shooter",
  "entryDirection": "right",
  "nodes": [
    { "type": "strafe", "preferredX": 1300, "verticalSpeed": 180.0 },
    { "type": "dodge", "dodgeRadius": 100.0, "dodgeSpeed": 300.0, "reactionTime": 0.2 },
    { "type": "attack", "attackId": "elite_homing" }
  ]
}
```

```json
{
  "id": "elite_hit_and_run",
  "entryDirection": "right",
  "nodes": [
    { "type": "maintain_distance", "targetDistance": 500, "moveSpeed": 200.0, "tolerance": 100 },
    { "type": "retreat_on_low_hp", "hpThreshold": 0.3, "retreatSpeed": 250.0 },
    { "type": "attack", "attackId": "elite_pierce_beam" }
  ]
}
```

### 4d. Boss Movement

Bosses use `boss_phase` to control positioning across the fight.

```json
{
  "id": "boss_phase_movement",
  "type": "boss_phase",
  "phases": [
    { "hpPercent": 1.0, "targetPosition": { "x": 1500, "y": 540 }, "transitionTime": 0.0 },
    { "hpPercent": 0.66, "targetPosition": { "x": 1300, "y": 300 }, "transitionTime": 1.0 },
    { "hpPercent": 0.33, "targetPosition": { "x": 960, "y": 540 }, "transitionTime": 1.5 }
  ]
}
```

---

## 5. Enemy Roles & Synergy

Waves become more interesting when enemies have **roles** that interact. Roles are defined by combining specific AI profiles with specific affixes.

### Role Definitions

| Role        | Behaviour                                                   | Profile Traits                              |
|-------------|-------------------------------------------------------------|---------------------------------------------|
| **Tank**    | High HP, armored affix, moves to front of formation         | `shielded` + `armored` affixes, slow movement, large hitbox |
| **DPS**     | Glass cannon, stays behind tank, fires heavy attacks         | Low HP, high damage attack config, `strafe` movement |
| **Healer**  | Regenerates nearby allies' HP over time                     | `shield_ally` behaviour node, medium HP, avoids player |
| **Commander** | Buffs nearby fodder (speed, fire rate); death weakens group | New `command_aura` behaviour node, visible aura effect |

### Commander Aura (New Behaviour Node)

```json
{
  "type": "command_aura",
  "auraRadius": 200.0,
  "speedMultiplier": 1.5,
  "cooldownMultiplier": 0.7,
  "buffVisual": "yellow_pulse"
}
```

When a commander is active, all enemies within `auraRadius` get their movement speed multiplied and attack cooldowns reduced. When the commander dies, buffs are removed immediately — creating a **priority target** for the player.

### Revenge Mechanic

A new affix: `vengeful`.

```json
{
  "id": "vengeful",
  "displayName": "Vengeful",
  "description": "Powers up when a nearby ally dies.",
  "incompatibleWith": [],
  "allowedRarities": ["Rare", "Unique"],
  "modifiers": {
    "vengefulRadius": 250.0,
    "vengefulSpeedBonus": 1.3,
    "vengefulDamageBonus": 1.5
  }
}
```

When an enemy with the `vengeful` affix detects an ally death within radius, it gains a temporary speed and damage buff (stacks up to 3 times, 5s duration per stack). Visual: enemy flashes red briefly and gets a subtle red glow.

### Synergy Wave Examples

The `WaveSpawner` can spawn **role-based wave templates** at higher wave numbers:

| Wave Template        | Composition                                     | First Appears |
|----------------------|-------------------------------------------------|---------------|
| `basic_shooters`     | 6 fodder with `aimed_shot`, 2 with `spray`      | Wave 1        |
| `mixed_fodder`       | 4 straight + 2 reverse_entry + 2 dive_top       | Wave 3        |
| `tank_and_dps`       | 2 tanks (front) + 4 DPS (behind)                | Wave 5        |
| `commander_squad`    | 1 commander + 5 buffed fodder                   | Wave 7        |
| `healer_escort`      | 1 healer + 2 rare DPS + 3 fodder                | Wave 9        |
| `ambush`             | 4 warp_in + 4 reverse_entry (simultaneous)       | Wave 6        |
| `vengeful_pack`      | 4 rare enemies with `vengeful` affix             | Wave 10       |

---

## 6. Offensive Affixes

Extending spec-0002's affix system with **offensive modifiers** that enhance enemy attacks.

### New Affix Definitions

| Affix ID           | Display Name     | Effect                                  | Allowed Rarities   |
|--------------------|------------------|-----------------------------------------|--------------------|
| `aggressive`       | Aggressive       | Attack cooldown ×0.6 (fires 40% faster) | Magic, Rare, Unique |
| `multishot`        | Multishot        | +2 projectile count per attack          | Rare, Unique        |
| `homing_shots`     | Tracking         | Enables homing on all projectiles       | Rare, Unique        |
| `piercing_shots`   | Piercing         | +1 pierce on all projectiles            | Magic, Rare, Unique |
| `vengeful`         | Vengeful         | Buffs on nearby ally death (see §5)     | Rare, Unique        |
| `deadeye`          | Deadeye          | Projectile speed ×1.5                   | Magic, Rare, Unique |

### AffixModifiers Extension

```csharp
// New fields added to AffixModifiers struct
public float AttackCooldownMultiplier;   // default 1.0 (aggressive sets 0.6)
public int   BonusProjectileCount;       // default 0 (multishot adds 2)
public bool  ForcedHoming;               // default false (homing_shots enables)
public int   BonusPierce;                // default 0 (piercing_shots adds 1)
public float ProjectileSpeedMultiplier;  // default 1.0 (deadeye sets 1.5)
public float VengefulRadius;             // default 0 (vengeful sets 250)
public float VengefulSpeedBonus;         // default 1.0
public float VengefulDamageBonus;        // default 1.0
```

These modifiers are applied to the `EnemyAttackConfig` at spawn time (combined with the base attack config), not per-frame.

---

## 7. Telegraphing & Fairness Rules

Every enemy attack **must** give the player a chance to react. These rules are mandatory for all attacks in the game.

### 7a. Visual Telegraphs

| Attack Type           | Telegraph Method                                  | Minimum Duration |
|-----------------------|---------------------------------------------------|------------------|
| Aimed shot / burst    | Enemy flashes white, brief charge-up glow         | 0.3s             |
| Spread / spray        | Enemy flashes white + directional arc indicator    | 0.4s             |
| Homing missile        | Enemy glows yellow + targeting reticle on player   | 0.5s             |
| Suicide dive          | Enemy turns red + accelerates with trail effect    | 0.6s             |
| Mine drop             | No telegraph (mines are slow-damage zone denial)   | 0.0s             |
| Boss bullet curtain   | Screen-edge warning markers showing bullet lanes   | 0.8s             |
| Boss laser sweep      | Thin red line preview of sweep path                | 1.0s             |
| Boss summon adds      | Boss glows + spawn positions flash                 | 0.5s             |

### 7b. Projectile Readability

- **Enemy projectiles are always red/orange.** Player projectiles are always blue/cyan. No exceptions.
- Enemy projectiles are **larger and slower** than player projectiles. This makes them visible and dodgeable.
- Maximum enemy projectile speed: `500f` for normal attacks, `600f` for boss attacks. Player bullets travel at `800f` — the player's shots are always faster.
- Minimum projectile size: `6×6` pixels. Anything smaller is invisible at 1080p.

### 7c. Attack Density Limits

To prevent unfair screen flooding:

- **Fodder enemies:** max 1 attack in flight per enemy at a time.
- **Elite enemies:** max 3 attacks in flight per enemy at a time.
- **Bosses:** max 24 projectiles in flight at a time (enforced by pool partitioning or cooldown gating).
- **Global cap:** `Constants.MaxEnemyProjectiles = 128`. If the pool is full, enemies delay their attacks rather than dropping them silently.

### 7d. Audio Cues

- Each attack type has a distinct sound effect.
- Boss attacks get a louder, more dramatic cue.
- Homing projectiles emit a continuous tracking tone while pursuing.

---

## 8. EnemyAiState Extensions

New fields needed in `EnemyAiState` to support combat:

```csharp
// Added to EnemyAiState struct
public float TelegraphTimer;        // counts up during telegraph phase
public bool  IsTelegraphing;        // true = charging up, don't fire yet
public int   BurstShotsRemaining;   // remaining shots in current burst
public float BurstTimer;            // countdown to next burst round
public bool  SuicideDiveActive;     // suicide dive movement override
public int   VengefulStacks;        // 0-3, buff stacks from ally deaths
public float VengefulTimer;         // remaining duration of vengeful buff
public float WarpShimmerTimer;      // warp-in countdown
public bool  WarpComplete;          // has warp-in finished
public float DodgeCooldownTimer;    // prevent constant dodge spam
```

---

## 9. CollisionSystem Extensions

`CollisionSystem` gains a new check: **enemy projectiles vs player**.

```csharp
// New collision check in CollisionSystem
public void CheckEnemyProjectileVsPlayer(
    ObjectPool<EnemyProjectile> enemyProjectiles,
    Player player)
{
    if (player.IsInvincible) return;

    for (int i = 0; i < enemyProjectiles.Count; i++)
    {
        var proj = enemyProjectiles[i];
        if (!proj.Active) continue;

        if (CheckOverlap(proj.Position, proj.Width, proj.Height,
                          player.Position, player.Width, player.Height))
        {
            player.TakeHit();           // existing i-frame logic
            proj.Active = false;        // despawn projectile on hit
            // pierce projectiles: decrement hits remaining instead
            break;                      // only one hit per frame
        }
    }
}
```

---

## 10. File Locations

```
Entities/
  EnemyProjectile.cs              # Pooled enemy bullet entity

Systems/
  CombatSystem/
    EnemyAttackConfig.cs          # Attack definition data class
    EnemyAttackRegistry.cs        # Loads attack JSONs from Assets/attacks/

  AiSystem/
    Handlers/
      AttackHandler.cs            # Fires projectiles per EnemyAttackConfig
      DiveHandler.cs              # Diagonal entry, curve to horizontal
      WarpHandler.cs              # Shimmer-in appearance
      StrafeHandler.cs            # Vertical movement at fixed X
      DodgeHandler.cs             # React to player projectiles
      MaintainDistanceHandler.cs  # Keep target range from player
      RetreatOnLowHpHandler.cs    # Flee when HP low
      BossPhaseHandler.cs         # HP-threshold positioning
      CommandAuraHandler.cs       # Buff nearby allies

Assets/
  attacks/                        # Attack definition JSONs
    aimed_shot.json
    burst_fire.json
    suicide_dive.json
    mine_layer.json
    spray.json
    elite_rapid.json
    elite_spread.json
    elite_homing.json
    elite_pierce.json
    elite_multishot_homing.json
    boss_bullet_curtain.json
    boss_targeted_barrage.json
    boss_expanding_ring.json
    boss_laser_sweep.json

  ai_profiles/                    # Extended with new profiles
    fodder_shooter.json
    fodder_burst.json
    fodder_mine_layer.json
    fodder_suicide.json
    reverse_entry.json
    dive_top.json
    dive_bottom.json
    warp_in.json
    elite_strafe_shooter.json
    elite_hit_and_run.json
    boss_default.json

  affixes/                        # New offensive affixes
    aggressive.json
    multishot.json
    homing_shots.json
    piercing_shots.json
    vengeful.json
    deadeye.json
```

---

## 11. Implementation Phases

### Phase 0 — Foundation ✅ Complete
- [x] `EnemyProjectile` entity + pool.
- [x] `EnemyAttackConfig` data class + JSON loader.
- [x] `AttackHandler` behaviour node (cooldown → telegraph → fire).
- [x] `CollisionSystem` enemy-projectile-vs-player check.
- [x] `aimed_shot` attack + `fodder_shooter` AI profile — first enemy that shoots back.
- [x] Visual telegraph (white flash) and red projectile color.

### Phase 1 — Fodder Variety
- [ ] Remaining fodder attacks: `burst_fire`, `spray`, `mine_layer`, `suicide_dive`.
- [ ] New entry directions: `reverse_entry`, `dive_top`, `dive_bottom`.
- [ ] Wire fodder attacks into `WaveSpawner` rotation.

### Phase 2 — Elite Combat
- [ ] Elite attack configs using `ProjectileParameters`.
- [ ] Offensive affixes: `aggressive`, `deadeye`, `piercing_shots`.
- [ ] `strafe`, `dodge`, `maintain_distance` movement handlers.
- [ ] Elite AI profiles that stay on-screen and fight.
- [ ] Higher-tier offensive affixes: `multishot`, `homing_shots`.

### Phase 3 — Roles & Synergy
- [ ] `command_aura` behaviour handler.
- [ ] `vengeful` affix.
- [ ] `retreat_on_low_hp` handler.
- [ ] Role-based wave templates in `WaveSpawner`.
- [ ] Healer role via `shield_ally` handler (already spec'd in spec-0004).

### Phase 4 — Bosses
- [ ] `boss_phase` movement handler.
- [ ] Boss signature attacks: bullet curtain, targeted barrage, summon adds, laser sweep, expanding ring.
- [ ] Phase transition effects (invulnerability flash, screen shake).
- [ ] First boss encounter (wave 10 or wave 15).

---

## Open Questions

1. **Should mines be destructible?** Player could shoot mines to clear them, adding a tactical choice. Leaning yes — it rewards attentive players. (Sounds good)
2. **Should elite enemies drop their attack config as a "preview" of what gems the player could build?** Flavor text like "This enemy was using: Homing + Multishot" on death could teach the build system. (no, let the player figure that out.)
3. **How many fodder per wave should shoot vs. be passive?** Too many shooters at once overwhelms. Suggest: wave 1-3 = 25% shooters, wave 4-7 = 50%, wave 8+ = 75%. (lets test this one, later we can modify)
4. **Should boss HP thresholds be fixed or configurable per boss JSON?** Leaning toward per-boss JSON for variety. (configurable)
5. **Should the `warp_in` shimmer block player projectiles?** If invulnerable during warp, player bullets pass through — feels fair since the enemy can't attack either. (Ok, lets test this)
6. **Commander death: instant debuff removal or gradual fade?** Instant removal creates a more dramatic "kill the commander!" moment. (Gradual fade)
