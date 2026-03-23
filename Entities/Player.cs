using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Entities;

public class Player : Entity
{
    private float _shootTimer;

    public int Health = 3;
    public int Score;

    public Player()
    {
        Width = 48f;
        Height = 32f;
        Position = new Vector2(100f, Constants.ScreenHeight / 2f);
        Active = true;
    }

    public void Update(float dt, InputManager input, ObjectPool<Projectile> bulletPool)
    {
        // Movement
        Position += input.Movement * Constants.PlayerSpeed * dt;

        // Clamp to screen
        Position.X = Math.Clamp(Position.X, 0f, Constants.ScreenWidth - Width);
        Position.Y = Math.Clamp(Position.Y, 0f, Constants.ScreenHeight - Height);

        // Shooting
        _shootTimer -= dt;
        if (input.ShootPressed && _shootTimer <= 0f)
        {
            var bullet = bulletPool.Get();
            if (bullet != null)
            {
                bullet.Spawn(
                    new Vector2(Position.X + Width, Position.Y + Height / 2f - 2f),
                    new Vector2(Constants.BulletSpeed, 0f)
                );
            }
            _shootTimer = Constants.PlayerShootCooldown;
        }
    }

    public override void Update(float dt)
    {
        // Use the overload with InputManager instead
    }

    public override void Draw()
    {
        if (!Active) return;
        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), Color.Blue);
    }
}
