using System.Numerics;
using rtypeClone.Core;
using rtypeClone.Entities;

namespace rtypeClone.Systems;

public class WaveSpawner
{
    private float _spawnTimer;
    private const float SpawnInterval = 1.5f;

    private static readonly EnemyMovePattern[] Patterns =
    {
        EnemyMovePattern.Straight,
        EnemyMovePattern.Sine,
        EnemyMovePattern.Zigzag,
    };

    public void Update(float dt, ObjectPool<Enemy> enemyPool)
    {
        _spawnTimer -= dt;
        if (_spawnTimer <= 0f)
        {
            var enemy = enemyPool.Get();
            if (enemy != null)
            {
                float y = 50f + Random.Shared.Next(0, Constants.ScreenHeight - 100);
                var pattern = Patterns[Random.Shared.Next(Patterns.Length)];

                // Vary horizontal speed slightly per enemy
                float speed = Constants.EnemyBaseSpeed + Random.Shared.Next(-40, 41);

                enemy.Spawn(
                    new Vector2(Constants.ScreenWidth + 40f, y),
                    new Vector2(-speed, 0f),
                    health: 1,
                    pattern: pattern
                );
            }
            _spawnTimer = SpawnInterval;
        }
    }
}
