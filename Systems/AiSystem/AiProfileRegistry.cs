using System.Text.Json;

namespace rtypeClone.Systems.AiSystem;

public class AiProfileRegistry
{
    private readonly Dictionary<string, AiProfile> _profiles = new();

    public AiProfileRegistry(string directory)
    {
        if (!Directory.Exists(directory)) return;

        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var profile = JsonSerializer.Deserialize<AiProfile>(json);
            if (profile != null && !string.IsNullOrEmpty(profile.Id))
                _profiles[profile.Id] = profile;
        }
    }

    public AiProfile Get(string id) => _profiles[id];
    public bool Has(string id) => _profiles.ContainsKey(id);
}
