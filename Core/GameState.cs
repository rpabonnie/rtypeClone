using System.Numerics;
using Raylib_cs;
using rtypeClone.Entities;
using rtypeClone.Systems;
using rtypeClone.Systems.AiSystem;
using rtypeClone.Systems.GemSystem;
using rtypeClone.Systems.RaritySystem;

namespace rtypeClone.Core;

public class GameState
{
    private readonly Player _player;
    private readonly ObjectPool<Projectile> _bulletPool;
    private readonly ObjectPool<Enemy> _enemyPool;
    private readonly ObjectPool<DamageNumber> _damageNumberPool;
    private readonly ScrollingBackground _background;
    private readonly WaveSpawner _waveSpawner;
    private readonly CollisionSystem _collisionSystem;
    private readonly AiSystem _aiSystem;
    private readonly AffixRegistry _affixRegistry;
    private readonly GemSystem _gemSystem;

    public bool DebugOverlay;

    public GameState()
    {
        _player = new Player();
        _bulletPool = new ObjectPool<Projectile>(Constants.BulletPoolSize);
        _enemyPool = new ObjectPool<Enemy>(Constants.EnemyPoolSize);
        _damageNumberPool = new ObjectPool<DamageNumber>(Constants.DamageNumberPoolSize);
        _background = new ScrollingBackground();
        _waveSpawner = new WaveSpawner();
        _collisionSystem = new CollisionSystem();
        _aiSystem = new AiSystem("Assets/ai_profiles");
        _affixRegistry = new AffixRegistry("Assets/affixes");
        _gemSystem = new GemSystem("Assets/gems");
    }

    public void Update(float dt, InputManager input)
    {
        // Toggle debug overlay
        if (Raylib.IsKeyPressed(KeyboardKey.F3))
            DebugOverlay = !DebugOverlay;
        if (Raylib.IsGamepadAvailable(0)
            && Raylib.IsGamepadButtonDown(0, GamepadButton.LeftThumb)
            && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightThumb))
            DebugOverlay = !DebugOverlay;

        _background.Update(dt);
        _player.Update(dt, input, _bulletPool, _gemSystem);
        _waveSpawner.Update(dt, _enemyPool, _affixRegistry);

        var aiCtx = new AiContext(dt, _player.Position, Constants.ScreenWidth, Constants.ScreenHeight);

        _bulletPool.ForEachActive((bullet, i) =>
        {
            bullet.Update(dt);
            if (bullet.IsOffScreen())
                _bulletPool.Return(i);
        });

        _enemyPool.ForEachActive((enemy, i) =>
        {
            enemy.UpdateAi(dt, _aiSystem, in aiCtx);
            if (enemy.IsOffScreen(Constants.EnemySpawnMargin))
                _enemyPool.Return(i);
        });

        _damageNumberPool.ForEachActive((dn, i) =>
        {
            dn.Update(dt);
            if (!dn.Active)
                _damageNumberPool.Return(i);
        });

        _collisionSystem.CheckCollisions(_player, _bulletPool, _enemyPool, _damageNumberPool);
    }

    public void Draw()
    {
        _background.Draw();
        _player.Draw();

        _bulletPool.ForEachActive((bullet, _) => bullet.Draw());
        _enemyPool.ForEachActive((enemy, _) => enemy.Draw());
        _damageNumberPool.ForEachActive((dn, _) => dn.Draw());

        // Debug overlay
        if (DebugOverlay)
        {
            DebugDraw.DrawFrameTime();
            _player.DrawDebugHitbox();
            _enemyPool.ForEachActive((enemy, _) =>
            {
                DebugDraw.DrawHitbox(enemy);
                DebugDraw.DrawAiLabel(enemy);
            });
            _bulletPool.ForEachActive((bullet, _) => DebugDraw.DrawHitbox(bullet));
        }

        // HUD
        Raylib.DrawText($"HP: {_player.Health}", 10, 10, 24, Color.White);
        Raylib.DrawText($"Score: {_player.Score}", 10, 40, 24, Color.White);
    }
}
