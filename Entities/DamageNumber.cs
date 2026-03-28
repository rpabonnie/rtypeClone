using System.Numerics;
using Raylib_cs;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Entities;

public class DamageNumber : Entity
{
    private string _text = "";
    private float _lifetime;
    private float _maxLifetime;
    private Color _color;
    private float _driftSpeed;

    public void Activate(Vector2 position, int amount, DamageType type)
    {
        Position = position;
        _text = amount.ToString();
        _color = ColorForType(type);
        _lifetime = 0f;
        _maxLifetime = 0.8f;
        _driftSpeed = 60f;
        Active = true;
    }

    public override void Update(float dt)
    {
        _lifetime += dt;
        Position.Y -= _driftSpeed * dt;
        if (_lifetime >= _maxLifetime) Active = false;
    }

    public override void Draw()
    {
        if (!Active) return;
        float alpha = 1f - (_lifetime / _maxLifetime);
        byte a = (byte)(255 * alpha);
        Raylib.DrawText(_text, (int)Position.X, (int)Position.Y, 16,
            new Color(_color.R, _color.G, _color.B, a));
    }

    private static Color ColorForType(DamageType t) => t switch
    {
        DamageType.NonElemental => Color.White,
        DamageType.Energy => new Color((byte)100, (byte)200, (byte)255, (byte)255),
        DamageType.Fire => Color.Orange,
        DamageType.Cold => Color.SkyBlue,
        _ => Color.White
    };
}
