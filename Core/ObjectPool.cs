namespace rtypeClone.Core;

public class ObjectPool<T> where T : class, new()
{
    private readonly T[] _pool;
    private readonly bool[] _active;

    public int Capacity { get; }

    public ObjectPool(int capacity)
    {
        Capacity = capacity;
        _pool = new T[capacity];
        _active = new bool[capacity];

        for (int i = 0; i < capacity; i++)
            _pool[i] = new T();
    }

    public T? Get()
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (!_active[i])
            {
                _active[i] = true;
                return _pool[i];
            }
        }
        return null; // Pool exhausted
    }

    public void Return(int index)
    {
        if (index >= 0 && index < Capacity)
            _active[index] = false;
    }

    public bool IsActive(int index) => _active[index];
    public T GetAt(int index) => _pool[index];

    public void ForEachActive(Action<T, int> action)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (_active[i])
                action(_pool[i], i);
        }
    }
}
