namespace rtypeClone.Systems.GemSystem;

/// <summary>
/// Applies support gems to a base ProjectileParameters.
/// Called once when the loadout changes (not per-frame).
/// Result is cached in GemSystem.ResolvedParameters[slot].
/// </summary>
public static class GemModifierPipeline
{
    public static ProjectileParameters Resolve(
        GemDefinition skillGem,
        ReadOnlySpan<GemDefinition?> supportGems)
    {
        var p = skillGem.BaseProjectileParameters;

        foreach (var sup in supportGems)
        {
            if (sup == null) continue;

            // Tag compatibility check: support's requiresTags must all be in skill's tags
            if (!TagsCompatible(skillGem.Tags, sup.RequiresTags))
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

    private static void Apply(ref ProjectileParameters p, in GemModifiers m)
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

    private static bool TagsCompatible(string[] skillTags, string[] requiredTags)
    {
        if (requiredTags.Length == 0) return true;

        foreach (var req in requiredTags)
        {
            bool found = false;
            foreach (var tag in skillTags)
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
