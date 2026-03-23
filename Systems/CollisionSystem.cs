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
                    enemy.Health--;
                    if (enemy.Health <= 0)
                    {
                        enemies.Return(ei);
                        player.Score += 100;
                    }
                }
            });
        });

        // Enemies vs Player
        enemies.ForEachActive((enemy, ei) =>
        {
            if (Raylib.CheckCollisionRecs(player.Bounds, enemy.Bounds))
            {
                enemies.Return(ei);
                player.Health--;
            }
        });
    }
}
