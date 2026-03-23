using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;

namespace rtypeClone.Entities;

public enum EnemyMovePattern
{
    Straight,
    Sine,
    Zigzag,
}

public class Enemy : Entity
{
    public int Health;

    private EnemyMovePattern _pattern;
    private float _aliveTimer;        // time since spawn, drives sine wave
    private float _spawnY;            // original Y for sine offset
    private float _zigzagTimer;       // countdown to next direction flip
    private float _zigzagDirection;   // +1 or -1

    public Enemy()
    {
        Width = 40f;
        Height = 32f;
    }

    public void Spawn(Vector2 position, Vector2 velocity, int health = 1,
                      EnemyMovePattern pattern = EnemyMovePattern.Straight)
    {
        Position = position;
        Velocity = velocity;
        Health = health;
        Active = true;
        _pattern = pattern;
        _aliveTimer = 0f;
        _spawnY = position.Y;
        _zigzagTimer = Constants.EnemyZigzagInterval;
        _zigzagDirection = 1f;
    }

    public override void Update(float dt)
    {
        _aliveTimer += dt;

        switch (_pattern)
        {
            case EnemyMovePattern.Straight:
                Position += Velocity * dt;
                break;

            case EnemyMovePattern.Sine:
                Position.X += Velocity.X * dt;
                Position.Y = _spawnY + MathF.Sin(_aliveTimer * Constants.EnemySineFrequency)
                             * Constants.EnemySineAmplitude;
                break;

            case EnemyMovePattern.Zigzag:
                Position.X += Velocity.X * dt;
                Position.Y += _zigzagDirection * Constants.EnemyZigzagVerticalSpeed * dt;

                // Clamp to screen bounds
                if (Position.Y < 0f)
                {
                    Position.Y = 0f;
                    _zigzagDirection = 1f;
                    _zigzagTimer = Constants.EnemyZigzagInterval;
                }
                else if (Position.Y + Height > Constants.ScreenHeight)
                {
                    Position.Y = Constants.ScreenHeight - Height;
                    _zigzagDirection = -1f;
                    _zigzagTimer = Constants.EnemyZigzagInterval;
                }

                _zigzagTimer -= dt;
                if (_zigzagTimer <= 0f)
                {
                    _zigzagDirection = -_zigzagDirection;
                    _zigzagTimer = Constants.EnemyZigzagInterval;
                }
                break;
        }
    }

    public override void Draw()
    {
        if (!Active) return;
        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), Color.Red);
    }
}
