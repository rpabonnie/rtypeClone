namespace rtypeClone.Systems.AiSystem;

public struct EnemyAiState
{
    public float AliveTimer;
    public float SpawnY;

    // Zigzag
    public float ZigzagTimer;
    public float ZigzagDirection; // +1 or -1

    // Charge / Retreat (Phase 2+)
    public int PhaseIndex;
    public float PhaseTimer;
    public bool ChargeActive;

    // Shooting (Phase 2+)
    public float FireCooldownTimer;

    // Attack (enemy combat)
    public float AttackCooldownTimer;
    public float TelegraphTimer;
    public bool IsTelegraphing;
    public int BurstShotsRemaining;
    public float BurstTimer;

    // Formation (Phase 3)
    public int FormationSlot;
}
