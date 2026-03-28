using rtypeClone.Entities;

namespace rtypeClone.Tests;

public class RarityTests
{
    [Fact]
    public void ScoreMultiplier_Normal_Is1()
    {
        Assert.Equal(1f, RarityConstants.ScoreMultiplier(EnemyRarity.Normal));
    }

    [Fact]
    public void ScoreMultiplier_Magic_Is2()
    {
        Assert.Equal(2f, RarityConstants.ScoreMultiplier(EnemyRarity.Magic));
    }

    [Fact]
    public void ScoreMultiplier_Rare_Is5()
    {
        Assert.Equal(5f, RarityConstants.ScoreMultiplier(EnemyRarity.Rare));
    }

    [Fact]
    public void ScoreMultiplier_Unique_Is10()
    {
        Assert.Equal(10f, RarityConstants.ScoreMultiplier(EnemyRarity.Unique));
    }

    [Fact]
    public void MinAffixes_Normal_IsZero()
    {
        Assert.Equal(0, RarityConstants.MinAffixes(EnemyRarity.Normal));
    }

    [Fact]
    public void MinAffixes_Magic_Is1()
    {
        Assert.Equal(1, RarityConstants.MinAffixes(EnemyRarity.Magic));
    }

    [Fact]
    public void MaxAffixes_Rare_Is4()
    {
        Assert.Equal(4, RarityConstants.MaxAffixes(EnemyRarity.Rare));
    }

    [Fact]
    public void DemoteOneTier_Unique_BecomesRare()
    {
        Assert.Equal(EnemyRarity.Rare, RarityConstants.DemoteOneTier(EnemyRarity.Unique));
    }

    [Fact]
    public void DemoteOneTier_Rare_BecomesMagic()
    {
        Assert.Equal(EnemyRarity.Magic, RarityConstants.DemoteOneTier(EnemyRarity.Rare));
    }

    [Fact]
    public void DemoteOneTier_Magic_BecomesNormal()
    {
        Assert.Equal(EnemyRarity.Normal, RarityConstants.DemoteOneTier(EnemyRarity.Magic));
    }

    [Fact]
    public void DemoteOneTier_Normal_StaysNormal()
    {
        Assert.Equal(EnemyRarity.Normal, RarityConstants.DemoteOneTier(EnemyRarity.Normal));
    }
}
