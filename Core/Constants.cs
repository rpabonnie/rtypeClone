namespace rtypeClone.Core;

public static class Constants
{
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;

    // Player
    public const float PlayerSpeed = 400f;
    public const float PlayerShootCooldown = 0.15f;

    // Projectiles — normal
    public const float BulletSpeed = 800f;
    public const int BulletPoolSize = 64;
    public const float NormalBulletWidth = 12f;
    public const float NormalBulletHeight = 4f;
    public const int NormalBulletDamage = 1;

    // Projectiles — charged
    public const float ChargeTimeLevel1 = .85f;
    public const float ChargedBulletSpeed = 600f;
    public const float ChargedBulletWidth = 32f;
    public const float ChargedBulletHeight = 16f;
    public const int ChargedBulletDamage = 3;

    // Enemies
    public const int EnemyPoolSize = 32;
    public const float EnemySpawnMargin = 60f;
}
