namespace rtypeClone.Systems.DropSystem;

public struct DropTableEntry
{
    public string GemId;
    public int Weight;
}

public class DropTable
{
    public string Id { get; init; } = "";
    public bool GuaranteedDrop { get; init; }
    public float DropChance { get; init; }
    public DropTableEntry[] Entries { get; init; } = [];
    public int TotalWeight { get; init; }
}
