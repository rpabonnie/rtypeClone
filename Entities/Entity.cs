using System.Numerics;
using Raylib_cs;

namespace rtypeClone.Entities;

public abstract class Entity
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Width;
    public float Height;
    public bool Active;

    public Rectangle Bounds => new(Position.X, Position.Y, Width, Height);

    public abstract void Update(float dt);
    public abstract void Draw();

    public bool IsOffScreen(float rightMargin = 0f)
    {
        return Position.X + Width < 0
            || Position.X > Core.Constants.ScreenWidth + rightMargin
            || Position.Y + Height < 0
            || Position.Y > Core.Constants.ScreenHeight;
    }
}
