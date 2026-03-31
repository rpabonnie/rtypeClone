using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Entities;
using rtypeClone.Systems.CombatSystem;
using rtypeClone.Systems.DropSystem;

namespace rtypeClone.Systems;

public class CollisionSystem
{
    private const int BaseKillScore = 100;

    // Pre-built explosion ring directions — 6 bullets at 60° intervals.
    // Computed once at startup, never allocated per frame.
    private static readonly Vector2[] ExplosionRingDirs = BuildRingDirs(6);

    public void CheckEnemyProjectileVsPlayer(ObjectPool<EnemyProjectile> pool, Player player)
    {
        if (player.IsInvincible) return;

        bool hit = false;
        pool.ForEachActive((proj, i) =>
        {
            if (hit) return; // Only one hit per frame
            if (!proj.Active) return;

            if (Raylib.CheckCollisionRecs(proj.Bounds, player.Bounds))
            {
                player.TakeHit();
                proj.HitsRemaining--;
                if (proj.HitsRemaining <= 0)
                    pool.Return(i);
                hit = true;
            }
        });
    }

    public void CheckCollisions(Player player, ObjectPool<Projectile> bullets,
                                ObjectPool<Enemy> enemies, ObjectPool<DamageNumber> damageNumbers,
                                DropSystem.DropSystem dropSystem, ObjectPool<DroppedGem> droppedGemPool,
                                GemInventory gemInventory,
                                ObjectPool<EnemyProjectile> enemyProjectiles)
    {
        // Bullets vs Enemies
        bullets.ForEachActive((bullet, bi) =>
        {
            if (!bullet.Active) return; // Already consumed by pierce logic

            enemies.ForEachActive((enemy, ei) =>
            {
                if (!bullet.Active) return; // Consumed during this sweep
                if (!enemy.Active) return;

                if (Raylib.CheckCollisionRecs(bullet.Bounds, enemy.Bounds))
                {
                    var dmg = new DamageEvent(bullet.Damage);
                    int dealt = enemy.TakeDamage(dmg);

                    // Show damage number for any hit that deals damage
                    if (dealt > 0)
                    {
                        var numPos = new Vector2(enemy.Position.X + enemy.Width / 2f,
                                                 enemy.Position.Y - 10f);
                        var dn = damageNumbers.Get();
                        if (dn != null)
                            dn.Activate(numPos, dealt, dmg.Type);
                    }

                    if (!enemy.Health.IsAlive)
                    {
                        // Suicide dive: spawn explosion ring on death
                        if (enemy.AiState.SuicideDiveActive)
                            SpawnExplosionRing(enemy.Position, enemy.Width, enemy.Height,
                                               enemyProjectiles);

                        dropSystem.Roll(enemy.Rarity, droppedGemPool, enemy.Position);
                        enemies.Return(ei);
                        int score = (int)(BaseKillScore * RarityConstants.ScoreMultiplier(enemy.Rarity));
                        player.Score += score;
                    }

                    // Pierce: decrement hits remaining, despawn when exhausted
                    bullet.HitsRemaining--;
                    if (bullet.HitsRemaining <= 0)
                        bullets.Return(bi);
                }
            });
        });

        // Enemies vs Player (skip if invincible)
        if (!player.IsInvincible)
        {
            enemies.ForEachActive((enemy, ei) =>
            {
                if (Raylib.CheckCollisionRecs(player.Bounds, enemy.Bounds))
                {
                    // Suicide dive contact: also spawn explosion ring
                    if (enemy.AiState.SuicideDiveActive)
                        SpawnExplosionRing(enemy.Position, enemy.Width, enemy.Height,
                                           enemyProjectiles);

                    enemies.Return(ei);
                    player.TakeHit();
                }
            });
        }

        // Player vs DroppedGems — collect on overlap
        droppedGemPool.ForEachActive((gem, gi) =>
        {
            if (Raylib.CheckCollisionRecs(player.Bounds, gem.Bounds))
            {
                gemInventory.Add(gem.GemId);
                gem.Active = false;
                droppedGemPool.Return(gi);
            }
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns <c>ExplosionRingDirs.Length</c> slow projectiles outward from the
    /// enemy centre, simulating a death explosion for suicide-dive enemies.
    /// </summary>
    private static void SpawnExplosionRing(Vector2 enemyPos, float w, float h,
                                           ObjectPool<EnemyProjectile> pool)
    {
        const float Speed    = 180f;
        const float Lifetime = 1.8f;
        const int   Damage   = 1;
        const float Size     = 8f;

        var center = new Vector2(enemyPos.X + w / 2f - Size / 2f,
                                 enemyPos.Y + h / 2f - Size / 2f);

        foreach (var dir in ExplosionRingDirs)
        {
            var proj = pool.Get();
            if (proj == null) break; // pool exhausted — skip remaining

            proj.Position       = center;
            proj.Width          = Size;
            proj.Height         = Size;
            proj.Velocity       = dir * Speed;
            proj.Damage         = Damage;
            proj.Lifetime       = Lifetime;
            proj.Age            = 0f;
            proj.Homing         = false;
            proj.HomingStrength = 0f;
            proj.HitsRemaining  = 1;
            proj.IsStationary   = false;
            proj.Speed          = Speed;
            proj.Active         = true;
        }
    }

    private static Vector2[] BuildRingDirs(int count)
    {
        var dirs = new Vector2[count];
        float step = 2f * MathF.PI / count;
        for (int i = 0; i < count; i++)
            dirs[i] = new Vector2(MathF.Cos(i * step), MathF.Sin(i * step));
        return dirs;
    }
}
