namespace rtypeClone.Systems.RaritySystem;

/// <summary>
/// Numeric modifiers applied to an enemy by an affix.
/// All fields default to "no effect" values so unset modifiers are safe.
/// Combined at spawn time, read per-frame — no allocation.
/// </summary>
public struct AffixModifiers
{
    public float SpeedMultiplier;
    public int   ShieldHp;
    public int   SplitsOnDeath;
    public float PhysicalDamageReduction;
    public float HpRegenPerSecond;
    public float DamageMultiplier;
    public int   ProjectileCount;

    /// <summary>Identity: changes nothing when combined.</summary>
    public static AffixModifiers None => new()
    {
        SpeedMultiplier = 1f,
        DamageMultiplier = 1f,
    };

    /// <summary>
    /// Combines another set of modifiers into this one.
    /// Multipliers multiply, flat values add.
    /// </summary>
    public void Combine(in AffixModifiers other)
    {
        SpeedMultiplier *= other.SpeedMultiplier;
        ShieldHp += other.ShieldHp;
        SplitsOnDeath += other.SplitsOnDeath;
        PhysicalDamageReduction = 1f - (1f - PhysicalDamageReduction) * (1f - other.PhysicalDamageReduction);
        HpRegenPerSecond += other.HpRegenPerSecond;
        DamageMultiplier *= other.DamageMultiplier;
        ProjectileCount += other.ProjectileCount;
    }
}
