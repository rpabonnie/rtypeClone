using System.Numerics;
using Raylib_cs;
using rtypeClone.Entities;
using rtypeClone.Systems;
using rtypeClone.Systems.AiSystem;
using rtypeClone.Systems.CombatSystem;
using rtypeClone.Systems.ModuleSystem;
using rtypeClone.Systems.RaritySystem;
using rtypeClone.Systems.DropSystem;
using rtypeClone.Systems.UI;

namespace rtypeClone.Core;

public enum GameScene
{
    Playing,
    PauseMenu,
    Inventory
}

public class GameState
{
    private readonly Player _player;
    private readonly ObjectPool<Projectile> _bulletPool;
    private readonly ObjectPool<Enemy> _enemyPool;
    private readonly ObjectPool<EnemyProjectile> _enemyProjectilePool;
    private readonly ObjectPool<DamageNumber> _damageNumberPool;
    private readonly ScrollingBackground _background;
    private readonly WaveSpawner _waveSpawner;
    private readonly CollisionSystem _collisionSystem;
    private readonly AiSystem _aiSystem;
    private readonly AffixRegistry _affixRegistry;
    private readonly EnemyAttackRegistry _attackRegistry;
    private readonly ModuleSystem _moduleSystem;
    private readonly ObjectPool<DroppedGem> _droppedGemPool;
    private readonly DropTableRegistry _dropTableRegistry;
    private readonly DropSystem _dropSystem;
    private readonly GemInventory _gemInventory;
    private readonly PauseMenu _pauseMenu;
    private readonly LoadoutScreen _loadoutScreen;

    public GameScene Scene { get; private set; } = GameScene.Playing;
    public bool DebugOverlay;
    public bool ExitRequested { get; private set; }

    // Track whether MenuPressed was already consumed this frame to avoid double-toggle
    private bool _menuConsumed;

    public GameState()
    {
        _player = new Player();
        _bulletPool = new ObjectPool<Projectile>(Constants.BulletPoolSize);
        _enemyPool = new ObjectPool<Enemy>(Constants.EnemyPoolSize);
        _enemyProjectilePool = new ObjectPool<EnemyProjectile>(Constants.EnemyProjectilePoolSize);
        _damageNumberPool = new ObjectPool<DamageNumber>(Constants.DamageNumberPoolSize);
        _background = new ScrollingBackground();
        _waveSpawner = new WaveSpawner();
        _collisionSystem = new CollisionSystem();
        _attackRegistry = new EnemyAttackRegistry("Assets/attacks");
        _aiSystem = new AiSystem("Assets/ai_profiles", _enemyProjectilePool, _attackRegistry);
        _affixRegistry = new AffixRegistry("Assets/affixes");
        _moduleSystem = new ModuleSystem("Assets/modules");
        _droppedGemPool = new ObjectPool<DroppedGem>(Constants.DroppedGemPoolSize);
        _dropTableRegistry = new DropTableRegistry("Assets/drop_tables");
        _dropSystem = new DropSystem(_dropTableRegistry);
        _gemInventory = new GemInventory();
        _pauseMenu = new PauseMenu();
        _loadoutScreen = new LoadoutScreen();
    }

    public void Update(float dt, InputManager input)
    {
        // Toggle debug overlay (works in all scenes)
        if (Raylib.IsKeyPressed(KeyboardKey.F3))
            DebugOverlay = !DebugOverlay;
        if (Raylib.IsGamepadAvailable(0)
            && Raylib.IsGamepadButtonDown(0, GamepadButton.LeftThumb)
            && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightThumb))
            DebugOverlay = !DebugOverlay;

        _menuConsumed = false;

        switch (Scene)
        {
            case GameScene.Playing:
                UpdatePlaying(dt, input);
                break;
            case GameScene.PauseMenu:
                UpdatePauseMenu(input);
                break;
            case GameScene.Inventory:
                UpdateInventory(input);
                break;
        }
    }

    private void UpdatePlaying(float dt, InputManager input)
    {
        // Controller Start/Menu → inventory directly; Keyboard Escape → pause menu
        if (input.InventoryPressed)
        {
            _loadoutScreen.Reset();
            Scene = GameScene.Inventory;
            _menuConsumed = true;
            return;
        }
        if (input.PauseMenuPressed)
        {
            _pauseMenu.Reset();
            Scene = GameScene.PauseMenu;
            _menuConsumed = true;
            return;
        }

        _background.Update(dt);
        _player.Update(dt, input, _bulletPool, _moduleSystem);
        _waveSpawner.Update(dt, _enemyPool, _affixRegistry, _aiSystem);

        var aiCtx = new AiContext(dt, _player.Position, Constants.ScreenWidth, Constants.ScreenHeight);

        _bulletPool.ForEachActive((bullet, i) =>
        {
            bullet.Update(dt);
            bullet.UpdateHoming(dt, _enemyPool);
            if (bullet.IsOffScreen())
                _bulletPool.Return(i);
        });

        _enemyPool.ForEachActive((enemy, i) =>
        {
            enemy.UpdateAi(dt, _aiSystem, in aiCtx);
            if (enemy.IsOffScreen(Constants.EnemySpawnMargin))
                _enemyPool.Return(i);
        });

        _enemyProjectilePool.ForEachActive((proj, i) =>
        {
            proj.Update(dt);
            proj.UpdateHoming(dt, _player.Position);
            if (!proj.Active || proj.IsOffScreen())
                _enemyProjectilePool.Return(i);
        });

        _damageNumberPool.ForEachActive((dn, i) =>
        {
            dn.Update(dt);
            if (!dn.Active)
                _damageNumberPool.Return(i);
        });

        _droppedGemPool.ForEachActive((gem, i) =>
        {
            gem.Update(dt);
            if (!gem.Active)
                _droppedGemPool.Return(i);
        });

        _collisionSystem.CheckCollisions(_player, _bulletPool, _enemyPool, _damageNumberPool,
            _dropSystem, _droppedGemPool, _gemInventory, _enemyProjectilePool);
        _collisionSystem.CheckEnemyProjectileVsPlayer(_enemyProjectilePool, _player);
    }

    private void UpdatePauseMenu(InputManager input)
    {
        // Escape toggles back to playing
        if (input.MenuPressed && !_menuConsumed)
        {
            Scene = GameScene.Playing;
            return;
        }

        var result = _pauseMenu.Update(input);
        switch (result)
        {
            case PauseMenuResult.Resume:
                Scene = GameScene.Playing;
                break;
            case PauseMenuResult.Inventory:
                _loadoutScreen.Reset();
                Scene = GameScene.Inventory;
                break;
            case PauseMenuResult.Exit:
                ExitRequested = true;
                break;
        }
    }

    private void UpdateInventory(InputManager input)
    {
        _loadoutScreen.Update(input, DebugOverlay, _moduleSystem);
        if (_loadoutScreen.ShouldClose)
        {
            _moduleSystem.RebuildCache();
            Scene = GameScene.Playing;
        }
    }

    public void Draw()
    {
        // Always draw the game world as background
        DrawGameWorld();

        // Overlay the active UI scene on top
        switch (Scene)
        {
            case GameScene.PauseMenu:
                _pauseMenu.Draw();
                break;
            case GameScene.Inventory:
                _loadoutScreen.Draw(_moduleSystem, DebugOverlay);
                break;
        }
    }

    private void DrawGameWorld()
    {
        _background.Draw();
        _player.Draw();

        _bulletPool.ForEachActive((bullet, _) => bullet.Draw());
        _enemyPool.ForEachActive((enemy, _) => enemy.Draw());
        _droppedGemPool.ForEachActive((gem, _) => gem.Draw());
        _enemyProjectilePool.ForEachActive((proj, _) => proj.Draw());
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
