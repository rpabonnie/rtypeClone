using System.Numerics;
using rtypeClone.Core;
using rtypeClone.Entities;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Systems.AiSystem.Handlers;

public class AttackHandler : IBehaviourHandler
{
    public string TypeName => "attack";

    private readonly ObjectPool<EnemyProjectile> _projectilePool;
    private readonly EnemyAttackRegistry _attackRegistry;

    public AttackHandler(ObjectPool<EnemyProjectile> projectilePool, EnemyAttackRegistry attackRegistry)
    {
        _projectilePool = projectilePool;
        _attackRegistry = attackRegistry;
    }

    public void Update(
        ref Vector2 position,
        ref Vector2 velocity,
        ref EnemyAiState state,
        AiNodeConfig config,
        in AiContext ctx)
    {
        if (string.IsNullOrEmpty(config.AttackId)) return;
        if (!_attackRegistry.Has(config.AttackId)) return;

        var attackCfg = _attackRegistry.Get(config.AttackId);

        // Handle burst firing (fires remaining shots from a burst in progress)
        if (state.BurstShotsRemaining > 0)
        {
            state.BurstTimer -= ctx.Dt;
            if (state.BurstTimer <= 0f)
            {
                FireProjectiles(position, attackCfg, ctx.PlayerPosition);
                state.BurstShotsRemaining--;
                state.BurstTimer = attackCfg.BurstInterval;
            }
            return;
        }

        // Cooldown
        if (state.AttackCooldownTimer > 0f)
        {
            state.AttackCooldownTimer -= ctx.Dt;
            return;
        }

        // Telegraph phase
        if (!state.IsTelegraphing)
        {
            // Start telegraph
            state.IsTelegraphing = true;
            state.TelegraphTimer = 0f;
            return;
        }

        // Accumulate telegraph time
        state.TelegraphTimer += ctx.Dt;
        if (state.TelegraphTimer < attackCfg.TelegraphTime)
            return;

        // Telegraph complete — fire!
        state.IsTelegraphing = false;
        state.TelegraphTimer = 0f;

        FireProjectiles(position, attackCfg, ctx.PlayerPosition);

        // Set up burst if burstCount > 1 (first shot already fired)
        if (attackCfg.BurstCount > 1)
        {
            state.BurstShotsRemaining = attackCfg.BurstCount - 1;
            state.BurstTimer = attackCfg.BurstInterval;
        }

        state.AttackCooldownTimer = attackCfg.Cooldown;
    }

    private void FireProjectiles(Vector2 enemyPos, EnemyAttackConfig cfg, Vector2 playerPos)
    {
        // Compute base aim direction
        Vector2 aimDir;
        if (cfg.AimMode == "at_player")
        {
            var enemyCenter = new Vector2(enemyPos.X + 20f, enemyPos.Y + 16f); // approximate enemy center
            aimDir = playerPos - enemyCenter;
            if (aimDir.LengthSquared() > 0.001f)
                aimDir = Vector2.Normalize(aimDir);
            else
                aimDir = new Vector2(-1f, 0f);
        }
        else // "fixed_left" or default
        {
            aimDir = new Vector2(-1f, 0f);
        }

        // Apply aim offset
        if (MathF.Abs(cfg.AimOffsetDeg) > 0.01f)
        {
            float rad = cfg.AimOffsetDeg * MathF.PI / 180f;
            aimDir = RotateVector(aimDir, rad);
        }

        // Spawn position: left edge of enemy, vertically centered
        var spawnPos = new Vector2(enemyPos.X, enemyPos.Y + 16f - cfg.ProjectileHeight / 2f);

        if (cfg.Count <= 1)
        {
            // Single projectile
            var proj = _projectilePool.Get();
            if (proj != null)
                proj.Spawn(spawnPos, cfg, aimDir);
        }
        else
        {
            // Fan spread
            float totalSpread = cfg.SpreadAngleDeg * MathF.PI / 180f;
            float step = cfg.Count > 1 ? totalSpread / (cfg.Count - 1) : 0f;
            float startAngle = -totalSpread / 2f;

            for (int i = 0; i < cfg.Count; i++)
            {
                float angle = startAngle + step * i;
                var dir = RotateVector(aimDir, angle);
                var proj = _projectilePool.Get();
                if (proj != null)
                    proj.Spawn(spawnPos, cfg, dir);
            }
        }
    }

    private static Vector2 RotateVector(Vector2 v, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
