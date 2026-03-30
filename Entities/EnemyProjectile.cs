using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Entities;

public class EnemyProjectile : Entity
{
    public int Damage;
    public float Lifetime;
    public float Age;
    public bool Homing;
    public float HomingStrength;
    public int HitsRemaining;
    public bool IsStationary;
    public float Speed;

    public void Spawn(Vector2 position, EnemyAttackConfig config, Vector2 aimDirection)
    {
        Position = position;
        Width = MathF.Max(config.ProjectileWidth, 6f);
        Height = MathF.Max(config.ProjectileHeight, 6f);
        Damage = config.Damage;
        Lifetime = config.Lifetime;
        Age = 0f;
        Homing = config.Homing;
        HomingStrength = config.HomingStrength;
        HitsRemaining = config.Pierce + 1;
        IsStationary = config.Stationary;
        Speed = config.ProjectileSpeed;

        if (IsStationary)
        {
            Velocity = Vector2.Zero;
        }
        else
        {
            Velocity = aimDirection * config.ProjectileSpeed;
        }

        Active = true;
    }

    public override void Update(float dt)
    {
        Age += dt;
        if (Age >= Lifetime)
        {
            Active = false;
            return;
        }

        if (!IsStationary)
        {
            Position += Velocity * dt;
        }
    }

    public void UpdateHoming(float dt, Vector2 playerPosition)
    {
        if (!Homing || !Active || IsStationary) return;

        var center = new Vector2(Position.X + Width / 2f, Position.Y + Height / 2f);
        var desired = Vector2.Normalize(playerPosition - center);
        var current = Velocity.LengthSquared() > 0.001f
            ? Vector2.Normalize(Velocity)
            : new Vector2(-1f, 0f);

        var blended = Vector2.Lerp(current, desired, HomingStrength * dt);
        if (blended.LengthSquared() > 0.001f)
        {
            Velocity = Vector2.Normalize(blended) * Speed;
        }
    }

    public override void Draw()
    {
        if (!Active) return;

        // Red/orange color distinct from player's blue/cyan bullets
        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), Color.Orange);
        // Inner core for visual pop
        if (Width > 6f && Height > 6f)
        {
            Raylib.DrawRectangleV(
                new Vector2(Position.X + 1f, Position.Y + 1f),
                new Vector2(Width - 2f, Height - 2f),
                Color.Red);
        }
    }
}
