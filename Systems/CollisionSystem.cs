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

    public void CheckCollisions(Player player, ObjectPool<Projectile> bullets,
                                ObjectPool<Enemy> enemies, ObjectPool<DamageNumber> damageNumbers,
                                DropSystem.DropSystem dropSystem, ObjectPool<DroppedGem> droppedGemPool,
                                GemInventory gemInventory)
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
                        // Roll drop before returning to pool (need position + rarity)
                        dropSystem.Roll(enemy.Rarity, droppedGemPool, enemy.Position);
                        enemies.Return(ei);
                        int score = (int)(BaseKillScore * RarityConstants.ScoreMultiplier(enemy.Rarity));
                        player.Score += score;
                    }

                    // Pierce: decrement hits remaining, despawn when exhausted
                    bullet.HitsRemaining--;
                    if (bullet.HitsRemaining <= 0)
                    {
                        bullets.Return(bi);
                    }
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
}
