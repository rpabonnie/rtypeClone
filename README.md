# R-Type Clone

A native Windows 2D side-scrolling space shooter inspired by the classic 90s arcade game R-Type. Built with C#, .NET 10, and Raylib-cs.

## About This Project

This is a **learning project** by a novice in game design. The goal is to explore game design fundamentals, architecture, and gameplay mechanics through hands-on development. We're not aiming for a blockbuster—we're aiming to learn, iterate, and create something fun along the way.

**Good ideas, thoughtful direction, and guidance are always welcome.** If you're interested in contributing or have suggestions, jump in!

## Immediate Goals

### Enemy Levels & Progression
- **Basic Enemies** — Standard fodder with straightforward behavior
- **Elite Enemies** — More challenging variants with upgraded capabilities
- **Bosses** — Major encounters that mark level completion

### Victory Conditions
- Defeat a required number of enemies throughout the level
- Survive and defeat the level boss to advance

### Life System
- **3 lives** per run, **3 hits per life**
- Enemy collisions destroy most enemy types on impact (except elites and bosses)
- **Invincibility frames** (3–5 frames) after taking damage to prevent instant death

### Weapon Variety & Power-ups
- Multiple weapon types (standard shot, charged shot, and variants)
- Power-up system for both regular and charging modes
- Collectible items to enhance player capabilities

## Tech Stack

- **Language:** C# (.NET 10)
- **Framework:** [Raylib-cs](https://www.nuget.org/packages/Raylib-cs)
- **Input:** Xbox controller (primary), keyboard/mouse (secondary)
- **Target Resolution:** 1920×1080

See [CLAUDE.md](CLAUDE.md) for detailed architecture rules, coding standards, and design decisions.

## Quick Start

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

### Clean
```bash
dotnet clean
```

## Project Structure

```
rtypeClone/
  Program.cs              # Entry point: window init, main loop
  Core/                   # Core systems: game state, input, object pools
  Entities/               # Player, enemies, projectiles, bosses, power-ups
  Systems/                # Collision, spawning, background, audio
  Assets/                 # Sprites and audio files
```

See [CLAUDE.md](CLAUDE.md) for the full architecture documentation.

## Contributing

This is an open learning space. Whether you're:
- Fixing bugs
- Adding features (enemies, weapons, power-ups)
- Improving game feel (balance, feedback, juice)
- Suggesting design direction

...your contributions and ideas are welcome. Please check [CLAUDE.md](CLAUDE.md) for coding standards and architecture guidelines before submitting changes.

## License

[Add your preferred license here]
