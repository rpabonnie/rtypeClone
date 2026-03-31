using System.Numerics;
using rtypeClone.Core;
using rtypeClone.Entities;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Systems.AiSystem;

public class AiSystem
{
    private readonly BehaviourRegistry _registry;
    private readonly AiProfileRegistry _profiles;

    public AiSystem(string profilesDirectory, ObjectPool<EnemyProjectile> enemyProjectilePool,
                    EnemyAttackRegistry attackRegistry)
    {
        _registry = new BehaviourRegistry(enemyProjectilePool, attackRegistry);
        _profiles = new AiProfileRegistry(profilesDirectory);
    }

    public void Update(
        ref Vector2 position,
        ref Vector2 velocity,
        ref EnemyAiState state,
        string profileId,
        in AiContext ctx)
    {
        var profile = _profiles.Get(profileId);
        foreach (var node in profile.Nodes)
        {
            var handler = _registry.Get(node.Type);
            handler.Update(ref position, ref velocity, ref state, node, in ctx);
        }
    }

    public bool HasProfile(string id) => _profiles.Has(id);

    /// <summary>
    /// Returns the entryDirection string for a given profile ("right", "left", "top", "bottom").
    /// Used by WaveSpawner to place enemies on the correct screen edge.
    /// </summary>
    public string GetEntryDirection(string profileId) =>
        _profiles.Has(profileId) ? _profiles.Get(profileId).EntryDirection : "right";
}
