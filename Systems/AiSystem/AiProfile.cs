using System.Text.Json.Serialization;

namespace rtypeClone.Systems.AiSystem;

public class AiProfile
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("nodes")]
    public AiNodeConfig[] Nodes { get; init; } = [];
}
