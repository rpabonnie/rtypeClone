# Changelog

All notable changes to this project will be documented in this file.

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
