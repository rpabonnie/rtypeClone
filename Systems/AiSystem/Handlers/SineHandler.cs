using System.Numerics;

namespace rtypeClone.Systems.AiSystem.Handlers;

public class SineHandler : IBehaviourHandler
{
    public string TypeName => "sine";

    public void Update(
        ref Vector2 position,
        ref Vector2 velocity,
        ref EnemyAiState state,
        AiNodeConfig config,
        in AiContext ctx)
    {
        position.X += velocity.X * ctx.Dt;
        position.Y = state.SpawnY + MathF.Sin(state.AliveTimer * config.Frequency) * config.Amplitude;
    }
}
