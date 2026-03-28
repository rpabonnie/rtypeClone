namespace rtypeClone.Systems.CombatSystem;

public readonly struct DamageEvent
{
    public readonly int Amount;
    public readonly DamageType Type;
    public readonly bool BypassShield;

    public DamageEvent(int amount, DamageType type = DamageType.NonElemental, bool bypassShield = false)
    {
        Amount = amount;
        Type = type;
        BypassShield = bypassShield;
    }
}
