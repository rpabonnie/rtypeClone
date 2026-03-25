# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- Project repository initialized with LICENSE
- Research document: game framework selection (research-0001)
  - Evaluated Raylib-cs, MonoGame, Godot C#, Unity, and FNA
  - Recommended Raylib-cs for CLI-friendly workflow, minimal boilerplate, and NuGet availability
- This changelog
- spec-0001: Gem Skill System — PoE-style 4-slot gem system replacing hard-coded shots; GemRegistry,
  GemModifierPipeline, PlayerLoadout, ProjectileParameters struct, JSON schema for gem definitions
- spec-0002: Enemy Rarity System — Normal/Magic/Rare/Unique tiers; AffixDefinition, AffixRegistry,
  RarityRoller, color constants, score multipliers, affix incompatibility rules, Unique preset JSON
- spec-0003: Enemy HP System — EnemyHealth struct with shield layers, DamageEvent/DamageType value
  types, pooled floating DamageNumbers, conditional health bars for Rare/Unique enemies
- spec-0004: Enemy AI Profile System — file-based JSON AI profiles replacing EnemyMovePattern enum;
  IBehaviourHandler, EnemyAiState, AiContext, BehaviourRegistry; 8 built-in node types including
  straight, sine, zigzag (ported), charge, retreat, formation, shoot_at_player, shield_ally
- spec-0005: Drop Table System — weighted random gem drops by enemy rarity; JSON drop tables,
  guaranteed Unique drops, DroppedGem pooled entity, GemInventory on Player
- spec-0006: Ship Loadout Socket System — between-level 4×3 socket grid UI; controller + keyboard
  navigation, gem swap with incompatibility feedback, resolved stats tooltip, save/load to JSON
- spec-0007: Tooling & Development Pipeline — framework confirmation (Raylib-cs stays), art pipeline
  (Krita + Free Texture Packer for hand-drawn art), VS Code JSON schemas for data editing,
  ImGui.NET level editor plan, AI debug overlay, four-phase development order (Phase 0–3)
