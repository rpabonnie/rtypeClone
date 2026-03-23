# CLAUDE.md — R-Type Clone Project Instructions

## Project Overview

Native Windows 2D side-scrolling space shooter inspired by 90s R-Type.
Primary input: Xbox controller. Secondary: keyboard/mouse. Both must always work.
Target resolution: 1920x1080 (no retro/pixel-art scaling mode).

## Tech Stack

- **Language:** C# (.NET 10)
- **Framework:** [Raylib-cs](https://www.nuget.org/packages/Raylib-cs) NuGet package
- **No GUI tools:** Unity, Godot, MonoGame Content Pipeline, and any GUI editors are **not used**. This is a pure code + CLI project.
- **Decision record:** See `docs/research-0001-game-framework-selection.md` and `docs/adr/` for rationale.

## Commands

```bash
dotnet build          # Compile the project
dotnet run            # Build and run the game
dotnet clean          # Remove build artifacts
dotnet add package <name>  # Add a NuGet dependency
```

## Architecture Rules

### Game Loop

Enforce a clean separation between logic and rendering:

```
while (!WindowShouldClose)
{
    Update(deltaTime);   // All game logic, input, physics, collisions
    BeginDrawing();
    Draw();              // All rendering — NO logic here
    EndDrawing();
}
```

- `Update()` receives delta time and handles **all** state changes: input, movement, spawning, collisions, scoring.
- `Draw()` reads current state and renders. It must **never** mutate game state.
- Use `Raylib.GetFrameTime()` for delta time. Never assume a fixed frame rate.

### Input

- **Xbox controller is the primary input device.** All gameplay must be fully playable with a standard Xbox controller (XInput). UI prompts should show controller buttons by default.
- Use `Raylib.IsGamepadAvailable()`, `Raylib.IsGamepadButtonPressed()`, `Raylib.GetGamepadAxisMovement()`.
- Keyboard/mouse must also work at all times. Wrap input behind an abstraction so adding either input path is seamless.
- When both are connected, the most recently used device dictates UI button prompts.

### Performance — Minimize GC Pressure

Inside the game loop (Update + Draw), follow these rules strictly:

1. **No allocations per frame.** Do not use `new` for classes, do not create temporary lists, do not concatenate strings with `+` inside the loop.
2. **Object pooling** for all frequently created/destroyed objects: bullets, enemies, particles, explosions. Pre-allocate pools at startup.
3. **Use structs** for small, short-lived data (vectors, rects, collision results).
4. **Pre-allocate collections.** Size lists/arrays at init, reuse them. Use array indexing over LINQ in hot paths.
5. **Cache formatted strings.** If displaying score/health, update the cached string only when the value changes.

### Entity Structure

Keep entities in separate files under a clear folder structure. Do **not** let `Program.cs` grow into a monolith.

```
rtypeClone/
  Program.cs              # Entry point only: window init, main loop, shutdown
  Core/
    GameState.cs          # Central game state, scene management
    InputManager.cs       # Controller + keyboard abstraction
    ObjectPool.cs         # Generic object pool implementation
  Entities/
    Entity.cs             # Base class or interface for all game objects
    Player.cs             # Player ship: movement, shooting, power-ups
    Enemy.cs              # Enemy types and behavior patterns
    Projectile.cs         # Bullets, missiles, beams (pooled)
    Boss.cs               # Boss enemies (unique behavior, health bars)
    PowerUp.cs            # Collectible items
    Hazard.cs             # Environmental hazards (planned, add later)
  Systems/
    CollisionSystem.cs    # Collision detection and resolution
    ScrollingBackground.cs# Parallax background layers
    WaveSpawner.cs        # Enemy wave patterns and timing
    AudioManager.cs       # Sound effects and music
  Assets/                 # Sprites, sounds, music (loaded directly, no pipeline)
```

**Key principles:**
- Each entity manages its own `Update()` and `Draw()` methods.
- `Program.cs` stays thin — it initializes systems and runs the loop.
- Planned entity types: bosses (near-term), power-ups (near-term), environmental hazards (later). Each gets its own file in `Entities/`.
- Systems operate on collections of entities, not individual ones.

## Coding Standards

- Use `float` for all game math (positions, velocities, timers). Avoid `double`.
- Use `System.Numerics.Vector2` or Raylib's built-in vector types for positions/velocities.
- Constants (screen size, speeds, pool sizes) go in a static `Constants.cs` or at the top of the relevant class — not scattered as magic numbers.
- Keep methods short. If a method exceeds ~40 lines, it probably needs splitting.

## What NOT to Do

- Do not add Unity, Godot, or any engine with a mandatory GUI editor.
- Do not put game logic inside `Draw()`.
- Do not allocate inside the hot loop (no `new List<>()`, no string concatenation, no LINQ).
- Do not let `Program.cs` become the dumping ground for all game code.
- Do not hardcode input to keyboard-only. Controller must always work.
