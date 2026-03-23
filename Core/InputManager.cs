using System.Numerics;
using Raylib_cs;

namespace rtypeClone.Core;

public enum InputDevice
{
    Keyboard,
    Gamepad
}

public class InputManager
{
    private const int GamepadIndex = 0;
    private const float DeadZone = 0.2f;

    public InputDevice ActiveDevice { get; private set; } = InputDevice.Gamepad;
    public Vector2 Movement { get; private set; }
    public bool ShootPressed { get; private set; }

    public void Update()
    {
        Vector2 kbMovement = Vector2.Zero;
        bool kbUsed = false;

        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up))    { kbMovement.Y -= 1f; kbUsed = true; }
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down))   { kbMovement.Y += 1f; kbUsed = true; }
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left))   { kbMovement.X -= 1f; kbUsed = true; }
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right))  { kbMovement.X += 1f; kbUsed = true; }

        bool kbShoot = Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsMouseButtonPressed(MouseButton.Left);
        if (kbShoot) kbUsed = true;

        Vector2 gpMovement = Vector2.Zero;
        bool gpUsed = false;
        bool gpShoot = false;

        if (Raylib.IsGamepadAvailable(GamepadIndex))
        {
            float axisX = Raylib.GetGamepadAxisMovement(GamepadIndex, GamepadAxis.LeftX);
            float axisY = Raylib.GetGamepadAxisMovement(GamepadIndex, GamepadAxis.LeftY);

            if (MathF.Abs(axisX) > DeadZone || MathF.Abs(axisY) > DeadZone)
            {
                gpMovement = new Vector2(axisX, axisY);
                gpUsed = true;
            }

            if (Raylib.IsGamepadButtonPressed(GamepadIndex, GamepadButton.RightFaceDown))
            {
                gpShoot = true;
                gpUsed = true;
            }
        }

        // Most recently used device wins
        if (gpUsed) ActiveDevice = InputDevice.Gamepad;
        else if (kbUsed) ActiveDevice = InputDevice.Keyboard;

        if (ActiveDevice == InputDevice.Gamepad && Raylib.IsGamepadAvailable(GamepadIndex))
        {
            Movement = gpMovement;
            ShootPressed = gpShoot;
        }
        else
        {
            Movement = kbMovement;
            ShootPressed = kbShoot;
        }

        // Normalize to prevent diagonal speed boost
        if (Movement.LengthSquared() > 1f)
            Movement = Vector2.Normalize(Movement);
    }
}
