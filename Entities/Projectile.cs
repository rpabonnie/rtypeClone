using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Systems.ModuleSystem;

namespace rtypeClone.Entities;

public class Projectile : Entity
{
    public int Damage;
    public int Pierce;
    public int HitsRemaining;
    public int ChargeLevel;
    public bool Homing;
    public float HomingStrength;
    public float Speed; // Cached for homing (needs to maintain constant speed while turning)
    private Rectangle _srcRect;

    /// <summary>
    /// Spawn using resolved ProjectileParameters. Data-driven path.
    /// </summary>
    public void Spawn(Vector2 position, in ProjectileParameters param)
    {
        Position = position;
        Speed = param.Speed;
        Velocity = new Vector2(param.Speed, 0f);
        Width = param.Width;
        Height = param.Height;
        Damage = param.Damage;
        Pierce = param.Pierce;
        HitsRemaining = param.Pierce + 1;
        Homing = param.Homing;
        HomingStrength = param.HomingStrength;
        Active = true;

        if (param.Width >= 20f)
        {
            ChargeLevel = 1;
            _srcRect = AssetManager.ChargedBulletSrc;
        }
        else
        {
            ChargeLevel = 0;
            _srcRect = AssetManager.NormalBulletSrc;
        }
    }

    /// <summary>
    /// Legacy spawn path — kept for backward compatibility.
    /// </summary>
    public void Spawn(Vector2 position, Vector2 velocity, int chargeLevel = 0)
    {
        Position = position;
        Velocity = velocity;
        Speed = velocity.Length();
        ChargeLevel = chargeLevel;
        Homing = false;
        HomingStrength = 0f;
        Active = true;

        if (chargeLevel >= 1)
        {
            Width = Constants.ChargedBulletWidth;
            Height = Constants.ChargedBulletHeight;
            Damage = Constants.ChargedBulletDamage;
            _srcRect = AssetManager.ChargedBulletSrc;
        }
        else
        {
            Width = Constants.NormalBulletWidth;
            Height = Constants.NormalBulletHeight;
            Damage = Constants.NormalBulletDamage;
            _srcRect = AssetManager.NormalBulletSrc;
        }
    }

    public override void Update(float dt)
    {
        Position += Velocity * dt;
    }

    /// <summary>
    /// Steers this projectile toward the nearest enemy. Called after Update()
    /// only for homing projectiles. Rotates velocity toward target while
    /// maintaining constant speed.
    /// </summary>
    public void UpdateHoming(float dt, ObjectPool<Enemy> enemies)
    {
        if (!Homing || !Active) return;

        // Find nearest active enemy
        var bulletCenter = new Vector2(Position.X + Width / 2f, Position.Y + Height / 2f);
        float bestDistSq = float.MaxValue;
        Vector2 bestTarget = default;
        bool found = false;

        enemies.ForEachActive((enemy, _) =>
        {
            var enemyCenter = new Vector2(enemy.Position.X + enemy.Width / 2f,
                                          enemy.Position.Y + enemy.Height / 2f);
            float distSq = Vector2.DistanceSquared(bulletCenter, enemyCenter);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestTarget = enemyCenter;
                found = true;
            }
        });

        if (!found) return;

        // Steer toward target: blend current direction with desired direction
        var desired = Vector2.Normalize(bestTarget - bulletCenter);
        var current = Velocity.LengthSquared() > 0.001f
            ? Vector2.Normalize(Velocity)
            : new Vector2(1f, 0f);

        // Lerp direction by homing strength (higher = tighter tracking)
        var blended = Vector2.Lerp(current, desired, HomingStrength * dt);
        if (blended.LengthSquared() > 0.001f)
        {
            Velocity = Vector2.Normalize(blended) * Speed;
        }
    }

    public override void Draw()
    {
        if (!Active) return;

        Rectangle dest = new(Position.X, Position.Y, Width, Height);
        Raylib.DrawTexturePro(
            AssetManager.BulletSheet,
            _srcRect,
            dest,
            Vector2.Zero,
            0f,
            Color.White
        );
    }
}
