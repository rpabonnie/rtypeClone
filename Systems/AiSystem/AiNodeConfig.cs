using System.Text.Json.Serialization;

namespace rtypeClone.Systems.AiSystem;

public class AiNodeConfig
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    // Sine
    [JsonPropertyName("amplitude")]
    public float Amplitude { get; init; }
    [JsonPropertyName("frequency")]
    public float Frequency { get; init; }

    // Zigzag
    [JsonPropertyName("verticalSpeed")]
    public float VerticalSpeed { get; init; }
    [JsonPropertyName("flipInterval")]
    public float FlipInterval { get; init; }

    // Charge / Retreat (Phase 2+)
    [JsonPropertyName("chargeSpeed")]
    public float ChargeSpeed { get; init; }
    [JsonPropertyName("targetPlayer")]
    public bool TargetPlayer { get; init; }
    [JsonPropertyName("retreatDistance")]
    public float RetreatDistance { get; init; }
    [JsonPropertyName("retreatSpeed")]
    public float RetreatSpeed { get; init; }
    [JsonPropertyName("duration")]
    public float Duration { get; init; }

    // Shooting (Phase 2+)
    [JsonPropertyName("fireCooldown")]
    public float FireCooldown { get; init; }
    [JsonPropertyName("projectileSpeed")]
    public float ProjectileSpeed { get; init; }

    // Formation (Phase 3)
    [JsonPropertyName("role")]
    public string Role { get; init; } = "";
    [JsonPropertyName("slotCount")]
    public int SlotCount { get; init; }
    [JsonPropertyName("slotSpacing")]
    public float SlotSpacing { get; init; }

    // Shield (Phase 3)
    [JsonPropertyName("shieldRadius")]
    public float ShieldRadius { get; init; }
    [JsonPropertyName("shieldedRarity")]
    public string ShieldedRarity { get; init; } = "";

    // Attack (references an EnemyAttackConfig by id)
    [JsonPropertyName("attackId")]
    public string AttackId { get; init; } = "";

    // Dive entry handler
    [JsonPropertyName("diveSpeed")]
    public float DiveSpeed { get; init; } = 400f;
    [JsonPropertyName("curveAfter")]
    public float CurveAfter { get; init; } = 300f;
    [JsonPropertyName("exitDirection")]
    public string ExitDirection { get; init; } = "left";
}
