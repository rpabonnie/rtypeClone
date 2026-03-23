using System.Numerics;
using Raylib_cs;

namespace rtypeClone.Entities;

public class Projectile : Entity
{
    public Projectile()
    {
        Width = 12f;
        Height = 4f;
    }

    public void Spawn(Vector2 position, Vector2 velocity)
    {
        Position = position;
        Velocity = velocity;
        Active = true;
    }

    public override void Update(float dt)
    {
        Position += Velocity * dt;
    }

    public override void Draw()
    {
        if (!Active) return;
        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), Color.Yellow);
    }
}
