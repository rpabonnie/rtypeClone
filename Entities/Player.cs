using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Entities;

public class Player : Entity
{
    private float _chargeTimer;
    private bool _isCharging;
    private float _iFrameTimer;
    public int Health = 3;
    public int Score;
    public bool IsInvincible => _iFrameTimer > 0f;

    public Player()
    {
        Width = 48f;
        Height = 32f;
        Position = new Vector2(100f, Constants.ScreenHeight / 2f);
        Active = true;
    }

    public void TakeHit()
    {
        if (IsInvincible) return;
        Health--;
        _iFrameTimer = Constants.IFrameDuration;
    }

    public void Update(float dt, InputManager input, ObjectPool<Projectile> bulletPool)
    {
        // Invincibility timer
        if (_iFrameTimer > 0f)
            _iFrameTimer -= dt;

        // Movement
        Position += input.Movement * Constants.PlayerSpeed * dt;
        Position.X = Math.Clamp(Position.X, 0f, Constants.ScreenWidth - Width);
        Position.Y = Math.Clamp(Position.Y, 0f, Constants.ScreenHeight - Height);

        // Shooting: tap fires immediately, hold charges for big shot on release
        if (input.ShootPressed)
        {
            FireBullet(bulletPool, 0);
            _isCharging = true;
            _chargeTimer = 0f;
        }

        if (_isCharging && input.ShootHeld)
        {
            _chargeTimer += dt;
        }

        if (_isCharging && input.ShootReleased)
        {
            if (_chargeTimer >= Constants.ChargeTimeLevel1)
            {
                FireBullet(bulletPool, 1);
            }
            _isCharging = false;
            _chargeTimer = 0f;
        }
    }

    private void FireBullet(ObjectPool<Projectile> pool, int chargeLevel)
    {
        float speed = chargeLevel >= 1 ? Constants.ChargedBulletSpeed : Constants.BulletSpeed;
        var bullet = pool.Get();
        if (bullet != null)
        {
            bullet.Spawn(
                new Vector2(Position.X + Width, Position.Y + Height / 2f - 2f),
                new Vector2(speed, 0f),
                chargeLevel
            );
        }
    }

    public override void Update(float dt)
    {
        // Use the overload with InputManager instead
    }

    public override void Draw()
    {
        if (!Active) return;

        // Skip rendering every other flash interval during i-frames
        if (IsInvincible)
        {
            int flashIndex = (int)(_iFrameTimer / Constants.IFrameFlashInterval);
            if (flashIndex % 2 == 1) return;
        }

        // Charge indicator — only show after initial tap phase
        if (_isCharging && _chargeTimer > 0.1f)
        {
            float chargePercent = MathF.Min(_chargeTimer / Constants.ChargeTimeLevel1, 1f);
            bool fullyCharged = chargePercent >= 1f;

            // Charge bar under ship
            float barWidth = chargePercent * Width;
            Color barColor = fullyCharged ? Color.Orange : Color.Yellow;
            Raylib.DrawRectangleV(
                new Vector2(Position.X, Position.Y + Height + 4f),
                new Vector2(barWidth, 4f),
                barColor
            );

            // Pulsing glow around ship while charging
            byte alpha = (byte)(100 + (int)(80f * MathF.Sin(_chargeTimer * 8f)));
            Color glowColor = fullyCharged
                ? new Color((byte)255, (byte)160, (byte)0, alpha)
                : new Color((byte)255, (byte)255, (byte)0, alpha);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(Position.X - 4f, Position.Y - 4f, Width + 8f, Height + 8f),
                2f, glowColor
            );

            // Energy ball at nose of ship when fully charged
            if (fullyCharged)
            {
                float pulse = 6f + 2f * MathF.Sin(_chargeTimer * 12f);
                Raylib.DrawCircleV(
                    new Vector2(Position.X + Width + 4f, Position.Y + Height / 2f),
                    pulse,
                    new Color((byte)255, (byte)200, (byte)50, (byte)200)
                );
            }
        }

        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), Color.Blue);
    }

    public void DrawDebugHitbox()
    {
        Raylib.DrawRectangleLinesEx(Bounds, 1f, Color.Lime);
    }
}
