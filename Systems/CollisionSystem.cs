using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Entities;

namespace rtypeClone.Systems;

public class CollisionSystem
{
    public void CheckCollisions(Player player, ObjectPool<Projectile> bullets, ObjectPool<Enemy> enemies)
    {
        // Bullets vs Enemies
        bullets.ForEachActive((bullet, bi) =>
        {
            enemies.ForEachActive((enemy, ei) =>
            {
                if (Raylib.CheckCollisionRecs(bullet.Bounds, enemy.Bounds))
                {
                    bullets.Return(bi);
                    enemy.Health -= bullet.Damage;
                    if (enemy.Health <= 0)
                    {
                        enemies.Return(ei);
                        player.Score += 100;
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
