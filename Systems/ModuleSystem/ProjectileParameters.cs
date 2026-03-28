namespace rtypeClone.Systems.ModuleSystem;

/// <summary>
/// Value type describing how a projectile behaves.
/// Resolved once when the loadout changes, cached, and read per-fire — no per-frame allocation.
/// </summary>
public struct ProjectileParameters
{
    public float Speed;
    public float Width;
    public float Height;
    public int   Damage;
    public int   Pierce;
    public bool  Homing;
    public float HomingStrength;
    public float RadiusMultiplier;
    public int   Count;
    public float SpreadAngleDeg;

    /// <summary>Convenience: returns default parameters matching the old hard-coded normal shot.</summary>
    public static ProjectileParameters DefaultNormal => new()
    {
        Speed = 800f,
        Width = 12f,
        Height = 4f,
        Damage = 1,
        Pierce = 0,
        Homing = false,
        HomingStrength = 0f,
        RadiusMultiplier = 1f,
        Count = 1,
        SpreadAngleDeg = 0f
    };

    /// <summary>Convenience: returns default parameters matching the old hard-coded charged shot.</summary>
    public static ProjectileParameters DefaultCharged => new()
    {
        Speed = 600f,
        Width = 32f,
        Height = 16f,
        Damage = 3,
        Pierce = 0,
        Homing = false,
        HomingStrength = 0f,
        RadiusMultiplier = 1f,
        Count = 1,
        SpreadAngleDeg = 0f
    };
}
