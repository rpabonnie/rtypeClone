using System.Text.Json;
using System.Text.Json.Serialization;

namespace rtypeClone.Systems.ModuleSystem;

/// <summary>
/// Loads all ModuleDefinitions from Assets/modules/*.json at startup.
/// Read-only after initialization — no locks needed.
/// </summary>
public class ModuleRegistry
{
    private readonly Dictionary<string, ModuleDefinition> _modules = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,  // ProjectileParameters uses public fields, not properties
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ModuleRegistry(string modulesDirectory)
    {
        if (!Directory.Exists(modulesDirectory))
            return;

        foreach (var file in Directory.GetFiles(modulesDirectory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var raw = JsonSerializer.Deserialize<ModuleJsonModel>(json, JsonOpts);
            if (raw?.Id == null) continue;

            var def = new ModuleDefinition
            {
                Id = raw.Id,
                DisplayName = raw.DisplayName ?? raw.Id,
                Category = raw.Category,
                WeaponCategory = raw.WeaponCategory,
                BaseProjectileParameters = raw.BaseProjectileParameters ?? ProjectileParameters.DefaultNormal,
                ChargedProjectileParameters = raw.ChargedProjectileParameters ?? default,
                HasChargedMode = raw.ChargedProjectileParameters != null,
                Modifiers = ConvertModifiers(raw.Modifiers),
                RequiresTags = raw.RequiresTags ?? [],
                Tags = raw.Tags ?? []
            };
            _modules[def.Id] = def;
        }
    }

    public ModuleDefinition Get(string id) => _modules[id];
    public bool TryGet(string id, out ModuleDefinition? def) => _modules.TryGetValue(id, out def);
    public IReadOnlyCollection<ModuleDefinition> All => _modules.Values;
    public int Count => _modules.Count;

    private static ModuleModifiers ConvertModifiers(ModuleModifiersJson? m)
    {
        if (m == null) return ModuleModifiers.None;
        return new ModuleModifiers
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

    private class ModuleJsonModel
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public ModuleCategory Category { get; set; }
        public WeaponCategory? WeaponCategory { get; set; }
        public ProjectileParameters? BaseProjectileParameters { get; set; }
        public ProjectileParameters? ChargedProjectileParameters { get; set; }
        public ModuleModifiersJson? Modifiers { get; set; }
        public string[]? RequiresTags { get; set; }
        public string[]? Tags { get; set; }
    }

    private class ModuleModifiersJson
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
