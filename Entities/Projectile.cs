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
    private Rectangle _srcRect;

    /// <summary>
    /// Spawn using resolved ProjectileParameters. Data-driven path.
    /// </summary>
    public void Spawn(Vector2 position, in ProjectileParameters param)
    {
        Position = position;
        Velocity = new Vector2(param.Speed, 0f);
        Width = param.Width;
        Height = param.Height;
        Damage = param.Damage;
        Pierce = param.Pierce;
        HitsRemaining = param.Pierce + 1; // pierce=0 means 1 hit then despawn, pierce=2 means 3 hits
        Active = true;

        // Determine visual based on size (charged shots are larger)
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
    /// Legacy spawn path — still used internally for backward compatibility.
    /// Will be removed once all callers use ProjectileParameters.
    /// </summary>
    public void Spawn(Vector2 position, Vector2 velocity, int chargeLevel = 0)
    {
        Position = position;
        Velocity = velocity;
        ChargeLevel = chargeLevel;
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
