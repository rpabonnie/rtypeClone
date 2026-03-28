# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased] — Phase 1: Rarity + Gem Data Model

### Added
- **EnemyRarity enum** (`Entities/EnemyRarity.cs`) — `Normal`, `Magic`, `Rare`, `Unique` tiers with per-rarity colors, score multipliers, affix count ranges, and `DemoteOneTier()` for split children.
- **AffixDefinition + AffixModifiers** (`Systems/RaritySystem/`) — data model for enemy modifiers loaded from `Assets/affixes/*.json`. `AffixModifiers` struct supports multiplicative/additive combining with zero allocation.
- **AffixRegistry** (`Systems/RaritySystem/AffixRegistry.cs`) — loads affix JSONs at startup with pre-built per-rarity lookup lists.
- **RarityRoller** (`Systems/RaritySystem/RarityRoller.cs`) — weighted random rarity draw with per-wave escalation (later waves shift toward higher tiers). Span-based affix rolling with incompatibility checks. Builds display names from affix combinations.
- **5 starter affix JSONs** — `fast` (60% speed), `shielded` (5 shield HP), `splitter` (splits on death), `armored` (50% phys reduction), `regenerating` (1 HP/sec).
- **Unique enemy preset** — `the_guardian.json` in `Assets/uniques/`.
- **GemSystem** (`Systems/GemSystem/`) — PoE-style gem skill system with:
  - `ProjectileParameters` struct with `DefaultNormal` / `DefaultCharged` presets.
  - `GemModifiers` struct for support gem effects.
  - `GemDefinition` class with `GemCategory` and `SkillCategory` enums.
  - `GemRegistry` loading from `Assets/gems/*.json`.
  - `GemModifierPipeline` — resolves skill + support gems into cached `ProjectileParameters`.
  - `PlayerLoadout` — 4 skill slots × 2 support slots (12 sockets total).
  - `GemSystem` — owns registry, loadout, and resolved parameter cache.
- **Starter gem JSONs** — `shot_normal.json`, `shot_charged.json`.

### Changed
- **Enemy** — gains `Rarity` field and `DisplayName` string. `Draw()` uses rarity color instead of hard-coded `Color.Red`. Magic/Rare/Unique enemies show name label above them.
- **WaveSpawner** — uses `RarityRoller` to assign rarity and affixes on spawn. Tracks wave number; rarity weights escalate each wave. Higher-rarity enemies get more HP. Affix modifiers (speed, shield) applied at spawn.
- **CollisionSystem** — kill score uses `RarityConstants.ScoreMultiplier()` (×2 Magic, ×5 Rare, ×10 Unique).
- **Player** — `Update()` accepts `GemSystem` parameter. `FireBullet()` replaced by `FireGemBullet()` which reads resolved `ProjectileParameters` from gem system. Slot 0 = tap fire (normal), slot 1 = charge-release (charged).
- **Projectile** — new `Spawn(position, ProjectileParameters)` overload. Legacy `Spawn(position, velocity, chargeLevel)` kept for backward compatibility.
- **GameState** — creates and owns `AffixRegistry` and `GemSystem`. Passes them to `WaveSpawner.Update()` and `Player.Update()`.

---

## [Unreleased] — Phase 0: Foundation

### Added
- **EnemyHealth struct** (`Entities/EnemyHealth.cs`) — replaces `int Health` on Enemy. Supports base HP + shield layers, damage application with shield-first absorption, and HP/shield regen methods.
- **DamageType enum** (`Systems/CombatSystem/DamageType.cs`) — `NonElemental`, `Energy`, `Fire`, `Cold`. All current weapons use NonElemental; elemental types reserved for gem system (Phase 1).
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
