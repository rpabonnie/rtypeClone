using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Systems.AiSystem;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Entities;

public class Enemy : Entity
{
    public EnemyHealth Health;
    public string AiProfileId = "";
    public EnemyAiState AiState;
    public EnemyRarity Rarity;

    /// <summary>
    /// Display name shown above Magic/Rare/Unique enemies.
    /// Built from affix names at spawn time — cached, not allocated per frame.
    /// </summary>
    public string DisplayName = "";

    public Enemy()
    {
        Width = 40f;
        Height = 32f;
    }

    public void Spawn(Vector2 position, Vector2 velocity, int hp = 1, int shield = 0,
                      string aiProfileId = "straight", EnemyRarity rarity = EnemyRarity.Normal,
                      string displayName = "")
    {
        Position = position;
        Velocity = velocity;
        Health = EnemyHealth.Create(hp, shield);
        AiProfileId = aiProfileId;
        Rarity = rarity;
        DisplayName = displayName;
        AiState = new EnemyAiState
        {
            AliveTimer = 0f,
            SpawnY = position.Y,
            ZigzagTimer = Constants.EnemyZigzagInterval,
            ZigzagDirection = 1f,
        };
        Active = true;
    }

    public int TakeDamage(DamageEvent dmg) => Health.ApplyDamage(dmg);

    public override void Update(float dt)
    {
        // Movement is now driven by AiSystem via GameState — this is a fallback
        AiState.AliveTimer += dt;
        Position += Velocity * dt;
    }

    public void UpdateAi(float dt, AiSystem aiSystem, in AiContext ctx)
    {
        AiState.AliveTimer += dt;
        aiSystem.Update(ref Position, ref Velocity, ref AiState, AiProfileId, in ctx);
    }

    public override void Draw()
    {
        if (!Active) return;

        // Use rarity color as the enemy tint (replaces hard-coded Color.Red)
        Color tint = RarityConstants.GetColor(Rarity);
        Raylib.DrawRectangleV(Position, new Vector2(Width, Height), tint);

        // Rarity name label above Magic/Rare/Unique enemies
        if (Rarity != EnemyRarity.Normal && DisplayName.Length > 0)
        {
            int textWidth = Raylib.MeasureText(DisplayName, 16);
            float labelX = Position.X + Width / 2f - textWidth / 2f;
            float labelY = Position.Y - 22f;

            // Health bar pushes the name label higher
            if (Health.MaxHp > 1)
                labelY -= 10f;

            Raylib.DrawText(DisplayName, (int)labelX, (int)labelY, 16, tint);
        }

        // Health bar for enemies with more than 1 max HP
        if (Health.MaxHp > 1)
            DrawHealthBar();
    }

    private void DrawHealthBar()
    {
        const float BarWidth = 48f;
        const float BarHeight = 5f;
        float barY = Position.Y - BarHeight - 4f;

        Raylib.DrawRectangleV(new Vector2(Position.X, barY),
            new Vector2(BarWidth, BarHeight), Color.DarkGray);

        float hpW = BarWidth * Health.HpPercent;
        Raylib.DrawRectangleV(new Vector2(Position.X, barY),
            new Vector2(hpW, BarHeight), Color.Green);

        if (Health.HasShield)
        {
            float shW = BarWidth * Health.ShieldPercent;
            Raylib.DrawRectangleV(new Vector2(Position.X, barY),
                new Vector2(shW, BarHeight),
                new Color((byte)100, (byte)180, (byte)255, (byte)200));
        }
    }
}
