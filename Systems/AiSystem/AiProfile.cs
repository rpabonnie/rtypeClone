using System.Text.Json.Serialization;

namespace rtypeClone.Systems.AiSystem;

public class AiProfile
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("nodes")]
    public AiNodeConfig[] Nodes { get; init; } = [];

    /// <summary>
    /// Which screen edge the enemy spawns from.
    /// "right" (default) = off right edge, moving left.
    /// "left"  = off left edge, moving right (surprise/reverse entry).
    /// "top"   = off top edge, diving downward.
    /// "bottom"= off bottom edge, diving upward.
    /// </summary>
    [JsonPropertyName("entryDirection")]
    public string EntryDirection { get; init; } = "right";
}
