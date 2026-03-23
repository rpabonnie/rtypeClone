using System.Numerics;
using rtypeClone.Core;
using rtypeClone.Entities;

namespace rtypeClone.Systems;

public class WaveSpawner
{
    private float _spawnTimer;
    private const float SpawnInterval = 1.5f;

    public void Update(float dt, ObjectPool<Enemy> enemyPool)
    {
        _spawnTimer -= dt;
        if (_spawnTimer <= 0f)
        {
            var enemy = enemyPool.Get();
            if (enemy != null)
            {
                float y = 50f + Random.Shared.Next(0, Constants.ScreenHeight - 100);
                enemy.Spawn(
                    new Vector2(Constants.ScreenWidth + 40f, y),
                    new Vector2(-200f, 0f)
                );
            }
            _spawnTimer = SpawnInterval;
        }
    }
}
