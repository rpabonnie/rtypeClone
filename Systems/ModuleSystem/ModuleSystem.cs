namespace rtypeClone.Systems.ModuleSystem;

/// <summary>
/// Owns the module registry, player loadout, and resolved parameter cache.
/// GameState holds one ModuleSystem instance.
/// </summary>
public class ModuleSystem
{
    public ModuleRegistry Registry { get; }
    public PlayerLoadout  Loadout  { get; }

    /// <summary>
    /// Cached resolved parameters — updated on loadout change, not per frame.
    /// Index = weapon slot index (0..3).
    /// </summary>
    public readonly ProjectileParameters[] ResolvedParameters =
        new ProjectileParameters[PlayerLoadout.WeaponSlotCount];

    /// <summary>
    /// Cached resolved charged parameters per slot. Only valid when HasChargedMode[slot] is true.
    /// </summary>
    public readonly ProjectileParameters[] ResolvedChargedParameters =
        new ProjectileParameters[PlayerLoadout.WeaponSlotCount];

    /// <summary>Whether each slot's module has a charged fire mode.</summary>
    public readonly bool[] HasChargedMode = new bool[PlayerLoadout.WeaponSlotCount];

    // Scratch array to avoid allocating when resolving supports
    private readonly ModuleDefinition?[] _supportScratch = new ModuleDefinition?[PlayerLoadout.SupportSlotCount];

    public ModuleSystem(string modulesDirectory)
    {
        Registry = new ModuleRegistry(modulesDirectory);
        Loadout = new PlayerLoadout();

        // Default loadout: slot 0 = normal shot (tap fires base, hold fires charged)
        if (Registry.TryGet("shot_normal", out _))
            Loadout.TryEquipWeapon(0, "shot_normal", Registry);

        RebuildCache();
    }

    /// <summary>
    /// Rebuilds the ResolvedParameters cache from the current loadout.
    /// Call this whenever the loadout changes (not per frame).
    /// </summary>
    public void RebuildCache()
    {
        for (int slot = 0; slot < PlayerLoadout.WeaponSlotCount; slot++)
        {
            var weaponId = Loadout.WeaponModules[slot].ModuleId;
            if (weaponId == null || !Registry.TryGet(weaponId, out var weaponDef) || weaponDef == null)
            {
                ResolvedParameters[slot] = default;
                ResolvedChargedParameters[slot] = default;
                HasChargedMode[slot] = false;
                continue;
            }

            // Gather support modules for this slot
            for (int s = 0; s < PlayerLoadout.SupportSlotCount; s++)
            {
                var supId = Loadout.SupportModules[slot, s].ModuleId;
                if (supId != null && Registry.TryGet(supId, out var supDef))
                    _supportScratch[s] = supDef;
                else
                    _supportScratch[s] = null;
            }

            var supports = new ReadOnlySpan<ModuleDefinition?>(_supportScratch);
            ResolvedParameters[slot] = ModulePipeline.Resolve(weaponDef, supports);
            HasChargedMode[slot] = weaponDef.HasChargedMode;

            if (weaponDef.HasChargedMode)
            {
                ResolvedChargedParameters[slot] =
                    ModulePipeline.ResolveCharged(weaponDef, supports);
            }
            else
            {
                ResolvedChargedParameters[slot] = default;
            }
        }
    }

    /// <summary>
    /// Returns resolved parameters for a weapon slot.
    /// Returns default (zero) if the slot is empty.
    /// </summary>
    public ref readonly ProjectileParameters GetActive(int slot)
    {
        if (slot < 0 || slot >= PlayerLoadout.WeaponSlotCount)
            return ref ResolvedParameters[0];
        return ref ResolvedParameters[slot];
    }

    /// <summary>Returns true if the given weapon slot has a module equipped.</summary>
    public bool HasWeaponModule(int slot)
    {
        if (slot < 0 || slot >= PlayerLoadout.WeaponSlotCount) return false;
        return Loadout.WeaponModules[slot].ModuleId != null;
    }

    /// <summary>Returns resolved charged parameters for a weapon slot.</summary>
    public ref readonly ProjectileParameters GetCharged(int slot)
    {
        if (slot < 0 || slot >= PlayerLoadout.WeaponSlotCount)
            return ref ResolvedChargedParameters[0];
        return ref ResolvedChargedParameters[slot];
    }

    /// <summary>Returns true if the weapon module in the given slot supports charge-fire.</summary>
    public bool SlotHasChargedMode(int slot)
    {
        if (slot < 0 || slot >= PlayerLoadout.WeaponSlotCount) return false;
        return HasChargedMode[slot];
    }
}
