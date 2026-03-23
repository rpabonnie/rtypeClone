using rtypeClone.Entities;
using rtypeClone.Systems;

namespace rtypeClone.Core;

public class GameState
{
    private readonly Player _player;
    private readonly ObjectPool<Projectile> _bulletPool;
    private readonly ObjectPool<Enemy> _enemyPool;
    private readonly ScrollingBackground _background;
    private readonly WaveSpawner _waveSpawner;
    private readonly CollisionSystem _collisionSystem;

    public GameState()
    {
        _player = new Player();
        _bulletPool = new ObjectPool<Projectile>(Constants.BulletPoolSize);
        _enemyPool = new ObjectPool<Enemy>(Constants.EnemyPoolSize);
        _background = new ScrollingBackground();
        _waveSpawner = new WaveSpawner();
        _collisionSystem = new CollisionSystem();
    }

    public void Update(float dt, InputManager input)
    {
        _background.Update(dt);
        _player.Update(dt, input, _bulletPool);
        _waveSpawner.Update(dt, _enemyPool);

        _bulletPool.ForEachActive((bullet, i) =>
        {
            bullet.Update(dt);
            if (bullet.IsOffScreen())
                _bulletPool.Return(i);
        });

        _enemyPool.ForEachActive((enemy, i) =>
        {
            enemy.Update(dt);
            if (enemy.IsOffScreen())
                _enemyPool.Return(i);
        });

        _collisionSystem.CheckCollisions(_player, _bulletPool, _enemyPool);
    }

    public void Draw()
    {
        _background.Draw();
        _player.Draw();

        _bulletPool.ForEachActive((bullet, _) => bullet.Draw());
        _enemyPool.ForEachActive((enemy, _) => enemy.Draw());
    }
}
