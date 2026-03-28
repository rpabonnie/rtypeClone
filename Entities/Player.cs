using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Systems.ModuleSystem;

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

    public bool IsDead => Health <= 0;

    public void TakeHit()
    {
        if (IsInvincible || IsDead) return;
        Health--;
        if (Health > 0)
            _iFrameTimer = Constants.IFrameDuration;
    }

    /// <summary>Active weapon slot index (0..3). Tap fires base, hold+release fires charged.</summary>
    public int ActiveWeaponSlot;

    /// <summary>
    /// Main update with module system. Tap fires base params, charge-release fires charged params from the same slot.
    /// </summary>
    public void Update(float dt, InputManager input, ObjectPool<Projectile> bulletPool, ModuleSystem moduleSystem)
    {
        // Invincibility timer
        if (_iFrameTimer > 0f)
            _iFrameTimer -= dt;

        // Movement
        Position += input.Movement * Constants.PlayerSpeed * dt;
        Position.X = Math.Clamp(Position.X, 0f, Constants.ScreenWidth - Width);
        Position.Y = Math.Clamp(Position.Y, 0f, Constants.ScreenHeight - Height);

        // Shooting: tap fires base parameters, hold charges for charged on release
        if (input.ShootPressed)
        {
            FireBullet(bulletPool, moduleSystem, ActiveWeaponSlot, charged: false);
            _isCharging = true;
            _chargeTimer = 0f;
        }

        if (_isCharging && input.ShootHeld)
        {
            _chargeTimer += dt;
        }

        if (_isCharging && input.ShootReleased)
        {
            if (_chargeTimer >= Constants.ChargeTimeLevel1
                && moduleSystem.SlotHasChargedMode(ActiveWeaponSlot))
            {
                FireBullet(bulletPool, moduleSystem, ActiveWeaponSlot, charged: true);
            }
            _isCharging = false;
            _chargeTimer = 0f;
        }
    }

    /// <summary>
    /// Fire projectile(s) using resolved parameters from the given weapon slot.
    /// Handles multishot (Count > 1) by fanning bullets across SpreadAngleDeg.
    /// </summary>
    private void FireBullet(ObjectPool<Projectile> pool, ModuleSystem moduleSystem, int slot, bool charged)
    {
        if (!moduleSystem.HasWeaponModule(slot)) return;

        ref readonly var param = ref charged
            ? ref moduleSystem.GetCharged(slot)
            : ref moduleSystem.GetActive(slot);

        int count = Math.Max(param.Count, 1);
        float totalSpread = param.SpreadAngleDeg * MathF.PI / 180f; // convert to radians
        float startAngle = count > 1 ? -totalSpread / 2f : 0f;
        float step = count > 1 ? totalSpread / (count - 1) : 0f;

        var nosePos = new Vector2(Position.X + Width, Position.Y + Height / 2f);

        for (int i = 0; i < count; i++)
        {
            var bullet = pool.Get();
            if (bullet == null) break;

            float angle = startAngle + step * i;
            bullet.Spawn(
                new Vector2(nosePos.X, nosePos.Y - param.Height / 2f),
                in param
            );

            // Apply spread angle to velocity if non-zero
            if (MathF.Abs(angle) > 0.001f)
            {
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                bullet.Velocity = new Vector2(
                    param.Speed * cos,
                    param.Speed * sin
                );
            }
        }
    }

    public override void Update(float dt)
    {
        // Use the overload with InputManager + ModuleSystem instead
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
