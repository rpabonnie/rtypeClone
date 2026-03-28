using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone;

public static class Program
{
    public static void Main()
    {
        Raylib.InitWindow(Constants.ScreenWidth, Constants.ScreenHeight, "R-Type Clone");
        Raylib.ToggleBorderlessWindowed();
        Raylib.SetTargetFPS(60);

        AssetManager.Load();

        var gameState = new GameState();
        var inputManager = new InputManager();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            inputManager.Update();
            gameState.Update(dt, inputManager);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            gameState.Draw();
            Raylib.EndDrawing();
        }

        AssetManager.Unload();
        Raylib.CloseWindow();
    }
}
