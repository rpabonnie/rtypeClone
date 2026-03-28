using Raylib_cs;

namespace rtypeClone.Entities;

public enum EnemyRarity { Normal, Magic, Rare, Unique }

public static class RarityConstants
{
    public static readonly Color NormalColor = Color.White;
    public static readonly Color MagicColor  = new Color((byte)136, (byte)136, (byte)255, (byte)255);
    public static readonly Color RareColor   = new Color((byte)255, (byte)215, (byte)0,   (byte)255);
    public static readonly Color UniqueColor = new Color((byte)200, (byte)80,  (byte)0,   (byte)255);

    public static Color GetColor(EnemyRarity r) => r switch
    {
        EnemyRarity.Normal => NormalColor,
        EnemyRarity.Magic  => MagicColor,
        EnemyRarity.Rare   => RareColor,
        EnemyRarity.Unique => UniqueColor,
        _ => NormalColor
    };

    public static float ScoreMultiplier(EnemyRarity r) => r switch
    {
        EnemyRarity.Normal => 1f,
        EnemyRarity.Magic  => 2f,
        EnemyRarity.Rare   => 5f,
        EnemyRarity.Unique => 10f,
        _ => 1f
    };

    /// <summary>
    /// Max affix count for each rarity tier.
    /// Unique enemies use preset affixes, so this isn't applied to them.
    /// </summary>
    public static int MaxAffixes(EnemyRarity r) => r switch
    {
        EnemyRarity.Normal => 0,
        EnemyRarity.Magic  => 2,
        EnemyRarity.Rare   => 4,
        EnemyRarity.Unique => 0, // preset-driven
        _ => 0
    };

    public static int MinAffixes(EnemyRarity r) => r switch
    {
        EnemyRarity.Normal => 0,
        EnemyRarity.Magic  => 1,
        EnemyRarity.Rare   => 2,
        EnemyRarity.Unique => 0,
        _ => 0
    };

    /// <summary>
    /// Returns the rarity one tier below the given one, floored at Normal.
    /// Used for splitsOnDeath children.
    /// </summary>
    public static EnemyRarity DemoteOneTier(EnemyRarity r) => r switch
    {
        EnemyRarity.Unique => EnemyRarity.Rare,
        EnemyRarity.Rare   => EnemyRarity.Magic,
        EnemyRarity.Magic  => EnemyRarity.Normal,
        _ => EnemyRarity.Normal
    };
}
