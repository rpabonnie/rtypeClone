namespace rtypeClone.Systems.ModuleSystem;

public enum ModuleCategory { Weapon, Support }
public enum WeaponCategory { Shot, Shield, Beam, Mine }

/// <summary>
/// Immutable definition of a ship module, loaded from Assets/modules/*.json at startup.
/// Weapon modules carry base projectile parameters. Support modules carry modifiers.
/// </summary>
public class ModuleDefinition
{
    public string               Id                          { get; init; } = "";
    public string               DisplayName                 { get; init; } = "";
    public ModuleCategory       Category                    { get; init; }
    public WeaponCategory?      WeaponCategory              { get; init; }
    public ProjectileParameters BaseProjectileParameters    { get; init; }
    public ProjectileParameters ChargedProjectileParameters { get; init; }
    public bool                 HasChargedMode              { get; init; }
    public ModuleModifiers      Modifiers                   { get; init; }
    public string[]             RequiresTags                { get; init; } = [];
    public string[]             Tags                        { get; init; } = [];
}
