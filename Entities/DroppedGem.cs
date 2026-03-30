using System.Numerics;
using Raylib_cs;

namespace rtypeClone.Entities;

public class DroppedGem : Entity
{
    public string GemId = "";

    private float _bobTimer;
    private float _baseY;

    private const float BobSpeed = 2.0f;
    private const float BobAmplitude = 4.0f;
    private const float DriftSpeed = 30f;
    private const float MaxLifetime = 8f;
    private float _age;

    public DroppedGem()
    {
        Width = 16f;
        Height = 16f;
    }

    public void Spawn(Vector2 position, string gemId)
    {
        Position = position;
        _baseY = position.Y;
        GemId = gemId;
        _bobTimer = 0f;
        _age = 0f;
        Active = true;
    }

    public override void Update(float dt)
    {
        _age += dt;
        if (_age >= MaxLifetime)
        {
            Active = false;
            return;
        }

        _bobTimer += dt;
        Position.Y = _baseY + MathF.Sin(_bobTimer * BobSpeed) * BobAmplitude;
        Position.X -= DriftSpeed * dt;

        if (Position.X < -32f)
            Active = false;
    }

    public override void Draw()
    {
        if (!Active) return;

        // Diamond shape placeholder — bright cyan gem
        var center = new Vector2(Position.X + Width / 2f, Position.Y + Height / 2f);
        float half = 7f;

        // Draw a diamond using two triangles
        var top = new Vector2(center.X, center.Y - half);
        var right = new Vector2(center.X + half, center.Y);
        var bottom = new Vector2(center.X, center.Y + half);
        var left = new Vector2(center.X - half, center.Y);

        Raylib.DrawTriangle(top, right, left, Color.SkyBlue);
        Raylib.DrawTriangle(left, right, bottom, Color.SkyBlue);

        // Bright center highlight
        Raylib.DrawCircleV(center, 2f, Color.White);
    }
}
