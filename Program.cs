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
        Raylib.SetExitKey(KeyboardKey.Null); // Escape is used for pause menu, not window close

        AssetManager.Load();

        var gameState = new GameState();
        var inputManager = new InputManager();

        while (!Raylib.WindowShouldClose() && !gameState.ExitRequested)
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
