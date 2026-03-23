using Raylib_cs;

namespace rtypeClone.Core;

public static class AssetManager
{
    public static Texture2D BulletSheet { get; private set; }

    // Source rectangles for bullet sprites on M484BulletCollection1.png (520x361)
    // Normal bullet: small cyan diamond projectile (row 2, ~y:60)
    public static readonly Rectangle NormalBulletSrc = new(160f, 56f, 16f, 16f);
    // Charged bullet: large cyan energy orb (bottom row)
    public static readonly Rectangle ChargedBulletSrc = new(320f, 332f, 24f, 24f);

    public static void Load()
    {
        BulletSheet = Raylib.LoadTexture("Assets/M484BulletCollection1.png");
    }

    public static void Unload()
    {
        Raylib.UnloadTexture(BulletSheet);
    }
}
