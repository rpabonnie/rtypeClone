using System.Numerics;

namespace rtypeClone.Systems.AiSystem;

public readonly struct AiContext
{
    public readonly float Dt;
    public readonly Vector2 PlayerPosition;
    public readonly float ScreenWidth;
    public readonly float ScreenHeight;

    public AiContext(float dt, Vector2 playerPosition, float screenWidth, float screenHeight)
    {
        Dt = dt;
        PlayerPosition = playerPosition;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
    }
}
