using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Entities;

public class Projectile : Entity
{
    public int Damage;
    public int ChargeLevel;

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
        }
        else
        {
            Width = Constants.NormalBulletWidth;
            Height = Constants.NormalBulletHeight;
            Damage = Constants.NormalBulletDamage;
        }
    }

    public override void Update(float dt)
    {
        Position += Velocity * dt;
    }

    public override void Draw()
    {
        if (!Active) return;
        Color color = ChargeLevel >= 1 ? Color.Orange : Color.Yellow;
        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), color);
    }
}
