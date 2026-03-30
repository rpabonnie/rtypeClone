using rtypeClone.Core;
using rtypeClone.Entities;
using rtypeClone.Systems.AiSystem.Handlers;
using rtypeClone.Systems.CombatSystem;

namespace rtypeClone.Systems.AiSystem;

public class BehaviourRegistry
{
    private readonly Dictionary<string, IBehaviourHandler> _handlers = new();

    public BehaviourRegistry(ObjectPool<EnemyProjectile> enemyProjectilePool, EnemyAttackRegistry attackRegistry)
    {
        Register(new StraightHandler());
        Register(new SineHandler());
        Register(new ZigzagHandler());
        Register(new AttackHandler(enemyProjectilePool, attackRegistry));
    }

    public void Register(IBehaviourHandler handler) =>
        _handlers[handler.TypeName] = handler;

    public IBehaviourHandler Get(string typeName) => _handlers[typeName];
}
