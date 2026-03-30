using System.Text.Json.Serialization;

namespace rtypeClone.Systems.CombatSystem;

public class EnemyAttackConfig
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("category")]
    public string Category { get; init; } = "projectile";

    [JsonPropertyName("cooldown")]
    public float Cooldown { get; init; } = 2.0f;

    [JsonPropertyName("telegraphTime")]
    public float TelegraphTime { get; init; } = 0.4f;

    [JsonPropertyName("burstCount")]
    public int BurstCount { get; init; } = 1;

    [JsonPropertyName("burstInterval")]
    public float BurstInterval { get; init; } = 0.1f;

    [JsonPropertyName("projectileSpeed")]
    public float ProjectileSpeed { get; init; } = 350f;

    [JsonPropertyName("damage")]
    public int Damage { get; init; } = 1;

    [JsonPropertyName("lifetime")]
    public float Lifetime { get; init; } = 5f;

    [JsonPropertyName("count")]
    public int Count { get; init; } = 1;

    [JsonPropertyName("spreadAngleDeg")]
    public float SpreadAngleDeg { get; init; } = 0f;

    [JsonPropertyName("homing")]
    public bool Homing { get; init; }

    [JsonPropertyName("homingStrength")]
    public float HomingStrength { get; init; }

    [JsonPropertyName("pierce")]
    public int Pierce { get; init; }

    [JsonPropertyName("stationary")]
    public bool Stationary { get; init; }

    [JsonPropertyName("aimMode")]
    public string AimMode { get; init; } = "at_player";

    [JsonPropertyName("aimOffsetDeg")]
    public float AimOffsetDeg { get; init; }

    [JsonPropertyName("projectileWidth")]
    public float ProjectileWidth { get; init; } = 8f;

    [JsonPropertyName("projectileHeight")]
    public float ProjectileHeight { get; init; } = 8f;
}
