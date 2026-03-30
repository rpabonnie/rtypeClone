namespace rtypeClone.Core;

/// <summary>
/// Stores gem IDs the player has collected. Persists between levels.
/// </summary>
public class GemInventory
{
    private readonly List<string> _gems;

    public GemInventory(int initialCapacity = 32)
    {
        _gems = new List<string>(initialCapacity);
    }

    public void Add(string gemId) => _gems.Add(gemId);
    public bool Remove(string gemId) => _gems.Remove(gemId);
    public IReadOnlyList<string> All => _gems;
    public int Count => _gems.Count;

    /// <summary>
    /// Collects all active DroppedGems (e.g. at wave end) and returns them to pool.
    /// </summary>
    public void CollectAll(ObjectPool<Entities.DroppedGem> pool)
    {
        pool.ForEachActive((gem, i) =>
        {
            _gems.Add(gem.GemId);
            pool.Return(i);
        });
    }
}
