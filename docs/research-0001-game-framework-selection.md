# Research: C# 2D Game Framework Selection for R-Type Clone

| Field       | Value                                      |
|-------------|--------------------------------------------|
| ID          | research-0001                              |
| Status      | Draft                                      |
| Author      | rpabo                                      |
| Created     | 2026-03-23                                 |
| Updated     | 2026-03-23                                 |
| Decision    | **Raylib-cs** (recommended)                |

## Context

We need a C# game framework for building a native Windows 2D side-scrolling space shooter (R-Type clone). The project is maintained by a solo developer who prefers a CLI-driven workflow (no mandatory GUI editors). The game requires Xbox controller support, smooth 2D scrolling, sprite rendering, and particle effects.

## Evaluation Criteria

1. **Xbox controller support** - Built-in gamepad API, XInput compatibility
2. **Simplicity / minimal boilerplate** - How quickly can you get a sprite on screen
3. **CLI-friendly** - Can you build and run entirely from the command line without a GUI editor
4. **NuGet package availability** - Available as a NuGet package for standard .NET projects
5. **Performance for 2D** - Rendering performance for a side-scroller with many sprites/bullets
6. **Learning curve** - Time to productivity for a solo developer
7. **Community and documentation** - Quality of docs, examples, and community support

---

## Option 1: Raylib-cs (C# bindings for Raylib)

Raylib-cs provides C# bindings for Raylib, a simple and easy-to-use C library for game programming. It wraps the native Raylib library via P/Invoke.

### Pros
- **Extremely minimal boilerplate.** A complete game loop with window, input, and rendering fits in ~30 lines of code. No engine ceremony, no scene graphs, no mandatory abstractions.
- **First-class gamepad support.** Raylib wraps XInput directly. `Raylib.IsGamepadButtonPressed()` and `Raylib.GetGamepadAxisMovement()` work out of the box with Xbox controllers.
- **Fully CLI-driven.** It is a plain NuGet package (`Raylib-cs`). Create a project with `dotnet new console`, add the package, and `dotnet run`. No editor, no project wizard, no IDE dependency.
- **Available on NuGet.** `dotnet add package Raylib-cs` pulls in the managed bindings and the native Raylib binary for your platform.
- **Excellent 2D performance.** Raylib uses OpenGL under the hood. Sprite batching, texture atlases, and efficient draw calls handle thousands of bullets and enemies easily.
- **Low learning curve.** The API is procedural and immediate-mode. If you can write a `while` loop, you can write a Raylib game. Hundreds of standalone examples exist.
- **Active development.** Raylib (upstream C library) is actively maintained by Ramon Santamaria. The C# bindings track upstream closely.

### Cons
- **No built-in ECS or scene management.** You architect everything yourself. For a focused project like an R-Type clone this is fine; for a large RPG it would be more work.
- **Smaller C# community.** The Raylib community is large, but most examples are in C. C# examples exist but are fewer.
- **P/Invoke layer.** The bindings call into native code. Debugging across the managed/native boundary can occasionally be tricky.
- **No built-in asset pipeline.** No content processor like MonoGame's MGCB. You load files directly (PNG, WAV, OGG), which is simpler but less optimized for large asset sets.

---

## Option 2: MonoGame

MonoGame is the open-source successor to Microsoft's XNA Framework. It is a mature, well-established framework for 2D and 3D game development in C#.

### Pros
- **Mature and battle-tested.** Ships like Celeste, Stardew Valley, and many others were built with MonoGame/XNA.
- **Strong gamepad support.** `GamePad.GetState(PlayerIndex.One)` provides XInput support inherited from XNA. Xbox controllers work natively.
- **NuGet available.** `dotnet new mgdesktopgl` (via MonoGame templates) or direct NuGet packages. CLI project creation is supported.
- **Content pipeline (MGCB).** Built-in tool for processing and optimizing textures, audio, fonts, and effects. Useful for larger projects.
- **Large C# community.** Extensive tutorials, books, and community resources specifically for C# game dev.
- **Cross-platform.** Targets Windows, Linux, macOS, consoles, and mobile with the same codebase.

### Cons
- **More boilerplate than Raylib.** Requires a `Game` subclass, `LoadContent`, `Update`, `Draw` lifecycle methods, and a content pipeline setup even for simple projects.
- **Content pipeline friction.** MGCB (MonoGame Content Builder) has a GUI editor (MGCB Editor) that many workflows depend on. It can be run from CLI but adds complexity.
- **Heavier setup.** Installing templates, understanding the content pipeline, and configuring the project takes more upfront time.
- **Moderate learning curve.** The XNA pattern (Game/SpriteBatch/Content) requires understanding several abstractions before you can render a sprite.
- **Development pace has slowed.** MonoGame updates are less frequent than in its early years, though the project remains maintained.

---

## Option 3: Godot with C#

Godot is a full game engine with an integrated editor, scene system, and physics. It supports C# via .NET integration (Godot 4.x uses .NET 6+).

### Pros
- **Full-featured engine.** Built-in physics, collision detection, animation system, particle systems, tilemap editor, and audio bus system out of the box.
- **C# support via .NET.** Godot 4.x supports C# as a first-class scripting language alongside GDScript.
- **Gamepad support.** Godot's Input system maps Xbox controllers with configurable input actions. Works well once configured.
- **Large and growing community.** Godot has seen explosive growth. Extensive docs, tutorials, and community projects.
- **Free and open source.** MIT licensed, no royalties, no strings attached.

### Cons
- **Editor-centric workflow.** Godot is fundamentally designed around its GUI editor. Scenes, nodes, and resources are edited visually. While you can script in C#, the editor is practically mandatory for scene composition, input mapping, and resource management.
- **Not CLI-friendly.** You cannot effectively develop a Godot game without the Godot editor. Headless/CLI builds exist for export, but development requires the GUI. This is a major drawback for a CLI-driven workflow.
- **Not a NuGet package.** Godot is a standalone engine binary, not a library you add to a .NET project. Your project lives inside Godot's project structure.
- **C# is second-class.** GDScript has better documentation, more examples, and faster iteration. C# support occasionally lags behind GDScript features and has had stability issues.
- **Heavyweight for a side-scroller.** The full engine (scene tree, signals, nodes) adds conceptual overhead that is unnecessary for a focused 2D shooter.
- **Export complexity.** Exporting a native Windows build requires export templates and configuration through the editor.

---

## Option 4: Unity

Unity is the most widely used commercial game engine, supporting C# scripting with a massive ecosystem of tools and assets.

### Pros
- **Massive ecosystem.** Asset Store, thousands of tutorials, large professional community, and extensive documentation.
- **Excellent gamepad support.** Unity's new Input System package provides robust Xbox controller support with action maps and rebinding.
- **Proven 2D capabilities.** Sprite renderer, 2D physics, tilemap system, animation, and particle system built in.
- **Industry standard.** Knowledge transfers to professional game development. Many shipped 2D games use Unity.

### Cons
- **Mandatory GUI editor.** Unity is entirely editor-driven. You cannot create, build, or iterate on a Unity project without the Unity Editor. CLI builds exist (`-batchmode`) but development without the editor is not practical.
- **Not CLI-friendly at all.** Project creation, scene editing, component configuration, prefab management, and testing all require the Unity Editor GUI. This is the worst option for a CLI workflow.
- **Not a NuGet package.** Unity is a standalone engine with its own package manager (UPM). It does not integrate with standard .NET tooling.
- **Heavy and slow.** The editor is resource-intensive. Project open times, domain reloads on script changes, and general editor overhead are significant for a solo developer on a simple 2D game.
- **Licensing concerns.** Unity's pricing and licensing changes (the 2023 Runtime Fee controversy) have eroded community trust. While largely walked back, the precedent remains.
- **Massive overkill.** Unity's 3D-first architecture, DOTS, render pipelines, and package system add enormous complexity for a 2D side-scroller.
- **Opaque project structure.** Unity projects contain many auto-generated files, Library folders, and metadata that do not play well with version control or CLI inspection.

---

## Option 5: FNA

FNA is a reimplementation of XNA 4.0 focused on accuracy and preservation. It is an alternative to MonoGame with different design goals.

### Pros
- **Pixel-perfect XNA compatibility.** If you know XNA, you know FNA. API-identical to XNA 4.0.
- **Excellent preservation focus.** Used by professional ports (Celeste, TowerFall, Stardew Valley console ports by Ethan Lee).
- **Gamepad support.** Same XNA GamePad API as MonoGame. SDL2 backend provides solid controller support.
- **Simpler than MonoGame.** No content pipeline required; loads raw assets directly. Closer to Raylib's simplicity in this regard.

### Cons
- **Not on NuGet.** FNA is distributed as source code via GitHub. You clone the repo and reference it directly. No `dotnet add package`.
- **Smaller community than MonoGame.** Fewer tutorials and community resources. Documentation is thinner.
- **SDL2 dependency.** Requires SDL2 native libraries, which must be managed separately.
- **Less actively developed for new features.** FNA's goal is preservation, not new features.

---

## Comparison Matrix

| Criterion                    | Raylib-cs | MonoGame | Godot C# | Unity  | FNA    |
|------------------------------|-----------|----------|-----------|--------|--------|
| Xbox controller support      | ✅        | ✅       | ✅        | ✅     | ✅     |
| Simplicity / minimal boiler  | ✅        | ⚠️       | ⚠️        | ❌     | ⚠️     |
| CLI-friendly (no GUI needed) | ✅        | ✅       | ❌        | ❌     | ✅     |
| NuGet package available      | ✅        | ✅       | ❌        | ❌     | ❌     |
| 2D performance               | ✅        | ✅       | ✅        | ✅     | ✅     |
| Learning curve (low = good)  | ✅        | ⚠️       | ⚠️        | ⚠️     | ⚠️     |
| Community / docs             | ⚠️        | ✅       | ✅        | ✅     | ❌     |

### Legend
- ✅ Excellent / fully meets criteria
- ⚠️ Adequate / meets criteria with caveats
- ❌ Poor / does not meet criteria

---

## Recommendation

**Raylib-cs** is the recommended framework for this project.

### Rationale

1. **CLI-first workflow.** Raylib-cs is the only option that is both a NuGet package and requires zero GUI tooling. `dotnet new console` + `dotnet add package Raylib-cs` + `dotnet run` gets you from nothing to a running game window in under a minute. This aligns perfectly with a CLI-driven, editor-agnostic workflow.

2. **Minimal boilerplate.** For a focused project like an R-Type clone, Raylib's procedural API means you write game logic, not engine glue. There is no `Game` base class, no scene tree, no component system to learn. You write a game loop and call draw functions.

3. **Xbox controller support is trivial.** Raylib's gamepad API wraps XInput directly. Checking button states and axis values is a single function call with no configuration or input mapping setup.

4. **Right-sized for the project.** An R-Type clone needs sprite rendering, input handling, collision detection, audio, and a game loop. Raylib provides exactly these primitives without the overhead of a full engine. You build the game architecture you need, nothing more.

5. **Performance is not a concern.** Raylib's OpenGL backend handles the rendering demands of a 2D side-scroller (scrolling backgrounds, dozens of enemies, hundreds of bullets, particle effects) with ease.

### Trade-offs accepted

- We will implement our own entity management, collision detection, and scene/state management. For an R-Type clone, this is straightforward and gives us full control.
- C#-specific Raylib examples are fewer than MonoGame examples, but the C examples translate almost 1:1 and the API is self-documenting.

### Rejected alternatives

- **MonoGame** is a strong second choice but adds unnecessary complexity (content pipeline, XNA lifecycle) for this project scope.
- **Godot** and **Unity** are rejected primarily because they require GUI editors, which conflicts with the CLI-driven workflow requirement.
- **FNA** lacks NuGet distribution and has a smaller community, making it less practical despite being technically capable.

---

## Next Steps

1. Create a new .NET console project with Raylib-cs
2. Validate Xbox controller input with a minimal prototype
3. Implement a basic scrolling background and sprite rendering test
4. Establish the project architecture (game loop, entity system, state management)

---

## References

- Raylib (upstream): https://www.raylib.com/
- Raylib-cs NuGet: https://www.nuget.org/packages/Raylib-cs
- Raylib-cs GitHub: https://github.com/ChristopherPHill/Raylib-cs
- MonoGame: https://www.monogame.net/
- Godot Engine: https://godotengine.org/
- Unity: https://unity.com/
- FNA: https://fna-xna.github.io/
