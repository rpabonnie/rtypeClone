namespace rtypeClone.Systems.ModuleSystem;

/// <summary>
/// Applies support modules to a base ProjectileParameters.
/// Called once when the loadout changes (not per-frame).
/// Result is cached in ModuleSystem.ResolvedParameters[slot].
/// </summary>
public static class ModulePipeline
{
    public static ProjectileParameters ResolveCharged(
        ModuleDefinition weaponModule,
        ReadOnlySpan<ModuleDefinition?> supportModules)
    {
        return ResolveFromBase(weaponModule.ChargedProjectileParameters, weaponModule.Tags, supportModules);
    }

    public static ProjectileParameters Resolve(
        ModuleDefinition weaponModule,
        ReadOnlySpan<ModuleDefinition?> supportModules)
    {
        return ResolveFromBase(weaponModule.BaseProjectileParameters, weaponModule.Tags, supportModules);
    }

    private static ProjectileParameters ResolveFromBase(
        ProjectileParameters baseParams,
        string[] weaponTags,
        ReadOnlySpan<ModuleDefinition?> supportModules)
    {
        var p = baseParams;

        foreach (var sup in supportModules)
        {
            if (sup == null) continue;

            // Tag compatibility check: support's requiresTags must all be in weapon's tags
            if (!TagsCompatible(weaponTags, sup.RequiresTags))
                continue;

            var mods = sup.Modifiers;
            Apply(ref p, in mods);
        }

        // Clamp to sane minimums
        if (p.Damage < 1) p.Damage = 1;
        if (p.Count < 1) p.Count = 1;
        if (p.Speed < 50f) p.Speed = 50f;

        return p;
    }

    private static void Apply(ref ProjectileParameters p, in ModuleModifiers m)
    {
        // Flat additions first, then multipliers
        p.Damage += m.DamageFlat;
        p.Damage = (int)(p.Damage * m.DamageMultiplier);

        p.Pierce += m.PierceDelta;
        p.Count += m.CountDelta;
        p.Speed += m.SpeedFlat;

        if (m.HomingOverride)
        {
            p.Homing = true;
            p.HomingStrength = MathF.Max(p.HomingStrength, m.HomingStrength);
        }

        p.RadiusMultiplier *= m.RadiusMultiplier;
        p.SpreadAngleDeg += m.SpreadAngleDeg;
    }

    private static bool TagsCompatible(string[] weaponTags, string[] requiredTags)
    {
        if (requiredTags.Length == 0) return true;

        foreach (var req in requiredTags)
        {
            bool found = false;
            foreach (var tag in weaponTags)
            {
                if (string.Equals(tag, req, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }
}
