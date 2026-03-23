using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Entities;

public class Player : Entity
{
    private float _shootTimer;
    private float _chargeTimer;
    private bool _isCharging;

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
        Position.X = Math.Clamp(Position.X, 0f, Constants.ScreenWidth - Width);
        Position.Y = Math.Clamp(Position.Y, 0f, Constants.ScreenHeight - Height);

        // Charge shooting
        _shootTimer -= dt;

        if (input.ShootPressed && _shootTimer <= 0f)
        {
            _isCharging = true;
            _chargeTimer = 0f;
        }

        if (_isCharging && input.ShootHeld)
        {
            _chargeTimer += dt;
        }

        if (_isCharging && input.ShootReleased)
        {
            int chargeLevel = _chargeTimer >= Constants.ChargeTimeLevel1 ? 1 : 0;
            float speed = chargeLevel >= 1 ? Constants.ChargedBulletSpeed : Constants.BulletSpeed;

            var bullet = bulletPool.Get();
            if (bullet != null)
            {
                bullet.Spawn(
                    new Vector2(Position.X + Width, Position.Y + Height / 2f - 2f),
                    new Vector2(speed, 0f),
                    chargeLevel
                );
            }

            _isCharging = false;
            _chargeTimer = 0f;
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

        // Charge indicator
        if (_isCharging)
        {
            float chargePercent = MathF.Min(_chargeTimer / Constants.ChargeTimeLevel1, 1f);
            float barWidth = chargePercent * Width;
            Color barColor = chargePercent >= 1f ? Color.Orange : Color.Yellow;

            Raylib.DrawRectangleV(
                new Vector2(Position.X, Position.Y + Height + 4f),
                new Vector2(barWidth, 4f),
                barColor
            );

            if (chargePercent >= 1f)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(Position.X - 3f, Position.Y - 3f, Width + 6f, Height + 6f),
                    2f, Color.Orange
                );
            }
        }

        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), Color.Blue);
    }
}
