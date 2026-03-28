using System.Numerics;
using Raylib_cs;
using rtypeClone.Entities;

namespace rtypeClone.Systems;

public static class DebugDraw
{
    public static void DrawHitbox(Entity entity)
    {
        Raylib.DrawRectangleLinesEx(entity.Bounds, 1f, Color.Lime);
    }

    public static void DrawAiLabel(Enemy enemy)
    {
        int textWidth = Raylib.MeasureText(enemy.AiProfileId, 14);
        Raylib.DrawText(enemy.AiProfileId,
            (int)(enemy.Position.X + enemy.Width / 2f - textWidth / 2f),
            (int)(enemy.Position.Y - 20f),
            14, Color.Yellow);
    }

    public static void DrawFrameTime()
    {
        float frameMs = Raylib.GetFrameTime() * 1000f;
        int fps = Raylib.GetFPS();
        Raylib.DrawText($"{frameMs:F1}ms  {fps} FPS",
            Core.Constants.ScreenWidth - 200, 10, 20, Color.Lime);
    }
}
