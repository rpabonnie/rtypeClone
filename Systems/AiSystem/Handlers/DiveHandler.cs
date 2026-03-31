using System.Numerics;

namespace rtypeClone.Systems.AiSystem.Handlers;

/// <summary>
/// Handles top/bottom swoop entry. The enemy dives vertically until it has
/// traveled <see cref="AiNodeConfig.CurveAfter"/> pixels, then curves to fly
/// horizontally left (the standard exit direction).
///
/// Entry velocity Y-component is set by WaveSpawner based on the profile's
/// entryDirection ("top" → positive Y, "bottom" → negative Y). DiveHandler
/// uses the sign of that initial Y to determine dive direction on the first
/// frame, then manages velocity itself for the remainder of the movement.
/// </summary>
public class DiveHandler : IBehaviourHandler
{
    public string TypeName => "dive";

    public void Update(
        ref Vector2 position,
        ref Vector2 velocity,
        ref EnemyAiState state,
        AiNodeConfig config,
        in AiContext ctx)
    {
        if (!state.DiveCurved)
        {
            // First frame: if velocity hasn't been set yet (spawned with zero Y),
            // infer direction from spawn position (above or below screen centre).
            if (state.DiveDistanceTraveled == 0f && velocity.Y == 0f)
            {
                bool fromTop = state.SpawnY < ctx.ScreenHeight / 2f;
                velocity = new Vector2(0f, fromTop ? config.DiveSpeed : -config.DiveSpeed);
            }

            // Move in current (vertical) direction
            position += velocity * ctx.Dt;
            state.DiveDistanceTraveled += MathF.Abs(velocity.Y) * ctx.Dt;

            // Curve to horizontal once enough distance is covered
            if (state.DiveDistanceTraveled >= config.CurveAfter)
            {
                state.DiveCurved = true;
                velocity = new Vector2(-config.DiveSpeed, 0f);
            }
        }
        else
        {
            // Horizontal cruise after the curve
            position += velocity * ctx.Dt;
        }
    }
}
