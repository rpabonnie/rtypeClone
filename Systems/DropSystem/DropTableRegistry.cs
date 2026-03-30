using System.Text.Json;
using System.Text.Json.Serialization;
using rtypeClone.Entities;

namespace rtypeClone.Systems.DropSystem;

public class DropTableRegistry
{
    private readonly Dictionary<string, DropTable> _tables = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
    };

    public DropTableRegistry(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var raw = JsonSerializer.Deserialize<DropTableJson>(json, JsonOpts);
            if (raw?.Id == null) continue;

            int totalWeight = 0;
            var entries = new DropTableEntry[raw.Entries?.Length ?? 0];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new DropTableEntry
                {
                    GemId = raw.Entries![i].GemId ?? "",
                    Weight = raw.Entries[i].Weight,
                };
                totalWeight += entries[i].Weight;
            }

            _tables[raw.Id] = new DropTable
            {
                Id = raw.Id,
                GuaranteedDrop = raw.GuaranteedDrop,
                DropChance = raw.DropChance,
                Entries = entries,
                TotalWeight = totalWeight,
            };
        }
    }

    public DropTable Get(string id) => _tables[id];

    public DropTable GetForRarity(EnemyRarity rarity) => rarity switch
    {
        EnemyRarity.Normal => Get("drops_normal"),
        EnemyRarity.Magic => Get("drops_magic"),
        EnemyRarity.Rare => Get("drops_rare"),
        EnemyRarity.Unique => Get("drops_unique_default"),
        _ => Get("drops_normal"),
    };

    public int Count => _tables.Count;

    private class DropTableJson
    {
        public string? Id { get; set; }
        public bool GuaranteedDrop { get; set; }
        public float DropChance { get; set; }
        public DropTableEntryJson[]? Entries { get; set; }
    }

    private class DropTableEntryJson
    {
        public string? GemId { get; set; }
        public int Weight { get; set; }
    }
}
