using System.Numerics;
using rtypeClone.Core;
using rtypeClone.Entities;

namespace rtypeClone.Systems.DropSystem;

public class DropSystem
{
    private readonly DropTableRegistry _tables;

    public DropSystem(DropTableRegistry registry)
    {
        _tables = registry;
    }

    /// <summary>
    /// Rolls a drop for the killed enemy. If drop occurs, spawns a DroppedGem from the pool.
    /// No allocations — gem ID string comes from the pre-loaded registry.
    /// </summary>
    public void Roll(EnemyRarity rarity, ObjectPool<DroppedGem> pool, Vector2 spawnPosition,
                     string? overrideTableId = null)
    {
        var table = overrideTableId != null
            ? _tables.Get(overrideTableId)
            : _tables.GetForRarity(rarity);

        if (!table.GuaranteedDrop && Random.Shared.NextSingle() > table.DropChance)
            return;

        string gemId = WeightedRandom(table);
        var gem = pool.Get();
        if (gem != null)
            gem.Spawn(spawnPosition, gemId);
    }

    private static string WeightedRandom(DropTable table)
    {
        int roll = Random.Shared.Next(table.TotalWeight);
        int acc = 0;
        foreach (ref var entry in table.Entries.AsSpan())
        {
            acc += entry.Weight;
            if (roll < acc) return entry.GemId;
        }
        return table.Entries[^1].GemId;
    }
}
