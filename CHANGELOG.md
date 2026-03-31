# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased] — M2: Fodder Attack Variety (spec-0008 Phase 1)

### Added
- **`burst_fire` attack** — 3-round burst fired left, 0.3s telegraph, 3.0s cooldown. Enemy flashes white, then sends 3 sequential shots.
- **`spray` attack** — fan of 3 bullets at 45° spread fired left, 0.5s telegraph, 3.5s cooldown.
- **`mine_layer` attack** — drops a stationary mine at the enemy's position every 2.0s, no telegraph. Mines last 5s, deal 2 damage, and render as a yellow/gold square distinct from moving projectiles.
- **`suicide_dive` attack** — after a 0.6s red telegraph flash, the enemy locks onto the player and charges at 600 px/s. On contact or death, spawns a ring of 6 slow projectiles outward in all directions.
- **`reverse_entry` profile** — enemy spawns off the left edge and moves right, surprising the player from behind.
- **`dive_top` profile** — enemy swoops in from above, dives downward 320px then curves left.
- **`dive_bottom` profile** — enemy swoops in from below, dives upward 320px then curves left.
- **`fodder_burst` profile** — straight movement + burst fire attack.
- **`fodder_spray` profile** — sine movement + spray attack.
- **`fodder_mine` profile** — straight movement + mine layer.
- **`fodder_suicide` profile** — sine movement + suicide dive.
- **`DiveHandler`** (`Systems/AiSystem/Handlers/DiveHandler.cs`) — new behaviour handler for top/bottom entry swoops. Reads `diveSpeed` and `curveAfter` from node config; infers dive direction (down/up) from spawn Y position relative to screen centre.

### Changed
- **`AiProfile`** — added `entryDirection` JSON field ("right" default, "left", "top", "bottom"). Used by WaveSpawner to determine spawn edge.
- **`AiNodeConfig`** — added `diveSpeed`, `curveAfter`, `exitDirection` fields for DiveHandler.
- **`EnemyAiState`** — added `SuicideDiveActive`, `DiveDistanceTraveled`, `DiveCurved` fields.
- **`AttackHandler`** — handles `category: "suicide_dive"` attacks: sets `SuicideDiveActive` and redirects enemy velocity toward the player at 600 px/s instead of spawning a projectile. Mines now spawn at enemy centre rather than left edge.
- **`BehaviourRegistry`** — registers `DiveHandler`.
- **`AiSystem`** — added `GetEntryDirection(profileId)` helper for WaveSpawner.
- **`WaveSpawner`** — now accepts `AiSystem` to read entry direction per profile. Spawn position and initial velocity are edge-specific. Profile pool expanded to 11 profiles with wave-scaled shooter ratio (25% → 50% → 75% across waves 1-3, 4-7, 8+).
- **`CollisionSystem`** — `CheckCollisions` now accepts `ObjectPool<EnemyProjectile>` to support explosion ring spawning. Spawns 6-bullet ring on suicide-dive enemy death (both bullet-kill and contact-kill paths).
- **`EnemyProjectile.Draw()`** — mines (`IsStationary`) render with a distinct yellow/gold pulsing style; moving projectiles unchanged (orange/red).
- **`GameState`** — threads `_aiSystem` into WaveSpawner and `_enemyProjectilePool` into CheckCollisions.
- **spec-0008** — Phase 1 checkboxes ticked, status updated to `In Progress (Phase 1 complete)`.

---

## [Unreleased] — M1: Spec Status Sync

### Changed
- **spec-0001** (Gem/Module Skill System) — status updated to `Implemented`. Added implementation note documenting the gem→module rename and noting deferred weapon modules (`shot_spread`, `shot_rapid`, `shield_forward`) for Milestone M6.
- **spec-0002** (Enemy Rarity System) — status updated to `Implemented`. Added implementation note documenting shipped content (4 tiers, 5 defensive affixes, wave escalation, score multipliers). Offensive affixes deferred to spec-0008 M3.
- **spec-0003** (Enemy HP System) — status updated to `Implemented`. Added implementation note confirming all goals shipped (EnemyHealth struct, DamageEvent, DamageNumbers pool, health/shield bars).
- **spec-0004** (Enemy AI Profile System) — status updated to `Implemented` for the core framework. Added implementation note clarifying that additional handlers are delivered incrementally through spec-0008 milestones rather than in this spec.
- **spec-0006** (Ship Loadout Socket System) — status updated from `Draft` to `In Progress`. Added note describing what's implemented (Module Bay, scene management, debug picker, persistence) vs. what remains (inventory panel integration, gem swapping from collected inventory).
- **spec-0008** (Enemy Combat System) — status updated from `Draft` to `In Progress (Phase 0 complete)`. Phase 0 checkboxes ticked. Spec restored from orphaned commit in prior session.

### Added
- **spec-0008-enemy-combat-system.md** — added to `docs/specs/` (was authored in a prior session but never merged to main; recovered from git history).

---

## [Unreleased] — Drop Table System (spec-0005)

### Added
- **Drop table system** (`Systems/DropSystem/`) — weighted-random gem drops tied to enemy rarity. Normal enemies have 10% drop chance, Magic 40%, Rare and Unique 100%. Drop tables loaded from `Assets/drop_tables/*.json` at startup.
- **DroppedGem entity** (`Entities/DroppedGem.cs`) — pooled entity that drifts left with a sine-wave bob after spawning at an enemy's death position. Despawns after 8s or when off-screen. Rendered as a diamond placeholder.
- **GemInventory** (`Core/GemInventory.cs`) — simple list-backed inventory of collected module IDs. Player collects gems by overlapping them during gameplay.
- **Player gem collection** — `CollisionSystem` checks player-vs-gem overlap and adds collected gem IDs to the inventory.
- **4 drop table JSON files** — `drops_normal`, `drops_magic`, `drops_rare`, `drops_unique_default` in `Assets/drop_tables/`.

---

## [Unreleased] — Enemy Combat Foundation (Phase 0)

### Added
- **Enemy projectiles** (`Entities/EnemyProjectile.cs`) — pooled projectile entity for enemy attacks. Red/orange visuals distinct from player's blue/cyan bullets. Supports homing, pierce, lifetime, and stationary modes.
- **EnemyAttackConfig** (`Systems/CombatSystem/EnemyAttackConfig.cs`) — data class for enemy attack definitions loaded from JSON. Fields: cooldown, telegraph time, burst count, spread, aim mode, projectile stats.
- **EnemyAttackRegistry** (`Systems/CombatSystem/EnemyAttackRegistry.cs`) — loads all attack definitions from `Assets/attacks/*.json` at startup.
- **AttackHandler** (`Systems/AiSystem/Handlers/AttackHandler.cs`) — AI behaviour handler that makes enemies shoot. Supports telegraph → fire → cooldown cycle, burst fire, fan spread, aimed and fixed-left aim modes.
- **Enemy telegraph visual** — enemies flash white during telegraph phase to warn the player before firing.
- **`aimed_shot.json`** — first enemy attack config (2.5s cooldown, 0.4s telegraph, aimed at player, speed 350).
- **`fodder_shooter` AI profile** — sine wave movement + aimed shot attack. Added to WaveSpawner's profile pool so some enemies spawn as shooters.
- **Enemy projectile vs player collision** — `CollisionSystem.CheckEnemyProjectileVsPlayer()` detects hits, triggers `player.TakeHit()`, respects invincibility frames. One hit per frame max.
- **`EnemyProjectilePoolSize` constant** (128) in `Constants.cs`.

### Changed
- **AiSystem** constructor now receives `ObjectPool<EnemyProjectile>` and `EnemyAttackRegistry` to pass through to `BehaviourRegistry` → `AttackHandler`.
- **BehaviourRegistry** constructor accepts pool + registry dependencies; registers `AttackHandler` alongside existing handlers.
- **EnemyAiState** struct — added `AttackCooldownTimer`, `TelegraphTimer`, `IsTelegraphing`, `BurstShotsRemaining`, `BurstTimer` fields.
- **AiNodeConfig** — added `AttackId` property for linking behaviour nodes to attack configs.
- **GameState** — initializes enemy projectile pool and attack registry; updates and draws enemy projectiles; runs enemy-projectile-vs-player collision check.
- **WaveSpawner** — added `"fodder_shooter"` to the AI profile pool.

---

## [Unreleased] — Phase 2: Module System Rename + UI

### Added
- **Pause menu** (`Systems/UI/PauseMenu.cs`) — Escape opens a dim overlay with Resume, Inventory, and Exit options. D-pad/arrow navigation, A/Enter to select.
- **Loadout screen** (`Systems/UI/LoadoutScreen.cs`) — "Module Bay" showing 4 weapon slots in a 2×2 grid with module name, category, tap/charged stats, and support slot status. Cursor navigates weapon and support sub-slots.
- **Debug module panel** — when F3 debug overlay is active, pressing A/Enter on any slot in the loadout screen opens a picker listing all registered modules filtered by category (weapon or support). Allows equipping any module or clearing slots for testing combinations.
- **Scene management** — `GameScene` enum (Playing, PauseMenu, Inventory) in `GameState`. Game world freezes while menus are open.
- **Menu input bindings** — `PauseMenuPressed`, `InventoryPressed`, `ConfirmPressed`, `CancelPressed`, `NavigateUp/Down/Left/Right` in `InputManager`. Split by source: Escape → pause menu, controller Start → inventory directly.
- **Unified tap + charge per module** — `ModuleDefinition` carries both `BaseProjectileParameters` (tap) and `ChargedProjectileParameters` (hold-release). Each shot module defines both fire modes in a single JSON. `ModulePipeline.ResolveCharged()` resolves charged params through the same support pipeline.
- **5 support module JSONs** — `Amplifier Coil` (+dmg), `Penetrator Lens` (+pierce), `Velocity Booster` (+speed), `Spread Emitter` (+multishot), `Tracking Beacon` (homing).
- **Exit key override** — `SetExitKey(Null)` so Escape drives the pause menu instead of closing the window.

### Changed
- **Gem → Module rename** — complete codebase rename. `GemSystem/` → `ModuleSystem/`, `GemDefinition` → `ModuleDefinition`, `GemCategory` → `ModuleCategory` (Skill→Weapon, Support stays), `SkillCategory` → `WeaponCategory`, `GemModifiers` → `ModuleModifiers`, `GemModifierPipeline` → `ModulePipeline`, `GemRegistry` → `ModuleRegistry`, `GemSlot` → `ModuleSlot`, `GemSystem` → `ModuleSystem`. JSON folder `Assets/gems/` → `Assets/modules/`. All consumer references updated (Player, Projectile, GameState, LoadoutScreen).
- **Player** — fires both tap and charge from the same active weapon slot (slot 0 by default) via `ActiveWeaponSlot` field. `FireGemBullet` → `FireBullet`.
- **ModuleSystem** — caches both `ResolvedParameters` and `ResolvedChargedParameters` per slot. `HasChargedMode[]` tracks which slots support charge-fire. Default loadout simplified to slot 0 = `shot_normal` only.
- **Program.cs** — main loop checks `gameState.ExitRequested` flag for clean shutdown from pause menu Exit option.

### Removed
- **Entire `Systems/GemSystem/` directory** — replaced by `Systems/ModuleSystem/`.
- **`Assets/gems/` directory** — replaced by `Assets/modules/`.
- **`shot_charged.json`** — charged parameters now live inside `shot_normal.json`.

---

## [Unreleased] — Phase 1: Rarity + Module Data Model

### Added
- **EnemyRarity enum** (`Entities/EnemyRarity.cs`) — `Normal`, `Magic`, `Rare`, `Unique` tiers with per-rarity colors, score multipliers, affix count ranges, and `DemoteOneTier()` for split children.
- **AffixDefinition + AffixModifiers** (`Systems/RaritySystem/`) — data model for enemy modifiers loaded from `Assets/affixes/*.json`. `AffixModifiers` struct supports multiplicative/additive combining with zero allocation.
- **AffixRegistry** (`Systems/RaritySystem/AffixRegistry.cs`) — loads affix JSONs at startup with pre-built per-rarity lookup lists.
- **RarityRoller** (`Systems/RaritySystem/RarityRoller.cs`) — weighted random rarity draw with per-wave escalation (later waves shift toward higher tiers). Span-based affix rolling with incompatibility checks. Builds display names from affix combinations.
- **5 starter affix JSONs** — `fast` (60% speed), `shielded` (5 shield HP), `splitter` (splits on death), `armored` (50% phys reduction), `regenerating` (1 HP/sec).
- **Unique enemy preset** — `the_guardian.json` in `Assets/uniques/`.
- **Module system** (`Systems/ModuleSystem/`) — ship module system with:
  - `ProjectileParameters` struct with `DefaultNormal` / `DefaultCharged` presets.
  - `ModuleModifiers` struct for support module effects.
  - `ModuleDefinition` class with `ModuleCategory` and `WeaponCategory` enums.
  - `ModuleRegistry` loading from `Assets/modules/*.json`.
  - `ModulePipeline` — resolves weapon + support modules into cached `ProjectileParameters`.
  - `PlayerLoadout` — 4 weapon slots × 2 support slots (12 sockets total).
  - `ModuleSystem` — owns registry, loadout, and resolved parameter cache.
- **Starter weapon module JSON** — `shot_normal.json` ("Standard Blaster") with integrated tap + charged parameters.

### Changed
- **Enemy** — gains `Rarity` field and `DisplayName` string. `Draw()` uses rarity color instead of hard-coded `Color.Red`. Magic/Rare/Unique enemies show name label above them.
- **WaveSpawner** — uses `RarityRoller` to assign rarity and affixes on spawn. Tracks wave number; rarity weights escalate each wave. Higher-rarity enemies get more HP. Affix modifiers (speed, shield) applied at spawn.
- **CollisionSystem** — kill score uses `RarityConstants.ScoreMultiplier()` (×2 Magic, ×5 Rare, ×10 Unique).
- **Player** — `Update()` accepts `ModuleSystem` parameter. Fires projectiles using resolved `ProjectileParameters` from module system.
- **Projectile** — new `Spawn(position, ProjectileParameters)` overload. Legacy `Spawn(position, velocity, chargeLevel)` kept for backward compatibility.
- **GameState** — creates and owns `AffixRegistry` and `ModuleSystem`. Passes them to `WaveSpawner.Update()` and `Player.Update()`.

---

## [Unreleased] — Phase 0: Foundation

### Added
- **EnemyHealth struct** (`Entities/EnemyHealth.cs`) — replaces `int Health` on Enemy. Supports base HP + shield layers, damage application with shield-first absorption, and HP/shield regen methods.
- **DamageType enum** (`Systems/CombatSystem/DamageType.cs`) — `NonElemental`, `Energy`, `Fire`, `Cold`. All current weapons use NonElemental; elemental types reserved for module system (Phase 1).
- **DamageEvent struct** (`Systems/CombatSystem/DamageEvent.cs`) — value type describing a single hit (amount, type, bypass-shield flag). Passed through CollisionSystem with zero allocation.
- **DamageNumber pooled entity** (`Entities/DamageNumber.cs`) — floating damage text that drifts upward and fades out. Only shown for multi-HP enemies (fodder dies in 1 hit, no clutter).
- **AI Profile System** (`Systems/AiSystem/`) — JSON-driven enemy AI replacing the `EnemyMovePattern` enum. Each profile is a list of behaviour nodes executed per frame.
  - `IBehaviourHandler` interface for composable behaviours.
  - `BehaviourRegistry` for handler lookup.
  - `AiProfileRegistry` loads profiles from `Assets/ai_profiles/*.json`.
  - `AiContext` read-only struct provides world state to handlers.
  - `EnemyAiState` per-enemy mutable state struct (no heap allocation).
  - Three handlers ported from existing code: `StraightHandler`, `SineHandler`, `ZigzagHandler`.
- **AI profile JSON files** — `straight.json`, `sine_wave.json`, `zigzag.json` in `Assets/ai_profiles/`.
- **Debug overlay** (`Systems/DebugDraw.cs`) — toggled with F3 (keyboard) or both thumbsticks (controller). Shows entity hitboxes, AI profile labels above enemies, and frame time/FPS.
- **Sprite atlas loader** in `AssetManager` — `LoadAtlas()` parses Free Texture Packer JSON format, `GetSourceRect()` provides frame rectangle lookup. Infrastructure-only; no atlas content yet.
- **Health bars** on enemies with `MaxHp > 1` — green HP fill + blue shield overlay. Not visible in Phase 0 (all current enemies are 1 HP).
- **HUD** — player HP and score displayed top-left.

### Changed
- **Enemy class** — uses `EnemyHealth` struct instead of `int Health`, `AiProfileId` + `EnemyAiState` instead of `EnemyMovePattern` enum. Movement logic moved to AI handlers.
- **CollisionSystem** — builds `DamageEvent` from projectile damage, calls `Enemy.TakeDamage()`, spawns `DamageNumber` for multi-HP enemies.
- **GameState** — owns `DamageNumber` pool and `AiSystem`. Passes `AiContext` to enemies each frame. Handles debug overlay toggle.
- **WaveSpawner** — assigns AI profile IDs (`"straight"`, `"sine_wave"`, `"zigzag"`) instead of enum values.
- **CLAUDE.md** — added mandatory push-to-GitHub and PR-before-main workflow rules. Added Art & Visual Placeholders section (geometric shapes acceptable until final art phase).

### Removed
- `EnemyMovePattern` enum — replaced by JSON AI profiles.
