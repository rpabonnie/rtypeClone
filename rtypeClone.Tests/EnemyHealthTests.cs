using rtypeClone.Entities;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Tests;

public class EnemyHealthTests
{
    [Fact]
    public void Create_SetsMaxAndCurrent()
    {
        var hp = EnemyHealth.Create(10, shield: 5);
        Assert.Equal(10, hp.MaxHp);
        Assert.Equal(10, hp.CurrentHp);
        Assert.Equal(5, hp.ShieldMax);
        Assert.Equal(5, hp.ShieldCurrent);
        Assert.True(hp.IsAlive);
        Assert.True(hp.HasShield);
    }

    [Fact]
    public void ApplyDamage_NoShield_DealsDirect()
    {
        var hp = EnemyHealth.Create(10);
        int dealt = hp.ApplyDamage(new DamageEvent(3));

        Assert.Equal(7, hp.CurrentHp);
        Assert.Equal(3, dealt);
        Assert.True(hp.IsAlive);
    }

    [Fact]
    public void ApplyDamage_KillsAtZero()
    {
        var hp = EnemyHealth.Create(3);
        int dealt = hp.ApplyDamage(new DamageEvent(5));

        Assert.Equal(0, hp.CurrentHp);
        Assert.Equal(3, dealt); // Only 3 HP was actually removed
        Assert.False(hp.IsAlive);
    }

    [Fact]
    public void ApplyDamage_ShieldAbsorbsFirst()
    {
        var hp = EnemyHealth.Create(10, shield: 5);
        int dealt = hp.ApplyDamage(new DamageEvent(3));

        Assert.Equal(2, hp.ShieldCurrent); // 5 - 3
        Assert.Equal(10, hp.CurrentHp);     // HP untouched
        Assert.Equal(0, dealt);              // Returned overflow (0 hp damage)
    }

    [Fact]
    public void ApplyDamage_ShieldOverflow_DamagesHp()
    {
        var hp = EnemyHealth.Create(10, shield: 3);
        int dealt = hp.ApplyDamage(new DamageEvent(7));

        Assert.Equal(0, hp.ShieldCurrent);  // Shield depleted
        Assert.Equal(6, hp.CurrentHp);       // 10 - 4 overflow
        Assert.Equal(4, dealt);              // 4 HP damage dealt
    }

    [Fact]
    public void ApplyDamage_BypassShield_IgnoresShield()
    {
        var hp = EnemyHealth.Create(10, shield: 5);
        int dealt = hp.ApplyDamage(new DamageEvent(3, bypassShield: true));

        Assert.Equal(5, hp.ShieldCurrent); // Shield untouched
        Assert.Equal(7, hp.CurrentHp);      // Direct HP damage
        Assert.Equal(3, dealt);
    }

    [Fact]
    public void RegenHp_CapsAtMax()
    {
        var hp = EnemyHealth.Create(10);
        hp.ApplyDamage(new DamageEvent(5));
        Assert.Equal(5, hp.CurrentHp);

        hp.RegenHp(3);
        Assert.Equal(8, hp.CurrentHp);

        hp.RegenHp(100);
        Assert.Equal(10, hp.CurrentHp); // Capped at max
    }

    [Fact]
    public void RegenShield_CapsAtMax()
    {
        var hp = EnemyHealth.Create(10, shield: 5);
        hp.ApplyDamage(new DamageEvent(3)); // Removes 3 shield
        Assert.Equal(2, hp.ShieldCurrent);

        hp.RegenShield(1);
        Assert.Equal(3, hp.ShieldCurrent);

        hp.RegenShield(100);
        Assert.Equal(5, hp.ShieldCurrent); // Capped at max
    }

    [Fact]
    public void HpPercent_ReturnsCorrectRatio()
    {
        var hp = EnemyHealth.Create(10);
        Assert.Equal(1f, hp.HpPercent);

        hp.ApplyDamage(new DamageEvent(3));
        Assert.Equal(0.7f, hp.HpPercent, 0.001f);
    }

    [Fact]
    public void ShieldPercent_ReturnsCorrectRatio()
    {
        var hp = EnemyHealth.Create(10, shield: 4);
        Assert.Equal(1f, hp.ShieldPercent);

        hp.ApplyDamage(new DamageEvent(2)); // Removes 2 shield
        Assert.Equal(0.5f, hp.ShieldPercent, 0.001f);
    }
}
