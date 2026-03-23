using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Entities;

public class Enemy : Entity
{
    public int Health;

    public Enemy()
    {
        Width = 40f;
        Height = 32f;
    }

    public void Spawn(Vector2 position, Vector2 velocity, int health = 1)
    {
        Position = position;
        Velocity = velocity;
        Health = health;
        Active = true;
    }

    public override void Update(float dt)
    {
        Position += Velocity * dt;
    }

    public override void Draw()
    {
        if (!Active) return;
        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), Color.Red);
    }
}
