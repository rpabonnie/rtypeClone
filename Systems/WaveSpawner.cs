using System.Numerics;
using rtypeClone.Core;
using rtypeClone.Entities;
using rtypeClone.Systems.RaritySystem;

namespace rtypeClone.Systems;

public class WaveSpawner
{
    private float _spawnTimer;
    private int _waveNumber;
    private int _spawnsThisWave;
    private const float SpawnInterval = 1.5f;
    private const int SpawnsPerWave = 8;

    private static readonly string[] ProfileIds = { "straight", "sine_wave", "zigzag", "fodder_shooter" };

    private readonly RarityRoller _rarityRoller = new();

    // Scratch buffer for affix rolling — max 4 affixes (Rare ceiling)
    private readonly string[] _affixScratch = new string[4];

    public void Update(float dt, ObjectPool<Enemy> enemyPool, AffixRegistry affixRegistry)
    {
        _spawnTimer -= dt;
        if (_spawnTimer <= 0f)
        {
            var enemy = enemyPool.Get();
            if (enemy != null)
            {
                float y = 50f + Random.Shared.Next(0, Constants.ScreenHeight - 100);
                string profileId = ProfileIds[Random.Shared.Next(ProfileIds.Length)];
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

                // Apply speed multiplier from affixes
                speed *= combined.SpeedMultiplier;

                // Higher rarity enemies get more HP
                int hp = rarity switch
                {
                    EnemyRarity.Normal => 1,
                    EnemyRarity.Magic  => 3,
                    EnemyRarity.Rare   => 8,
                    EnemyRarity.Unique => 20,
                    _ => 1
                };

                int shield = combined.ShieldHp;

                string displayName = RarityRoller.BuildDisplayName(rarity, rolledAffixes, affixRegistry);

                enemy.Spawn(
                    new Vector2(Constants.ScreenWidth + 40f, y),
                    new Vector2(-speed, 0f),
                    hp: hp,
                    shield: shield,
                    aiProfileId: profileId,
                    rarity: rarity,
                    displayName: displayName
                );
            }

            _spawnTimer = SpawnInterval;
            _spawnsThisWave++;

            if (_spawnsThisWave >= SpawnsPerWave)
            {
                _waveNumber++;
                _spawnsThisWave = 0;
            }
        }
    }
}
