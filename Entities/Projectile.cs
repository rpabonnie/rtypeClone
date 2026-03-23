using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Entities;

public class Projectile : Entity
{
    public int Damage;
    public int ChargeLevel;
    private Rectangle _srcRect;

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
