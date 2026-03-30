using System.Text.Json;

namespace rtypeClone.Systems.CombatSystem;

public class EnemyAttackRegistry
{
    private readonly Dictionary<string, EnemyAttackConfig> _attacks = new();

    public EnemyAttackRegistry(string attackDirectory)
    {
        if (!Directory.Exists(attackDirectory)) return;

        foreach (var file in Directory.GetFiles(attackDirectory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var config = JsonSerializer.Deserialize<EnemyAttackConfig>(json);
            if (config != null && !string.IsNullOrEmpty(config.Id))
                _attacks[config.Id] = config;
        }
    }

    public EnemyAttackConfig Get(string id) => _attacks[id];
    public bool Has(string id) => _attacks.ContainsKey(id);
}
