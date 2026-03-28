using System.Numerics;

namespace rtypeClone.Systems.AiSystem;

public interface IBehaviourHandler
{
    string TypeName { get; }

    void Update(
        ref Vector2 position,
        ref Vector2 velocity,
        ref EnemyAiState state,
        AiNodeConfig config,
        in AiContext ctx);
}
