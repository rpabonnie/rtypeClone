using System.Text.Json;
using System.Text.Json.Serialization;

namespace rtypeClone.Systems.GemSystem;

/// <summary>
/// Loads all GemDefinitions from Assets/gems/*.json at startup.
/// Read-only after initialization — no locks needed.
/// </summary>
public class GemRegistry
{
    private readonly Dictionary<string, GemDefinition> _gems = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,  // ProjectileParameters uses public fields, not properties
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public GemRegistry(string gemsDirectory)
    {
        if (!Directory.Exists(gemsDirectory))
            return;

        foreach (var file in Directory.GetFiles(gemsDirectory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var raw = JsonSerializer.Deserialize<GemJsonModel>(json, JsonOpts);
            if (raw?.Id == null) continue;

            var def = new GemDefinition
            {
                Id = raw.Id,
                DisplayName = raw.DisplayName ?? raw.Id,
                Category = raw.Category,
                SkillCategory = raw.SkillCategory,
                BaseProjectileParameters = raw.BaseProjectileParameters ?? ProjectileParameters.DefaultNormal,
                Modifiers = ConvertModifiers(raw.Modifiers),
                RequiresTags = raw.RequiresTags ?? [],
                Tags = raw.Tags ?? []
            };
            _gems[def.Id] = def;
        }
    }

    public GemDefinition Get(string id) => _gems[id];
    public bool TryGet(string id, out GemDefinition? def) => _gems.TryGetValue(id, out def);
    public IReadOnlyCollection<GemDefinition> All => _gems.Values;
    public int Count => _gems.Count;

    private static GemModifiers ConvertModifiers(GemModifiersJson? m)
    {
        if (m == null) return GemModifiers.None;
        return new GemModifiers
        {
            DamageFlat = m.DamageFlat ?? 0,
            DamageMultiplier = m.DamageMultiplier ?? 1f,
            PierceDelta = m.PierceDelta ?? 0,
            HomingOverride = m.HomingOverride ?? false,
            HomingStrength = m.HomingStrength ?? 0f,
            RadiusMultiplier = m.RadiusMultiplier ?? 1f,
            CountDelta = m.CountDelta ?? 0,
            SpreadAngleDeg = m.SpreadAngleDeg ?? 0f,
            SpeedFlat = m.SpeedFlat ?? 0f,
        };
    }

    // ── JSON deserialization models (nullable for optional fields) ──

    private class GemJsonModel
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public GemCategory Category { get; set; }
        public SkillCategory? SkillCategory { get; set; }
        public ProjectileParameters? BaseProjectileParameters { get; set; }
        public GemModifiersJson? Modifiers { get; set; }
        public string[]? RequiresTags { get; set; }
        public string[]? Tags { get; set; }
    }

    private class GemModifiersJson
    {
        public int? DamageFlat { get; set; }
        public float? DamageMultiplier { get; set; }
        public int? PierceDelta { get; set; }
        public bool? HomingOverride { get; set; }
        public float? HomingStrength { get; set; }
        public float? RadiusMultiplier { get; set; }
        public int? CountDelta { get; set; }
        public float? SpreadAngleDeg { get; set; }
        public float? SpeedFlat { get; set; }
    }
}
