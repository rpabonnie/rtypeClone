using System.Numerics;

namespace rtypeClone.Systems.AiSystem;

public class AiSystem
{
    private readonly BehaviourRegistry _registry;
    private readonly AiProfileRegistry _profiles;

    public AiSystem(string profilesDirectory)
    {
        _registry = new BehaviourRegistry();
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
}
