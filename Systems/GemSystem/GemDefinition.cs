namespace rtypeClone.Systems.GemSystem;

public enum GemCategory { Skill, Support }
public enum SkillCategory { Shot, Shield, Beam, Mine }

/// <summary>
/// Immutable definition of a gem type, loaded from Assets/gems/*.json at startup.
/// Skill gems carry base projectile parameters. Support gems carry modifiers.
/// </summary>
public class GemDefinition
{
    public string               Id                       { get; init; } = "";
    public string               DisplayName              { get; init; } = "";
    public GemCategory          Category                 { get; init; }
    public SkillCategory?       SkillCategory            { get; init; }
    public ProjectileParameters BaseProjectileParameters { get; init; }
    public GemModifiers         Modifiers                { get; init; }
    public string[]             RequiresTags             { get; init; } = [];
    public string[]             Tags                     { get; init; } = [];
}
