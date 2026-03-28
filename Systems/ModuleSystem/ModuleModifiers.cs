namespace rtypeClone.Systems.ModuleSystem;

/// <summary>
/// Modifiers applied by support modules to a weapon module's base ProjectileParameters.
/// Flat values add, multipliers multiply. Evaluated once on loadout change, not per frame.
/// </summary>
public struct ModuleModifiers
{
    public int   DamageFlat;
    public float DamageMultiplier;
    public int   PierceDelta;
    public bool  HomingOverride;
    public float HomingStrength;
    public float RadiusMultiplier;
    public int   CountDelta;
    public float SpreadAngleDeg;
    public float SpeedFlat;

    /// <summary>Identity modifier — changes nothing when applied.</summary>
    public static ModuleModifiers None => new()
    {
        DamageMultiplier = 1f,
        RadiusMultiplier = 1f,
    };
}
