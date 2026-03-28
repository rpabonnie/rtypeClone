namespace rtypeClone.Systems.GemSystem;

/// <summary>
/// Represents one gem socket — either a skill slot or a support slot.
/// </summary>
public struct GemSlot
{
    /// <summary>Gem definition ID, or null if the slot is empty.</summary>
    public string? GemId;
}

/// <summary>
/// The player's current skill configuration.
/// 4 skill slots, each with 1 skill gem and 2 linked support gem slots (12 sockets total).
/// Mutable — changed via the loadout screen between levels.
/// </summary>
public class PlayerLoadout
{
    public const int SkillSlotCount = 4;
    public const int SupportSlotCount = 2;

    /// <summary>Skill gem in each slot. Index = skill slot index.</summary>
    public readonly GemSlot[] SkillGems = new GemSlot[SkillSlotCount];

    /// <summary>Support gems. [skillIndex, supportIndex] — 0..1 per skill slot.</summary>
    public readonly GemSlot[,] SupportGems = new GemSlot[SkillSlotCount, SupportSlotCount];

    /// <summary>
    /// Equip a skill gem into a slot. Returns false if the gem isn't a Skill category.
    /// </summary>
    public bool TryEquipSkill(int slot, string gemId, GemRegistry registry)
    {
        if (slot < 0 || slot >= SkillSlotCount) return false;
        if (!registry.TryGet(gemId, out var def) || def == null) return false;
        if (def.Category != GemCategory.Skill) return false;

        SkillGems[slot].GemId = gemId;
        return true;
    }

    /// <summary>
    /// Equip a support gem linked to a skill slot.
    /// Returns false if the gem isn't a Support category or tag-incompatible.
    /// </summary>
    public bool TryEquipSupport(int skillSlot, int supportSlot, string gemId, GemRegistry registry)
    {
        if (skillSlot < 0 || skillSlot >= SkillSlotCount) return false;
        if (supportSlot < 0 || supportSlot >= SupportSlotCount) return false;
        if (!registry.TryGet(gemId, out var def) || def == null) return false;
        if (def.Category != GemCategory.Support) return false;

        // Check tag compatibility with the skill gem in this slot
        var skillId = SkillGems[skillSlot].GemId;
        if (skillId != null && registry.TryGet(skillId, out var skillDef) && skillDef != null)
        {
            if (def.RequiresTags.Length > 0)
            {
                foreach (var req in def.RequiresTags)
                {
                    bool found = false;
                    foreach (var tag in skillDef.Tags)
                    {
                        if (string.Equals(tag, req, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
            }
        }

        SupportGems[skillSlot, supportSlot].GemId = gemId;
        return true;
    }

    /// <summary>
    /// Clear a slot. supportSlot = -1 clears the skill gem; 0..1 clears a support gem.
    /// </summary>
    public void ClearSlot(int skillSlot, int supportSlot = -1)
    {
        if (skillSlot < 0 || skillSlot >= SkillSlotCount) return;

        if (supportSlot < 0)
            SkillGems[skillSlot].GemId = null;
        else if (supportSlot < SupportSlotCount)
            SupportGems[skillSlot, supportSlot].GemId = null;
    }
}
