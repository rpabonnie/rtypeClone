# Spec: Enemy HP System

| Field    | Value                            |
|----------|----------------------------------|
| ID       | spec-0003                        |
| Status   | Implemented                      |
| Author   | rpabo                            |
| Created  | 2026-03-25                       |
| Updated  | 2026-03-30                       |

## Implementation Note

Fully implemented: `EnemyHealth` struct (base HP + shield), `DamageType` enum, `DamageEvent` value type, `Enemy.TakeDamage()`, pooled `DamageNumber` entities with color-by-type and fade-out, health and shield bars rendered for all enemies with `MaxHp > 1`. All wired through `CollisionSystem`. Shield regeneration and elemental resistance/weakness remain deferred to a future spec.

## Overview

Replace the current `public int Health` field on `Enemy` with a richer `EnemyHealth` struct. The new system supports shield layers that absorb damage before base HP, damage types for future resistance/weakness mechanics, and a pooled floating damage number renderer. Health bars appear only for Rare and Unique enemies.

## Goals

- `EnemyHealth` struct with base HP + optional shield layer.
- `DamageEvent` value type passed from `CollisionSystem` to `Enemy.TakeDamage()`.
- `DamageType` enum for future resistance/weakness hooks.
- Pooled `DamageNumber` entities for floating text on hit.
- Conditional health bars for Rare and Unique enemies.
- No per-frame allocation in the damage or rendering paths.

## Non-Goals

- Player HP refactor (player health remains `int Health` for now).
- Elemental resistances (defined by `DamageType` but not consumed until a future spec).
- Damage-over-time / poison effects.

---

## Data Model

### DamageType

```csharp
// Systems/CombatSystem/DamageType.cs
public enum DamageType
{
    NonElemental, // standard lasers and charged shots (default)
    Energy,       // future beam weapons
    Fire,         // future fire gems
    Cold,         // future cold gems
}
```

### DamageEvent

```csharp
// Systems/CombatSystem/DamageEvent.cs
/// <summary>
/// Describes a single hit. Passed as a value type — no heap allocation.
/// </summary>
public readonly struct DamageEvent
{
    public readonly int        Amount;
    public readonly DamageType Type;
    public readonly bool       BypassShield; // true for pierce/armor-penetrating shots

    public DamageEvent(int amount, DamageType type = DamageType.NonElemental, bool bypassShield = false)
    {
        Amount       = amount;
        Type         = type;
        BypassShield = bypassShield;
    }
}
```

### EnemyHealth

```csharp
// Entities/EnemyHealth.cs
/// <summary>
/// Value type. Enemy carries one of these as a field.
/// Mutated in-place via ref — no boxing.
/// </summary>
public struct EnemyHealth
{
    public int MaxHp;
    public int CurrentHp;
    public int ShieldMax;     // 0 = no shield layer
    public int ShieldCurrent;

    public bool IsAlive      => CurrentHp > 0;
    public bool HasShield    => ShieldCurrent > 0;
    public float HpPercent   => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;
    public float ShieldPercent => ShieldMax > 0 ? (float)ShieldCurrent / ShieldMax : 0f;

    public static EnemyHealth Create(int hp, int shield = 0) => new EnemyHealth
    {
        MaxHp         = hp,
        CurrentHp     = hp,
        ShieldMax     = shield,
        ShieldCurrent = shield,
    };

    /// <summary>
    /// Applies a DamageEvent. Returns actual damage dealt to base HP (after shield).
    /// </summary>
    public int ApplyDamage(DamageEvent dmg)
    {
        if (HasShield && !dmg.BypassShield)
        {
            int shieldDmg = Math.Min(ShieldCurrent, dmg.Amount);
            ShieldCurrent -= shieldDmg;
            int overflow   = dmg.Amount - shieldDmg;
            if (overflow > 0)
                CurrentHp = Math.Max(0, CurrentHp - overflow);
            return overflow;
        }
        int dealt = Math.Min(CurrentHp, dmg.Amount);
        CurrentHp -= dealt;
        return dealt;
    }

    /// <summary>Restores shield (e.g., from regenerating affix).</summary>
    public void RegenShield(int amount) =>
        ShieldCurrent = Math.Min(ShieldMax, ShieldCurrent + amount);

    /// <summary>Regenerates HP (e.g., from regenerating affix).</summary>
    public void RegenHp(int amount) =>
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
}
```

---

## Enemy Integration

`Enemy` fields change from:

```csharp
public int Health;
```

to:

```csharp
public EnemyHealth Health;
```

`Enemy.Spawn()` initialises health:

```csharp
// shieldHp comes from combined affix modifiers (spec-0002)
Health = EnemyHealth.Create(hp, shieldHp);
```

`Enemy.TakeDamage(DamageEvent dmg)` replaces raw health subtraction:

```csharp
public int TakeDamage(DamageEvent dmg) => Health.ApplyDamage(dmg);
```

`CollisionSystem` builds the `DamageEvent` from `ProjectileParameters.Damage` (resolved by `GemModifierPipeline` — spec-0001) and calls `Enemy.TakeDamage()`.

---

## Floating Damage Numbers

### DamageNumber (pooled entity)

```csharp
// Entities/DamageNumber.cs
public class DamageNumber : Entity
{
    private string _text;       // pre-formatted, updated on Activate only
    private float  _lifetime;
    private float  _maxLifetime;
    private Color  _color;
    private float  _velocity;   // upward drift speed

    public void Activate(Vector2 position, int amount, DamageType type)
    {
        Position     = position;
        _text        = amount.ToString(); // one-time allocation per activation — pooled, so amortized
        _color       = ColorForType(type);
        _lifetime    = 0f;
        _maxLifetime = 0.8f;
        _velocity    = 60f;
        Active       = true;
    }

    public override void Update(float dt)
    {
        _lifetime  += dt;
        Position.Y -= _velocity * dt;
        if (_lifetime >= _maxLifetime) Active = false;
    }

    public override void Draw()
    {
        if (!Active) return;
        float alpha = 1f - (_lifetime / _maxLifetime);
        byte  a     = (byte)(255 * alpha);
        Raylib.DrawText(_text, (int)Position.X, (int)Position.Y, 16, new Color(_color.R, _color.G, _color.B, a));
    }

    private static Color ColorForType(DamageType t) => t switch
    {
        DamageType.NonElemental => Color.White,
        DamageType.Energy   => new Color((byte)100, (byte)200, (byte)255, (byte)255),
        DamageType.Fire     => Color.Orange,
        DamageType.Cold     => Color.SkyBlue,
        _ => Color.White
    };
}
```

### Pool Integration

`GameState` adds:

```csharp
private readonly ObjectPool<DamageNumber> _damageNumberPool;
```

Pool size: `Constants.DamageNumberPoolSize = 64`.

`CollisionSystem.CheckCollisions()` receives the pool and calls `SpawnDamageNumber()` on hit.

---

## Health Bar Rendering

Health bars render for any enemy with `MaxHp > 1` (i.e., not fodder). This covers Rare/Unique enemies and any enemy with bonus HP from affixes. Fodder dies in one hit and needs no bar. Drawn in `Enemy.Draw()`:

```csharp
private void DrawHealthBar()
{
    const float BarWidth  = 48f;
    const float BarHeight = 5f;
    float barY = Position.Y - BarHeight - 4f;

    // Background
    Raylib.DrawRectangleV(new Vector2(Position.X, barY), new Vector2(BarWidth, BarHeight), Color.DarkGray);

    // HP fill
    float hpW = BarWidth * Health.HpPercent;
    Raylib.DrawRectangleV(new Vector2(Position.X, barY), new Vector2(hpW, BarHeight), Color.Green);

    // Shield fill (drawn on top)
    if (Health.HasShield)
    {
        float shW = BarWidth * Health.ShieldPercent;
        Raylib.DrawRectangleV(
            new Vector2(Position.X, barY),
            new Vector2(shW, BarHeight),
            new Color((byte)100, (byte)180, (byte)255, (byte)200));
    }
}
```

---

## HP Regen (Affix Integration)

Enemies with the `regenerating` affix (spec-0002) call:

```csharp
Health.RegenHp((int)(_combinedModifiers.HpRegenPerSecond * dt + _regenAccumulator));
```

via an accumulator float to avoid fractional HP loss between frames.

---

## File Locations

```
Entities/
  EnemyHealth.cs
  DamageNumber.cs
Systems/
  CombatSystem/
    DamageEvent.cs
    DamageType.cs
```

---

## Open Questions

1. Should shield regenerate after a delay (like PoE ES) or only via affixes?
2. Does `DamageNumber` show the word "SHIELD" or a distinct color for shield hits vs HP hits?
3. Should `DamageType` affect existing enemies in Phase 0, or only after gem types are implemented?
