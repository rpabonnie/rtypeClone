using System.Numerics;
using rtypeClone.Core;
using rtypeClone.Entities;
using rtypeClone.Systems.AiSystem;
using rtypeClone.Systems.RaritySystem;

namespace rtypeClone.Systems;

public class WaveSpawner
{
    private float _spawnTimer;
    private int _waveNumber;
    private int _spawnsThisWave;
    private const float SpawnInterval = 1.5f;
    private const int SpawnsPerWave = 8;

    // Movement-only profiles (no shooting)
    private static readonly string[] MovementProfiles =
    {
        "straight", "sine_wave", "zigzag",
        "reverse_entry",
        "dive_top", "dive_bottom",
    };

    // Attack profiles (shoot back)
    private static readonly string[] AttackProfiles =
    {
        "fodder_shooter",    // sine + aimed shot
        "fodder_burst",      // straight + burst fire
        "fodder_spray",      // sine + spray
        "fodder_mine",       // straight + mine layer
        "fodder_suicide",    // sine + suicide dive
    };

    private readonly RarityRoller _rarityRoller = new();

    // Scratch buffer for affix rolling — max 4 affixes (Rare ceiling)
    private readonly string[] _affixScratch = new string[4];

    public void Update(float dt, ObjectPool<Enemy> enemyPool, AffixRegistry affixRegistry,
                       AiSystem.AiSystem aiSystem)
    {
        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;

        var enemy = enemyPool.Get();
        if (enemy != null)
        {
            // Wave-scaled shooter ratio: wave 1-3 = 25%, wave 4-7 = 50%, wave 8+ = 75%
            float shooterChance = _waveNumber switch
            {
                <= 2 => 0.25f,
                <= 6 => 0.50f,
                _    => 0.75f,
            };

            string profileId = Random.Shared.NextSingle() < shooterChance
                ? AttackProfiles[Random.Shared.Next(AttackProfiles.Length)]
                : MovementProfiles[Random.Shared.Next(MovementProfiles.Length)];

            float speed = Constants.EnemyBaseSpeed + Random.Shared.Next(-40, 41);

            // Roll rarity and affixes
            EnemyRarity rarity = _rarityRoller.RollRarity(_waveNumber);
            Span<string> affixSpan = _affixScratch.AsSpan();
            int affixCount = _rarityRoller.RollAffixes(rarity, affixRegistry, affixSpan);
            var rolledAffixes = affixSpan[..affixCount];

            // Compute combined affix modifiers
            var combined = AffixModifiers.None;
            foreach (var affixId in rolledAffixes)
            {
                if (affixRegistry.TryGet(affixId, out var affixDef) && affixDef != null)
                {
                    var mods = affixDef.Modifiers;
                    combined.Combine(in mods);
                }
            }

            speed *= combined.SpeedMultiplier;

            int hp = rarity switch
            {
                EnemyRarity.Normal => 1,
                EnemyRarity.Magic  => 3,
                EnemyRarity.Rare   => 8,
                EnemyRarity.Unique => 20,
                _                  => 1
            };

            int shield = combined.ShieldHp;
            string displayName = RarityRoller.BuildDisplayName(rarity, rolledAffixes, affixRegistry);

            // Determine spawn position and initial velocity from the profile's entry direction
            string entryDir = aiSystem.GetEntryDirection(profileId);
            var (spawnPos, spawnVel) = BuildSpawnTransform(entryDir, speed);

            enemy.Spawn(spawnPos, spawnVel,
                hp: hp, shield: shield,
                aiProfileId: profileId,
                rarity: rarity,
                displayName: displayName);
        }

        _spawnTimer = SpawnInterval;
        _spawnsThisWave++;

        if (_spawnsThisWave >= SpawnsPerWave)
        {
            _waveNumber++;
            _spawnsThisWave = 0;
        }
    }

    /// <summary>
    /// Returns the spawn position and initial velocity for a given entry direction.
    /// </summary>
    private static (Vector2 pos, Vector2 vel) BuildSpawnTransform(string entryDir, float speed)
    {
        const float Margin = 50f;

        return entryDir switch
        {
            // Enters from left edge, moves right
            "left" => (
                new Vector2(-Margin, RandomY()),
                new Vector2(speed, 0f)),

            // Enters from top edge, moves downward (DiveHandler takes over velocity)
            "top" => (
                new Vector2(RandomX(), -Margin),
                new Vector2(0f, 0f)),   // DiveHandler sets velocity on first frame

            // Enters from bottom edge, moves upward (DiveHandler takes over velocity)
            "bottom" => (
                new Vector2(RandomX(), Constants.ScreenHeight + Margin),
                new Vector2(0f, 0f)),   // DiveHandler sets velocity on first frame

            // Default: enters from right edge, moves left
            _ => (
                new Vector2(Constants.ScreenWidth + Margin, RandomY()),
                new Vector2(-speed, 0f)),
        };
    }

    private static float RandomY() =>
        50f + Random.Shared.Next(0, Constants.ScreenHeight - 100);

    private static float RandomX() =>
        400f + Random.Shared.Next(0, Constants.ScreenWidth - 800);
}
