using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Systems;

public class ScrollingBackground
{
    private float _layer1X;
    private float _layer2X;

    private const float Layer1Speed = 40f;
    private const float Layer2Speed = 80f;

    public void Update(float dt)
    {
        _layer1X -= Layer1Speed * dt;
        _layer2X -= Layer2Speed * dt;

        if (_layer1X <= -Constants.ScreenWidth) _layer1X = 0f;
        if (_layer2X <= -Constants.ScreenWidth) _layer2X = 0f;
    }

    public void Draw()
    {
        // Layer 1 — far stars
        Raylib.DrawRectangle((int)_layer1X, 0, Constants.ScreenWidth, Constants.ScreenHeight, new Color(10, 10, 30, 255));
        Raylib.DrawRectangle((int)_layer1X + Constants.ScreenWidth, 0, Constants.ScreenWidth, Constants.ScreenHeight, new Color(10, 10, 30, 255));

        // Layer 2 — near stars (placeholder dots)
        int starSpacing = 120;
        for (int i = 0; i < Constants.ScreenWidth / starSpacing + 2; i++)
        {
            float x = (int)_layer2X + i * starSpacing;
            Raylib.DrawCircle((int)x, 200, 2f, Color.White);
            Raylib.DrawCircle((int)x + 60, 500, 1.5f, Color.Gray);
            Raylib.DrawCircle((int)x + 30, 800, 2f, Color.White);
        }
    }
}
