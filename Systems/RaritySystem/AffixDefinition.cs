using rtypeClone.Entities;

namespace rtypeClone.Systems.RaritySystem;

/// <summary>
/// Immutable definition of an enemy affix, loaded from Assets/affixes/*.json.
/// </summary>
public class AffixDefinition
{
    public string        Id               { get; init; } = "";
    public string        DisplayName      { get; init; } = "";
    public string        Description      { get; init; } = "";
    public string[]      IncompatibleWith { get; init; } = [];
    public EnemyRarity[] AllowedRarities  { get; init; } = [];
    public AffixModifiers Modifiers       { get; init; }
}
