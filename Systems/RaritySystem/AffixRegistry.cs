using System.Text.Json;
using System.Text.Json.Serialization;
using rtypeClone.Entities;

namespace rtypeClone.Systems.RaritySystem;

/// <summary>
/// Loads all AffixDefinitions from Assets/affixes/*.json at startup.
/// Read-only after initialization.
/// </summary>
public class AffixRegistry
{
    private readonly Dictionary<string, AffixDefinition> _affixes = new();

    /// <summary>Pre-built per-rarity lists to avoid allocation when filtering at spawn time.</summary>
    private readonly Dictionary<EnemyRarity, List<AffixDefinition>> _byRarity = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,  // AffixModifiers uses public fields, not properties
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public AffixRegistry(string affixDirectory)
    {
        // Initialize per-rarity buckets
        foreach (EnemyRarity r in Enum.GetValues<EnemyRarity>())
            _byRarity[r] = new List<AffixDefinition>();

        if (!Directory.Exists(affixDirectory))
            return;

        foreach (var file in Directory.GetFiles(affixDirectory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var raw = JsonSerializer.Deserialize<AffixJsonModel>(json, JsonOpts);
            if (raw?.Id == null) continue;

            var def = new AffixDefinition
            {
                Id = raw.Id,
                DisplayName = raw.DisplayName ?? raw.Id,
                Description = raw.Description ?? "",
                IncompatibleWith = raw.IncompatibleWith ?? [],
                AllowedRarities = ParseRarities(raw.AllowedRarities),
                Modifiers = ConvertModifiers(raw.Modifiers),
            };
            _affixes[def.Id] = def;

            foreach (var r in def.AllowedRarities)
                _byRarity[r].Add(def);
        }
    }

    public AffixDefinition Get(string id) => _affixes[id];
    public bool TryGet(string id, out AffixDefinition? def) => _affixes.TryGetValue(id, out def);

    /// <summary>Returns affixes allowed for the given rarity. No allocation — returns cached list.</summary>
    public IReadOnlyList<AffixDefinition> GetForRarity(EnemyRarity rarity) => _byRarity[rarity];

    public int Count => _affixes.Count;

    private static EnemyRarity[] ParseRarities(string[]? names)
    {
        if (names == null || names.Length == 0)
            return [];

        var result = new EnemyRarity[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            if (Enum.TryParse<EnemyRarity>(names[i], ignoreCase: true, out var r))
                result[i] = r;
        }
        return result;
    }

    private static AffixModifiers ConvertModifiers(AffixModifiersJson? m)
    {
        if (m == null) return AffixModifiers.None;
        return new AffixModifiers
        {
            SpeedMultiplier = m.SpeedMultiplier ?? 1f,
            ShieldHp = m.ShieldHp ?? 0,
            SplitsOnDeath = m.SplitsOnDeath ?? 0,
            PhysicalDamageReduction = m.PhysicalDamageReduction ?? 0f,
            HpRegenPerSecond = m.HpRegenPerSecond ?? 0f,
            DamageMultiplier = m.DamageMultiplier ?? 1f,
            ProjectileCount = m.ProjectileCount ?? 0,
        };
    }

    // ── JSON deserialization models ──

    private class AffixJsonModel
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string[]? IncompatibleWith { get; set; }
        public string[]? AllowedRarities { get; set; }
        public AffixModifiersJson? Modifiers { get; set; }
    }

    private class AffixModifiersJson
    {
        public float? SpeedMultiplier { get; set; }
        public int? ShieldHp { get; set; }
        public int? SplitsOnDeath { get; set; }
        public float? PhysicalDamageReduction { get; set; }
        public float? HpRegenPerSecond { get; set; }
        public float? DamageMultiplier { get; set; }
        public int? ProjectileCount { get; set; }
    }
}
