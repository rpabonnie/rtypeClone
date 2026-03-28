using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Systems.UI;

public enum PauseMenuResult
{
    None,
    Resume,
    Inventory,
    Exit
}

/// <summary>
/// Simple pause menu with Inventory and Exit options.
/// Drawn as a dim overlay on top of the frozen game.
/// </summary>
public class PauseMenu
{
    private const int OptionCount = 3;
    private static readonly string[] Labels = ["Resume", "Inventory", "Exit"];
    private static readonly PauseMenuResult[] Results =
        [PauseMenuResult.Resume, PauseMenuResult.Inventory, PauseMenuResult.Exit];

    private int _selected;

    public void Reset()
    {
        _selected = 0;
    }

    public PauseMenuResult Update(InputManager input)
    {
        if (input.NavigateUp)
            _selected = (_selected - 1 + OptionCount) % OptionCount;
        if (input.NavigateDown)
            _selected = (_selected + 1) % OptionCount;

        if (input.ConfirmPressed)
            return Results[_selected];

        // Cancel (B / Escape) resumes
        // Note: Escape also triggers MenuPressed which GameState handles,
        // so we use CancelPressed only for the B button path here.
        // GameState will handle the Escape toggle.

        return PauseMenuResult.None;
    }

    public void Draw()
    {
        // Dim overlay
        Raylib.DrawRectangle(0, 0, Constants.ScreenWidth, Constants.ScreenHeight,
            new Color((byte)0, (byte)0, (byte)0, (byte)160));

        // Title
        const string title = "PAUSED";
        int titleWidth = Raylib.MeasureText(title, 48);
        Raylib.DrawText(title,
            Constants.ScreenWidth / 2 - titleWidth / 2,
            Constants.ScreenHeight / 2 - 120,
            48, Color.White);

        // Options
        for (int i = 0; i < OptionCount; i++)
        {
            int fontSize = 32;
            int textWidth = Raylib.MeasureText(Labels[i], fontSize);
            int x = Constants.ScreenWidth / 2 - textWidth / 2;
            int y = Constants.ScreenHeight / 2 - 30 + i * 50;

            Color color = i == _selected ? Color.Yellow : Color.Gray;
            Raylib.DrawText(Labels[i], x, y, fontSize, color);

            if (i == _selected)
            {
                // Draw selection arrow
                Raylib.DrawText(">", x - 30, y, fontSize, Color.Yellow);
            }
        }

        // Input hint at bottom
        const string hint = "[W/S or D-Pad] Navigate   [Enter or A] Select";
        int hintWidth = Raylib.MeasureText(hint, 20);
        Raylib.DrawText(hint,
            Constants.ScreenWidth / 2 - hintWidth / 2,
            Constants.ScreenHeight / 2 + 160,
            20, Color.DarkGray);
    }
}
