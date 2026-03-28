namespace rtypeClone.Systems.ModuleSystem;

/// <summary>
/// Represents one module socket — either a weapon slot or a support slot.
/// </summary>
public struct ModuleSlot
{
    /// <summary>Module definition ID, or null if the slot is empty.</summary>
    public string? ModuleId;
}

/// <summary>
/// The player's current module configuration.
/// 4 weapon slots, each with 2 linked support module slots (12 sockets total).
/// Mutable — changed via the loadout screen between levels.
/// </summary>
public class PlayerLoadout
{
    public const int WeaponSlotCount = 4;
    public const int SupportSlotCount = 2;

    /// <summary>Weapon module in each slot. Index = weapon slot index.</summary>
    public readonly ModuleSlot[] WeaponModules = new ModuleSlot[WeaponSlotCount];

    /// <summary>Support modules. [weaponIndex, supportIndex] — 0..1 per weapon slot.</summary>
    public readonly ModuleSlot[,] SupportModules = new ModuleSlot[WeaponSlotCount, SupportSlotCount];

    /// <summary>
    /// Equip a weapon module into a slot. Returns false if the module isn't a Weapon category.
    /// </summary>
    public bool TryEquipWeapon(int slot, string moduleId, ModuleRegistry registry)
    {
        if (slot < 0 || slot >= WeaponSlotCount) return false;
        if (!registry.TryGet(moduleId, out var def) || def == null) return false;
        if (def.Category != ModuleCategory.Weapon) return false;

        WeaponModules[slot].ModuleId = moduleId;
        return true;
    }

    /// <summary>
    /// Equip a support module linked to a weapon slot.
    /// Returns false if the module isn't a Support category or tag-incompatible.
    /// </summary>
    public bool TryEquipSupport(int weaponSlot, int supportSlot, string moduleId, ModuleRegistry registry)
    {
        if (weaponSlot < 0 || weaponSlot >= WeaponSlotCount) return false;
        if (supportSlot < 0 || supportSlot >= SupportSlotCount) return false;
        if (!registry.TryGet(moduleId, out var def) || def == null) return false;
        if (def.Category != ModuleCategory.Support) return false;

        // Check tag compatibility with the weapon module in this slot
        var weaponId = WeaponModules[weaponSlot].ModuleId;
        if (weaponId != null && registry.TryGet(weaponId, out var weaponDef) && weaponDef != null)
        {
            if (def.RequiresTags.Length > 0)
            {
                foreach (var req in def.RequiresTags)
                {
                    bool found = false;
                    foreach (var tag in weaponDef.Tags)
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

        SupportModules[weaponSlot, supportSlot].ModuleId = moduleId;
        return true;
    }

    /// <summary>
    /// Clear a slot. supportSlot = -1 clears the weapon module; 0..1 clears a support module.
    /// </summary>
    public void ClearSlot(int weaponSlot, int supportSlot = -1)
    {
        if (weaponSlot < 0 || weaponSlot >= WeaponSlotCount) return;

        if (supportSlot < 0)
            WeaponModules[weaponSlot].ModuleId = null;
        else if (supportSlot < SupportSlotCount)
            SupportModules[weaponSlot, supportSlot].ModuleId = null;
    }
}
