using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Entities;

public struct EnemyHealth
{
    public int MaxHp;
    public int CurrentHp;
    public int ShieldMax;
    public int ShieldCurrent;

    public bool IsAlive => CurrentHp > 0;
    public bool HasShield => ShieldCurrent > 0;
    public float HpPercent => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;
    public float ShieldPercent => ShieldMax > 0 ? (float)ShieldCurrent / ShieldMax : 0f;

    public static EnemyHealth Create(int hp, int shield = 0) => new EnemyHealth
    {
        MaxHp = hp,
        CurrentHp = hp,
        ShieldMax = shield,
        ShieldCurrent = shield,
    };

    public int ApplyDamage(DamageEvent dmg)
    {
        if (HasShield && !dmg.BypassShield)
        {
            int shieldDmg = Math.Min(ShieldCurrent, dmg.Amount);
            ShieldCurrent -= shieldDmg;
            int overflow = dmg.Amount - shieldDmg;
            if (overflow > 0)
                CurrentHp = Math.Max(0, CurrentHp - overflow);
            return overflow;
        }
        int dealt = Math.Min(CurrentHp, dmg.Amount);
        CurrentHp -= dealt;
        return dealt;
    }

    public void RegenShield(int amount) =>
        ShieldCurrent = Math.Min(ShieldMax, ShieldCurrent + amount);

    public void RegenHp(int amount) =>
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
}
