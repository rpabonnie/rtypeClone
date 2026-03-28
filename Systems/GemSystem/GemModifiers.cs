namespace rtypeClone.Systems.GemSystem;

/// <summary>
/// Modifiers applied by support gems to a skill gem's base ProjectileParameters.
/// Flat values add, multipliers multiply. Evaluated once on loadout change, not per frame.
/// </summary>
public struct GemModifiers
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
    public static GemModifiers None => new()
    {
        DamageMultiplier = 1f,
        RadiusMultiplier = 1f,
    };
}
