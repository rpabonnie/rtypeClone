using System.Numerics;

namespace rtypeClone.Systems.AiSystem.Handlers;

public class ZigzagHandler : IBehaviourHandler
{
    public string TypeName => "zigzag";

    public void Update(
        ref Vector2 position,
        ref Vector2 velocity,
        ref EnemyAiState state,
        AiNodeConfig config,
        in AiContext ctx)
    {
        position.X += velocity.X * ctx.Dt;
        position.Y += state.ZigzagDirection * config.VerticalSpeed * ctx.Dt;

        // Clamp to screen bounds and flip direction
        if (position.Y < 0f)
        {
            position.Y = 0f;
            state.ZigzagDirection = 1f;
            state.ZigzagTimer = config.FlipInterval;
        }
        else if (position.Y + 32f > ctx.ScreenHeight) // 32f = enemy height
        {
            position.Y = ctx.ScreenHeight - 32f;
            state.ZigzagDirection = -1f;
            state.ZigzagTimer = config.FlipInterval;
        }

        state.ZigzagTimer -= ctx.Dt;
        if (state.ZigzagTimer <= 0f)
        {
            state.ZigzagDirection = -state.ZigzagDirection;
            state.ZigzagTimer = config.FlipInterval;
        }
    }
}
