namespace rtypeClone.Systems.GemSystem;

/// <summary>
/// Owns the gem registry, player loadout, and resolved parameter cache.
/// GameState holds one GemSystem instance.
/// </summary>
public class GemSystem
{
    public GemRegistry   Registry { get; }
    public PlayerLoadout Loadout  { get; }

    /// <summary>
    /// Cached resolved parameters — updated on loadout change, not per frame.
    /// Index = skill slot index (0..3).
    /// </summary>
    public readonly ProjectileParameters[] ResolvedParameters =
        new ProjectileParameters[PlayerLoadout.SkillSlotCount];

    // Scratch array to avoid allocating when resolving supports
    private readonly GemDefinition?[] _supportScratch = new GemDefinition?[PlayerLoadout.SupportSlotCount];

    public GemSystem(string gemsDirectory)
    {
        Registry = new GemRegistry(gemsDirectory);
        Loadout = new PlayerLoadout();

        // Default loadout: slot 0 = normal shot, slot 1 = charged shot
        if (Registry.TryGet("shot_normal", out _))
            Loadout.TryEquipSkill(0, "shot_normal", Registry);
        if (Registry.TryGet("shot_charged", out _))
            Loadout.TryEquipSkill(1, "shot_charged", Registry);

        RebuildCache();
    }

    /// <summary>
    /// Rebuilds the ResolvedParameters cache from the current loadout.
    /// Call this whenever the loadout changes (not per frame).
    /// </summary>
    public void RebuildCache()
    {
        for (int slot = 0; slot < PlayerLoadout.SkillSlotCount; slot++)
        {
            var skillId = Loadout.SkillGems[slot].GemId;
            if (skillId == null || !Registry.TryGet(skillId, out var skillDef) || skillDef == null)
            {
                ResolvedParameters[slot] = default;
                continue;
            }

            // Gather support gems for this slot
            for (int s = 0; s < PlayerLoadout.SupportSlotCount; s++)
            {
                var supId = Loadout.SupportGems[slot, s].GemId;
                if (supId != null && Registry.TryGet(supId, out var supDef))
                    _supportScratch[s] = supDef;
                else
                    _supportScratch[s] = null;
            }

            ResolvedParameters[slot] = GemModifierPipeline.Resolve(
                skillDef, new ReadOnlySpan<GemDefinition?>(_supportScratch));
        }
    }

    /// <summary>
    /// Returns resolved parameters for a skill slot.
    /// Returns default (zero) if the slot is empty.
    /// </summary>
    public ref readonly ProjectileParameters GetActive(int slot)
    {
        if (slot < 0 || slot >= PlayerLoadout.SkillSlotCount)
            return ref ResolvedParameters[0];
        return ref ResolvedParameters[slot];
    }

    /// <summary>Returns true if the given skill slot has a gem equipped.</summary>
    public bool HasSkillGem(int slot)
    {
        if (slot < 0 || slot >= PlayerLoadout.SkillSlotCount) return false;
        return Loadout.SkillGems[slot].GemId != null;
    }
}
