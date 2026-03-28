using System.Numerics;
using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Entities;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Systems;

public class CollisionSystem
{
    private const int BaseKillScore = 100;

    public void CheckCollisions(Player player, ObjectPool<Projectile> bullets,
                                ObjectPool<Enemy> enemies, ObjectPool<DamageNumber> damageNumbers)
    {
        // Bullets vs Enemies
        bullets.ForEachActive((bullet, bi) =>
        {
            enemies.ForEachActive((enemy, ei) =>
            {
                if (Raylib.CheckCollisionRecs(bullet.Bounds, enemy.Bounds))
                {
                    bullets.Return(bi);

                    var dmg = new DamageEvent(bullet.Damage);
                    int dealt = enemy.TakeDamage(dmg);

                    // Show damage number only for enemies that survive the hit (multi-HP)
                    if (enemy.Health.IsAlive && enemy.Health.MaxHp > 1)
                    {
                        var numPos = new Vector2(enemy.Position.X + enemy.Width / 2f,
                                                 enemy.Position.Y - 10f);
                        var dn = damageNumbers.Get();
                        if (dn != null)
                            dn.Activate(numPos, dmg.Amount, dmg.Type);
                    }

                    if (!enemy.Health.IsAlive)
                    {
                        enemies.Return(ei);
                        // Score is base × rarity multiplier
                        int score = (int)(BaseKillScore * RarityConstants.ScoreMultiplier(enemy.Rarity));
                        player.Score += score;
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
    }
}
