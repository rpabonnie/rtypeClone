using System.Numerics;

namespace rtypeClone.Systems.AiSystem.Handlers;

public class StraightHandler : IBehaviourHandler
{
    public string TypeName => "straight";

    public void Update(
        ref Vector2 position,
        ref Vector2 velocity,
        ref EnemyAiState state,
        AiNodeConfig config,
        in AiContext ctx)
    {
        position += velocity * ctx.Dt;
    }
}
