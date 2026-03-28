# Changelog

All notable changes to this project will be documented in this file.

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
