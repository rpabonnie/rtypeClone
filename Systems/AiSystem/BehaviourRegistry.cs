using rtypeClone.Systems.AiSystem.Handlers;

namespace rtypeClone.Systems.AiSystem;

public class BehaviourRegistry
{
    private readonly Dictionary<string, IBehaviourHandler> _handlers = new();

    public BehaviourRegistry()
    {
        Register(new StraightHandler());
        Register(new SineHandler());
        Register(new ZigzagHandler());
    }

    public void Register(IBehaviourHandler handler) =>
        _handlers[handler.TypeName] = handler;

    public IBehaviourHandler Get(string typeName) => _handlers[typeName];
}
